# SprintBoard

SprintBoard is a full-stack collaborative task management application inspired by Kanban-style workflows.

The project was built to explore modern backend development practices using **C# and .NET**, with a strong focus on **Clean Architecture, maintainability, authentication, authorization, automated testing, integration testing, and collaborative features**.

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
* JWT Token Validation
* Code Coverage

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
* Member removal and voluntary board leave
* Cards with status management
* Card checklists
* Authorization based on board membership and roles
* Global API exception handling
* Automated testing for business rules, controllers, middleware and authorization flows
* End-to-end HTTP integration tests for critical backend workflows

---

## 🏗️ Architecture

The backend follows **Clean Architecture** principles and is divided into:

```text
SprintBoard.Domain
SprintBoard.Application
SprintBoard.Infrastructure
SprintBoard.api
```

### Domain

Contains the core entities and domain rules.

### Application

Contains:

* Application services
* Business rules
* DTOs
* Interfaces
* Authorization logic
* Use-case orchestration

### Infrastructure

Contains:

* Entity Framework Core
* SQL Server persistence
* Repository implementations
* Email infrastructure
* Database configuration

### API

Contains:

* Controllers
* JWT authentication
* HTTP services
* Global exception middleware
* Dependency injection configuration
* Application startup configuration

### Frontend

The React + TypeScript frontend is located in:

```text
sprintboard-web
```

### Tests

Automated tests are located in:

```text
SprintBoard.Test
```

---

## 🧪 Automated Tests

SprintBoard has an extensive automated test suite built with **xUnit v3**, **Moq**, **Microsoft.Testing.Platform**, and ASP.NET Core integration testing tools.

The project currently has:

**✅ 340 automated tests passing**

```text
Total:   340
Passed:  340
Failed:  0
Skipped: 0
```

The test suite covers multiple testing layers instead of relying only on isolated unit tests.

---

## 🔬 Test Strategy

### Unit Tests

Unit tests validate individual business components and rules in isolation.

Covered services include:

* `AuthService`
* `UserService`
* `BoardService`
* `CardService`
* `CardTaskService`
* `InvitationService`
* `MembershipAuthorizationService`
* `JwtTokenService`

The tests validate both successful and failure scenarios, including:

* Input validation
* Missing resources
* Invalid operations
* Authorization failures
* Duplicate resources
* Role restrictions
* Repository interactions
* JWT claims
* JWT expiration
* JWT signature validation

---

### Controller Tests

The API controllers are tested independently from the HTTP server.

Covered controllers include:

* `AuthController`
* `UsersController`
* `BoardsController`
* `CardsController`
* `CardTasksController`
* `InvitationsController`

Controller tests validate:

* HTTP responses
* Request handling
* Service calls
* Authentication requirements
* Authorization behavior
* Invalid requests
* Resource creation
* Updates
* Deletion flows

---

### Middleware Tests

`GlobalExceptionMiddleware` is covered by automated tests.

The tests validate mappings such as:

```text
ArgumentException          → 400 Bad Request
UnauthorizedAccessException → 401 Unauthorized
ForbiddenAccessException   → 403 Forbidden
KeyNotFoundException       → 404 Not Found
InvalidOperationException  → 409 Conflict
Unexpected Exception       → 500 Internal Server Error
```

They also validate:

* JSON response content type
* Trace IDs
* Successful pipeline continuation
* Generic protection against exposing internal server errors

---

## 🌐 Integration Tests

SprintBoard includes HTTP integration tests using:

* `WebApplicationFactory`
* Real ASP.NET Core HTTP pipeline
* Real controllers
* Real application services
* Real repositories
* Real Entity Framework Core
* Real JWT generation and validation
* SQLite in-memory database

External services such as email delivery are replaced during integration tests so automated tests never send real emails.

### API Pipeline Tests

The integration suite validates:

* Protected endpoints returning `401 Unauthorized`
* Invalid request models returning `400 Bad Request`
* Malformed JSON handling
* Unknown routes returning `404 Not Found`
* Global exception middleware behavior through the real HTTP pipeline

### Authentication Flow

Integration tests validate real flows such as:

```text
Register
   ↓
JWT generation
   ↓
Authenticated request
   ↓
Controller
   ↓
Application service
   ↓
Repository
   ↓
SQLite database
```

### Board Flow

The authenticated board integration flow validates:

* User registration
* User login
* JWT authentication
* Board creation
* Board persistence
* Retrieving user boards
* Retrieving boards by ID

### Collaboration Flow

Multi-user integration tests validate real collaboration scenarios.

Examples include:

```text
Owner registers
      ↓
Owner creates Board
      ↓
Second user registers
      ↓
Second user cannot access Board
      ↓
Owner creates invitation
      ↓
User accepts invitation
      ↓
Membership is created
      ↓
User gains Board access
```

Additional tests validate:

