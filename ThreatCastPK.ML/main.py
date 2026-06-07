from fastapi import FastAPI
from pydantic import BaseModel
import joblib
import numpy as np
import os
import psycopg2
from typing import List

app = FastAPI(title="ThreatCast PK ML Service")

# Load pre-trained model and label encoders
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
MODEL_PATH = os.path.join(BASE_DIR, "threat_model.pkl")
LE_ATTACK_PATH = os.path.join(BASE_DIR, "le_attack.pkl")
LE_PROTO_PATH = os.path.join(BASE_DIR, "le_proto.pkl")
LE_GEO_PATH = os.path.join(BASE_DIR, "le_geo.pkl")
LE_SEG_PATH = os.path.join(BASE_DIR, "le_seg.pkl")

try:
    model = joblib.load(MODEL_PATH)
    le_attack = joblib.load(LE_ATTACK_PATH)
    le_proto = joblib.load(LE_PROTO_PATH)
    le_geo = joblib.load(LE_GEO_PATH)
    le_seg = joblib.load(LE_SEG_PATH)
    print("[ML] Trained model and encoders loaded successfully.")
except Exception as e:
    model = None
    print(f"[ML] Error loading model files: {e}")

DB_URL = os.getenv("DATABASE_URL", "")

def get_db_connection():
    if not DB_URL:
        return None
    try:
        return psycopg2.connect(DB_URL)
    except Exception:
        return None

class AttackEventInput(BaseModel):
    attack_type: str
    anomaly_score: float
    packet_length: float
    protocol: str
    geo_location: str
    network_segment: str

class CampaignRequest(BaseModel):
    events: List[AttackEventInput]

@app.get("/")
def root():
    return {"message": "ThreatCast PK ML Service Running"}

@app.get("/health")
def health():
    return {"status": "healthy", "model_loaded": model is not None}

@app.post("/detect-campaign")
def detect_campaign(request: CampaignRequest):
    if not model:
        return {"error": "ML model not loaded on server."}
    if not request.events:
        return {"is_campaign": False, "alert_level": "NORMAL", "message": "No events provided"}

    processed_features = []
    for e in request.events:
        try:
            attack_enc = le_attack.transform([e.attack_type])[0]
        except Exception:
            attack_enc = 0
            
        try:
            proto_enc = le_proto.transform([e.protocol])[0]
        except Exception:
            proto_enc = 0
            
        try:
            geo_enc = le_geo.transform([e.geo_location])[0]
        except Exception:
            geo_enc = 0
            
        try:
            seg_enc = le_seg.transform([e.network_segment])[0]
        except Exception:
            seg_enc = 0

        processed_features.append([
            attack_enc,
            e.anomaly_score,
            e.packet_length,
            proto_enc,
            geo_enc,
            seg_enc
        ])

    features = np.array(processed_features)
    predictions = model.predict(features)
    
    anomaly_count = int(np.sum(predictions == -1))
    is_campaign = anomaly_count > len(predictions) * 0.3

    return {
        "is_campaign": is_campaign,
        "alert_level": "CRITICAL" if is_campaign else "NORMAL",
        "anomaly_count": anomaly_count,
        "total_events": len(predictions),
        "message": "Coordinated campaign detected!" if is_campaign else "Normal activity"
    }

@app.get("/sector-risk")
def sector_risk():
    conn = get_db_connection()
    if conn:
        try:
            cur = conn.cursor()
            cur.execute("""
                SELECT sector, COUNT(*) as attack_count
                FROM "AttackReports"
                WHERE "CreatedAt" >= NOW() - INTERVAL '7 days'
                  AND "IsApproved" = true
                GROUP BY sector
            """)
            rows = cur.fetchall()
            cur.close()
            conn.close()
            risk_map = {}
            for sector, count in rows:
                if count >= 20:
                    risk_map[sector] = "HIGH"
                elif count >= 8:
                    risk_map[sector] = "MEDIUM"
                else:
                    risk_map[sector] = "LOW"
            return risk_map if risk_map else _default_sector_risk()
        except Exception:
            conn.close()
            return _default_sector_risk()
    return _default_sector_risk()

def _default_sector_risk():
    return {
        "Banking": "HIGH",
        "Telecom": "MEDIUM",
        "Healthcare": "LOW",
        "Government": "HIGH",
        "Education": "LOW",
        "Energy": "MEDIUM"
    }

@app.get("/heatmap-data")
def get_heatmap_data():
    conn = get_db_connection()
    if conn:
        try:
            cur = conn.cursor()
            cur.execute("""
                SELECT "City", "Latitude", "Longitude", "Severity", "AttackType"
                FROM "AttackReports"
                WHERE "IsApproved" = true
                  AND "CreatedAt" >= NOW() - INTERVAL '7 days'
                LIMIT 200
            """)
            rows = cur.fetchall()
            cur.close()
            conn.close()
            if rows:
                return {"attacks": [
                    {"city": r[0], "lat": r[1], "lng": r[2],
                     "severity": r[3], "type": r[4]}
                    for r in rows
                ]}
        except Exception:
            conn.close()
    return {"attacks": [
        {"city": "Karachi",    "lat": 24.8607, "lng": 67.0011, "severity": 5, "type": "DDoS"},
        {"city": "Lahore",     "lat": 31.5204, "lng": 74.3587, "severity": 4, "type": "Ransomware"},
        {"city": "Islamabad",  "lat": 33.6844, "lng": 73.0479, "severity": 3, "type": "Phishing"},
        {"city": "Peshawar",   "lat": 34.0151, "lng": 71.5249, "severity": 4, "type": "Malware"},
        {"city": "Quetta",     "lat": 30.1798, "lng": 66.9750, "severity": 2, "type": "Phishing"},
        {"city": "Faisalabad", "lat": 31.4504, "lng": 73.1350, "severity": 3, "type": "DDoS"}
    ]}