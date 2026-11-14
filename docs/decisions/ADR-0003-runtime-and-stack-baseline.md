# ADR-0003: Runtime and Stack Baseline

- **Status:** Accepted
- **Date:** 2026-11-07
- **Phase:** 1

## Context

`PROJECT_SPEC.md` defines the technology families (ASP.NET Core, EF Core, SQLite, React, TypeScript, Tailwind CSS, xUnit, Vitest, Docker) but does not pin versions. The development machine initially had no .NET SDK, only an end-of-life .NET 6 runtime, and Node.js 20.11.1, which is end-of-life and below the requirements of current frontend tooling.

## Decision

Use the following baseline:

| Component | Version |
|---|---|
| .NET / ASP.NET Core | 10.0 (SDK 10.0.403, LTS) |
| EF Core | 10.0.14 |
| SQLite provider | 10.0.14 |
| Node.js | 26.13.0 LTS |
| React | 19.3.1 |
| TypeScript | 5.9.3 |
| Vite | 8.3.6 |
| Tailwind CSS | 4.3.5 |
| React Router | 8.5.1 |
| Vitest | 5.1.0 |
| xUnit | v3 4.0.2 |

### .NET 10 instead of .NET 8

.NET 8 reaches end of support on 2026-11-10. .NET 10 is the current LTS release, supported through 2028-11-14.

### Node 26 LTS instead of Node 20

Node 20 is end-of-life. Vite 8 requires Node `^20.19.0 || >=22.12.0`; Vitest 5 requires `^22.12.0 || ^24.0.0 || >=26.0.0`; jsdom 31 requires `^22.22.2 || ^24.15.0 || >=26.0.0`. Node 26 LTS satisfies all current tooling.

### TypeScript 5.9.3 instead of 7.x

TypeScript 7 is the latest stable release, but the current `typescript-eslint` release supports TypeScript `<6.1.0`. Pinning 5.9.3 keeps type checking and linting reliable. This will be revisited when the lint toolchain supports TypeScript 7.

### EF Core with SQLite

SQLite is the specified local store. EF Core provides migrations, model configuration, and a design-time workflow. A single `PselDbContext` is the initial transaction boundary for the modular monolith.

### Development proxy instead of CORS

The Vite dev server proxies `/api` to the API. This keeps the browser on one origin and avoids opening a broad CORS policy during development.

### API contract version 1 under `/api`

The specification's endpoint table uses `/api/...`. Treating this as contract version 1 preserves that shape. Breaking changes require a new base path or an explicitly documented versioning mechanism.

## Alternatives considered

- Keep .NET 8: rejected because support ends within days of this decision.
- Keep Node 20 and downgrade Vite/Vitest: rejected because it retains an end-of-life runtime.
- Use TypeScript 7 immediately: deferred due to lint toolchain incompatibility.
- Use a separate database server: rejected for a local-first application.
- Open CORS in development: rejected in favor of the Vite proxy.

## Consequences

### Positive

- Supported runtimes with current security patches.
- Current frontend tooling without legacy pins.
- Reproducible builds through `global.json`, central package management, and a committed npm lockfile.
- Narrower development security surface.

### Negative

- The developer machine must install .NET 10 and Node 26.
- TypeScript is one major version behind the latest release.
- Version pins must be reviewed as support windows and tooling compatibility change.

### Review triggers

- TypeScript 7 support in `typescript-eslint`.
- Node 26 maintenance status changes.
- .NET 10 end-of-support planning.
- Any change to the execution isolation approach.
