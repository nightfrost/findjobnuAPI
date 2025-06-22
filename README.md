# findjobnuAPI

A minimal ASP.NET Core Web API for managing and searching job postings.

## Features

- RESTful endpoints for job postings (list, search, get by ID)
- Pagination and filtering support
- Entity Framework Core with SQL Server
- OpenAPI/Swagger documentation
- CORS enabled for all origins
- .NET 8, C# 12

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server instance

### Configuration

1. Update the `appsettings.json` file with your SQL Server connection string under `ConnectionStrings:FindjobnuConnection`.

### Build and Run
`dotnet build dotnet run --project findjobnuAPI`

The API will be available at `https://localhost:5001` (or as configured).

### API Documentation

Swagger UI is available at `/swagger` when the application is running.

## Endpoints

- `GET /api/jobindexposts`  
  List job postings (supports pagination).

- `GET /api/jobindexposts/search`  
  Search job postings by title, location, category, and date.

- `GET /api/jobindexposts/{id}`  
  Get a job posting by ID.

## Project Structure

- `Program.cs` – Application entry point and configuration
- `Endpoints/JobIndexPostsEndpoints.cs` – API endpoint definitions
- `Models/JobIndexPosts.cs` – Job posting model
- `Repositories/Context/FindjobnuContext.cs` – EF Core DB context

## Dependencies

- Microsoft.EntityFrameworkCore.SqlServer
- Swashbuckle.AspNetCore (Swagger/OpenAPI)
- Microsoft.AspNetCore.OpenApi

## License

MIT