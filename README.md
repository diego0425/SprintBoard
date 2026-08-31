# SprintBoard

SprintBoard is a full-stack collaborative task management application inspired by Kanban-style workflows.

The project was built to demonstrate modern software development practices using **C# and .NET**, with a strong focus on **Clean Architecture, maintainability, authentication, authorization, automated testing, and collaborative features**.

> 🧪 **291 automated tests** covering business rules, authentication, authorization, JWT, services, and API controllers.

---

## 🚀 Technologies

### Backend

- C#
- .NET
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Authentication
- Clean Architecture

### Frontend

- React
- TypeScript
- Vite
- Axios

### Testing

- xUnit
- Moq
- Unit Testing
- Controller Testing
- Authorization Testing
- JWT Token Validation
- Dependency Mocking
- Arrange / Act / Assert pattern

---

## ✨ Features

- User registration and authentication
- JWT-based authentication
- User profile management
- Profile image upload
- Board creation and management
- Collaborative board members
- Owner, Admin and Member roles
- Board invitations by email
- Invitation acceptance and rejection
- Member role management
- Member removal
- Voluntary board leave
- Cards with workflow status management
- Card checklists
- Checklist completion tracking
- Authorization based on board membership and roles
- Centralized authorization rules
- Global API exception handling
- Automated tests for business rules
- Automated tests for authorization flows
- Automated controller tests

---

## 🏗️ Architecture

The backend follows **Clean Architecture** principles and is divided into separate projects with clearly defined responsibilities.

```text
SprintBoard
├── SprintBoard.Domain
├── SprintBoard.Application
├── SprintBoard.Infrastructure
├── SprintBoard.api
├── SprintBoard.Test
└── sprintboard-web
```

### `SprintBoard.Domain`

Contains the core domain entities, enums, and business concepts.

Main entities include:

- `User`
- `Board`
- `BoardMember`
- `BoardInvitation`
- `Card`
- `CardTask`

### `SprintBoard.Application`

Contains application services, DTOs, repository contracts, authorization rules, and application-level business logic.

### `SprintBoard.Infrastructure`

Contains infrastructure implementations such as:

- Entity Framework Core
- SQL Server persistence
- Repository implementations
- Database migrations
- External service implementations

### `SprintBoard.api`

Contains the ASP.NET Core Web API, including:

- Controllers
- Authentication configuration
- JWT token generation
- Current user resolution
- Middleware
- Dependency injection configuration

### `SprintBoard.Test`

Contains the automated test suite for services, authorization rules, authentication, JWT, and controllers.

### `sprintboard-web`

Contains the frontend application developed with **React and TypeScript**.

---

## 🔐 Authentication

SprintBoard uses **JWT Bearer Authentication**.

Users can register and log in through the authentication API.

After successful authentication, the API generates a JWT containing information about the authenticated user.

The token includes claims such as:

- User ID
- Email
- Username

Protected API endpoints require a valid JWT.

JWT behavior is also covered by automated tests, including:

- Token generation
- User claims
- Issuer
- Audience
- Expiration
- Cryptographic signature validation

---

## 🛡️ Authorization

SprintBoard implements role-based and membership-based authorization for collaborative boards.

Authorization rules are centralized through the `MembershipAuthorizationService`.

### Available roles

#### Owner

The board owner has full control over the board.

The Owner can:

- Update the board
- Delete the board
- Invite members
- Remove members
- Change member roles
- Manage cards
- Manage card checklists
- View board members

The Owner cannot leave their own board. The board must be deleted instead.

#### Admin

Administrators can perform management operations according to the board authorization rules.

They can:

- Manage allowed board resources
- Invite users
- Remove regular Members
- Manage cards
- Manage card checklists

Administrators cannot remove the Owner or another Admin when restricted by the business rules.

#### Member

Members can participate in boards they belong to and manage the resources allowed by the application's authorization rules.

Authorization scenarios are extensively covered by automated tests.

---

## 📋 Boards

Users can create and manage collaborative boards.

Supported operations include:

- Create boards
- List boards available to the authenticated user
- Retrieve a board
- Update a board
- Delete a board
- Invite users
- List board members
- Change member roles
- Remove members
- Leave boards

When a board is created, the creator automatically becomes its **Owner**.

---

## ✉️ Board Invitations

SprintBoard supports email-based board invitations.

Authorized users can invite another user by email.

The invitation system includes:

- Secure invitation tokens
- Accept invitation flow
- Decline invitation flow
- Expiration handling
- Duplicate pending invitation prevention
- Existing membership validation
- Email validation
- Accept and decline links

Accepted users are added to the board with the **Member** role.

---

## 🗂️ Cards

Boards contain cards that represent tasks.

Cards support:

- Creation
- Listing by board
- Title
- Description
- Position
- Status changes
- Editing
- Deletion

### Card workflow

