# ADR-0002: Isolate Untrusted Code with Docker

- **Status:** Accepted; implemented in Phase 4
- **Date:** 2026-11-07
- **Phase:** 1 decision, Phase 4 implementation

## Context

PSEL will execute learner-submitted code. That code is untrusted and may attempt to access files, secrets, the network, or host resources, or to exhaust CPU, memory, processes, or disk.

The specification requires execution outside the ASP.NET Core process, with a runner abstraction and Docker-based isolation.

## Decision

Untrusted code will execute only in short-lived, per-submission Docker containers controlled by a server-side execution manager.

Required controls:

- Non-root user inside the container.
- Network disabled by default.
- CPU, memory, process, and wall-clock limits.
- Restricted filesystem and per-submission temporary workspace.
- Read-only mounts where practical.
- Output-size limits.
- No Docker socket, application secrets, user files, or sensitive host mounts.
- Server-controlled image, command, environment, and mounts.
- Cleanup after success, failure, timeout, and cancellation.
- Bounded concurrent executions.

The API process must never compile, interpret, or run submitted code. The Docker socket must never be exposed to the untrusted process.

The runner abstraction was designed in Phase 4 from actual execution requirements; the implementation is recorded in [ADR-0006](ADR-0006-submission-queue-and-docker-runner.md).

## Alternatives considered

### Execute code in the API process with a timeout

Rejected. A timeout does not prevent filesystem access, network access, secret exposure, or process/resource abuse. This violates the specification.

### Execute code as a local child process with a restricted user

Rejected as the primary boundary. Process isolation on the developer's host is weaker and more error-prone than a container, and cleanup/resource enforcement are harder to make reliable.

### gVisor or Firecracker immediately

Deferred. These provide stronger isolation but add operational complexity. Docker is the specified initial approach, and its limitations will be documented explicitly.

### Remote execution service

Rejected for a local-first product at this stage. It adds network, authentication, and data-handling concerns without a current requirement.

## Consequences

### Positive

- Submitted code is separated from the application process and its privileges.
- Resource and network restrictions can be enforced by the runner.
- The boundary is replaceable behind a runner abstraction.
- The design aligns with the specification.

### Negative

- Docker Desktop and WSL2 are required from Phase 4 onward.
- Container startup adds latency.
- Docker is not an absolute security boundary.
- The developer machine's kernel and Docker daemon remain trusted components.
- Resource limits need tuning for the available hardware.

### Mitigations

- Document the limitation in [EXECUTION_SECURITY.md](../EXECUTION_SECURITY.md).
- Test timeout, output flood, network, filesystem, resource-limit, and cleanup cases.
- Fail closed if required controls cannot be enforced.
- Consider stronger isolation technologies in a later hardening phase.
