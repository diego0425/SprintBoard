# SprintBoard

SprintBoard is a full-stack collaborative task management application inspired by Kanban-style workflows.

The project was built to explore modern backend and full-stack engineering practices using **C#/.NET and React**, with a strong focus on **Clean Architecture, authentication, authorization, automated testing, Docker, production-oriented configuration, health monitoring, structured logging and collaborative workflows**.

---

# 🚀 Technologies

## Backend

- C#
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Bearer Authentication
- Serilog
- Clean Architecture

## Frontend

- React
- TypeScript
- Vite
- Axios
- Nginx

## Testing

- xUnit v3
- Moq
- Microsoft.Testing.Platform
- ASP.NET Core `WebApplicationFactory`
- SQLite In-Memory
- Coverlet.MTP
- Unit Testing
- Controller Testing
- Middleware Testing
- Authorization Testing
- Integration Testing
- HTTP Pipeline Testing
- Logging Testing
- Code Coverage

## DevOps / Infrastructure

- Docker
- Docker Compose
- Multi-stage Docker builds
- SQL Server 2022 container
- Persistent Docker volumes
- Nginx reverse proxy
- Environment-based configuration
- Health checks
- Structured logging
- Correlation IDs

---

# ✨ Features

- User registration and authentication
- JWT-based authentication
- User profile management
- Profile image upload
- Persistent profile image storage
- Board creation and management
- Collaborative board members
- Owner, Admin and Member roles
- Board invitations by email
- Invitation acceptance and rejection
- Member role management
- Member removal
- Voluntary board leave
- Cards with status management
- Card checklists
- Authorization based on board membership and roles
- Global API exception handling
- Structured HTTP request logging
- Business-event logging
- Correlation IDs for request tracing
- API liveness and readiness health checks
- Automated tests across multiple application layers
- Full HTTP integration testing
- Dockerized backend, frontend and database
- Persistent database storage
- Persistent uploaded-file storage

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

- Core entities
- Domain behavior
- Domain rules
- Board roles
- Card and checklist state

---

## Application

Contains:

- Application services
- Business rules
- DTOs
- Interfaces
- Authorization logic
- Use-case orchestration

---

## Infrastructure

Contains:

- Entity Framework Core
- SQL Server persistence
- Repository implementations
- Database configuration
- SMTP email infrastructure

---

## API

Contains:

- Controllers
- JWT authentication
- HTTP services
- Local file storage
- Correlation ID middleware
- Global exception middleware
- Health checks
- Serilog configuration
- Dependency injection
- Environment-specific configuration

---

# 🌐 Frontend

The frontend is built with **React, TypeScript and Vite**.

It communicates with the backend through Axios.

When running through Docker, Nginx serves the React production build and acts as a reverse proxy for:

```text
/api/*
/uploads/*
```

Both API requests and uploaded profile images can therefore be accessed through the same frontend origin.

---

# 🔎 Logging and Observability

SprintBoard includes structured application logging using **Serilog**.

The application records HTTP requests, application failures and important business events while avoiding sensitive information such as passwords, JWT tokens and invitation tokens.

Business events currently include:

```text
User registration
Board creation
Board updates
Board deletion
Member role changes
Member removal
Board leave
Invitation creation
Invitation acceptance
Invitation rejection
Profile updates
Profile image updates
```

---

## Structured HTTP Logging

HTTP requests are logged with structured properties including:

```text
RequestMethod
RequestPath
StatusCode
Elapsed
CorrelationId
```

Example:

```text
[21:42:13 INF] [f94ea2be20fb4021b4cb98a082f5c893]
HTTP GET /api/v1/boards responded 200 in 31.7271 ms
```

Log levels reflect request outcomes:

```text
2xx / 3xx → Information
4xx       → Warning
5xx       → Error
```

Framework and Entity Framework Core log levels are configured to reduce unnecessary SQL noise during normal execution.

---

# 🔗 Correlation IDs

Every HTTP request receives a correlation identifier.

SprintBoard uses the header:

```text
X-Correlation-ID
```

Clients may provide a valid correlation ID or allow the application to generate one automatically.

The identifier is:

- Stored in `HttpContext.TraceIdentifier`
- Returned through the response header
- Added to structured application logs
- Included in standardized API error responses

This allows multiple events produced by one HTTP request to be traced together.

```text
HTTP Request
     ↓
Correlation ID
     ↓
Business Event
     ↓
Exception / Response
```

---

# ⚠️ Exception Handling

SprintBoard uses a global exception middleware to convert application exceptions into standardized HTTP responses.

Mappings include:

```text
ArgumentException           → 400 Bad Request
UnauthorizedAccessException → 401 Unauthorized
ForbiddenAccessException    → 403 Forbidden
KeyNotFoundException        → 404 Not Found
InvalidOperationException   → 409 Conflict
Unexpected Exception        → 500 Internal Server Error
```

