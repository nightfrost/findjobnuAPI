# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade findjobnuAPI\AuthService\AuthService.csproj
4. Upgrade findjobnuAPI\findjobnuAPI\FindjobnuService.csproj
5. Upgrade findjobnuAPI\FindJobNuTesting\FindJobNuTesting.csproj
6. Upgrade findjobnuAPI\JobAgentWorker\JobAgentWorkerService.csproj

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

Table below contains projects that do belong to the dependency graph for selected projects and should not be included in the upgrade.

| Project name                                   | Description                 |
|:-----------------------------------------------|:---------------------------:|
|                                                |                             |

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                  | Current Version | New Version | Description                                   |
|:----------------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Microsoft.AspNetCore.Authentication.JwtBearer |   8.0.19        |  10.0.0     | Recommended for .NET 10.0                      |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore |   8.0.19     |  10.0.0     | Recommended for .NET 10.0                      |
| Microsoft.AspNetCore.Mvc.Testing              |   8.0.19        |  10.0.0     | Recommended for .NET 10.0                      |
| Microsoft.AspNetCore.OpenApi                  |   8.0.19        |  10.0.0     | Recommended for .NET 10.0                      |
| Microsoft.EntityFrameworkCore                 |   8.0.19        |  10.0.0     | Recommended for .NET 10.0                      |
| Microsoft.EntityFrameworkCore.InMemory        |   8.0.19        |  10.0.0     | Recommended for .NET 10.0                      |
| Microsoft.EntityFrameworkCore.SqlServer       |   8.0.19        |  10.0.0     | Recommended for .NET 10.0                      |
| Microsoft.EntityFrameworkCore.Tools           |   8.0.19        |  10.0.0     | Recommended for .NET 10.0                      |
| Microsoft.Extensions.Hosting                  |   8.0.0         |  10.0.0     | Recommended for .NET 10.0                      |
| Newtonsoft.Json                               |   13.0.3        |  13.0.4     | Patch update                                   |

### Project upgrade details
This section contains details about each project upgrade and modifications that need to be done in the project.

#### findjobnuAPI\\AuthService\\AuthService.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Authentication.JwtBearer should be updated from `8.0.19` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Identity.EntityFrameworkCore should be updated from `8.0.19` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.OpenApi should be updated from `8.0.19` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.SqlServer should be updated from `8.0.19` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.Tools should be updated from `8.0.19` to `10.0.0` (*recommended for .NET 10.0*)
  - Newtonsoft.Json should be updated from `13.0.3` to `13.0.4` (*patch update*)

Other changes:
  - None

#### findjobnuAPI\\findjobnuAPI\\FindjobnuService.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Authentication.JwtBearer should be updated from `8.0.19` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.OpenApi should be updated from `8.0.19` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.SqlServer should be updated from `8.0.19` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.Tools should be updated from `8.0.19` to `10.0.0` (*recommended for .NET 10.0*)
  - Newtonsoft.Json should be updated from `13.0.3` to `13.0.4` (*patch update*)

Other changes:
  - None

#### findjobnuAPI\\FindJobNuTesting\\FindJobNuTesting.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - Microsoft.EntityFrameworkCore.InMemory should be updated from `8.0.19` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Mvc.Testing should be updated from `8.0.19` to `10.0.0` (*recommended for .NET 10.0*)
  - Newtonsoft.Json should be updated from `13.0.3` to `13.0.4` (*patch update*)

Other changes:
  - None

#### findjobnuAPI\\JobAgentWorker\\JobAgentWorkerService.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - Microsoft.EntityFrameworkCore should be updated from `8.0.19` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.SqlServer should be updated from `8.0.19` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.Tools should be updated from `8.0.19` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.Extensions.Hosting should be updated from `8.0.0` to `10.0.0` (*recommended for .NET 10.0*)

Other changes:
  - None
