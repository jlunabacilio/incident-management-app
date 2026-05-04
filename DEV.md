# Running Locally (Dev Mode)

## Prerequisites

- .NET 8 SDK
- Node.js 18+
- npm

## Backend

```bash
cd src/backend
dotnet run --project IncidentReport.API
```

API runs at **http://localhost:5000**

## Frontend

```bash
cd src/frontend
npm install
npm run dev
```

App runs at **http://localhost:3000**

## Demo Accounts

| Email               | Role       |
|---------------------|------------|
| tech@demo.com       | Technician |
| supervisor@demo.com | Supervisor |
| safety@demo.com     | Safety Officer |

> Password for all demo accounts is set during seed — check the backend seed data.

## Useful Commands

| Command | Description |
|---------|-------------|
| `dotnet build` | Build backend solution (from `src/backend`) |
| `dotnet test`  | Run xUnit tests (from `src/backend`) |
| `npm run build` | Production build of frontend (from `src/frontend`) |