Expected request failures are logged as warnings.

Unexpected failures are logged as errors together with the original server-side exception and stack trace.

Internal exception details are not returned to clients for unexpected failures.

---

# ❤️ Health Checks

SprintBoard exposes dedicated application health endpoints.

## Liveness

```text
GET /health/live
```

Checks whether the API process is running.

## Readiness

```text
GET /health/ready
```

Checks whether the application is ready to serve traffic and can access the configured database.

## Aggregate Health

```text
GET /health
```

Executes all registered health checks.

Database readiness uses Entity Framework Core health checks.

---

# 🐳 Docker

SprintBoard can run as a complete containerized environment using **Docker Compose**.

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
           │
           │ /api/*
           │ /uploads/*
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
└──────────────────────┘
```

The environment contains three main services:

```text
sprintboard-web
sprintboard-api
sprintboard-sqlserver
```

---

## Frontend Container

The frontend uses a multi-stage Docker build.

```text
Node.js
   ↓
npm ci
   ↓
npm run build
   ↓
dist/
   ↓
Nginx
```

Nginx serves the React production files and forwards API requests to:

```text
http://api:8080
```

Example:

```text
Browser
/api/v1/boards

      ↓

Nginx

      ↓

http://api:8080/api/v1/boards
```

Uploaded files are also proxied:

```text
Browser
/uploads/profiles/...

      ↓

Nginx

      ↓

ASP.NET Core
```

Nginx allows request bodies up to:

```text
5 MB
```

for profile image uploads.

---

## Backend Container

The API uses a multi-stage .NET build.

```text
.NET 10 SDK
      ↓
dotnet restore
      ↓
dotnet publish
      ↓
ASP.NET Core Runtime
```

The final runtime image contains only the published application and ASP.NET Core runtime.

---

## Database Container

SQL Server 2022 runs in its own container.

The API connects internally using:

```text
sqlserver
```

rather than `localhost`.

---

# 💾 Persistent Docker Storage

SprintBoard currently uses two Docker named volumes.

## Database

```text
sprintboard-sql-data
```

Stores SQL Server data.

## Uploaded Files

```text
sprintboard-upload-data
```

Stores uploaded profile images.

Both survive:

```bash
docker compose down
docker compose up
```

Removing containers therefore does not automatically delete SprintBoard database data or profile images.

---

# ▶️ Running SprintBoard with Docker

## Requirements

Install:

- Docker Desktop
- WSL 2 on Windows
- Hardware virtualization enabled

---

## Environment Variables

Create a `.env` file in the repository root.

The repository provides:

```text
.env.example
```

Example:

```env
SA_PASSWORD=your-local-sql-server-password

JWT_KEY=your-secure-jwt-key-at-least-32-bytes

SMTP_USERNAME=your-smtp-username
SMTP_PASSWORD=your-smtp-password
```

Do not commit the real `.env` file.

Sensitive configuration is intentionally kept outside committed application settings.

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
4. Wait for the SQL Server health check
5. Start the API
6. Start Nginx
7. Connect the services through the Docker network
8. Mount persistent database and upload volumes

---

# 🌍 Application URLs

## SprintBoard

```text
http://localhost:3000
```

## Swagger

```text
http://localhost:8080/swagger
```

Swagger is available when the API runs in the Development environment.

## API

```text
http://localhost:8080/api/v1
```

## Health

```text
http://localhost:8080/health
```

## Liveness

```text
http://localhost:8080/health/live
```

## Readiness

```text
http://localhost:8080/health/ready
```

---

# 🐳 Docker Commands

Check running containers:

```bash
docker compose ps
```

View API logs:

```bash
docker compose logs api
```

Follow API logs:

```bash
docker compose logs -f api
```

Stop SprintBoard:

```bash
docker compose down
```

Persistent data remains available.

To also remove persistent volumes:

```bash
docker compose down -v
```

> Warning: this deletes both the SQL Server database and persisted uploaded files.

---

# 🔐 Authentication

SprintBoard uses JWT Bearer authentication.

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

JWT configuration is loaded through environment-specific configuration.

The API validates:

- Signing key
- Issuer
- Audience
- Token lifetime
- Minimum signing-key length

Missing or invalid critical JWT configuration prevents the API from starting with unsafe settings.

---

# 🛡️ Authorization

SprintBoard implements board membership and role-based authorization.

Available roles:

## Owner

Owners can:

- Manage boards
- Manage members
- Change member roles
- Remove members
- Invite users
- Delete boards

Owners cannot leave their own board.

## Admin

Admins can:

- Invite users
- Manage permitted board resources
- Remove regular Members

Admins cannot perform operations reserved exclusively for Owners.

## Member

Members can participate in boards and manage resources allowed by their role.

Members cannot perform Owner or Admin-only operations.

Authorization rules are centralized through:

```text
MembershipAuthorizationService
```

---

# 📧 Board Invitations

Board Owners and authorized Administrators can invite users by email.

```text
Invitation creation
      ↓
Secure random token
      ↓
Email delivery
      ↓
Accept / Decline
      ↓
Membership update
```

SMTP credentials are supplied externally and are not stored in committed configuration.

The Development environment currently supports sandbox SMTP testing.

---

# 🖼️ Profile Images

Authenticated users can upload profile images using:

```text
JPEG
PNG
WEBP
```

Maximum request size:

```text
5 MB
```

Uploaded images are persisted locally in Docker using:

```text
sprintboard-upload-data
```

The public image URL is generated from environment-specific file-storage configuration.

Storage access is abstracted through:

```text
IFileStorageService
```

which allows the local implementation to be replaced by an external object-storage provider in the future.

---

# 🗄️ Database Migrations

During Development, SprintBoard automatically applies Entity Framework Core migrations when the application starts.

Production migration execution is intentionally handled separately.

The recommended production strategy is to generate an idempotent migration script:

```powershell
dotnet ef migrations script `
  --idempotent `
  --project SprintBoard.Infrastructure `
  --startup-project SprintBoard.api `
  --context SprintBoardDbContext `
  --output migration-production.sql
```

The script can then be reviewed, backed up against and explicitly executed before the new API version is deployed.

This prevents every production application instance from independently attempting schema migrations during startup.

---

# 🧪 Automated Tests

SprintBoard has an extensive automated test suite.

Current status:

```text
Total:   361
Passed:  361
Failed:  0
Skipped: 0
```

## ✅ 361 automated tests passing

The test suite includes:

- Unit tests
- Service tests
- Authorization tests
- Controller tests
- Middleware tests
- JWT tests
- HTTP pipeline tests
- Integration tests
- Multi-user collaboration tests
- Health-check tests
- File-storage tests
- Correlation ID tests
- Logging tests

---

# 🔬 Test Strategy

## Service Tests

Covered services include:

```text
AuthService
UserService
BoardService
CardService
CardTaskService
InvitationService
MembershipAuthorizationService
JwtTokenService
```

Tests validate:

- Input validation
- Missing resources
- Invalid operations
- Authorization failures
- Duplicate resources
- Role restrictions
- Repository interactions
- JWT claims
- JWT expiration
- JWT signatures
- Persistence behavior
- File-storage behavior

---

## Controller Tests

Covered controllers include:

```text
AuthController
UsersController
BoardsController
CardsController
CardTasksController
InvitationsController
```

Controller tests validate endpoint behavior independently from the complete HTTP server.

Business-event logging is also covered using an in-memory test logger.

Logging tests currently verify events including:

```text
User registration
Board creation
Invitation acceptance
Invitation rejection
Profile update
Profile image update
```

Tests also verify that sensitive information such as passwords, email addresses in registration logs, invitation tokens and uploaded filenames are not accidentally exposed by those business events.

---

## Middleware Tests

Covered middleware includes:

```text
GlobalExceptionMiddleware
CorrelationIdMiddleware
```

Tests verify:

- Exception-to-status-code mapping
- JSON error responses
- Trace identifiers
- Warning logs for handled failures
- Error logs for unexpected failures
- Original exception preservation
- Internal-error protection
- Correlation ID generation
- Client-provided correlation IDs
- Rejection of invalid correlation IDs

---

# 🌐 Integration Tests

SprintBoard integration tests use:

- `WebApplicationFactory`
- ASP.NET Core real HTTP pipeline
- Real controllers
- Real application services
- Real repositories
- Entity Framework Core
- Real JWT generation and validation
- SQLite in-memory relational database

External integrations such as real email delivery are replaced by test doubles.

---

## API Pipeline Tests

The integration suite validates:

```text
401 Unauthorized
400 Bad Request
Malformed JSON
Unknown routes
Global exception handling
Correlation ID responses
Liveness health checks
Readiness health checks
Aggregate health checks
```

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

## Collaboration Flow

Multi-user integration tests validate scenarios such as:

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

Additional scenarios include:

- Admin invitations
- Member access
- Role promotion
- Member-to-Admin promotion
- Member removal
- Admin removing Members
- Admin restrictions
- Member leaving a board
- Owner being prevented from leaving their own board
- Unauthorized users being denied access

---

# 📊 Code Coverage

Code coverage is collected using:

- Coverlet.MTP
- Microsoft.Testing.Platform
- Cobertura

Generated Entity Framework migrations and third-party library code are excluded from the application coverage metrics shown below.

## Current Coverage

```text
Line Coverage:   88.57%
Branch Coverage: 83.24%

Lines Covered:      1783 / 2013
Branches Covered:    298 / 358
```

## Coverage by Project

| Project | Line Coverage | Branch Coverage |
|---|---:|---:|
| SprintBoard.Application | 100.00% | 100.00% |
| SprintBoard.api | 95.85% | 71.67% |
| SprintBoard.Domain | 77.93% | 53.95% |
| SprintBoard.Infrastructure | 63.17% | 0.00% |

The Application layer, where most SprintBoard business rules and use-case orchestration live, currently maintains:

```text
100% Line Coverage
100% Branch Coverage
```

The API layer currently has more than:

```text
95% Line Coverage
```

Infrastructure coverage remains lower because external infrastructure and integration boundaries are intentionally isolated from much of the unit-test suite.

---

# 📈 Generating Code Coverage

Run:

```powershell
dotnet test `
  --coverlet `
  --coverlet-output-format cobertura `
  --coverlet-include "[SprintBoard.*]*" `
  --coverlet-exclude "[Moq]*" `
  --coverlet-exclude-by-file "**/Migrations/**"
```

Coverage reports are generated under:

```text
TestResults/
```

Test result and coverage artifacts are ignored by Git.

---

# ▶️ Running Tests

From the repository root:

```bash
dotnet test
```

Expected result:

```text
Total:   361
Passed:  361
Failed:  0
Skipped: 0
```

---

# 📁 Project Structure

```text
SprintBoard/
│
├── SprintBoard.Domain/
│
├── SprintBoard.Application/
│
├── SprintBoard.Infrastructure/
│
├── SprintBoard.api/
│   ├── Auth/
│   ├── Controllers/
│   ├── Errors/
│   ├── Middlewares/
│   ├── Services/
│   └── Dockerfile
│
├── SprintBoard.Test/
│   ├── Authorization/
│   ├── Controllers/
│   ├── Integration/
│   ├── Logging/
│   ├── Middlewares/
│   └── Services/
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
├── .env.example
├── .gitignore
├── global.json
└── README.md
```

---

# 🎨 Branding

SprintBoard includes its own favicon and browser branding.

```text
sprintboard-web/public/favicon.png
```

---

# 📌 Project Status

SprintBoard is under active development.

## ✅ Completed

- Clean Architecture backend
- ASP.NET Core Web API
- React + TypeScript frontend
- JWT authentication
- User management
- Profile image support
- Persistent profile-image uploads
- Board management
- Cards
- Card checklists
- Board membership
- Owner / Admin / Member roles
- Email invitation workflow
- Invitation acceptance and rejection
- Member role management
- Global exception handling
- Environment-specific configuration
- Development database migrations
- Production database migration strategy
- API health checks
- Database readiness checks
- Structured Serilog logging
- HTTP request logging
- Correlation IDs
- Business-event logging
- Sensitive-log protection tests
- Automated service tests
- Authorization tests
- JWT tests
- Controller tests
- Middleware tests
- HTTP pipeline integration tests
- Authentication integration tests
- Board integration tests
- Multi-user collaboration tests
- SQLite integration-test environment
- 361 automated tests
- 88.57% line coverage
- 83.24% branch coverage
- 100% Application line coverage
- 100% Application branch coverage
- Microsoft.Testing.Platform
- Coverlet code coverage
- Dockerized ASP.NET Core API
- Dockerized React frontend
- Nginx frontend hosting
- Nginx API reverse proxy
- Nginx uploaded-file proxy
- Dockerized SQL Server 2022
- Persistent SQL Server volume
- Persistent uploaded-file volume
- Docker Compose orchestration
- Full-stack Docker environment
- SprintBoard favicon / browser branding

---

# 🚧 Next Improvements

Planned improvements include:

- Production deployment
- CI/CD pipeline
- HTTPS and reverse-proxy hardening
- Production secrets provider
- Production SMTP provider
- Production-grade password hashing
- Rate limiting
- Forwarded-header configuration
- External object storage
- Centralized log collection
- Metrics
- Distributed tracing
- Expanded API documentation
- Additional frontend improvements

---

# 🎯 Current Development Stage

The core application, automated testing, Docker containerization and initial production-observability phases are complete.

Current quality metrics:

```text
Automated Tests:  361
Passing:          361
Failed:           0
Skipped:          0

Line Coverage:    88.57%
Branch Coverage:  83.24%

Application:
100% Line Coverage
100% Branch Coverage
```

The complete application can be started with:

```bash
docker compose up -d --build
```

SprintBoard currently includes:

```text
Containerized full-stack environment
Persistent database storage
Persistent uploaded-file storage
Environment-based configuration
Health monitoring
Structured logging
Request correlation
Extensive automated testing
```

The next major focus is further **production hardening, deployment and CI/CD**.

---

# 👨‍💻 Author

**Diego Sousa Mello**

Backend Developer focused on **C# and .NET**.
