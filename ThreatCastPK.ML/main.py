from fastapi import FastAPI

app = FastAPI()

@app.get("/")
def root():
    return {"message": "ThreatCast PK ML Service Running"}

@app.get("/health")
def health():
    return {"status": "healthy"}