Cards can move between:

```text
ToDo
Doing
Done
```

Card operations are protected by board membership authorization.

---

## ✅ Card Checklists

Cards can contain checklist items through `CardTask`.

Checklist functionality includes:

- Create checklist items
- List checklist items
- Update checklist item titles
- Mark items as completed
- Mark completed items as pending
- Delete checklist items
- Control checklist position

Checklist operations are also protected by board membership authorization.

---

## 👤 User Profile

Authenticated users can manage their own profile.

Supported operations include:

- Retrieve profile information
- Update full name
- Update username
- Change password
- Upload profile image

Profile image uploads currently support:

- JPEG
- PNG
- WebP

Username uniqueness and password validation are handled by the application layer.

---

## 🧪 Automated Tests

SprintBoard includes an extensive automated test suite built with **xUnit** and **Moq**.

The test suite verifies both successful operations and failure scenarios across the application's main layers.

### Current test status

**✅ 291 automated tests**

Run the complete test suite with:

```bash
dotnet test
```

---

## 🔬 Test Coverage

The automated test suite currently covers:

- Authentication
- User registration
- User login
- Password validation
- User profile management
- Profile image upload
- JWT generation
- JWT claims
- JWT expiration
- JWT signature validation
- Board creation
- Board retrieval
- Board updates
- Board deletion
- Board membership
- Board roles
- Board invitations
- Invitation acceptance
- Invitation rejection
- Invitation expiration
- Member removal
- Board leaving
- Card creation
- Card listing
- Card updates
- Card deletion
- Card workflow status changes
- Card checklist creation
- Card checklist updates
- Checklist completion
- Checklist deletion
- Membership authorization
- Owner permissions
- Admin permissions
- Member permissions
- Invalid input scenarios
- Missing resource scenarios
- Forbidden operations
- Repository interactions
- Persistence behavior
- Controller responses
- HTTP status results
- Authentication context propagation

---

## 🧪 Tested Services

The following application services have automated test coverage:

- `AuthService`
- `UserService`
- `BoardService`
- `CardService`
- `CardTaskService`
- `InvitationService`
- `MembershipAuthorizationService`
- `JwtTokenService`

---

## 🎮 Tested Controllers

The API controller layer also has automated tests.

Currently tested controllers include:

- `AuthController`
- `UsersController`
- `BoardsController`
- `CardsController`
- `CardTasksController`

Controller tests validate behavior such as:

- `200 OK`
- `201 Created`
- `204 No Content`
- Authentication context
- Service integration
- Resource creation
- Resource updates
- Resource deletion
- Authorization failures
- Invalid requests
- Missing resources

---

## 🧩 Testing Strategy

Tests follow the **Arrange / Act / Assert** pattern.

Dependencies such as repositories, authorization services, storage services, and external services are isolated using **Moq**.

The current automated testing strategy covers primarily:

```text
Controller
    ↓
Application Service
    ↓
Mocked Dependencies
```

This allows controller behavior and application business rules to be tested without requiring a real database or external infrastructure.

---

## ⚠️ Error Handling

SprintBoard includes centralized API error handling.

Application exceptions and validation failures are handled through the API pipeline, providing consistent HTTP responses to clients.

The current unit and controller tests validate exception propagation at their respective layers.

Full HTTP pipeline behavior will also be covered through integration tests.

---

## 📌 Project Status

SprintBoard is currently under active development.

### ✅ Completed

- Core backend API
- Clean Architecture structure
- JWT authentication
- User registration
- User login
- User profile management
- Profile image support
- Board management
- Card management
- Card checklists
- Collaborative board members
- Owner / Admin / Member roles
- Membership-based authorization
- Role-based authorization
- Email invitation flow
- Invitation acceptance and rejection
- React + TypeScript frontend integration
- Global error handling
- Automated service tests
- Automated authorization tests
- Automated authentication tests
- Automated JWT tests
- Automated controller tests
- Git/GitHub project organization

---

## 🚀 Next Improvements

The core application and its automated unit/controller test suite are already implemented.

Planned improvements include:

- Integration tests for the complete HTTP request pipeline
- Global exception middleware tests
- Automated test coverage reporting
- Docker support
- CI/CD pipeline
- Production-ready email delivery
- Cloud deployment
- Improved API documentation
- Expanded project documentation

---

## 🎯 Project Goals

SprintBoard was developed as a portfolio project focused on demonstrating practical backend and full-stack development skills.

The project emphasizes:

- Clean and maintainable architecture
- Separation of concerns
- REST API development
- Authentication and authorization
- Business-rule implementation
- Collaborative application design
- Automated testing
- Dependency isolation
- Modern C# and .NET development
- React and TypeScript integration
- Git-based development workflow

---

## 👨‍💻 Author

**Diego Sousa Mello**

Backend Developer focused on **C# and .NET**.
