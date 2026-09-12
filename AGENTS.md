# Repository Guidelines

## Project Structure & Architecture

The .NET 10 solution is `src/Hma.slnx`. Domain code uses `Entities/`, `Enums/`, `Models/`, `Rules/`, `Normalization/`, and `Formatting/`. Application ports live in `Abstractions/`, shared concerns in `Common/`, and use cases/contracts in `Features/<Feature>/`. WPF uses `Abstractions/`, `Infrastructure/`, and `Presentation/Features/<Feature>/{Views,ViewModels,Models}`. SQL/ETL is in `database/`; specifications are in `docs/`.

Domain must not reference EF, WPF, or SQL; Application must not use `System.Windows` or connection strings. Desktop calls Application, never `SqlClient` or `HmaDbContext`. Do not add ASP.NET/JWT/Swagger now. Treat `legacy/` as read-only behavioral reference.

## Survey Before Editing

Before changes, inspect affected callers, interfaces, DI, XAML, entities, tests, and schema/ETL. Infer side effects, permissions, messages, and screen keys from code. Update all call sites; report contradictions instead of guessing.

## Build, Test, and Run

- `dotnet restore src/Hma.slnx` — restore packages.
- `dotnet build src/Hma.slnx` — compile the solution.
- `dotnet test src/Hma.slnx` — run xUnit tests.
- `$env:HMA_TEST_CONNECTION='Server=.;Trusted_Connection=True;Encrypt=False'; dotnet test src/Hma.Infrastructure.SqlServer.Tests` — run disposable SQL integration tests.
- `dotnet run --project src/Hma.Desktop.Wpf` — start the desktop app.

## Coding & WPF Conventions

Use four spaces, file-scoped namespaces, nullable annotations, and implicit usings. Use PascalCase for types/public members, camelCase for locals, `_camelCase` for fields, and `I` for interfaces. Prefer primary constructors, obvious `var`, LINQ, and immutable records. Async I/O accepts `CancellationToken` and uses an `Async` suffix.

Each top-level type belongs in a matching file/folder; never create `Helpers/` or `Utils/`. Keep views layout-only and code-behind minimal. ViewModels use CommunityToolkit.Mvvm, presentation models, immutable dirty-state snapshots, and Application commands/details. Never expose persisted entities or physical paths. User text is Vietnamese; identifiers and screen keys are English.

## Persistence, Security & Testing

Use EF Core through `IHmaDbContext`; do not add repositories or lazy loading. Use explicit `Include` and bounded queries. Schema changes require migrations and aligned `database/001_schema.sql`; preserve SQL names and decimal precision. Keep `LegacyId` only for ETL. Enforce permissions at the Application boundary and never commit secrets.

Application uses read-only `ICurrentUser`, `TimeProvider`, stream storage, and Application-owned reporting contracts. Name tests `<Subject>Tests.cs` and scenarios by behavior. Run architecture and WPF binding smoke tests after moves. Commits are short and imperative; PRs list verification, screenshots, and migration/ETL/permission/configuration impact.
