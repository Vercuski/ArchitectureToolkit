![Onion Softare Architecture](./Documentation/Images/ArchitectureToolkititecture.png)

# Template Structure

## Domain Layer
- Third Party Libraries
  - None

## Application Layer
- Third Party Libraries
  - MediatR
  - Microsoft.EntityFrameworkCore
  - Microsoft.Extensions.Hosting

## Presentation Layer
### Presentation.API
- Third Party Libraries
  - Microsoft.AspNetCore.OpenApi
  - Microsoft.OpenApi
  - Microsoft.VisualStudio.Azure.Containers.Tools.Targets
  - Scalar.AspNetCore
  
### Presentation.Console
- Third Party Libraries
  - Microsoft.Extensions.Hosting
  - Spectre.Console
  - Spectre.Console.Cli

### Presentation.Web
- Third Party Libraries
  - Microsoft.VisualStudio.Azure.Containers.Tools.Targets
  - MudBlazor
  
## Persistence Layer
- Third Party Libraries
  - Dapper
  - Microsoft.Data.SqlClient
  - Microsoft.EntityFrameworkCore.SqlServer
  - MySql.EntityFrameworkCore
  - MySqlConnector
  - Npgsql
  - Npgsql.EntityFrameworkCore.PostgreSQL

  SQL Server, PostgreSQL, and MySQL packages are all referenced because the
  database backend is swappable via `IDatabaseProvider` (`SqlServerDatabaseProvider` /
  `PostgreSqlDatabaseProvider` / `MySQLDatabaseProvider`). The active provider is
  selected per query/command side via `DatabasePlatformOptions` in each
  Presentation project's `appsettings.json`, resolved in `Persistence/DependencyInjection.cs`.

## Infrastructure Layer
- Third Party Libraries
  - Azure.Identity
- Framework References
  - Microsoft.AspNetCore.App (needed for `GlobalExceptionHandler` and the
    health check response writer, both of which are ASP.NET Core-specific
    cross-cutting concerns centralized in this layer)

## Testing Layer
- Third Party Libraries
  - coverlet.collector
  - FakeItEasy
  - Microsoft.NET.Test.Sdk
  - NetArchTest.Rules
  - NUnit
  - NUnit.Analyzers
  - NUnit3TestAdapter
