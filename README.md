# Solution Overview

This solution consists of three main projects:

- **findjobnuAPI**: ASP.NET Core Web API for managing and searching job postings, user profiles, saved jobs, and LinkedIn profile import.
- **AuthService**: Authentication and authorization service using ASP.NET Core Identity, JWT, refresh tokens, email confirmation, LinkedIn OAuth login/account linking, and account settings management (password, email, disable).
- **FindJobNuTesting**: Unit test project for API and service logic, using xUnit and Moq.

---

# findjobnuAPI

A minimal ASP.NET Core Web API for managing and searching job postings and user profiles.

## Features

- RESTful endpoints for job postings (list, search, get by ID)
- User profile management and saved jobs
- LinkedIn profile import via Python script (see LinkedIn endpoints below)
- Pagination and filtering support
- Automatic WebP conversion for stored banner/footer images (original MIME information also returned for fallback)
- Response compression (Brotli/Gzip) and short-lived response caching plus in-memory caching for search and recommendations
- Entity Framework Core with SQL Server
- OpenAPI/Swagger documentation
- CORS enabled for all origins
- JWT authentication (integrates with AuthService)
- .NET 10, C# 14

## Endpoints

- `GET /api/jobindexposts` – List job postings (supports pagination)
- `GET /api/jobindexposts/search` – Search job postings by title, location, category, and date (cached per query parameters)
- `GET /api/jobindexposts/{id}` – Get a job posting by ID
- `GET /api/jobindexposts/saved` – Get saved job posts for the authenticated user
- `GET /api/jobindexposts/recommended-jobs` – Get personalized recommendations (cached per user + paging)
- `GET /api/userprofile/{userid}` – Get user profile by user ID
- `PUT /api/userprofile/{id}` – Update user profile (JWT required)
- `POST /api/userprofile/` – Create user profile
- `GET /api/userprofile/{userId}/savedjobs` – Get saved job IDs for a user
- `POST /api/userprofile/{userId}/savedjobs/{jobId}` – Save a job for a user
- `DELETE /api/userprofile/{userId}/savedjobs/{jobId}` – Remove a saved job for a user
- `GET /api/cities` – List all cities
- **LinkedIn Profile Import:**
  - `POST /api/linkedin/import` – Import LinkedIn profile data for a user (requires LinkedIn credentials and user ID)

### Image payloads

Job posting responses now include:
- `BannerPicture` / `FooterPicture`: raw bytes (WebP when conversion succeeds)
- `BannerFormat` / `FooterFormat`: `webp` or `original`
- `BannerMimeType` / `FooterMimeType`: MIME to apply on the client
Frontends should prefer the WebP bytes when supported and fall back to `original` when necessary.

## Project Structure

- `Program.cs` – Application entry point and configuration
- `Endpoints/` – API endpoint definitions
- `Models/` – Data models (JobIndexPosts, UserProfile, etc.)
- `Repositories/Context/FindjobnuContext.cs` – EF Core DB context
- `Services/` – Business logic and data access (includes LinkedInProfileService)

---

# AuthService

Handles authentication and authorization for the solution.

## Features

- User registration and login (with email/password)
- LinkedIn OAuth login and account linking
- JWT access token and refresh token issuance
- Email confirmation for new users
- Token refresh and revocation endpoints
- Account settings: change password, change email (2-step), disable account
- ASP.NET Core Identity with Entity Framework Core
- .NET 10, C# 14

## Endpoints

Authentication & Tokens:
- `POST /api/auth/register` – Register a new user
- `POST /api/auth/login` – Login and receive JWT/refresh token
- `POST /api/auth/refresh-token` – Refresh JWT using a valid refresh token
- `POST /api/auth/revoke-token?refreshToken=...` – Revoke a specific refresh token
- `GET /api/auth/confirm-email?userId=...&token=...` – Confirm user email

Account Settings:
- `POST /api/auth/change-password` – Change password (requires old + new password)
- `POST /api/auth/change-email` – Initiate email change (requires current password; sends confirmation link)
- `GET /api/auth/confirm-change-email?userId=...&newEmail=...&token=...` – Confirm email change
- `POST /api/auth/disable-account` – Disable (lock out) account and revoke all refresh tokens

Information & Verification:
- `GET /api/auth/user-info` – Get authenticated user profile info
- `GET /api/auth/linkedin-verification/{userId}` – Check LinkedIn verification
- `GET /api/auth/protected-data` – Example protected resource

LinkedIn OAuth (if implemented):
- `GET /api/auth/linkedin/login`
- `GET /api/auth/linkedin/callback`

## Account Settings Flow

Change Password:
1. Call `POST /api/auth/change-password` with current and new password
2. All existing refresh tokens are revoked; user should re-authenticate as needed

Change Email:
1. Call `POST /api/auth/change-email` with current password and new email
2. Confirmation link sent to new email
3. User clicks `GET /api/auth/confirm-change-email` link -> email + username updated, tokens revoked

Disable Account:
1. Call `POST /api/auth/disable-account` with current password
2. Account locked indefinitely; all tokens revoked

---

# FindJobNuTesting

Unit test project for API and service logic.

## Features

- xUnit-based tests for service and repository logic
- Uses Moq for mocking dependencies
- In-memory EF Core for fast, isolated tests
- Code coverage via coverlet
- Tests include authentication flows (register, login, token refresh) and new account settings (password change, email change confirmation, disable account)

## Running Tests

```bash
dotnet test FindJobNuTesting
```
---

# Getting Started

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server instance (for production)

## Configuration

- Update `findjobnuAPI/appsettings.json` and `AuthService/appsettings.json` with your connection strings and JWT settings.
- Configure SMTP settings in `AuthService/appsettings.json` for email confirmation and email change flows.
- For LinkedIn features, configure LinkedIn OAuth credentials and data import settings.

## Build and Run

To build the solution:
```bash
dotnet build
```
To run the main API:
```bash
dotnet run --project findjobnuAPI
```
To run the AuthService:
```bash
dotnet run --project AuthService
```
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
- Serilog (logging)
- Moq, xUnit, coverlet.collector (testing)
- SkiaSharp (WebP conversion)

---

# License

MIT