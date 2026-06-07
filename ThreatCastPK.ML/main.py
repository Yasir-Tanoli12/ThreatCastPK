from fastapi import FastAPI
from pydantic import BaseModel
from sklearn.ensemble import IsolationForest
import numpy as np

app = FastAPI(title="ThreatCast PK ML Service")

# Input model for attack data
class AttackData(BaseModel):
    ip_count: float
    attack_frequency: float
    severity: float
    duration_hours: float

@app.get("/")
def root():
    return {"message": "ThreatCast PK ML Service Running"}

@app.get("/health")
def health():
    return {"status": "healthy"}

@app.post("/detect-campaign")
def detect_campaign(data: AttackData):
    features = np.array([[
        data.ip_count,
        data.attack_frequency,
        data.severity,
        data.duration_hours
    ]])
    model = IsolationForest(contamination=0.1, random_state=42)
    # Train on sample data (will be replaced with real DB data later)
    sample = np.array([
        [10, 5, 3, 2], [50, 20, 4, 6], [5, 2, 1, 1],
        [100, 50, 5, 12], [3, 1, 2, 0.5], [80, 40, 5, 8]
    ])
    model.fit(sample)
    result = model.predict(features)[0]
    return {
        "is_campaign": bool(result == -1),
        "alert_level": "HIGH" if result == -1 else "NORMAL",
        "message": "Coordinated campaign detected!" if result == -1 else "Normal activity"
    }

@app.get("/sector-risk")
def sector_risk():
    return {
        "Banking": "HIGH",
        "Telecom": "MEDIUM",
        "Healthcare": "LOW",
        "Government": "HIGH",
        "Education": "LOW",
        "Energy": "MEDIUM"
    }