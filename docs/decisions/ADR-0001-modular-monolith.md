# ADR-0001: Use a Modular Monolith

- **Status:** Accepted
- **Date:** 2026-11-07
- **Phase:** 1

## Context

PSEL is a local-first learning and assessment platform. It needs explicit domain boundaries between learning, challenges, submissions, execution, assessment, progress, and languages, but it is also a single-user application intended to run on one machine.

The specification requires a modular monolith and explicitly warns against premature microservices.

## Decision

Build the backend as a **modular monolith** hosted by a single ASP.NET Core process.

- The host (`Psel.Api`) composes the application.
- Persistence infrastructure (`Psel.Infrastructure`) owns EF Core and SQLite.
- Business modules will be introduced as separate class libraries when their phase begins.
- Each module will expose explicit composition methods for service registration and endpoint mapping.
- Modules must not depend on the API host or directly on another module's internal persistence.
- The application uses one SQLite database and, initially, one `PselDbContext` as the transaction boundary.

Phase 1 intentionally does not create seven empty module projects. Physical module assemblies are added when they contain real behavior, avoiding speculative structure while preserving the documented boundary.

## Alternatives considered

### Microservices

Rejected for the current product stage. They add deployment, discovery, network failure, and distributed transaction complexity without a demonstrated need for a local single-user application.

### Traditional layered monolith without modules

Rejected because it tends to blur domain boundaries and makes later separation harder. The module convention provides explicit ownership without distributed-system overhead.

### All module projects scaffolded in Phase 1

Deferred. Empty projects add build and navigation overhead and cannot enforce meaningful boundaries before there is behavior to isolate. The host/module composition pattern is documented and will be applied in Phase 2.

## Consequences

### Positive

- Simple local deployment.
- One transaction boundary and straightforward debugging.
- Explicit module ownership as the system grows.
- Modules can be extracted later if a documented need appears.
- Tests can target the host and modules independently.

### Negative

- Module boundaries rely on discipline until physical assemblies exist.
- A single process and database limit independent scaling.
- A shared `DbContext` requires care to keep module persistence details isolated.

### Mitigations

- Introduce module assemblies at the start of each phase.
- Keep module registration explicit.
- Add architecture tests when multiple modules exist.
- Document cross-module interfaces rather than sharing internal types.
