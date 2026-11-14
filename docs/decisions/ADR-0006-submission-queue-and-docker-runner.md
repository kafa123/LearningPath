# ADR-0006: Submission Queue and Docker Runner

- **Status:** Accepted
- **Date:** 2026-11-08
- **Phase:** 4

## Context

Phase 4 must persist a submission, execute it in an isolated container, and return a structured result without blocking the HTTP request or trusting client-supplied execution parameters.

The application is a local-first, single-user modular monolith with SQLite. It has no distributed infrastructure, message broker, or worker service. Execution is slow relative to an HTTP request because it includes container startup, a build, and a program run. The design must therefore separate the request path from execution while keeping the deployment simple.

Several forces apply:

- Submitted code is untrusted and must never run in the API process or with normal OS privileges.
- The client must not choose the image, command, mounts, environment, or resource limits.
- Challenge harnesses and hidden material are server-only and must not leak into client contracts.
- Docker may be absent. The API must still start and remain usable.
- The process may crash mid-execution, leaving state that must be observable and recoverable.
- The total codebase and operational surface should stay small for a local product.

## Decision

### 1. Separate execution and submission modules

- `Psel.Modules.Execution` owns the Docker boundary (`IDockerEngine`), the language runner contract (`ILanguageRunner`), the C# runner, execution options, limits, and cleanup. It depends on no other module.
- `Psel.Modules.Submissions` owns the `Submission` entity, validation, the queue, the worker, restart recovery, and HTTP endpoints. It references infrastructure, challenges, and execution.
- `Psel.Modules.Challenges` exposes harness content only through `IChallengeExecutionSource`; harness source is not part of `ChallengeDetail`.

### 2. Append-only submission rows with an in-row lifecycle

A submission is one immutable attempt. A new Run creates a new row. Status, timestamps, output, exit code, and diagnostics are updated only within that row's lifecycle. Terminal rows are never reopened.

Status transitions are `Queued → Running → terminal`. Terminals are `Succeeded`, `CompilationError`, `RuntimeError`, `TimeLimitExceeded`, `MemoryLimitExceeded`, `OutputLimitExceeded`, `ExecutionUnavailable`, `Cancelled`, `Interrupted`, and `InternalError`.

### 3. Bounded in-memory queue

`SubmissionQueue` is a bounded `Channel<Guid>` with capacity `MaxQueuedExecutions`. It carries identifiers only, never source content. When the queue is full, the submission is persisted as unavailable and the request returns `503` instead of growing without limit.

A durable queue was rejected for Phase 4 because the product is local and single-process, and because a durable queue would require broker/broker-like infrastructure and delivery semantics that are not in scope.

### 4. Background worker with bounded concurrency

`SubmissionWorker` is a `BackgroundService` that reads identifiers, acquires a semaphore slot, and processes each attempt in a separate task. Each attempt gets its own DI scope and short database transactions:

1. Load the row and verify it is still `Queued`.
2. Mark it `Running` and save.
3. Release the transaction, load server-side files and harness, and run the container.
4. Persist the terminal outcome.

No database transaction is held while code executes. Runner exceptions are logged and persisted as `InternalError`.

### 5. Docker process boundary

`ProcessDockerEngine` invokes the Docker CLI with `ProcessStartInfo.ArgumentList`; no shell concatenation is used. The runner supplies only a server-configured image, a generated container name, the workspace path, a fixed command, a timeout, an output budget, and server-controlled environment variables.

Isolation flags live in the engine, not in the runner or request contract: `--network none`, memory/swap, `--cpus`, `--pids-limit`, `--read-only`, a `/tmp` tmpfs, `--cap-drop ALL`, `--security-opt no-new-privileges`, `--user`, a read-only workspace mount, and `--workdir /workspace`.

The engine removes the container with `docker rm -f` after every path, inspects `State.OOMKilled`, and removes labelled stale containers at startup.

### 6. Server-side C# harness

The C# runner generates a console project, writes the submitted `.cs` files plus the trusted harness into a per-attempt workspace, and runs `dotnet build` then the produced assembly. The harness is looked up by challenge slug and language through `IChallengeExecutionSource`; the client never sends it.

The harness manifest field is optional and additive within schema version 1. A language without a valid harness has `canExecute: false` and cannot be submitted.

### 7. Fail closed

If Docker is disabled, missing, or unreachable; if the language or harness is unavailable; or if the runner cannot enforce the required controls, the attempt is persisted as unavailable. The API continues to serve the rest of its surface.

### 8. Restart recovery

Queued identifiers exist only in memory and are lost on shutdown. At startup, `SubmissionRecoveryService` marks stale `Queued` and `Running` rows as `Interrupted` with a completion timestamp. Attempts are not silently re-run, because re-running untrusted code without the learner's intent is worse than reporting the interruption.

## Alternatives considered

### Execute synchronously inside the POST request

Rejected. Container startup plus build plus run can take many seconds. A synchronous request would tie up a request thread, complicate cancellation, and provide no queue or concurrency control. It would also make the API's liveness depend on Docker.

### Reuse a single long-lived container per language

Rejected. A shared container weakens isolation between submissions, complicates cleanup, and allows one submission to affect another through leftover state. Short-lived per-submission containers are simpler to reason about.

### A durable queue (database table, broker, or job library)

Rejected for Phase 4. It adds operational and semantic complexity disproportionate to a local single-user product. The in-memory queue plus `Interrupted` recovery is honest about what happens on restart.

### Build and run in two containers

Rejected. The build produces the executable, and passing it between containers would add a shared writable volume or artifact channel. One container keeps the mount read-only and cleanup simple. The tradeoff is that the SDK image is larger and the compile step shares the container budget.

### Let the client specify image, command, or harness

Rejected. Those are trust-boundary inputs. Accepting them would let a client run arbitrary commands or exfiltrate server content.

### A dedicated runner service or remote sandbox

Rejected for Phase 4. It would add network, authentication, and deployment concerns to a local product. The `IDockerEngine` and `ILanguageRunner` boundaries keep the door open for a later remote implementation.

## Consequences

### Positive

- Submitted code never runs in the API process.
- The client has no path to image, command, mount, environment, or harness selection.
- The request path stays responsive and is protected by a bounded queue and bounded concurrency.
- The database never holds a transaction open during execution.
- Docker absence degrades cleanly to `ExecutionUnavailable`.
- Harness content is structurally absent from client contracts.
- Runner and Docker behavior can be tested without a live daemon.

### Negative

- Queued work is lost on shutdown and appears as `Interrupted` after restart.
- The system is single-process; a second API instance would not share the queue.
- The full SDK image is large and includes a compiler.
- A per-submission container adds startup latency.
- The harness is a fixed diagnostic run, not assessment, so the learner gets output but no score.
- Docker is not an absolute isolation boundary.

### Mitigations

- Document the queue, recovery, and Docker controls in [EXECUTION_SECURITY.md](../EXECUTION_SECURITY.md).
- Enforce server-side validation in both the submission service and the runner.
- Test validation, harness injection, Docker flags, outcome mapping, cleanup, and recovery with the automated suite.
- Treat live Docker execution as an operational check and state clearly that the automated suite does not start real containers.
- Keep `IDockerEngine` and `ILanguageRunner` narrow so a stronger or remote implementation can replace the current one.
