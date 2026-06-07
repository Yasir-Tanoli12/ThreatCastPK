from fastapi import FastAPI
from pydantic import BaseModel
from sklearn.ensemble import IsolationForest
import numpy as np
import os
import psycopg2
from typing import List

app = FastAPI(title="ThreatCast PK ML Service")

DB_URL = os.getenv("DATABASE_URL", "")

def get_db_connection():
    if not DB_URL:
        return None
    try:
        return psycopg2.connect(DB_URL)
    except Exception:
        return None

class AttackEvent(BaseModel):
    ip_count: float
    attack_frequency: float
    severity: float
    duration_hours: float

class CampaignRequest(BaseModel):
    events: List[AttackEvent]

@app.get("/")
def root():
    return {"message": "ThreatCast PK ML Service Running"}

@app.get("/health")
def health():
    return {"status": "healthy"}

@app.post("/detect-campaign")
def detect_campaign(request: CampaignRequest):
    if not request.events:
        return {"is_campaign": False, "alert_level": "NORMAL", "message": "No events provided"}

    features = np.array([
        [e.ip_count, e.attack_frequency, e.severity, e.duration_hours]
        for e in request.events
    ])

    sample = np.array([
        [10, 5, 3, 2], [50, 20, 4, 6], [5, 2, 1, 1],
        [100, 50, 5, 12], [3, 1, 2, 0.5], [80, 40, 5, 8],
        [15, 8, 2, 3], [60, 30, 5, 10], [7, 3, 1, 1.5]
    ])

    model = IsolationForest(contamination=0.2, random_state=42)
    model.fit(sample)

    predictions = model.predict(features)
    anomaly_count = int(np.sum(predictions == -1))
    is_campaign = anomaly_count > len(predictions) * 0.4

    return {
        "is_campaign": is_campaign,
        "alert_level": "HIGH" if is_campaign else "NORMAL",
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