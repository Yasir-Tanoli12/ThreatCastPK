from fastapi import FastAPI
from pydantic import BaseModel
import joblib
import numpy as np
import os
import psycopg2
from typing import List, Optional
from datetime import datetime, timezone, timedelta

app = FastAPI(title="ThreatCast PK ML Service")

BASE_DIR = os.path.dirname(os.path.abspath(__file__))

try:
    model    = joblib.load(os.path.join(BASE_DIR, "threat_model.pkl"))
    le_attack = joblib.load(os.path.join(BASE_DIR, "le_attack.pkl"))
    le_proto  = joblib.load(os.path.join(BASE_DIR, "le_proto.pkl"))
    le_geo    = joblib.load(os.path.join(BASE_DIR, "le_geo.pkl"))
    le_seg    = joblib.load(os.path.join(BASE_DIR, "le_seg.pkl"))
    print("[ML] Model and encoders loaded.")
except Exception as e:
    model = None
    print(f"[ML] Failed to load model: {e}")

# ── DB connection ──────────────────────────────────────────────────────────────
DB_URL = os.getenv("DATABASE_URL", "")
DB_PASSWORD = os.getenv("DB_PASSWORD", "")

def get_db():
    if DB_PASSWORD:
        # Use keyword args — no URL encoding needed
        try:
            return psycopg2.connect(
                host="aws-1-ap-southeast-1.pooler.supabase.com",
                port=5432,
                database="postgres",
                user="postgres.kekcojybactqxrjnamul",
                password=DB_PASSWORD,
                sslmode="require"
            )
        except Exception as e:
            print(f"[DB] Connection failed: {e}")
            return None
    if DB_URL:
        try:
            return psycopg2.connect(DB_URL)
        except Exception as e:
            print(f"[DB] Connection failed: {e}")
            return None
    return None

# ── Helpers ────────────────────────────────────────────────────────────────────
def safe_encode(encoder, value, default=0):
    try:
        return int(encoder.transform([value])[0])
    except Exception:
        return default

def encode_event(attack_type: str, anomaly_score: float,
                 packet_length: float, protocol: str,
                 geo_location: str, network_segment: str) -> list:
    return [
        safe_encode(le_attack, attack_type),
        float(anomaly_score),
        float(packet_length),
        safe_encode(le_proto, protocol),
        safe_encode(le_geo, geo_location),
        safe_encode(le_seg, network_segment),
    ]

def score_to_alert_level(anomaly_count: int, total: int) -> str:
    if total == 0:
        return "NORMAL"
    ratio = anomaly_count / total
    if ratio >= 0.5 or anomaly_count >= 50:
        return "CRITICAL"
    if ratio >= 0.3 or anomaly_count >= 20:
        return "HIGH"
    if ratio >= 0.15 or anomaly_count >= 10:
        return "MEDIUM"
    return "NORMAL"

# ── Request / Response models ──────────────────────────────────────────────────
class AttackEventInput(BaseModel):
    attack_type: str
    anomaly_score: float
    packet_length: float
    protocol: str
    geo_location: str
    network_segment: str

class CampaignRequest(BaseModel):
    events: List[AttackEventInput]

# ── Endpoints ──────────────────────────────────────────────────────────────────
@app.get("/")
def root():
    return {"message": "ThreatCast PK ML Service Running"}

@app.get("/health")
def health():
    db_ok = get_db() is not None
    return {
        "status": "healthy",
        "model_loaded": model is not None,
        "db_connected": db_ok
    }

# ── /detect-campaign — called by C# with pre-built event list ─────────────────
@app.post("/detect-campaign")
def detect_campaign(request: CampaignRequest):
    if not model:
        return {"error": "ML model not loaded."}
    if not request.events:
        return {
            "is_campaign": False,
            "alert_level": "NORMAL",
            "anomaly_count": 0,
            "total_events": 0,
            "anomaly_flags": [],
            "message": "No events provided"
        }

    features = np.array([
        encode_event(e.attack_type, e.anomaly_score, e.packet_length,
                     e.protocol, e.geo_location, e.network_segment)
        for e in request.events
    ])

    predictions = model.predict(features)
    anomaly_flags = [bool(p == -1) for p in predictions]
    anomaly_count = int(np.sum(predictions == -1))
    alert_level = score_to_alert_level(anomaly_count, len(predictions))
    is_campaign = alert_level != "NORMAL"

    return {
        "is_campaign": is_campaign,
        "alert_level": alert_level,
        "anomaly_count": anomaly_count,
        "total_events": len(predictions),
        "anomaly_flags": anomaly_flags,
        "message": "Coordinated campaign detected!" if is_campaign else "Normal activity"
    }

