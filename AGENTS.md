# Repository Guidelines

## Project Structure & Module Organization

This repository contains one ASP.NET Core 6 application combining MVC/Razor pages and a REST API.

- `Controllers/`: MVC actions and API endpoints (`ApiController` uses `/api/ApiAccess`).
- `Models/`, `ViewModels/`: database entities, SQL-view models, and request/view contracts.
- `Data/`, `Migrations/`: `ApplicationDbContext`, seed data, and EF Core migrations.
- `Services/`, `Utilites/`: business workflows, background processing, notifications, and initialization.
- `Views/`: Razor views grouped by feature; shared layouts live in `Views/Shared/`.
- `wwwroot/`: CSS, JavaScript, icons, and images.

Keep business rules in controllers/services rather than Razor views. Reuse shared CSS and partials before creating feature-specific duplicates.

## Build, Test, and Development Commands

```powershell
dotnet restore              # Restore NuGet packages
dotnet build               # Compile the application
dotnet run                 # Run locally
dotnet ef database update  # Apply EF Core migrations
dotnet format              # Apply standard .NET formatting
```

Configure `ConnectionStrings:DefaultConnection` using User Secrets or `appsettings.Development.json`. SQL Server and the expected `View_Users`, `View_Palets`, and `View_Transferencias` views are required for full functionality.

## Coding Style & Naming Conventions

Use four spaces in C# and consistent indentation in Razor, CSS, and JavaScript. Follow standard .NET naming: `PascalCase` for types, methods, and public properties; `camelCase` for locals and parameters; interfaces begin with `I`. Async methods should end in `Async`. Keep nullable annotations enabled and prefer async EF Core operations. Store dates in UTC and convert only for display. Centralize status names and colors instead of duplicating literals.

## Testing Guidelines

There is currently no automated test project. Every change must at minimum pass `dotnet build` and be manually verified for relevant roles and desktop/mobile layouts. For new tests, create an xUnit project such as `PaletsWebApp.Tests`; name tests `Method_Scenario_ExpectedResult`. Prioritize transfer state changes, pallet ownership, authorization, claims, and API responses.

## Commit & Pull Request Guidelines

Recent history uses concise Spanish messages and Conventional Commit prefixes. Prefer:

```text
feat: modernizar formulario de usuarios
fix: corregir fecha de recepción
```

Keep commits focused. Pull requests should explain the behavior changed, validation performed, affected roles/API endpoints, and database or configuration impact. Include before/after screenshots for UI changes and link the related issue when available.

## Security & Configuration

Never commit connection strings, SMTP credentials, Firebase keys, `Data/serviceAccountKey.json`, or generated `bin/`, `obj/`, and `.vs/` files. Keep placeholders only in `*.Example.json`. Review authorization whenever adding or changing MVC actions or API endpoints.