# ADR-0004: Learning Module and Shared EF Model Registration

- **Status:** Accepted
- **Date:** 2026-11-07
- **Phase:** 2

## Context

ADR-0001 established a modular monolith and deferred physical module assemblies until a phase gave them real behavior. Phase 2 introduces learning domains, modules, materials, objectives, competencies, prerequisites, seed data, and read-only endpoints.

The application must keep one SQLite database and one `PselDbContext` transaction boundary. At the same time, `Psel.Infrastructure` must not reference concrete modules, and the learning module must not depend on the API host. EF Core also needs to discover the learning entity configurations when building the shared model and when generating migrations.

Phase 1 used a design-time `PselDbContextFactory` in infrastructure. With module-supplied configuration, that factory could not assemble the real model without referencing the learning module, so it would silently diverge from runtime.

## Decision

1. Introduce `src/Modules/Psel.Modules.Learning` as the first physical module assembly.
2. The module owns:
   - domain entities;
   - EF Core `IEntityTypeConfiguration` implementations;
   - the seed catalog and an idempotent `IHostedService` seeder;
   - read-only endpoints under `/api`;
   - client-facing DTO contracts;
   - a composition entry point, `LearningModuleRegistration`.
3. The API host references the module and passes `LearningModuleRegistration.ModelAssembly` to `AddInfrastructure`.
4. Infrastructure stores the supplied assemblies in `ModuleAssemblyRegistry`. `PselDbContext` applies configurations from its own assembly first, then from each registered module assembly.
5. Keep a single `PselDbContext` and a single database. Module entities are not split into separate contexts.
6. Remove the infrastructure design-time factory. EF Core tooling uses `--startup-project src/Psel.Api`, so migrations use the same host configuration and module registry as runtime.
7. Seed the catalog through a hosted service registered after `DatabaseInitializer`. The seeder is idempotent by checking for any existing domain.

## Alternatives considered

### Put learning configurations in `Psel.Infrastructure`

Rejected. It would make infrastructure depend on a concrete module and invert the ownership that the modular monolith is meant to preserve.

### Give the learning module its own `DbContext` and database

Rejected for Phase 2. It would introduce a second transaction boundary and complicate cross-module relationships before there is a demonstrated need. The specification calls for one local database and a modular monolith.

### Keep the design-time factory and duplicate the model

Rejected. A separate design-time model can drift from runtime configuration and produce migrations that do not match the application. Using the API host as the startup project keeps one model source of truth.

### Seed synchronously during `DatabaseInitializer`

Rejected. Database initialization belongs to infrastructure, while catalog content belongs to the learning module. A hosted service preserves ownership and ordering without coupling infrastructure to learning data.

### Insert seed data unconditionally on every startup

Rejected. It would duplicate rows. The Phase 2 check for any existing domain is intentionally simple for read-only seed content; authoring will require explicit migration or upsert rules.

## Consequences

### Positive

- The learning boundary is physical and enforceable through project references.
- Infrastructure remains independent of concrete modules.
- One shared model and transaction boundary are preserved.
- Migrations use the runtime model and composition path.
- Seed data is owned by the learning module and is covered by integration tests.

### Negative

- `PselDbContext` now requires `ModuleAssemblyRegistry`; tests and tooling must supply it.
- The host must remember to register each module assembly with infrastructure.
- The simple "any domain exists" seed check does not support partial catalog updates.
- One shared context still allows accidental cross-module persistence access if discipline is not enforced.

### Mitigations

- Keep module registration explicit in the composition root.
- Add architecture tests when more modules exist.
- Replace the seed check with explicit versioned seed/upsert rules before content becomes editable.
- Continue documenting allowed dependency directions and review module boundaries.
