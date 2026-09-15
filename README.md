# SprintBoard

SprintBoard is a full-stack collaborative task management application inspired by Kanban-style workflows.

The project was built to explore modern backend and full-stack development practices using **C#/.NET and React**, with a strong focus on **Clean Architecture, authentication, authorization, automated testing, integration testing, Docker, and collaborative features**.

---

## 🚀 Technologies

### Backend

* C#
* .NET 10
* ASP.NET Core Web API
* Entity Framework Core
* SQL Server
* JWT Authentication
* Clean Architecture

### Frontend

* React
* TypeScript
* Vite
* Axios
* Nginx

### Testing

* xUnit v3
* Moq
* Microsoft.Testing.Platform
* ASP.NET Core `WebApplicationFactory`
* SQLite In-Memory
* Coverlet.MTP
* Unit Testing
* Controller Testing
* Middleware Testing
* Authorization Testing
* Integration Testing
* HTTP Pipeline Testing
* Code Coverage

### DevOps / Infrastructure

* Docker
* Docker Compose
* Multi-stage Docker builds
* SQL Server container
* Persistent Docker volumes
* Nginx reverse proxy
* Environment-based configuration

---

## ✨ Features

* User registration and authentication
* JWT-based authentication
* User profile management
* Profile image upload
* Board creation and management
* Collaborative board members
* Owner, Admin and Member roles
* Board invitations by email
* Invitation acceptance and rejection
* Member role management
* Member removal
* Voluntary board leave
* Cards with status management
* Card checklists
* Authorization based on board membership and roles
* Global API exception handling
* Automated tests across multiple application layers
* Full HTTP integration testing
* Dockerized backend, frontend and database
* Persistent SQL Server data through Docker volumes

---

# 🏗️ Architecture

The backend follows **Clean Architecture** principles and is divided into:

```text
SprintBoard.Domain
SprintBoard.Application
SprintBoard.Infrastructure
SprintBoard.api
```

The frontend is located in:

```text
sprintboard-web
```

Automated tests are located in:

```text
SprintBoard.Test
```

---

## Domain

Contains:

* Core entities
* Domain behavior
* Domain rules
* Board roles
* Card and checklist state

---

## Application

Contains:

* Application services
* Business rules
* DTOs
* Interfaces
* Authorization logic
* Use-case orchestration

---

## Infrastructure

Contains:

* Entity Framework Core
* SQL Server persistence
* Repository implementations
* Database configuration
* Email infrastructure

---

## API

Contains:

* Controllers
* JWT authentication
* HTTP services
* File storage service
* Global exception middleware
* Dependency injection configuration
* Application startup configuration

---

## Frontend

The frontend is built with React, TypeScript and Vite.

It communicates with the backend through Axios.

When running through Docker, Nginx serves the React production build and proxies:

```text
/api/*
```

to the ASP.NET Core API container.

---

# 🐳 Docker

SprintBoard can run as a complete containerized environment using **Docker Compose**.

The Docker environment contains three main services:

```text
Browser
   │
   │ http://localhost:3000
   ▼
┌──────────────────────┐
│ React + Nginx        │
│ sprintboard-web      │
│ Port 80              │
└──────────┬───────────┘
           │ /api/v1/*
           ▼
┌──────────────────────┐
│ ASP.NET Core .NET 10 │
│ sprintboard-api      │
│ Port 8080            │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│ SQL Server 2022      │
│ sqlserver:1433       │
└──────────┬───────────┘
           │
           ▼
    Persistent Volume
```

---

## Docker Services

### Frontend

The frontend uses a multi-stage Docker build.

The application is first compiled using Node.js:

```text
Node.js
   ↓
npm ci
   ↓
npm run build
   ↓
dist/
```

The production files are then served by **Nginx**.

Nginx also acts as a reverse proxy for backend requests.

Example:

```text
Browser request:
/api/v1/boards

        ↓

Nginx

        ↓

http://api:8080/api/v1/boards
```

This allows the browser to communicate with the frontend and backend through the same origin.

---

### Backend

The backend also uses a multi-stage Docker build.

```text
.NET 10 SDK
      ↓
dotnet restore
      ↓
dotnet publish
      ↓
ASP.NET Core Runtime
```

The final runtime image contains only the published application and the ASP.NET Core runtime.

---

### Database

SQL Server 2022 runs in its own container.

The API connects to it internally using the Docker service name:

```text
sqlserver
```

instead of `localhost`.

Database data is persisted through a named Docker volume:

```text
sprintboard-sql-data
```

This means database data survives:

```bash
docker compose down
docker compose up
```

Removing the Docker containers does not automatically remove the stored SprintBoard data.

---

# ▶️ Running SprintBoard with Docker

## Requirements

Install:

* Docker Desktop
* WSL 2 on Windows
* Hardware virtualization enabled

