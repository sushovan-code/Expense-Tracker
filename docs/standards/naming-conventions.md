# Naming conventions

Names describe intent in business language. No abbreviations except well-known ones (`Id`, `Url`, `Dto`, `Api`). The same concept has the same name in the database, API, backend and frontend.

## Repository layout
```
<product>/
  src/
    <Product>.AppHost/              Aspire orchestration
    <Product>.ServiceDefaults/      OpenTelemetry, health checks, resilience
    <Product>.<Module>.Api/         HTTP endpoints, hubs, DI composition root
    <Product>.<Module>.Application/ use cases, ports (interfaces), DTOs
    <Product>.<Module>.Domain/      entities, value objects, domain events (no dependencies)
    <Product>.<Module>.Infrastructure/ adapters: EF Core, Redis, messaging, AI, storage
    <Product>.<Module>.Contracts/   messages and events shared with other services
    web/<product>-web/              Angular or React app
  tests/
    <Product>.<Module>.UnitTests/
    <Product>.<Module>.IntegrationTests/
  docs/  (spec.md, architecture.md, adr/, standards/)
  infra/ (Terraform for this repo, if any)
  .github/ (workflows, templates)
```
Single-module apps omit `<Module>`: `Meridian.Api`, `Meridian.Application`, ...

## C# / .NET
| Element | Convention | Example |
| --- | --- | --- |
| Namespace | File-scoped, matches folder path | `namespace Meridian.Payments.Application.Transfers.CreateTransfer;` |
| Class, record, struct, enum | PascalCase noun | `TransferService`, `AccountBalance` |
| Interface | `I` + PascalCase | `ITransferRepository`, `IFileStorage` |
| Method | PascalCase verb; `Async` suffix when async | `CreateTransferAsync` |
| Property, constant | PascalCase | `MaxRetryCount` |
| Local variable, parameter | camelCase | `transferId` |
| Private field | `_camelCase` | `_timeProvider` |
| Boolean | `Is`, `Has`, `Can`, `Should` prefix | `IsLocked`, `HasCapacity` |
| Command / query | Imperative / `Get...` | `CreateTransfer`, `GetAccountBalance` |
| Handler / validator | Same name + suffix | `CreateTransferHandler`, `CreateTransferValidator` |
| Domain event | Past tense | `TransferCompleted`, `AccountLocked` |
| API DTO | `Request` / `Response` suffix | `CreateTransferRequest` |
| Options class | `Options` suffix, bound to a section | `CacheOptions` ← `Cache` |
| Extension registering DI | `Add<Layer>` | `AddInfrastructure(configuration)` |
| Test class / method | `<Subject>Tests`; `Method_State_ExpectedResult` | `CreateTransferHandlerTests.Handle_InsufficientFunds_ReturnsFailure` |

One public type per file; the file name equals the type name. Group by feature (vertical slice), not by technical type: `Application/Transfers/CreateTransfer/CreateTransfer.cs`, `CreateTransferHandler.cs`, `CreateTransferValidator.cs`.

## HTTP API
- Routes: `/api/v1/<plural-kebab-noun>/{id}` → `/api/v1/transfers/{transferId}`, `/api/v1/team-members`.
- Actions that are not CRUD: sub-resource verbs → `POST /api/v1/accounts/{accountId}/unlock`.
- JSON: camelCase properties; enums as strings; dates ISO 8601 UTC.
- Errors: RFC 9457 Problem Details.

## Database
- SQL Server: tables PascalCase plural (`Transfers`), columns PascalCase, PK `Id`, FK `<Entity>Id`.
- PostgreSQL: snake_case via `EFCore.NamingConventions` (`transfers`, `account_id`).
- Migrations: `<yyyyMMddHHmm>_<PascalCaseDescription>` (EF default timestamp + description).

## Messaging and cache keys
- Queues/topics: `<product>.<module>.<event-kebab>` → `meridian.payments.transfer-completed`.
- Cache keys: `<product>:<entity>:<id>:v<version>` → `meridian:account-balance:42:v1`.
- SignalR hubs: `<Purpose>Hub` (`NotificationsHub`, `ChatHub`); client methods camelCase (`notify`, `receiveToken`).

## Angular
- Follow the current official Angular style guide (standalone components, signals).
- Files and folders kebab-case: `transfer-list/transfer-list.ts`, `transfer.service.ts`.
- Selector prefix per app: `<app-prefix>-transfer-list` (e.g. `mb-`).
- Feature folders under `src/app/features/<feature>/`; shared UI under `src/app/shared/ui/`; core services under `src/app/core/`.

## React
- Components PascalCase files: `TransferList.tsx`; one component per file.
- Hooks: `useTransfers.ts`; contexts: `ThemeContext.tsx`.
- Folders kebab-case by feature: `src/features/transfers/`, shared UI in `src/components/ui/`.

## Git
- Branches: `feature/<issue>-<kebab>`, `fix/<issue>-<kebab>`, `chore/<kebab>`.
- Commits: Conventional Commits; PR title the same.
