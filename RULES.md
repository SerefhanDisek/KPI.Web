# KPI Web Engineering Rules

## Architecture

- This repository is a self-contained Onion Architecture solution containing Domain, Application, Infrastructure, and ASP.NET Core Web projects; it must not reference projects or source files outside the repository root.
- HTTP use cases are defined in the Application layer and follow CQRS with MediatR. Controllers are thin and only bind input, dispatch requests, and return results.
- The Web project must not contain Commands, Queries, or Handlers.
- Application and Infrastructure expose their own dependency-injection registration methods; Web acts only as the composition root and does not register concrete persistence services individually.
- Database access and business validation stay in the Infrastructure project. The web host does not access `KpiDbContext` from controllers or browser code.
- Public API contracts use Application DTOs or explicit request models; Domain entities are not returned directly.

## Web Experience

- Turkish is the interface language and terminology follows the established KPI product language.
- The application is a responsive working surface: primary actions and current data appear before explanatory content.
- Browser code calls same-origin `/api` endpoints through the shared API helper and presents loading, empty, success, and error states consistently.
- The static HTML shell versions its JavaScript and stylesheet URLs with the first 12 lowercase SHA-256 characters of each asset. Refresh the corresponding URL whenever that asset changes so browsers retrieve the matching interface version.
- Navigation, dialogs, forms, tables, and notifications remain keyboard accessible and usable on mobile layouts.
- Active/passive changes are non-destructive and require confirmation when initiated as a deactivation action.
- Select controls may support single or multiple selection; multi-select filters operate on all selected values rather than only the first.
- The page designer composes the existing design system's components and stores layout definitions through Application use cases; browser code never writes page definitions directly to persistence.

## API and Operations

- Async request flows propagate `CancellationToken`.
- Validation errors use the shared JSON problem shape and are shown to the user without leaking internal details.
- Authentication uses an HTTP-only cookie. Roles and permissions are managed as persisted data; role permissions are checked by the Application pipeline on every protected use case, while the web menu reflects the same permissions for usability.
- Evaluation read access does not grant edit access. Infrastructure checks record ownership for both edit queries and updates, preserves the existing administrator scope, and prevents changes to approved evaluations.
- Temporary evaluations may retain unanswered items with score 0. Permanent saves require all items to be scored 1–5 and both evaluation-create and evaluation-approve permissions; they set Approved status, approver, and approval time immediately. Evaluation status is a concurrency token so a stale draft update cannot overwrite a permanent save.
- KPI teams are the existing profession groups. A common KPI applies to everyone; a scoped KPI can target multiple professions through `kpi_criterion_profession`. Keep the legacy primary `ProfessionId` synchronized and include both legacy and linked scopes in selection and per-profession weight validation.
- Leave tracking has separate view and manage permissions, enforced through MediatR; management also requires view permission. New permissions follow the existing role seeding policy and do not grant additional access to non-administrator roles automatically.
- Leave records retain employee, date range, leave type, notes, active state, and actor/time audit fields. Lists filter and apply deterministic ordering before database pagination. Do not infer entitlement, duration, or approval calculations from date ranges.
- Bootstrap credentials are temporary and every bootstrap account must change its password at first sign-in.
- SQL Server configuration comes from `ConnectionStrings:KpiDatabase`; environment-specific values must not be committed.
- The web host may attempt database initialization at startup, but must remain available with a clear configuration state when SQL Server is temporarily unavailable.
- Keep EF Core runtime, SQL Server provider, and design packages on the same version. Use EF Core 10.0.2 or later in the 10.x line so design tooling does not expose NuGet BuildHost files as linked project content; keep the design package private to Infrastructure.

## Changes

- Keep changes small and feature-focused, preserve the existing stack, and do not add frontend package dependencies without a concrete need.
- Build the solution after changes. Update this file only for reusable conventions, not implementation history.
