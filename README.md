# TalentAI

An intelligent Applicant Tracking System (ATS) powered by AI for modern recruitment workflows.

## Overview

TalentAI streamlines the hiring process by combining resume parsing, job description analysis, AI-powered candidate matching, and ATS scoring — all within a clean MVC web application.

## Features

- **Resume Parsing** — Automatically extracts skills, experience, and education from uploaded PDFs
- **Job Description Parsing** — Analyzes job postings to identify required skills and experience
- **ATS Scoring** — Scores candidates against job requirements across skills, experience, and education
- **AI Matching** — Intelligent candidate-to-job matching with detailed score breakdowns
- **HR Management** — Admin creates HR accounts with automatic email credential delivery
- **Application Status Notifications** — Candidates receive branded emails (Approved / Rejected / Under Review)
- **Candidate Portal** — Candidates can apply for jobs, upload resumes, and track application status

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core MVC (.NET 10) |
| Database | MongoDB |
| Email | Gmail SMTP via MailKit |
| PDF Parsing | PdfPig |
| Containerization | Docker |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- MongoDB (local or containerized)
- Docker (optional, for containerized deployment)

## Run Locally

```bash
# Clone the repository
git clone <repo-url>
cd TalentAI

# Restore dependencies
dotnet restore

# Run the application
dotnet run
```

The app will be available at: **http://localhost:5000**

## Default Admin Account

On first startup, the application automatically seeds an admin account:

| Field | Value |
|-------|-------|
| Email | `admin@talentai.com` |
| Password | `admin123` |
| Role | Admin |

> **Note:** Change these credentials in a production environment.

## Run with Docker

```bash
# Build the image
docker build -t talentai .

# Run the container
docker run -p 5000:5000 talentai
```

Or using Docker Compose:

```bash
docker compose up --build
```

## MongoDB Setup

Make sure MongoDB is running before starting the application.

**Option 1 — Local installation:**
```bash
mongod --dbpath /data/db
```

**Option 2 — Docker container:**
```bash
docker run -d -p 27017:27017 \
  -e MONGO_INITDB_ROOT_USERNAME=tek-up \
  -e MONGO_INITDB_ROOT_PASSWORD=tek-up \
  mongo
```

## Configuration

All settings are managed in `appsettings.json`:

| Section | Purpose |
|---------|---------|
| `MongoSettings` | MongoDB connection string and database name |
| `EmailSettings` | Gmail SMTP credentials (host, port, username, app password) |
| `AISettings` | API key for AI-powered features |

## Project Structure

```
TalentAI/
├── Configurations/    # Settings classes (MongoSettings, EmailSettings, etc.)
├── Controllers/       # MVC controllers (Admin, HR, Candidate, Auth)
├── DTOs/              # Data transfer objects
├── Data/              # MongoDB context
├── Models/            # Domain models (User, Job, JobApplication, etc.)
├── Repositories/      # Data access layer
├── Services/          # Business logic (Email, Matching, Parsing, etc.)
├── Views/             # Razor views
├── wwwroot/           # Static files and uploaded resumes
├── Program.cs         # Application entry point and DI configuration
└── Dockerfile         # Container build instructions
```

## Notes

- Uploaded resumes are stored in `wwwroot/uploads/`
- AI features (matching, parsing) require a valid API key in `AISettings`
- Email sending uses Gmail App Password (not regular Gmail password)
- Email failures are logged but do not break application flow