* Owner invitations
* Admin invitations
* Member access
* Member removal
* Voluntary board leave
* Owner restrictions
* Role promotion
* Member → Admin promotion
* Admin removing Members
* Admin being prevented from removing another Admin
* Unauthorized users being denied access

---

## 🔐 Authorization

SprintBoard implements role and membership-based authorization for collaborative boards.

Available roles:

### Owner

Has full control over the board.

The Owner can:

* Manage the board
* Manage members
* Change member roles
* Remove members
* Invite users
* Delete the board

The Owner cannot leave their own board and must delete it instead.

### Admin

Admins can perform administrative operations within the board.

They can:

* Invite users
* Manage allowed board resources
* Remove regular Members

Admins cannot remove another Admin when the current authorization rules do not allow it.

### Member

Members can participate in boards and manage resources allowed by their role.

They cannot perform Owner/Admin-only operations.

Authorization rules are centralized through the `MembershipAuthorizationService` and are covered by unit, controller and integration tests.

---

## 📊 Code Coverage

Code coverage is collected using **Coverlet.MTP** with **Microsoft.Testing.Platform**.

Generated migrations and third-party libraries are excluded from the coverage calculation so the report reflects the actual SprintBoard source code.

### Current Coverage

```text
Line Coverage:   86.46%
Branch Coverage: 83.53%

Lines Covered:     1463 / 1692
Branches Covered:   279 / 334
```

### Coverage by Project

| Project | Line Coverage | Branch Coverage |
|---|---:|---:|
| SprintBoard.Application | 100% | 100% |
| SprintBoard.api | 93.11% | 66.66% |
| SprintBoard.Domain | 77.93% | 53.94% |
| SprintBoard.Infrastructure | 63.17% | 0% |

The application layer, where most business rules are implemented, currently has **100% line and branch coverage**.

Infrastructure coverage is lower because external integrations such as SMTP email delivery and physical file storage are intentionally isolated from the automated integration-test environment.

---

## ▶️ Running the Tests

Run the complete test suite from the repository root:

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

## 📈 Generating Code Coverage

SprintBoard uses **Microsoft.Testing.Platform** as its test runner.

The runner is configured through:

```text
global.json
```

To generate the coverage report:

```powershell
dotnet test `
  --coverlet `
  --coverlet-output-format cobertura `
  --coverlet-include "[SprintBoard.*]*" `
  --coverlet-exclude-by-file "**/Migrations/**"
```

The generated coverage files are stored under:

```text
TestResults/
```

Coverage reports and test-result artifacts are ignored by Git.

---

## 🧪 Tested Components

The current automated testing structure covers:

```text
SprintBoard.Test
├── Authorization
├── Auth
├── Controllers
├── Integration
├── Middlewares
└── Services
```

Main tested components include:

```text
AuthService
UserService
BoardService
CardService
CardTaskService
InvitationService
MembershipAuthorizationService
JwtTokenService

AuthController
UsersController
BoardsController
CardsController
CardTasksController
InvitationsController

GlobalExceptionMiddleware

API Pipeline
Authentication Flow
Board Flow
Board Collaboration Flow
```

---

## 🛡️ Security

SprintBoard applies several backend security practices:

* JWT authentication
* Protected API endpoints
* Role-based authorization
* Board membership validation
* Owner/Admin/Member permission checks
* Centralized authorization service
* Global exception handling
* `401 Unauthorized` and `403 Forbidden` handling
* Server-side resource ownership validation

Authorization rules are validated at multiple levels through unit, controller and integration tests.

---

## 📌 Project Status

SprintBoard is currently under active development.

### ✅ Completed

* Core backend API
* Clean Architecture backend structure
* JWT authentication
* User management
* Profile image support
* Boards
* Cards
* Card checklists
* Board members
* Owner / Admin / Member roles
* Email invitation flow
* Invitation acceptance and rejection
* Member role management
* React + TypeScript frontend integration
* Global exception handling
* Service unit tests
* Authorization tests
* JWT tests
* Controller tests
* Middleware tests
* HTTP pipeline integration tests
* Authentication integration tests
* Board integration tests
* Multi-user collaboration integration tests
* SQLite in-memory integration-test infrastructure
* Code coverage configuration
* 340 automated tests
* 86.46% line coverage
* 83.53% branch coverage
* Git/GitHub project organization

### 🚧 Next Improvements

* Docker support
* Production environment configuration
* Production email delivery
* Deployment
* Expanded API documentation
* CI/CD pipeline
* Additional frontend improvements

---

## 🎯 Current Development Stage

The automated testing phase is complete.

Current backend quality metrics:

```text
Automated Tests:  340
Passing:          340
Line Coverage:    86.46%
Branch Coverage:  83.53%
Application:      100% line / 100% branch coverage
```

The next major development phase is **Docker containerization**, followed by production preparation and deployment.

---

## 👨‍💻 Author

**Diego Sousa Mello**

Backend Developer focused on **C# and .NET**.
