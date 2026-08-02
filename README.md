# WebAPI — Product & User Management REST API

![CI](https://github.com/Lazzar19/TIAC-internship/actions/workflows/ci.yml/badge.svg)

A REST API service built during an internship program, implemented in **ASP.NET Core (.NET 10)** with a focus on clean layered architecture, secure authentication, and full containerization.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Features](#features)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
    - [Running Locally (without Docker)](#running-locally-without-docker)
    - [Running with Docker](#running-with-docker)
- [API Documentation](#api-documentation)
- [Authentication](#authentication)
- [Testing](#testing)
- [CI/CD](#cicd)
- [Future Improvements](#future-improvements)

## Overview

The API manages products (`Product`) and users (`User`), linked through a `UserProduct` join entity (tracking how many units of a given product a given user owns). It supports full CRUD operations, pagination and filtering, JWT authentication with role-based authorization, and input validation.

## Architecture

The project follows a **layered (Clean/Onion) architecture**, split into four separate projects within a single solution:

```
Domain  ←  Application  ←  Infrastructure  ←  Api
(core)                                       (outermost layer)
```

Dependencies flow in a single direction only — inner layers never depend on outer ones.

| Layer | Contains | Depends on |
|---|---|---|
| **Domain** | Entities (`Product`, `User`, `UserProduct`) — pure business model | Nothing (no references) |
| **Application** | DTOs, interfaces (`IProductRepository`, etc.), FluentValidation validators, mappings | Domain |
| **Infrastructure** | `ApplicationDbContext`, concrete repository implementations, `TokenService`, password hashing | Application + Domain |
| **Api** | Controllers, `Program.cs`, middleware, Swagger/JWT configuration | Everything else |

**Why this structure:** swapping the underlying technology (e.g. SQLite → PostgreSQL) only requires changes within the Infrastructure layer. Business logic is testable independently of the database, since controllers depend on interfaces (`IProductRepository`) rather than concrete implementations — this allows mocking in unit tests without a real database.

## Tech Stack

- **ASP.NET Core 10** — Web API framework
- **Entity Framework Core 10 + SQLite** — ORM and database
- **FluentValidation** — declarative input validation
- **JWT (JSON Web Tokens)** — authentication and authorization
- **PBKDF2 (Rfc2898DeriveBytes)** — salted password hashing
- **Swashbuckle / Swagger** — OpenAPI documentation
- **xUnit + Moq + FluentAssertions** — unit testing
- **Docker + Docker Compose** — containerization
- **GitHub Actions** — CI pipeline

## Features

- ✅ CRUD operations for products and users
- ✅ Repository pattern with interfaces (testability, swappable implementations)
- ✅ DTOs separated by purpose (`Create`, `Update`, read-only `Dto`) — prevents leaking internal/sensitive fields (e.g. `PasswordHash`) and prevents clients from setting server-generated fields (`Id`, `CreatedAt`)
- ✅ FluentValidation on all input models
- ✅ JWT authentication (register/login) with hashed passwords (PBKDF2 + salt + `FixedTimeEquals` comparison resistant to timing attacks)
- ✅ Pagination and filtering (`GET /api/product?pageNumber=1&pageSize=10&search=...&minPrice=...&maxPrice=...`)
- ✅ Swagger UI with JWT Bearer authorization support
- ✅ Dockerized deployment with persistent data (named volume)
- ✅ CI pipeline (build + test on every push)
- ✅ 24+ unit tests (validators, hashing, repository logic)

## Project Structure

```
WebAPI/
├── WebAPI.sln
├── Dockerfile
├── docker-compose.yml
├── .dockerignore
├── .github/workflows/ci.yml
├── WebAPI/                      # Api layer
│   ├── Controllers/
│   ├── Middleware/
│   ├── Program.cs
│   └── appsettings.json
├── WebAPI.Domain/                # Domain layer
│   └── (Product, User, UserProduct)
├── WebAPI.Application/            # Application layer
│   ├── Dtos/
│   ├── Interfaces/
│   ├── Validators/
│   └── Mappings/
├── WebAPI.Infrastructure/         # Infrastructure layer
│   ├── Migrations/
│   ├── ApplicationDbContext.cs
│   └── (Repository implementations, TokenService, PasswordHasher)
└── WebAPI.Tests/                  # Unit tests
```

## Getting Started

### Running Locally (without Docker)

**Prerequisites:** .NET 10 SDK

```bash
git clone https://github.com/Lazzar19/TIAC-internship.git
cd TIAC-internship

dotnet restore
dotnet ef database update --project WebAPI.Infrastructure --startup-project WebAPI

dotnet user-secrets set "Jwt:Key" "<random-string-at-least-32-characters>" --project WebAPI

dotnet run --project WebAPI
```

The API is available at `http://localhost:5080/swagger` (the port may vary — check the console output).

### Running with Docker

**Prerequisites:** Docker Desktop

1. Create a `.env` file in the root folder:
```
JWT_KEY=<random-string-at-least-32-characters>
```

2. Run:
```bash
docker compose up --build -d
```

3. The API is available at `http://localhost:8080/swagger`.

4. To stop (data is preserved in the named volume):
```bash
docker compose down
```

The database is stored in a Docker named volume (`webapi-data`), mounted at `/app/data` inside the container — data survives container restarts and removal. Migrations are applied automatically on every application startup.

## API Documentation

Full interactive documentation is available via Swagger UI (`/swagger`) once the app is running. Key endpoints:

| Method | Route | Description | Auth Required |
|---|---|---|---|
| `POST` | `/api/auth/register` | Register a new user | No |
| `POST` | `/api/auth/login` | Log in, returns a JWT token | No |
| `GET` | `/api/product` | List products (pagination + filtering) | Yes |
| `GET` | `/api/product/{id}` | Get product details | Yes |
| `POST` | `/api/product` | Create a product | Yes |
| `PUT` | `/api/product/{id}` | Update a product | Yes |
| `DELETE` | `/api/product/{id}` | Delete a product | Yes (Admin) |
| `GET` | `/api/user` | List users | Yes |
| `PUT` | `/api/user/{id}/password` | Change password | Yes |
| `GET` | `/api/users/{userId}/products` | Products assigned to a user | Yes |
| `POST` | `/api/users/{userId}/products` | Assign a product to a user | Yes |

## Authentication

The API uses **JWT Bearer** authentication. After a successful login:

1. Copy the returned `token`.
2. In Swagger UI, click **Authorize** and enter `Bearer <token>`.
3. All protected endpoints (`[Authorize]`) are now accessible for the duration of the token.

Passwords are stored exclusively as **PBKDF2 hashes** (100,000 iterations, HMAC-SHA256, 128-bit random salt per user) — never as plain text.

## Testing

```bash
dotnet test
```

The test project (`WebAPI.Tests`) covers:
- FluentValidation rules, including boundary cases (e.g. exactly at `MaximumLength`, `GreaterThan(0)` limits)
- Password hashing and verification
- Repository logic (pagination, filtering, `AddOrUpdate` behavior) using an EF Core in-memory database
- JWT token generation and claim content

## CI/CD

A GitHub Actions workflow (`.github/workflows/ci.yml`) automatically runs on every push and pull request to `main`:
1. Restores dependencies
2. Builds the project (Release configuration)
3. Runs all unit tests

## Future Improvements

- Refresh token mechanism (long-lived token to renew the access token without re-authenticating)
- Integration tests using `WebApplicationFactory`
- Health check endpoint (`/health`)
- Structured logging (Serilog)
- Rate limiting
- Replacing SQLite with PostgreSQL in the production Docker Compose setup