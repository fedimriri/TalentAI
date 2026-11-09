<p align="center">
  <img src="screenshots/login.png" alt="TalentAI" width="600"/>
</p>

<h1 align="center">TalentAI — AI-Powered Recruitment & ATS Platform</h1>

<p align="center">
  <em>An intelligent recruitment platform combining Resume Parsing, ATS Scoring, and AI Matching — built for modern hiring workflows.</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10"/>
  <img src="https://img.shields.io/badge/MongoDB-47A248?style=for-the-badge&logo=mongodb&logoColor=white" alt="MongoDB"/>
  <img src="https://img.shields.io/badge/Docker-Ready-2496ED?style=for-the-badge&logo=docker&logoColor=white" alt="Docker"/>
  <img src="https://img.shields.io/badge/AI-Powered-FF6F00?style=for-the-badge&logo=openai&logoColor=white" alt="AI Powered"/>
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="MIT License"/>
</p>

---

## 👨‍💻 Author

**Fadi Mriri** — Cloud & DevOps Engineer

📍 Tunis, Tunisia
📧 [fmriri2@gmail.com](mailto:fmriri2@gmail.com)

[![LinkedIn](https://img.shields.io/badge/LinkedIn-0A66C2?style=flat&logo=linkedin&logoColor=white)](https://linkedin.com/in/fadi-mriri)
[![GitHub](https://img.shields.io/badge/GitHub-181717?style=flat&logo=github&logoColor=white)](https://github.com/fedimriri)
[![Portfolio](https://img.shields.io/badge/Portfolio-8b5cf6?style=flat&logo=vercel&logoColor=white)](https://fadimriri-portfolio.vercel.app)

---

## 🚀 Overview

TalentAI is an **end-to-end AI-powered recruitment platform** designed to automate and enhance the hiring pipeline:

- **Parse resumes** into structured candidate profiles (skills, experience, education)
- **Analyze job descriptions** to extract requirements automatically
- **Score candidates** with an ATS engine across multiple dimensions
- **Match candidates to jobs** using AI-driven semantic analysis
- **Streamline HR workflows** with role-based dashboards and email notifications

---

## ✨ Features

### 🎯 Candidate Side
- Resume Upload & Parsing (PDF / DOCX)
- ATS Score Simulation
- Resume Editing & Profile Management
- Job Application Tracking

### 📋 HR Side
- Job Posting & Management
- Candidate Ranking (ATS + AI Score)
- Application Status Management (Approve / Reject / Shortlist)
- Secure Resume Viewing
- Filtering by Skills, Score & Experience

### 🧠 AI & Core Engine
| Phase | Module | Description |
|-------|--------|-------------|
| 3.1 | **Resume Parser** | Extracts skills, experience ranges, education from raw documents |
| 3.2 | **Job Description Analyzer** | Identifies required skills, experience years, qualifications |
| 3.3 | **Matching Algorithm** | Multi-dimensional scoring across skills, experience, education |
| 3.4 | **ATS Scoring** | Weighted composite score with detailed breakdown |
| 3.5 | **AI Semantic Matching** | Groq API (LLaMA) for intelligent context-aware matching |

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        ASP.NET Core MVC                         │
│                     (Controllers + Views)                       │
├─────────────┬───────────────┬───────────────┬───────────────────┤
│  Auth       │  Admin        │  HR           │  Candidate        │
│  Controller │  Controller   │  Controller   │  Controller       │
├─────────────┴───────────────┴───────────────┴───────────────────┤
│                        Services Layer                           │
├──────────┬──────────┬──────────┬──────────┬─────────────────────┤
│ Resume   │ Job      │ ATS      │ Matching │ Email               │
│ Parser   │ Parser   │ Scoring  │ Service  │ Service             │
├──────────┴──────────┴──────────┴──────────┴─────────────────────┤
│                     AI Integration (Groq API)                   │
├─────────────────────────────────────────────────────────────────┤
│                MongoDB (NoSQL Data Layer)                        │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🛠️ Tech Stack

| Category | Technology |
|----------|-----------|
| **Backend** | ASP.NET Core MVC (.NET 10) |
| **Database** | MongoDB |
| **AI** | Groq API (LLaMA models) |
| **PDF Parsing** | PdfPig |
| **DOCX Parsing** | OpenXML SDK |
| **Email** | Gmail SMTP via MailKit |
| **Containerization** | Docker & Docker Compose |
| **DevOps** | CI/CD Ready, Multi-stage Docker Build |

---

## 📸 Screenshots

<details>
<summary><strong>🔐 Login Page</strong></summary>
<br/>
<img src="screenshots/login.png" alt="Login Page" width="700"/>
</details>

<details>
<summary><strong>🛡️ Admin Dashboard</strong></summary>
<br/>
<img src="screenshots/admin_dashboard.png" alt="Admin Dashboard" width="700"/>
</details>

<details>
<summary><strong>📋 HR Dashboard — Jobs Feed</strong></summary>
<br/>
<img src="screenshots/hr_dashboard.png" alt="HR Dashboard" width="700"/>
</details>

<details>
<summary><strong>📝 Create Job Listing</strong></summary>
<br/>
<img src="screenshots/add_job.png" alt="Add Job Page" width="700"/>
</details>

---

## ⚙️ Installation

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- MongoDB (local or containerized)
- Docker *(optional)*

### Run Locally

```bash
# Clone the repository
git clone https://github.com/fedimriri/TalentAI.git
cd TalentAI

# Restore dependencies
dotnet restore

# Run the application
dotnet run
```

App available at: **http://localhost:5000**

---

## 🐳 Run with Docker

```bash
# Build the image
docker build -t talentai .

# Run the container
docker run -p 5000:5000 talentai
```

**Using Docker Compose** (connects to external MongoDB):

```bash
docker compose up --build
```

> **Note:** If your MongoDB runs in a separate compose project, TalentAI automatically joins the external `mongo_default` network to connect.

---

## 🔐 Default Admin

On first startup, the app auto-seeds an admin account:

| Field | Value |
|-------|-------|
| Email | `admin@talentai.com` |
| Password | `admin123` |
| Role | Admin |

> ⚠️ **Change these credentials in production.**

---

## ⚙️ Configuration

All settings are in `appsettings.json`:

| Section | Purpose | Key Values |
|---------|---------|------------|
| `MongoSettings` | Database connection | `ConnectionString`, `DatabaseName` |
| `AISettings` | AI integration | `GroqApiKey` |
| `EmailSettings` | SMTP notifications | `Host`, `Port`, `Username`, `Password`, `FromEmail` |

### SMTP (Gmail)
The app sends branded HTML emails for:
- HR account creation (purple theme)
- Application approved (green theme)
- Application rejected (red theme)
- Application under review (blue theme)

---

## 📁 Project Structure

```
TalentAI/
├── Configurations/       # Settings: Mongo, Email, AI
├── Controllers/          # MVC: Auth, Admin, HR, Candidate
├── Data/                 # MongoDbContext
├── DTOs/                 # Data Transfer Objects
├── Models/               # User, Job, JobApplication
├── Services/             # Core Business Logic
│   ├── ResumeParserService.cs
│   ├── JobParserService.cs
│   ├── MatchingService.cs
│   ├── AtsScoringService.cs
│   ├── EmailService.cs
│   └── EmailTemplateBuilder.cs
├── Views/                # Razor Views (dark-theme UI)
├── wwwroot/              # Static files & uploads
├── Dockerfile            # Multi-stage production build
├── compose.yaml          # Docker Compose config
└── Program.cs            # Entry point & DI setup
```

---

## 📄 License

MIT License © 2026 **Fadi Mriri**

---

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. **Do not** push directly to `main`
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Submit a Pull Request with a clear description

---

<p align="center">
  <strong>Built with a strong focus on DevOps, scalability, and AI-driven recruitment systems.</strong>
</p>

<p align="center">
  <sub>⭐ Star this repo if you find it useful!</sub>
</p>

---

## TalentAI DevOps deployment

The application runs as a .NET 10 container in the `talentai` Minikube profile. NGINX Ingress routes `talentai.local` to a ClusterIP Service and the Helm-managed Deployment. MongoDB Atlas is the existing external database. Terraform manages only the `talentai` namespace; Ansible validates local prerequisites. Prometheus, blackbox exporter and Grafana run in the `monitoring` namespace.

### Prerequisites and private configuration

Use Docker, kubectl, Minikube, Helm, Terraform, Ansible and .NET 10 on the Ubuntu host. Select Kubernetes context `talentai` before any cluster command. Keep the Atlas URI, Groq key, email credentials and Grafana password outside Git. `appsettings.json` and `appsettings.container.json` must contain no live credentials. The Kubernetes Secret `talentai-runtime` supplies the Atlas URI. A historical email password was exposed in public Git history and must be rotated outside the repository; do not reuse it.

### Local validation and deployment

```bash
ansible-playbook -i localhost, infrastructure/ansible/prepare.yml
terraform -chdir=infrastructure/terraform fmt -check
terraform -chdir=infrastructure/terraform validate
helm lint helm/talentai
kubectl config current-context
minikube status -p talentai
kubectl -n talentai get deployment,pods,service,ingress,hpa
```

The existing namespace and Helm release are already deployed. Do not reapply Terraform or reinstall Minikube merely to run these checks. For a private Atlas Compose check, use `docker compose -f compose.atlas.yaml up --no-build -d talentai`, then request `/health/live` and `/health/ready` on port 5000. The readiness endpoint pings Atlas.

### Docker Hub publishing and GitHub Actions

The image repository is `fedimriri/talentai`. `scripts/publish-image.sh` supports manual timestamp-tagged publication after a HIGH/CRITICAL Trivy scan. GitHub Actions CI uses the existing `talentai-host-runner` and tags each image with the full commit SHA. It restores and builds .NET, reports absence of test projects as a warning, fails on NuGet vulnerability findings or audit errors, builds the Docker image, requires a clean Trivy HIGH/CRITICAL result, then publishes to Docker Hub with `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` repository secrets. The new image-publishing CI path is pending a real GitHub run.

Manual CD accepts that full SHA tag on the same host runner. It checks context `talentai`, pulls and loads the image into Minikube, runs `scripts/deploy-local.sh`, waits for Helm rollout, checks the Ingress homepage and health endpoints, and uploads non-secret evidence. The CD workflow remains unverified until it is reviewed, committed and run on GitHub. Do not expose the local Kubernetes API publicly.

### Monitoring and autoscaling

Prometheus, blackbox exporter and Grafana are deployed as a small local stack. Access them through `kubectl -n monitoring port-forward service/prometheus 9090:9090` and `kubectl -n monitoring port-forward service/grafana 3000:3000`. The `TalentAI Overview` dashboard shows availability, CPU and memory. Metrics Server supplies HPA CPU metrics. A load test in `scripts/test-hpa.sh` demonstrated scaling from one to three replicas; the deployment later returned to one. This does not prove functional multi-replica use: sessions, ASP.NET Data Protection keys and CV files remain local to pods, and Minikube has one node.

For local Ingress access, get the current IP with `minikube ip -p talentai`, then run `curl --noproxy '*' --resolve talentai.local:80:MINIKUBE_IP http://talentai.local/health/ready` after replacing `MINIKUBE_IP`. A failed readiness check should be investigated through pod events and logs without printing Secrets. Save command outputs and genuine screenshots under `results/` and `labs/talentai-deployment/` for the French compte-rendu.
