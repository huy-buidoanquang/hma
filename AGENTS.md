# Repository Guidelines

## Project Structure & Architecture

The .NET 10 solution is `src/Hma.slnx`. `Hma.Domain` contains rules; `Hma.Application` owns use cases; `Hma.Infrastructure.SqlServer` implements persistence; `Hma.Reporting` exports PDF/Excel; and `Hma.Desktop.Wpf` is the host. Tests: `src/Hma.*.Tests`; SQL/ETL: `database/`; specifications: `docs/`.

Domain must not reference EF, WPF, or SQL; Application must not use `System.Windows` or connection strings. Desktop calls Application, never `SqlClient` or `HmaDbContext`. Do not add ASP.NET/JWT/Swagger in this phase. Treat `legacy/` as read-only behavioral reference; never compile or copy its frameworks or metadata.

## Survey Before Editing

Before every change, inspect affected callers, callees, interfaces, DI, XAML, entities, tests, and schema/ETL. Infer side effects, permissions, messages, and screen keys from code. Prefer the smallest fix in Domain/Application, reuse helpers, and update all call sites. Report contradictions instead of guessing.

## Build, Test, and Run

- `dotnet tool restore` — restore the pinned EF CLI.
- `dotnet restore src/Hma.slnx` — restore packages.
- `dotnet build src/Hma.slnx` — compile the solution.
- `dotnet test src/Hma.slnx` — run xUnit tests.
- `dotnet run --project src/Hma.Desktop.Wpf` — start the desktop app.
- `dotnet format src/Hma.slnx` — apply repository formatting.

## Coding & WPF Conventions

Use four spaces, file-scoped namespaces, nullable annotations, and implicit usings. Use PascalCase for types/public members, camelCase for locals, `_camelCase` for stored fields, and `I` for interfaces. Prefer primary constructors, obvious `var`, LINQ, and immutable records. I/O is async, accepts `CancellationToken`, and uses an `Async` suffix; never block on tasks.

Each top-level type belongs in a matching file/folder; only WPF partials, a private/file-scoped helper, and `DependencyInjection` are exceptions. Keep views layout-only and code-behind minimal. ViewModels use CommunityToolkit.Mvvm, `WorkspaceBase`, observable collections, bindings, and Application services. User text is Vietnamese; identifiers and screen keys are English.

## Persistence, Security & Testing

Use EF Core through `IHmaDbContext`; do not add another repository layer or lazy loading. Use explicit `Include` and bound list queries. Schema changes are code-first migrations—no ad-hoc `ALTER`/`EnsureCreated` patches—and `database/001_schema.sql` must stay aligned. Use singular PascalCase SQL names, `Id`/`{Table}Id`, `nvarchar`, and `datetime2`; preserve established decimal precision. Keep `LegacyId` only for ETL. Enforce permissions at the Application boundary, hash passwords with PBKDF2, and never commit production secrets or plaintext legacy passwords.

Name tests `<Subject>Tests.cs` and scenarios by behavior. Add xUnit/NSubstitute coverage in the owning layer. Use short imperative commits (for example, `improve excel import`). PRs must explain impact, link issues, list verification, include WPF screenshots, and call out migrations, ETL, permissions, or configuration changes.