---

## Environment Variables

Create a `.env` file in the repository root.

Example:

```env
SA_PASSWORD=your-local-sql-server-password
JWT_KEY=your-local-jwt-secret-key
```

Do not commit this file.

The repository `.gitignore` excludes environment files containing local secrets.

---

## Start the Application

From the repository root:

```bash
docker compose up -d --build
```

Docker will:

1. Build the React frontend
2. Build the ASP.NET Core backend
3. Start SQL Server
4. Wait for SQL Server health checks
5. Start the API
6. Start Nginx
7. Connect all services through the Docker network

---

## Application URLs

### SprintBoard

```text
http://localhost:3000
```

### Swagger

```text
http://localhost:8080/swagger
```

### API

```text
http://localhost:8080/api/v1
```

---

## Check Containers

```bash
docker compose ps
```

Expected services:

```text
sprintboard-web
sprintboard-api
sprintboard-sqlserver
```

---

## Stop SprintBoard

```bash
docker compose down
```

Database data remains stored in the Docker volume.

To also remove the volume:

```bash
docker compose down -v
```

> Warning: removing the volume deletes the containerized SQL Server database data.

---

# 🔐 Authentication

SprintBoard uses JWT Bearer authentication.

The flow is:

```text
Register / Login
       ↓
JWT generated
       ↓
Client stores token
       ↓
Authorization: Bearer <token>
       ↓
Protected API endpoints
```

JWT configuration is supplied through environment-specific application configuration.

---

# 🛡️ Authorization

SprintBoard implements board membership and role-based authorization.

Available roles:

## Owner

The Owner has full control over the board.

The Owner can:

* Manage the board
* Manage members
* Change member roles
* Remove members
* Invite users
* Delete the board

The Owner cannot leave their own board.

---

## Admin

Admins can:

* Invite users
* Manage permitted board resources
* Remove regular Members

Admins cannot remove other Admins when the current authorization rules forbid it.

---

## Member

Members can participate in boards and manage resources permitted by their role.

Members cannot perform Owner or Admin-only operations.

Authorization rules are centralized through:

```text
MembershipAuthorizationService
```

---

# 🧪 Automated Tests

SprintBoard has an extensive automated test suite.

Current status:

```text
Total:   340
Passed:  340
Failed:  0
Skipped: 0
```

## ✅ 340 automated tests passing

The suite contains:

* Unit tests
* Service tests
* Authorization tests
* Controller tests
* Middleware tests
* JWT tests
* HTTP pipeline tests
* Integration tests
* Multi-user collaboration tests

---

# 🔬 Test Strategy

## Unit Tests

Covered services include:

* `AuthService`
* `UserService`
* `BoardService`
* `CardService`
* `CardTaskService`
* `InvitationService`
* `MembershipAuthorizationService`
* `JwtTokenService`

The tests validate:

* Input validation
* Missing resources
* Invalid operations
* Authorization failures
* Duplicate resources
* Role restrictions
* Repository interactions
* JWT claims
* JWT expiration
* JWT signatures

---

## Controller Tests

Covered controllers include:

* `AuthController`
* `UsersController`
* `BoardsController`
* `CardsController`
* `CardTasksController`
* `InvitationsController`

Controller tests validate request handling and HTTP responses independently from the full server.

---

## Middleware Tests

`GlobalExceptionMiddleware` is covered by automated tests.

Exception mappings include:

```text
ArgumentException           → 400 Bad Request
UnauthorizedAccessException → 401 Unauthorized
ForbiddenAccessException    → 403 Forbidden
KeyNotFoundException        → 404 Not Found
InvalidOperationException   → 409 Conflict
Unexpected Exception        → 500 Internal Server Error
```

Tests also verify:

* JSON responses
* Trace IDs
* Pipeline continuation
* Protection against internal error exposure

---

# 🌐 Integration Tests

SprintBoard integration tests use:

* `WebApplicationFactory`
* ASP.NET Core real HTTP pipeline
* Real controllers
* Real application services
* Real repositories
* Entity Framework Core
* Real JWT generation and validation
* SQLite in-memory relational database

External infrastructure such as real email delivery is replaced during automated integration testing.

---

## API Pipeline Tests

The integration suite validates:

* `401 Unauthorized`
* `400 Bad Request`
* Malformed JSON
* Unknown routes
* Global exception middleware behavior

---

## Authentication Flow

```text
Register
   ↓
JWT generation
   ↓
Authenticated request
   ↓
Controller
   ↓
Application Service
   ↓
Repository
   ↓
SQLite Database
```

---

## Board Flow

Integration tests validate:

* Registration
* Login
* JWT authentication
* Board creation
* Board persistence
* Retrieving user boards
* Retrieving boards by ID

---

## Collaboration Flow

