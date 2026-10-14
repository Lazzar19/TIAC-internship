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
| **Domain** | Entities (`Product`, `User`, `UserProduct`, `RefreshToken`) and `Role` | Nothing (no references) |
| **Application** | DTOs, query parameters, validators, repository interfaces, and services for assignment, session revocation, and product cache coordination | Domain |
| **Infrastructure** | `ApplicationDbContext`, repositories, `TokenService`, password hashing, and the Redis cache | Application + Domain |
| **Api** | Controllers, `Program.cs`, middleware, Swagger/JWT configuration, health checks, rate limiting | Everything else |

Controllers authorize the caller, call a service, and map the result to an HTTP status. Repositories run queries and `SaveChanges`. After a successful save, the service updates the product cache. Assignment and refresh-token revocation can be tested without HTTP. The conditional stock update stays in the repository because it is a database statement. Swapping SQLite for PostgreSQL stays inside Infrastructure.

## Tech Stack

- **ASP.NET Core 10** — Web API framework
- **Entity Framework Core 10 + SQLite / PostgreSQL** — ORM and databases (SQLite for development, PostgreSQL for production)
- **FluentValidation** — declarative input validation
- **JWT (JSON Web Tokens)** — authentication with access + refresh token rotation
- **PBKDF2 (Rfc2898DeriveBytes)** — salted password hashing, 600,000 iterations, iteration count stored in the hash
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
- ✅ `GET /api/user/me` returns the user in the access token. The route does not take a user id
- ✅ JWT authentication (register/login) with hashed passwords (PBKDF2 + salt + `FixedTimeEquals` comparison resistant to timing attacks)
- ✅ Refresh token rotation: the database stores a SHA-256 hash, reuse of a revoked token ends that user's sessions, and a session cannot be refreshed after 7 days
- ✅ Rate limiting on register, login, and refresh (5 requests/min per IP) to mitigate brute-force attacks
- ✅ Stock changes only through assignment and unassignment. A conditional database update rejects a sale when stock is no longer available, and `RowVersion` rejects a conflicting product edit with HTTP 409
- ✅ Pagination and filtering (`GET /api/product?pageNumber=1&pageSize=10&search=...&minPrice=...&maxPrice=...`). Page number and page size are at least 1, and page size is at most 50. `GET /api/user` uses the same page shape
- ✅ Redis-backed caching for product reads. The list key includes a version, so a write is visible on the next read, and a cache failure falls back to the database
- ✅ Structured logging via Serilog (console sink, per-request request/response logging)
- ✅ Health check endpoint (`/health`) for the database, and Redis when it is configured
- ✅ Swagger UI with JWT Bearer authorization support
- ✅ Dockerized development stack with persistent SQLite data and Redis cache
- ✅ Docker Compose healthchecks for the API, PostgreSQL, and Redis. Redis has no published port and requires a password
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
│   └── Entities/                  # Product, User, UserProduct, RefreshToken, Role
├── WebAPI.Application/            # Application layer
│   ├── DTO/
│   ├── Interfaces/
│   ├── Services/
│   ├── Validators/
│   └── Mapping/
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
dotnet user-secrets set "Admin:Email" "admin@example.com" --project WebAPI
dotnet user-secrets set "Admin:Password" "<password>" --project WebAPI

dotnet run --project WebAPI
```

`Admin:Password` is read from user-secrets or environment variables. A value placed in `appsettings.json` is ignored. In Production the password is read only from the `Admin__Password` environment variable. If either value is missing, startup skips the admin account and writes a warning. Registration cannot set `Role`; the seeded account is the admin.

The API is available at `http://localhost:5080/swagger` (the port may vary — check the console output). Startup applies the EF Core migrations to SQLite. If a `sqlite.db` file was created before that change, delete it once. The old file has no `__EFMigrationsHistory` table, so migration cannot update it. Generate a new migration with `ASPNETCORE_ENVIRONMENT=Production` so the snapshot stays on PostgreSQL.

### Running with Docker

**Prerequisites:** Docker Desktop

1. Create a `.env` file in the root folder:
```
JWT_KEY=<random-string-at-least-32-characters>
REDIS_PASSWORD=<redis-password>
ADMIN_EMAIL=admin@example.com
ADMIN_PASSWORD=<password>
```

`REDIS_PASSWORD` is required. Compose refuses to start without it, and Redis does not publish a port. `ADMIN_EMAIL` and `ADMIN_PASSWORD` are optional. Without them the API starts and does not create an admin account. The API container is healthy when `GET /health` succeeds, which checks the database and Redis.

2. Run:
```bash
docker compose up --build -d
```

3. The API is available at `http://localhost:8080/swagger`.

4. To stop (data is preserved in the named volume):
```bash
docker compose down
```

