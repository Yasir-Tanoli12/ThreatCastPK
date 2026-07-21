<div align="center">

# 🛡️ ThreatCast PK

### Live Crowdsourced Cyberattack Intelligence Map of Pakistan

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Supabase-3ECF8E?logo=supabase&logoColor=white)](https://supabase.com/)
[![Python](https://img.shields.io/badge/Python-FastAPI-009688?logo=fastapi&logoColor=white)](https://fastapi.tiangolo.com/)
[![Azure](https://img.shields.io/badge/Deployed%20on-Azure-0078D4?logo=microsoftazure&logoColor=white)](https://azure.microsoft.com/)
[![License](https://img.shields.io/badge/License-Academic-lightgrey)]()

**[🌐 Live Demo](https://threatcastpk-web.azurewebsites.net)** · **[📡 API Docs](https://threatcastpk-api.azurewebsites.net/swagger)** · **[🐛 Report an Issue](../../issues)**

</div>

---

## 📖 Overview

**ThreatCast PK** is a real-time, crowdsourced cyberattack intelligence platform built specifically for Pakistan. It aggregates threat data from live external APIs, community reports, and machine learning-driven anomaly detection, then visualizes it on an animated, interactive map of Pakistan — updating live via WebSockets as new threats emerge.

Unlike a typical academic CRUD project, ThreatCast PK integrates **three external intelligence sources** (AbuseIPDB, GreyNoise, and a custom-trained ML model), pushes **real-time updates** to every connected client, and layers in a full **community forum**, **role-based moderation system**, and **email-verified authentication** — all built to reflect how a genuine security intelligence product would operate.

> Built as a semester project for **Visual Programming Lab (CS-284L)**, Air University Islamabad Campus — but engineered well past the minimum brief.

---

## ✨ Key Features

### 🗺️ Live Threat Map
- Real-time animated markers on an interactive Pakistan map (Leaflet.js), color-coded by attack type
- Auto-updates via SignalR the instant a new event is verified — no page refresh needed
- Time-range filters (1H / 6H / 24H / 7D), severity legend, and city/sector breakdowns
- 80+ Pakistani cities across all provinces and territories

### 📡 Multi-Source Threat Intelligence
- **AbuseIPDB** — polls the live blacklist for Pakistani IP ranges every 15 minutes and auto-generates verified attack events
- **GreyNoise** — filters out internet background noise/scanners so only genuine targeted attacks reach the map
- **Custom ML Model** — an Isolation Forest trained on a 40,000-row cybersecurity dataset detects *coordinated attack campaigns* in real time and broadcasts alert banners the moment a pattern is flagged

### 🤝 Crowdsourced Reporting
- Community members can submit attack reports with automatic IP reputation scoring
- Smart **auto-approval engine**: reports are instantly published to the map when reporter reputation and IP threat scores both clear the bar — otherwise routed to an admin moderation queue
- Reputation system rewards accurate reporting and penalizes false reports

### 🔐 Full Authentication Lifecycle
- Email/password + Google OAuth
- **Email verification required** before login — no unverified accounts can act on the platform
- Forgot-password flow with secure, expiring reset tokens
- Transactional emails (welcome, login alerts, password changes) via Resend, sent from a custom verified domain

### 💬 Community Forum
- Threaded discussions with categories, upvotes/downvotes, and hot/top/new sorting
- Markdown-rendered posts, post pinning (admin), content flagging, and view counts
- "Verified Reporter" flair for trusted contributors

### 🔔 Real-Time Alert Subscriptions
- Users configure up to 3 custom alert filters (attack type, city, sector, minimum severity)
- Matching threats trigger **both** an in-app SignalR notification **and** an email alert
- Fully asynchronous dispatch via a dedicated background channel — no blocking on the request path

### 🛡️ Admin & Moderation
- Full moderation queue with ML-assisted anomaly flagging on pending reports
- User management (grant/revoke reporter status, suspend/unsuspend)
- Append-only audit log for every administrative action
- Role hierarchy: `Public → Registered → Reporter → Admin`

### 📊 Analytics Dashboard
- Live stats, attack-type distribution, city breakdowns, 30-day trend charts
- Sector risk scoring (Low/Medium/High/Critical) recalculated every 30 minutes

---

## 🏗️ Architecture

```
┌────────────────────────────────────────────────────────────┐
│                    ThreatCastPK.Web                        │
│      Blazor Server · Leaflet.js · Chart.js · SignalR       │
└───────────────────────────┬────────────────────────────────┘
                            │ REST + WebSocket
┌───────────────────────────▼────────────────────────────────┐
│                    ThreatCastPK.API                        │
│        ASP.NET Core Web API · JWT Auth · SignalR Hub       │
│   Background Services: Threat Feed · Campaign Detection    │
│           · Notification Dispatch · Sector Risk            │
└──────────┬──────────────────────────────┬──────────────────┘
           │                              │
┌──────────▼──────────┐        ┌──────────▼──────────────────┐
│ ThreatCastPK.ML     │        │  External Intelligence APIs │
│ Python · FastAPI    │        │  AbuseIPDB · GreyNoise ·    │
│ Isolation Forest    │        │  Resend (Email) · Google    │
└──────────┬──────────┘        └─────────────────────────────┘
           │
┌──────────▼─────────────────────────────────────────────────┐
│              Supabase (PostgreSQL, Cloud)                  │
└────────────────────────────────────────────────────────────┘
```

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| **Frontend** | Blazor Server (.NET 10), Leaflet.js, Chart.js |
| **Real-Time** | SignalR (WebSockets) |
| **Backend API** | ASP.NET Core Web API, JWT Bearer Auth, BCrypt |
| **ML Service** | Python, FastAPI, scikit-learn (Isolation Forest) |
| **Database** | PostgreSQL (Supabase, cloud-hosted) |
| **ORM** | Entity Framework Core + Npgsql |
| **Email** | Resend API |
| **External Intel** | AbuseIPDB, GreyNoise Community API |
| **Auth** | JWT + Google OAuth 2.0 |
| **Hosting** | Azure App Service (API, Web, ML) |
| **CI/CD** | GitHub Actions |

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Python 3.11+](https://www.python.org/downloads/)
- A [Supabase](https://supabase.com/) PostgreSQL project
- API keys: [AbuseIPDB](https://www.abuseipdb.com/), [GreyNoise](https://viz.greynoise.io/), [Resend](https://resend.com/), [Google OAuth](https://console.cloud.google.com/)

### 1. Clone the repository
```bash
git clone https://github.com/Yasir-Tanoli12/ThreatCastPK.git
cd ThreatCastPK
```

### 2. Configure secrets
Create `ThreatCastPK.API/appsettings.Development.json` (excluded from git):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=postgres;Username=...;Password=...;Port=5432;SSL Mode=Require"
  },
  "Jwt": { "Key": "your-32-char-min-secret-key" },
  "AbuseIPDB": { "ApiKey": "your-key" },
  "GreyNoise": { "ApiKey": "your-key" },
  "Resend": { "ApiKey": "your-key" },
  "Authentication": {
    "Google": { "ClientId": "your-id", "ClientSecret": "your-secret" }
  }
}
```

### 3. Apply database migrations
```bash
dotnet ef database update --project ThreatCastPK.Database --startup-project ThreatCastPK.API
```

### 4. Run the API
```bash
dotnet run --project ThreatCastPK.API
```

### 5. Run the ML service
```bash
cd ThreatCastPK.ML
pip install -r requirements.txt
uvicorn main:app --reload --port 8000
```

### 6. Run the Blazor frontend
```bash
dotnet run --project ThreatCastPK.Web
```

Visit `https://localhost:5262` (or the port shown in your console).

---

## 📁 Project Structure

```
ThreatCastPK/
├── ThreatCastPK.API/          # ASP.NET Core Web API — controllers, auth, SignalR hub, background services
├── ThreatCastPK.Web/          # Blazor Server frontend — pages, components, JS interop
├── ThreatCastPK.Database/     # EF Core models, enums, DbContext, migrations
├── ThreatCastPK.ML/           # Python FastAPI ML microservice — Isolation Forest campaign detection
└── .github/workflows/         # CI/CD pipelines for API, Web, and ML deployments
```

---

## 🔑 Core CRUD Operations

| # | Operation | Entity |
|---|---|---|
| 1 | Submit Attack Report | AttackReport |
| 2 | Browse Reports | AttackReport |
| 3 | Moderation Panel (Approve/Reject) | AttackReport |
| 4 | User Registration + Login | User |
| 5 | Profile Management | User |
| 6 | Create + View Alert Subscriptions | AlertSubscription |
| 7 | Update + Delete Subscriptions | AlertSubscription |

---

## 🎓 Academic Context

Developed for **Visual Programming Lab (CS-284L)**, Spring 2026, Air University Islamabad Campus.

| Role | Owner |
|---|---|
| Backend, Auth, Security, Blazor UI | Haadi |
| Blazor UI, ML Model Training, ML Integration | Yasir |

---

## 📄 License

This project was built for academic purposes as part of a university course. Feel free to explore, fork, and learn from it.

---
