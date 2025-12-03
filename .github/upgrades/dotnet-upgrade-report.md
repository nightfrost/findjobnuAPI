# .NET 10.0 Upgrade Report

## Project target framework modifications

| Project name                                              | Old Target Framework | New Target Framework | Commits |
|:----------------------------------------------------------|:--------------------:|:--------------------:|:--------|
| findjobnuAPI\AuthService\AuthService.csproj               | net8.0               | net10.0              |         |
| findjobnuAPI\findjobnuAPI\FindjobnuService.csproj         | net8.0               | net10.0              |         |
| findjobnuAPI\FindJobNuTesting\FindJobNuTesting.csproj     | net8.0               | net10.0              |         |
| findjobnuAPI\JobAgentWorker\JobAgentWorkerService.csproj  | net8.0               | net10.0              |         |

## NuGet Packages

| Package Name                                  | Old Version | New Version | Commit Id |
|:----------------------------------------------|:-----------:|:-----------:|:---------|
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.19      | 10.0.0      |          |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.19   | 10.0.0      |          |
| Microsoft.AspNetCore.Mvc.Testing              | 8.0.19      | 10.0.0      |          |
| Microsoft.AspNetCore.OpenApi                  | 8.0.19      | 10.0.0      |          |
| Microsoft.EntityFrameworkCore                 | 8.0.19      | 10.0.0      |          |
| Microsoft.EntityFrameworkCore.InMemory        | 8.0.19      | 10.0.0      |          |
| Microsoft.EntityFrameworkCore.SqlServer       | 8.0.19      | 10.0.0      |          |
| Microsoft.EntityFrameworkCore.Tools           | 8.0.19      | 10.0.0      |          |
| Microsoft.Extensions.Hosting                  | 8.0.0       | 10.0.0      |          |
| Newtonsoft.Json                               | 13.0.3      | 13.0.4      |          |
| Microsoft.CodeAnalysis.Workspaces.MSBuild     | (none)      | 5.0.0       |          |
| Microsoft.Build                               | 17.8.3      | 17.10.5     |          |

## Project feature upgrades

### findjobnuAPI\AuthService\AuthService.csproj
- Target framework updated to net10.0.
- ASP.NET Core auth, identity, OpenAPI, EF Core packages updated.
- Newtonsoft.Json patch applied.

### findjobnuAPI\findjobnuAPI\FindjobnuService.csproj
- Target framework updated to net10.0.
- ASP.NET Core auth, OpenAPI, EF Core packages updated.
- Added Microsoft.CodeAnalysis.Workspaces.MSBuild 5.0.0 for version alignment.
- Updated Microsoft.Build to 17.10.5 (security advisory mitigation).
- Newtonsoft.Json patch applied.

### findjobnuAPI\FindJobNuTesting\FindJobNuTesting.csproj
- Target framework updated to net10.0.
- Test related ASP.NET Core and EF Core packages updated.
- Newtonsoft.Json patch applied.

### findjobnuAPI\JobAgentWorker\JobAgentWorkerService.csproj
- Target framework updated to net10.0.
- EF Core and Hosting packages updated.

## Next steps
- Run and validate unit/integration tests.
- Review runtime behavior in staging.
- Consider migrating from Newtonsoft.Json to System.Text.Json where feasible.

## Cost and token usage
- Token usage per model input/output not tracked in this environment.
