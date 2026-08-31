# SprintBoard

SprintBoard is a full-stack collaborative task management application inspired by Kanban-style workflows.

The project was built to explore modern backend development practices using **C# and .NET**, with a strong focus on **Clean Architecture, maintainability, authentication, authorization, automated testing, and collaborative features**.

## 🚀 Technologies

### Backend

* C#
* .NET
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

* xUnit
* Moq
* Unit Testing
* Authorization Testing
* JWT Token Validation

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
* Automated tests for business rules and authorization flows

## 🏗️ Architecture

The backend follows **Clean Architecture** principles and is divided into:

* `SprintBoard.Domain`
* `SprintBoard.Application`
* `SprintBoard.Infrastructure`
* `SprintBoard.api`

Automated tests are located in:

* `SprintBoard.Test`

The frontend is located in:

* `sprintboard-web`

## 🧪 Automated Tests

SprintBoard includes an automated test suite built with **xUnit** and **Moq**.

The current test suite covers:

* Authentication service
* User service
* Board service
* Card service
* CardTask / checklist service
* Invitation service
* Membership and authorization rules
* Owner, Admin and Member permission scenarios
* JWT token generation
* JWT claims
* JWT expiration
* JWT signature validation
* Success and failure scenarios
* Input validation
* Repository interaction and persistence behavior

### Current test status

**✅ 176 automated tests passing**

Run the complete test suite with:

```bash
dotnet test
```

Controller and API-level tests are the next stage of the automated testing strategy.

## 🧪 Tested Components

Current automated test coverage includes:

```text
SprintBoard.Test
├── Authorization
├── Services
└── Auth
```

Main tested components:

* `AuthService`
* `UserService`
* `BoardService`
* `CardService`
* `CardTaskService`
* `InvitationService`
* `MembershipAuthorizationService`
* `JwtTokenService`

The tests validate both successful operations and failure scenarios, including authorization restrictions, invalid input, missing resources and business rule violations.

## 🔐 Authorization

SprintBoard implements role and membership-based authorization for collaborative boards.

Available roles:

* **Owner** – Full control over the board, members and roles
* **Admin** – Can manage members and perform administrative operations
* **Member** – Can participate in boards and manage allowed resources

Authorization rules are centralized through the `MembershipAuthorizationService` and are covered by automated tests.

## 📌 Project Status

SprintBoard is currently under active development.

### Completed

* Core backend API
* JWT authentication
* User management
* Boards
* Cards
* Card checklists
* Board members
* Owner / Admin / Member roles
* Email invitation flow
* Profile image support
* React + TypeScript frontend integration
* Global error handling
* Automated unit tests
* Authorization tests
* JWT tests
* Git/GitHub project organization

### Next improvements

* Controller and API-level automated tests
* Docker support
* Production email delivery
* Deployment
* Expanded API and project documentation

## 👨‍💻 Author

**Diego Sousa Mello**

Backend Developer focused on **C# and .NET**.
