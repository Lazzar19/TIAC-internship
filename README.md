# WebAPI — Product & User Management REST API

![CI](https://github.com/Lazzar19/TIAC-internship/actions/workflows/ci.yml/badge.svg)

A REST API service built during an internship program, implemented in **ASP.NET Core (.NET 10)** with a focus on clean layered architecture, secure authentication, environment-specific infrastructure, Redis caching, and full containerization.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Features](#features)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
    - [Running Locally (without Docker)](#running-locally-without-docker)
    - [Running with Docker](#running-with-docker)
    - [Production Stack](#production-stack)
- [API Documentation](#api-documentation)
- [Authentication](#authentication)
- [Testing](#testing)
- [CI/CD](#cicd)
- [Future Improvements](#future-improvements)

## Overview

The API manages products (`Product`) and users (`User`), linked through a `UserProduct` join entity that tracks how many units of a given product a given user owns, with stock validation and atomic inventory updates on every assignment. It supports full CRUD operations, pagination and filtering, JWT authentication with refresh token rotation and role-based authorization, rate limiting, and input validation.

## Architecture

The project follows a **layered (Clean/Onion) architecture**, split into four separate projects within a single solution:

```
Domain  ←  Application  ←  Infrastructure  ←  Api
(core)                                       (outermost layer)
```

Dependencies flow in a single direction only — inner layers never depend on outer ones.

| Layer | Contains | Depends on |
|---|---|---|
| **Domain** | Entities (`Product`, `User`, `UserProduct`, `RefreshToken`) — pure business model | Nothing (no references) |
| **Application** | DTOs, interfaces (`IProductRepository`, etc.), FluentValidation validators, mappings | Domain |
| **Infrastructure** | `ApplicationDbContext`, concrete repository implementations, `TokenService`, password hashing, Redis cache integration | Application + Domain |
| **Api** | Controllers, `Program.cs`, middleware, Swagger/JWT configuration, health checks, rate limiting | Everything else |

**Why this structure:** swapping the underlying technology (e.g. SQLite → PostgreSQL) only requires changes within the Infrastructure layer. Business logic is testable independently of the database, since controllers depend on interfaces (`IProductRepository`) rather than concrete implementations — this allows mocking in unit tests without a real database.

## Tech Stack

- **ASP.NET Core 10** — Web API framework
- **Entity Framework Core 10 + SQLite / PostgreSQL** — ORM and databases (SQLite for development, PostgreSQL for production)
- **FluentValidation** — declarative input validation
- **JWT (JSON Web Tokens)** — authentication with access + refresh token rotation
- **PBKDF2 (Rfc2898DeriveBytes)** — salted password hashing
- **Redis** — distributed caching for hot reads, with version-based invalidation
- **Serilog** — structured logging with per-request logging
- **Swashbuckle / Swagger** — OpenAPI documentation
- **xUnit + Moq + FluentAssertions** — unit and integration testing
- **Docker + Docker Compose** — containerization (separate dev and production stacks)
- **GitHub Actions** — CI/CD pipeline with automated Docker image publishing

## Features

- ✅ CRUD operations for products and users
- ✅ Repository pattern with interfaces (testability, swappable implementations)
- ✅ DTOs separated by purpose (`Create`, `Update`, read-only `Dto`) — prevents leaking internal/sensitive fields (e.g. `PasswordHash`) and prevents clients from setting server-generated fields (`Id`, `CreatedAt`)
- ✅ FluentValidation on all input models
- ✅ JWT authentication (register/login) with hashed passwords (PBKDF2 + salt + `FixedTimeEquals` comparison resistant to timing attacks)
- ✅ Refresh token rotation with revocation support (login, refresh, logout)
- ✅ Rate limiting on authentication endpoints (5 requests/min per IP) to mitigate brute-force attacks
- ✅ Stock validation and atomic inventory decrement on product assignment, with automatic stock restoration on unassignment
- ✅ Pagination and filtering (`GET /api/product?pageNumber=1&pageSize=10&search=...&minPrice=...&maxPrice=...`)
- ✅ Redis-backed caching for product reads with version-based cache invalidation on writes
- ✅ Structured logging via Serilog (console sink, per-request request/response logging)
- ✅ Health check endpoint (`/health`) for container orchestration and uptime monitoring
- ✅ Swagger UI with JWT Bearer authorization support
- ✅ Dockerized development stack with persistent SQLite data and Redis cache
- ✅ Production Docker Compose stack with PostgreSQL + Redis, including container healthchecks
- ✅ CI/CD pipeline (build + test on every push, Docker image publish to GHCR on `main`)
- ✅ 70+ unit and integration tests (validators, hashing, repository logic, full HTTP request/response flows)

## Project Structure

```
WebAPI/
├── WebAPI.sln
├── Dockerfile
├── docker-compose.yml
├── docker-compose.prod.yml
├── .dockerignore
├── .github/workflows/ci.yml
├── WebAPI/                        # Api layer
│   ├── Controllers/
│   ├── Middleware/
│   ├── Program.cs
│   ├── appsettings.json
│   └── appsettings.Production.json
├── WebAPI.Domain/                 # Domain layer
│   └── (Product, User, UserProduct, RefreshToken)
├── WebAPI.Application/            # Application layer
│   ├── Dtos/
│   ├── Interfaces/
│   ├── Validators/
│   └── Mappings/
├── WebAPI.Infrastructure/         # Infrastructure layer
│   ├── Migrations/
│   ├── ApplicationDbContext.cs
│   ├── DatabaseExtensions.cs      # provider + cache DI registration
│   └── (Repository implementations, TokenService, PasswordHasher)
└── WebAPI.Tests/                  # Unit + integration tests
```

## Getting Started

### Running Locally (without Docker)

**Prerequisites:** .NET 10 SDK

```bash
git clone https://github.com/Lazzar19/TIAC-internship.git
cd TIAC-internship

dotnet restore

dotnet user-secrets set "Jwt:Key" "<random-string-at-least-32-characters>" --project WebAPI

dotnet run --project WebAPI
```

The API is available at `http://localhost:5080/swagger` (the port may vary — check the console output). In `Development`, the app syncs the SQLite schema automatically on startup.

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

The development stack uses SQLite (`webapi-data` volume mounted at `/app/data`) and Redis for caching.

### Production Stack

For a production-like local deployment, use the PostgreSQL + Redis compose file:

```bash
docker compose -f docker-compose.prod.yml up --build -d
```

Required environment variables:

```bash
POSTGRES_PASSWORD=<strong-postgres-password>
JWT_KEY=<random-string-at-least-32-characters>
```

The production stack applies EF Core migrations automatically on startup and uses container healthchecks to ensure the API only starts once PostgreSQL and Redis are ready.

## API Documentation

Full interactive documentation is available via Swagger UI (`/swagger`) once the app is running (Development only). Key endpoints:

| Method | Route | Description | Auth Required |
|---|---|---|---|
| `POST` | `/api/auth/register` | Register a new user | No |
| `POST` | `/api/auth/login` | Log in, returns access + refresh tokens | No |
| `POST` | `/api/auth/refresh` | Exchange a valid refresh token for a new token pair | No |
| `POST` | `/api/auth/logout` | Revoke all refresh tokens for the current user | Yes |
| `GET` | `/api/product` | List products (pagination + filtering) | Yes |
| `GET` | `/api/product/{id}` | Get product details | Yes |
| `POST` | `/api/product` | Create a product | Yes |
| `PUT` | `/api/product/{id}` | Update a product | Yes |
| `DELETE` | `/api/product/{id}` | Delete a product | Yes (Admin) |
| `GET` | `/api/user` | List users | Yes |
| `PUT` | `/api/user/{id}/password` | Change password | Yes |
| `GET` | `/api/users/{userId}/products` | Products assigned to a user | Yes |
| `POST` | `/api/users/{userId}/products` | Assign a product to a user (validates stock) | Yes |
| `DELETE` | `/api/users/{userId}/products/{productId}` | Unassign a product, restoring stock | Yes |
| `GET` | `/health` | Health check for container orchestration | No |

## Authentication

The API uses **JWT Bearer** authentication with refresh token rotation. After a successful login:

1. Copy the returned `token`.
2. In Swagger UI, click **Authorize** and enter `Bearer <token>`.
3. All protected endpoints (`[Authorize]`) are now accessible for the duration of the token.
4. When the access token expires, exchange the `refreshToken` via `/api/auth/refresh` for a new pair — the old refresh token is revoked on use.

Passwords are stored exclusively as **PBKDF2 hashes** (100,000 iterations, HMAC-SHA256, 128-bit random salt per user) — never as plain text. Login and refresh endpoints are rate-limited to mitigate brute-force attacks.

## Testing

```bash
dotnet test
```

The test project (`WebAPI.Tests`) covers:
- FluentValidation rules, including boundary cases (e.g. exactly at `MaximumLength`, `GreaterThan(0)` limits)
- Password hashing and verification
- Repository logic (pagination, filtering, stock validation) using an EF Core in-memory database
- JWT token generation and claim content
- Full HTTP integration tests via `WebApplicationFactory` — auth flows, admin-only authorization, product/user CRUD, rate limiting behavior

## CI/CD

A GitHub Actions workflow (`.github/workflows/ci.yml`) automatically runs on every push and pull request to `main`:
1. Restores dependencies
2. Builds the solution (Release configuration)
3. Runs all unit and integration tests
4. On pushes to `main`, builds and publishes a Docker image to GitHub Container Registry (GHCR)

## Future Improvements

- Optimistic concurrency (e.g. a `RowVersion` column) to fully eliminate stock race conditions under high concurrent load
- CORS policy configuration for browser-based clients
- API versioning (`/api/v1/...`)
- Soft delete for products and users, with audit history
- Integration test coverage for the PostgreSQL and Redis production paths (currently exercised manually)