The development stack uses SQLite (`webapi-data` volume mounted at `/app/data`) and Redis for caching. Startup applies the same EF Core migrations as PostgreSQL. A volume created by the older `EnsureCreated` startup needs to be removed once: `docker compose down -v`. Compose waits until Redis answers an authenticated ping, then until the API health check passes.

### Production Stack

For a production-like local deployment, use the PostgreSQL + Redis compose file:

```bash
docker compose -f docker-compose.prod.yml up --build -d
```

Required environment variables:

```bash
POSTGRES_PASSWORD=<strong-postgres-password>
REDIS_PASSWORD=<redis-password>
JWT_KEY=<random-string-at-least-32-characters>
ADMIN_EMAIL=admin@example.com
ADMIN_PASSWORD=<password>
```

`POSTGRES_PASSWORD` and `REDIS_PASSWORD` are required. `ADMIN_PASSWORD` is required only when the production stack should seed an admin account. The API does not read that password from `appsettings`.

PostgreSQL gets the same migrations on startup. The connection string comes from `ConnectionStrings__DefaultConnection`; `appsettings.Production.json` leaves that value empty. Compose starts the API after PostgreSQL and Redis are healthy. The API container is healthy when `GET /health` succeeds. Redis stays on the compose network, with a password and no published port.

## API Documentation

Full interactive documentation is available via Swagger UI (`/swagger`) once the app is running (Development only). `POST /api/auth/register` is the only public way to create an account. `POST /api/user` creates an account for an administrator.

Key endpoints:

| Method | Route | Description | Auth Required |
|---|---|---|---|
| `POST` | `/api/auth/register` | Register a new user. Returns 201 | No |
| `POST` | `/api/auth/login` | Log in, returns access + refresh tokens | No |
| `POST` | `/api/auth/refresh` | Exchange a valid refresh token for a new token pair | No |
| `POST` | `/api/auth/logout` | Revoke all refresh tokens for the user in the access token | Yes |
| `GET` | `/api/product` | List products (pagination + filtering) | Yes |
| `GET` | `/api/product/{id}` | Get product details | Yes |
| `POST` | `/api/product` | Create a product | Yes (Admin) |
| `PUT` | `/api/product/{id}` | Update name, description, and price. Stock is unchanged | Yes (Admin) |
| `DELETE` | `/api/product/{id}` | Delete a product. 409 while any user still owns units | Yes (Admin) |
| `GET` | `/api/user` | List users (`pageNumber`, `pageSize`) | Yes (Admin) |
| `GET` | `/api/user/me` | Current user from the access token | Yes |
| `GET` | `/api/user/{id}` | User details | Yes (self or Admin) |
| `POST` | `/api/user` | Create a user | Yes (Admin) |
| `PUT` | `/api/user/{id}` | Update a user | Yes (self or Admin) |
| `PUT` | `/api/user/{id}/password` | Change password | Yes (self or Admin) |
| `DELETE` | `/api/user/{id}` | Delete a user | Yes (Admin) |
| `GET` | `/api/users/{userId}/products` | Products assigned to a user | Yes (self or Admin) |
| `POST` | `/api/users/{userId}/products` | Set the total quantity a user owns. Replaces the previous quantity and adjusts stock by the difference | Yes (self or Admin) |
| `DELETE` | `/api/users/{userId}/products/{productId}` | Unassign a product, restoring stock | Yes (self or Admin) |
| `GET` | `/health` | Health check for the database, and Redis when it is configured | No |

## Authentication

The API uses **JWT Bearer** authentication with refresh token rotation. After a successful login:

1. Copy the returned `token`.
2. In Swagger UI, click **Authorize** and enter `Bearer <token>`.
3. All protected endpoints (`[Authorize]`) are now accessible for the duration of the token.
4. When the access token expires, exchange the `refreshToken` via `/api/auth/refresh` for a new pair — the old refresh token is revoked on use.

After logout the access token stays valid until it expires (30 minutes). Logout revokes refresh tokens, so they can no longer extend the session.

Passwords are stored exclusively as **PBKDF2 hashes** (HMAC-SHA256, 128-bit random salt per user) — never as plain text. New hashes use 600,000 iterations and store that count, so it can be raised later. Older `salt-subkey` hashes still verify at 100,000 iterations. A password must be 12 to 128 characters. Register, login, and refresh are rate-limited to mitigate brute-force attacks. The limit uses the connection's remote IP. Forwarded headers are not enabled, because this API is not configured to run behind a trusted proxy.

Error responses use `ProblemDetails`. The message is in `detail`, and an unhandled error is HTTP 500 in the same shape, with `traceId` and without a stack trace.

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

- CORS policy configuration for browser-based clients
- API versioning (`/api/v1/...`)
- Soft delete for products and users, with audit history
- Redis is covered when the cache client throws; the compose stack itself is still checked by running it