# ── /detect-campaign/auto — queries DB itself, writes results back ─────────────
@app.get("/detect-campaign/auto")
def detect_campaign_auto(hours: int = 6):
    if not model:
        return {"error": "ML model not loaded.", "is_campaign": False}

    conn = get_db()
    if not conn:
        return {"error": "Database unavailable.", "is_campaign": False}

    try:
        cur = conn.cursor()

        # Fetch real AttackEvents from last N hours
        cutoff = datetime.now(timezone.utc) - timedelta(hours=hours)
        cur.execute("""
            SELECT
                ae."Id",
                ae."AttackType",
                ae."Severity",
                ae."TargetSector",
                ae."OccurredAt",
                ae."SourceIP",
                l."CityName"
            FROM "AttackEvents" ae
            LEFT JOIN "Locations" l ON ae."LocationId" = l."Id"
            WHERE ae."OccurredAt" >= %s
            ORDER BY ae."OccurredAt" DESC
            LIMIT 200
        """, (cutoff,))

        rows = cur.fetchall()

        if len(rows) < 5:
            cur.close()
            conn.close()
            return {
                "is_campaign": False,
                "alert_level": "NORMAL",
                "anomaly_count": 0,
                "total_events": len(rows),
                "anomaly_flags": [],
                "message": f"Insufficient data ({len(rows)} events in last {hours}h)"
            }

        # Map DB rows to model features
        # attack_type from DB is enum string e.g. "DDoS", "Ransomware"
        # severity normalized to anomaly_score range (0-100)
        # protocol and network_segment use defaults since we don't store them
        event_ids = []
        features_list = []
        cities = []
        sectors = []

        for row in rows:
            event_id, attack_type, severity, target_sector, occurred_at, source_ip, city = row
            event_ids.append(str(event_id))
            cities.append(city or "Unknown")
            sectors.append(target_sector or "Other")

            features_list.append(encode_event(
                attack_type=str(attack_type),
                anomaly_score=float(severity) * 20.0,   # severity 1-5 → 20-100
                packet_length=500.0,                     # default — not stored
                protocol="TCP",                          # default — not stored
                geo_location=city or "Unknown",
                network_segment=str(target_sector)
            ))

        features = np.array(features_list)
        predictions = model.predict(features)
        anomaly_flags = [bool(p == -1) for p in predictions]
        anomaly_count = int(np.sum(predictions == -1))
        alert_level = score_to_alert_level(anomaly_count, len(predictions))
        is_campaign = alert_level != "NORMAL"

        affected_cities = list(set(
            cities[i] for i, flag in enumerate(anomaly_flags) if flag
        ))
        affected_sectors = list(set(
            sectors[i] for i, flag in enumerate(anomaly_flags) if flag
        ))

        # Write campaign to DB if detected
        if is_campaign:
            campaign_id = None
            try:
                # Check if campaign already recorded in last hour
                cur.execute("""
                    SELECT "Id" FROM "ThreatCampaigns"
                    WHERE "DetectedAt" >= NOW() - INTERVAL '1 hour'
                    LIMIT 1
                """)
                existing = cur.fetchone()

                if not existing:
                    import uuid
                    campaign_id = str(uuid.uuid4())
                    cur.execute("""
                        INSERT INTO "ThreatCampaigns"
                            ("Id", "IpRange", "DetectedAt", "AffectedCities",
                             "AffectedSectors", "ReportCount", "AlertLevel")
                        VALUES (%s, %s, NOW(), %s, %s, %s, %s)
                    """, (
                        campaign_id,
                        "Multiple",
                        ", ".join(affected_cities),
                        ", ".join(affected_sectors),
                        anomaly_count,
                        alert_level
                    ))
                    conn.commit()
                    print(f"[ML] Campaign written to DB: {campaign_id}")
            except Exception as e:
                print(f"[ML] Failed to write campaign: {e}")
                conn.rollback()

        cur.close()
        conn.close()

        return {
            "is_campaign": is_campaign,
            "alert_level": alert_level,
            "anomaly_count": anomaly_count,
            "total_events": len(rows),
            "anomaly_flags": anomaly_flags,
            "affected_cities": ", ".join(affected_cities),
            "affected_sectors": ", ".join(affected_sectors),
            "message": "Coordinated campaign detected!" if is_campaign else "Normal activity"
        }

    except Exception as e:
        print(f"[ML] Auto detection error: {e}")
        try:
            conn.close()
        except Exception:
            pass
        return {"error": str(e), "is_campaign": False}

# ── /sector-risk — queries real AttackEvents ───────────────────────────────────
@app.get("/sector-risk")
def sector_risk():
    conn = get_db()
    if not conn:
        return _default_sector_risk()

    try:
        cur = conn.cursor()
        # Query AttackEvents — correct table and column names
        cur.execute("""
            SELECT
                "TargetSector",
                COUNT(*) as event_count,
                MAX("Severity") as max_severity
            FROM "AttackEvents"
            WHERE "OccurredAt" >= NOW() - INTERVAL '24 hours'
            GROUP BY "TargetSector"
        """)
        rows = cur.fetchall()
        cur.close()
        conn.close()

        if not rows:
            return _default_sector_risk()

        risk_map = {}
        for sector, count, max_sev in rows:
            if max_sev >= 5 or count >= 21:
                risk_map[sector] = "Critical"
            elif count >= 6:
                risk_map[sector] = "High"
            elif count >= 1:
                risk_map[sector] = "Medium"
            else:
                risk_map[sector] = "Low"

        # Fill in missing sectors with Low
        all_sectors = ["Banking", "Telecom", "Healthcare",
                       "Education", "Government", "Energy", "Other"]
        for s in all_sectors:
            if s not in risk_map:
                risk_map[s] = "Low"

        return risk_map

    except Exception as e:
        print(f"[ML] Sector risk error: {e}")
        try:
            conn.close()
        except Exception:
            pass
        return _default_sector_risk()

def _default_sector_risk():
    return {
        "Banking": "High",
        "Telecom": "Medium",
        "Healthcare": "Low",
        "Government": "High",
        "Education": "Low",
        "Energy": "Medium",
        "Other": "Low"
    }