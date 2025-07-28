# Solution Overview

This solution consists of three main projects:

- **findjobnuAPI**: ASP.NET Core Web API for managing and searching job postings, user profiles, and saved jobs.
- **AuthService**: Authentication and authorization service using ASP.NET Core Identity, JWT, refresh tokens, and email confirmation.
- **FindJobNuTesting**: Unit test project for API and service logic, using xUnit and Moq.

---

# findjobnuAPI

A minimal ASP.NET Core Web API for managing and searching job postings and user profiles.

## Features

- RESTful endpoints for job postings (list, search, get by ID)
- User profile management and saved jobs
- Pagination and filtering support
- Entity Framework Core with SQL Server
- OpenAPI/Swagger documentation
- CORS enabled for all origins
- JWT authentication (integrates with AuthService)
- .NET 8, C# 12

## Endpoints

- `GET /api/jobindexposts` – List job postings (supports pagination)
- `GET /api/jobindexposts/search` – Search job postings by title, location, category, and date
- `GET /api/jobindexposts/{id}` – Get a job posting by ID
- `GET /api/userprofile/{userid}` – Get user profile by user ID
- `PUT /api/userprofile/{id}` – Update user profile (JWT required)
- `POST /api/userprofile/` – Create user profile
- `GET /api/userprofile/{userId}/savedjobs` – Get saved job IDs for a user
- `POST /api/userprofile/{userId}/savedjobs/{jobId}` – Save a job for a user
- `DELETE /api/userprofile/{userId}/savedjobs/{jobId}` – Remove a saved job for a user
- `GET /api/cities` – List all cities

## Project Structure

- `Program.cs` – Application entry point and configuration
- `Endpoints/` – API endpoint definitions
- `Models/` – Data models (JobIndexPosts, UserProfile, etc.)
- `Repositories/Context/FindjobnuContext.cs` – EF Core DB context
- `Services/` – Business logic and data access

---

# AuthService

Handles authentication and authorization for the solution.

## Features

- User registration and login (with email/password)
- JWT access token and refresh token issuance
- Email confirmation for new users
- Token refresh and revocation endpoints
- ASP.NET Core Identity with Entity Framework Core
- .NET 8, C# 12

## Endpoints (examples)

- `POST /api/auth/register` – Register a new user
- `POST /api/auth/login` – Login and receive JWT/refresh token
- `POST /api/auth/refresh-token` – Refresh JWT using a valid refresh token
- `POST /api/auth/revoke-token` – Revoke a refresh token
- `GET /api/auth/confirm-email` – Confirm user email

---

# FindJobNuTesting

Unit test project for API and service logic.

## Features

- xUnit-based tests for service and repository logic
- Uses Moq for mocking dependencies
- In-memory EF Core for fast, isolated tests
- Code coverage via coverlet

## Running Tests
dotnet test FindJobNuTesting
---

# Getting Started

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server instance (for production)

## Configuration

- Update `findjobnuAPI/appsettings.json` and `AuthService/appsettings.json` with your connection strings and JWT settings.
- For AuthService, configure SMTP settings for email confirmation.

## Build and Run

To build the solution:
dotnet build
To run the main API:
dotnet run --project findjobnuAPI
To run the AuthService:
dotnet run --project AuthService
The APIs will be available at their configured URLs (see launch settings or console output).

## API Documentation

Swagger UI is available at `/swagger` for both findjobnuAPI and AuthService when running.

---

# Dependencies

- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.EntityFrameworkCore.InMemory (testing)
- Swashbuckle.AspNetCore (Swagger/OpenAPI)
- Microsoft.AspNetCore.OpenApi
- Microsoft.AspNetCore.Authentication.JwtBearer
- Microsoft.AspNetCore.Identity.EntityFrameworkCore
- Moq, xUnit, coverlet.collector (testing)

---

# License

MIT