Multi-user tests validate scenarios such as:

```text
Owner registers
      ↓
Owner creates Board
      ↓
User B registers
      ↓
User B cannot access Board
      ↓
Owner invites User B
      ↓
User B accepts invitation
      ↓
Membership is created
      ↓
User B gains access
```

They also cover:

* Admin invitations
* Member access
* Role promotion
* Member to Admin promotion
* Member removal
* Admin removing Members
* Admin restrictions
* Member leaving a Board
* Owner being prevented from leaving their Board
* Unauthorized users being denied access

---

# 📊 Code Coverage

Code coverage is collected using:

* Coverlet.MTP
* Microsoft.Testing.Platform
* Cobertura reports

Generated Entity Framework migrations and third-party libraries are excluded from the coverage calculation.

## Current Coverage

```text
Line Coverage:   86.46%
Branch Coverage: 83.53%

Lines Covered:      1463 / 1692
Branches Covered:    279 / 334
```

## Coverage by Project

| Project | Line Coverage | Branch Coverage |
|---|---:|---:|
| SprintBoard.Application | 100% | 100% |
| SprintBoard.api | 93.11% | 66.66% |
| SprintBoard.Domain | 77.93% | 53.94% |
| SprintBoard.Infrastructure | 63.17% | 0% |

The application layer, where most business rules are implemented, has:

```text
100% Line Coverage
100% Branch Coverage
```

Infrastructure coverage is lower because external infrastructure such as SMTP delivery and physical file storage is intentionally isolated from most automated tests.

---

# 📈 Generating Code Coverage

The repository uses **Microsoft.Testing.Platform** as the test runner.

Run:

```powershell
dotnet test `
  --coverlet `
  --coverlet-output-format cobertura `
  --coverlet-include "[SprintBoard.*]*" `
  --coverlet-exclude-by-file "**/Migrations/**"
```

Generated reports are stored in:

```text
TestResults/
```

Test results and coverage artifacts are ignored by Git.

---

# ▶️ Running Tests

From the repository root:

```bash
dotnet test
```

Expected result:

```text
Total:   340
Passed:  340
Failed:  0
Skipped: 0
```

---

# 📁 Project Structure

```text
SprintBoard/
│
├── SprintBoard.Domain/
├── SprintBoard.Application/
├── SprintBoard.Infrastructure/
├── SprintBoard.api/
│   └── Dockerfile
│
├── SprintBoard.Test/
│
├── sprintboard-web/
│   ├── public/
│   │   └── favicon.png
│   ├── src/
│   ├── Dockerfile
│   └── nginx.conf
│
├── docker-compose.yml
├── .dockerignore
├── .gitignore
├── global.json
└── README.md
```

---

# 🎨 Branding

SprintBoard includes its own favicon and browser branding.

The favicon is served through the React/Vite frontend:

```text
sprintboard-web/public/favicon.png
```

---

# 📌 Project Status

SprintBoard is under active development.

## ✅ Completed

* Clean Architecture backend
* ASP.NET Core Web API
* React + TypeScript frontend
* JWT authentication
* User management
* Profile image support
* Board management
* Cards
* Card checklists
* Board membership
* Owner / Admin / Member roles
* Email invitation flow
* Invitation acceptance and rejection
* Member role management
* Global exception handling
* Automated service tests
* Authorization tests
* JWT tests
* Controller tests
* Middleware tests
* HTTP pipeline integration tests
* Authentication integration tests
* Board integration tests
* Multi-user collaboration integration tests
* SQLite integration-test environment
* 340 automated tests
* 86.46% line coverage
* 83.53% branch coverage
* Microsoft.Testing.Platform configuration
* Coverlet code coverage
* Dockerized ASP.NET Core API
* Dockerized React frontend
* Nginx production frontend
* Nginx API reverse proxy
* Dockerized SQL Server 2022
* Persistent SQL Server volume
* Docker Compose orchestration
* Automatic Development database migrations
* Full-stack Docker environment
* SprintBoard favicon / browser branding

---

## 🚧 Next Improvements

* Production environment configuration
* Secure production secrets management
* Production database migration strategy
* Production email delivery
* HTTPS configuration
* Deployment
* CI/CD pipeline
* Expanded API documentation
* Additional frontend improvements
* Monitoring and observability

---

# 🎯 Current Development Stage

The automated testing and Docker containerization phases are complete.

Current quality metrics:

```text
Automated Tests:  340
Passing:          340
Line Coverage:    86.46%
Branch Coverage:  83.53%
Application:      100% line / 100% branch coverage
```

The entire application can now be started with:

```bash
docker compose up -d --build
```

The next major phase is **production preparation and deployment**.

---

# 👨‍💻 Author

**Diego Sousa Mello**

Backend Developer focused on **C# and .NET**.
