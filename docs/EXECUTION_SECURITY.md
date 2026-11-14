# PSEL Execution Security

## 1. Scope

Phase 8 executes the same untrusted submitted C#, Python, and SQL content against server-selected public and hidden tests, including during examinations. This document describes the implemented boundary, the server-controlled controls, the fail-closed behavior, and the limits of container isolation so the project does not make false security claims.

Nothing in this document authorizes executing submitted code inside the ASP.NET Core process. The only execution path is a short-lived Docker container created by `Psel.Modules.Execution`.

## 2. Threat model

Submitted code is untrusted input. It may attempt to:

- Read or modify application files, databases, or secrets.
- Access the host filesystem or user files.
- Contact the network or exfiltrate data.
- Consume unbounded CPU, memory, processes, or disk.
- Produce unbounded output.
- Escape the intended execution environment through runtime or kernel vulnerabilities.
- Interfere with other submissions or the application process.

The application must assume that submitted code is hostile even when the learner is the local machine owner.

The server-side execution harness is trusted authoring content, while the submitted files are untrusted. Both are compiled or interpreted together inside the container. C# and Python packages provide their own harness; SQL packages use a server-built harness and cannot replace it. A harness is never accepted from the client and is never returned by a client API. Public and hidden test data are copied into the container by the server; hidden test names, descriptions, identifiers, and data are never returned to the browser. SQL schema and seed SQL are also server content, injected into the execution test manifest.

## 3. Non-negotiable rules

1. **Never execute submitted code inside the ASP.NET Core process.**
2. **Never execute submitted code with the application's normal OS privileges.**
3. **Never expose the Docker socket to submitted code.**
4. **Never accept arbitrary images, commands, mounts, environment variables, or runner arguments from the client.**
5. **Never expose hidden tests, reference solutions, or execution harnesses to the browser.**
6. **Never let the client choose which tests run, and never accept the server-reserved runner filenames.**
7. **Always clean up containers and workspaces after success, failure, timeout, and cancellation.**
8. **Never mount or attach the application database to an execution container; SQL challenges use a disposable in-memory database owned by the server harness.**

## 4. Implemented pipeline

```text
POST /api/submissions  { purpose: Run | Submit }
        │  validate challenge, language, harness, files, limits
        │  select public tests (Run) or public + hidden tests (Submit)
        ▼
Submission row (Queued) ──► bounded in-memory queue (Channel<Guid>)
        │
        ▼
SubmissionWorker (bounded concurrency, one DB scope per attempt)
        │  trust only the harness and test plan from IChallengeExecutionSource
        ▼
ILanguageRunner (CSharpDockerRunner, PythonDockerRunner, or SqlDockerRunner)
        │  writes the language harness, PselTests.json, and submitted files
        ▼
ProcessDockerEngine ──► docker run (isolated, short-lived container)
        │
        ▼
DockerRunResult ──► ExecutionOutcome ──► AssessmentGrader ──► Assessment + Submission row (terminal)
```

Audit the execution boundary in this order: the submission API validates client content, the worker selects trusted harness and tests, the runner writes a minimal workspace, and Docker applies the same isolation flags regardless of language. SQL adds a second internal boundary: the trusted harness confines the learner query to a disposable in-memory database.

### SQL execution

SQL packages declare `sql.schema` and an optional `sql.seed`. The loader reads both and merges them into each selected test's server-only `DataJson`; they are never part of `ChallengeDetail`. SQL packages cannot supply a harness, and an author-supplied `sql` harness is rejected at startup.

`SqlDockerRunner` accepts exactly one `.sql` file and writes:

- the learner query as `query.sql`;
- the server-owned `PselHarness.py`;
- the server-selected `PselTests.json` containing schema, seed, expectations, and optional plan constraints.

The harness creates a fresh `sqlite3.connect(":memory:")` connection per test, runs the trusted schema and seed, enables `PRAGMA query_only`, installs an authorizer that permits only `SQLITE_SELECT`, `SQLITE_READ`, `SQLITE_FUNCTION`, and `SQLITE_RECURSIVE`, and then executes the learner query. It also:

- rejects a query whose first keyword is not `SELECT` or `WITH`;
- caps results at 1000 rows and 64 columns;
- aborts a query after 5 seconds of wall-clock time;
- checks `EXPLAIN QUERY PLAN` only when a test declares `plan` constraints;
- emits `PSEL_SQL_COMPILE_ERROR` for syntax errors, incomplete statements, unrecognized tokens, and read-only violations, which the runner maps to `CompilationError`.

The application's SQLite database is never mounted into the container and is never opened by the harness. Even if the query-only controls were bypassed, the container has no path to application data.

### Examination sessions

Examinations reuse the same pipeline. `POST /api/examinations` validates that the challenge is executable, defines hidden tests, and declares at least one variant. The server selects a variant and time limit and creates an `ExaminationSession`; the client cannot request a specific variant or limit.

A submission carrying `examinationId` is checked by `IExaminationPolicy` before it is queued. The session must exist, be `InProgress`, not be expired, and target the same challenge. The purpose must be `Submit`; `Run` is rejected because practice runs do not produce examination evidence. The attempt link is recorded with the submission.

The examination detail response embeds the normal client challenge projection with `HintsMarkdown` removed. Hidden test names, descriptions, identifiers, data, reference solutions, and harness source remain server-side, exactly as in a practice `Submit`. After the session is completed or expires, submissions are rejected; the server does not rely on the browser to enforce the limit.

## 5. Server-controlled configuration

The following values come from `ExecutionOptions` (configuration section `Execution`) and are never part of any request contract:

| Control | Default | Meaning |
|---|---|---|
| `Enabled` | `true` | Master switch; when false, execution fails closed as unavailable |
| `DockerExecutable` | `docker` | Docker CLI path |
| `CSharpImage` | `mcr.microsoft.com/dotnet/sdk:10.0` | Image the C# runner may use |
| `PythonImage` | `python:3.13-slim` | Image the Python runner may use |
| `SqlImage` | `python:3.13-slim` | Image the SQL runner may use; includes the SQLite module |
| `TimeLimitSeconds` | `60` | Total wall-clock budget including compilation, restore, or interpreter startup |
| `MemoryLimitMb` | `512` | Container memory and swap cap |
| `CpuLimit` | `1.0` | CPU quota |
| `PidsLimit` | `128` | Process-count limit |
| `MaxOutputBytes` | `65536` | Combined captured stdout/stderr budget |
| `MaxConcurrentExecutions` | `2` | Worker concurrency |
| `MaxQueuedExecutions` | `64` | Bounded queue capacity |
| `ContainerUser` | `1000:1000` | Non-root container user |

Pull the language images before execution so a run does not depend on registry access at execution time; the container itself has no network, so it cannot restore additional packages. A missing image or failed pull surfaces as a container start failure, not as a learner result.

## 6. Implemented Docker controls

`ProcessDockerEngine.BuildRunArguments` applies these flags to every run:

- `--network none` — no network interface beyond loopback inside the container.
- `--memory` and `--memory-swap` — equal values disable swap for the container.
- `--cpus` — CPU quota from `CpuLimit`.
- `--pids-limit` — process-count limit.
- `--read-only` — the container root filesystem is read-only.
- `--tmpfs /tmp:rw,nosuid,nodev,size=256m` — writable scratch space for build output, package caches, Python bytecode cache, and the `HOME` directory, without executable or device access.
- `--cap-drop ALL` — all Linux capabilities dropped.
- `--security-opt no-new-privileges` — privilege escalation through setuid/setgid is blocked.
- `--user 1000:1000` — non-root container user.
- `--volume <workspace>:/workspace:ro` — the generated workspace is mounted read-only.
- `--workdir /workspace` — the command runs from the mounted workspace.
- `--env` — only server-controlled variables: C# build variables (`DOTNET_CLI_HOME`, `NUGET_PACKAGES`, telemetry opt-out, `HOME`), Python variables (`PYTHONDONTWRITEBYTECODE`, `PYTHONPYCACHEPREFIX`, `PYTHONUNBUFFERED`, `PYTHONIOENCODING`, `HOME`), or the same Python variables for the SQL harness.
- Server-controlled image, `/bin/sh -c`, and a fixed per-language command: a C# build-and-run command, or `python3 -B /workspace/PselHarness.py` for Python and SQL.

The Docker socket is never mounted. There is no `--privileged`, `--pid host`, `--network host`, or host-directory mount. The submission workspace contains only the generated language scaffolding (the C# project file when applicable), the server harness, the server-written `PselTests.json` manifest, and the validated submitted files. Submitted files named `Submission.csproj`, `PselHarness.cs`, `PselHarness.py`, or `PselTests.json` are rejected before the runner starts.

## 7. Fail-closed behavior

Execution refuses to proceed, returning `ExecutionUnavailable` rather than running with weaker controls, when:

- execution is disabled by configuration;
- the Docker engine is not reachable or the CLI is missing;
- the configured image cannot start a container;
- the challenge/language combination has no server-side harness;
- the selected purpose has no tests to run;
- the runner accepts only a subset of files and a submitted file is rejected.

The runner validates paths again immediately before writing the workspace. An invalid path becomes `InternalError` and no container is started. Invalid submission requests are rejected with `400` before they are queued.

The worker catches runner exceptions and persists `InternalError`. A runner crash is treated as a server fault, not a learner failure.

## 8. Output handling

- Standard output and standard error share a bounded byte budget (`MaxOutputBytes`).
- Exceeding the budget kills the process tree and records `outputTruncated`; the outcome becomes `OutputLimitExceeded`.
- Timeouts kill the process tree and map to `TimeLimitExceeded`.
- Container OOM (`State.OOMKilled`) maps to `MemoryLimitExceeded`.
- Non-zero exits are mapped to `CompilationError` when compiler or interpreter diagnostics are present, otherwise `RuntimeError`. For C# the markers are `error CS`, `Build FAILED`, and MSBUILD errors; for Python they are `SyntaxError`, `IndentationError`, and `TabError`.
- Output is stored and returned as plain text. The frontend renders it as text, never as HTML.
- The harness result block is delimited by `__PSEL_RESULTS_BEGIN__` and `__PSEL_RESULTS_END__`. The assessment module parses it and removes the entire block before output is persisted or returned.
- A missing, duplicated, incomplete, or malformed result block becomes `InternalRunnerError` and no partial score is invented.
- Submission logs record identifiers and outcomes, not submitted source text.

## 9. Cleanup

- The runner deletes its per-submission workspace in a `finally` block.
- `ProcessDockerEngine` runs `docker rm -f` after every attempt, including normal completion, timeout, cancellation, and start failure.
- Containers are labelled `psel.execution=true`.
- At startup the engine removes labelled containers left behind by a previous process, which covers host crashes and forced termination.
- The workspace root is the OS temporary directory unless `WorkspaceRoot` is configured.

## 10. Restart and cancellation

- Queued attempts live only in memory. They are lost on shutdown and are not silently retried.
- At startup, `SubmissionRecoveryService` marks stale `Queued` and `Running` rows as `Interrupted`.
- Worker cancellation during shutdown aborts execution and persists the interrupted/cancelled outcome. Terminal rows are never reopened.

## 11. Known limitations

Docker provides useful process and filesystem isolation, but it is **not an absolute security boundary**. Container escapes, kernel vulnerabilities, shared-kernel issues, and misconfiguration are possible. Running Docker Desktop on a developer workstation also means the host kernel and Docker daemon remain part of the trusted computing base.

Specific Phase 8 limitations:

- The queue is in-memory and single-process; it is not durable and does not survive a restart.
- The C# runtime image is a full SDK image. It is large and includes a compiler, which enlarges the attack surface relative to a minimal runtime image.
- The Python and SQL images are slim interpreter images with no optional packages. Executable Python and SQL challenges must use only the standard library because the container has no network and no package installation step.
- SQL challenges use SQLite syntax and semantics. The harness does not emulate MySQL or PostgreSQL, and it does not load extensions.
- The SQL harness reads the server-written test manifest, which includes schema, seed, and hidden expectations. The learner supplies only SQL, so it cannot read workspace files; `ATTACH` and extension loading are not available under the authorizer and Python's default SQLite configuration. Hidden data still never reaches the API.
- SQL statement time is capped per test, and rows/columns are capped, but a query that produces large intermediate results can still consume the container's memory or CPU budget until the outer limits stop it.
- Execution output can still reveal information the submitted program chooses to print. The harness is fixed, but it runs the learner's code.
- Test data is visible to submitted code at runtime through `/workspace/PselTests.json`, because each harness must read it. A determined submission can inspect test inputs and attempt to special-case them. Hidden test names and descriptions are still never returned to the client, and the server does not accept test selection from the client.
- The Docker daemon itself runs with host privileges. A compromised daemon is outside the isolation boundary this project provides.
- No authentication or per-user isolation exists. The API is for the local machine only.
- Examination time limits are enforced by the server when a session is created, read, or used for a submission. The client countdown is display-only; a delayed or manipulated browser clock cannot extend the server-side limit. There is no background job that expires idle sessions, so expiry is observed lazily on the next read or attempt.

The project must not claim that arbitrary code execution is perfectly safe. Stronger isolation technologies such as gVisor or Firecracker may be evaluated in a later hardening phase if the threat model requires them.

## 12. Local-first implications

- The application binds to loopback only and has no authentication in Phase 8.
- Execution is intended for the learner's own local machine.
- Do not expose the API or the execution service to an untrusted network.
- If remote or multi-user operation is ever required, authentication, authorization, tenant isolation, and a stronger execution boundary must be designed first.

## 13. Verification performed

The automated suite runs without a live Docker daemon:

- Docker argument tests assert the exact isolation flags, read-only mount, non-root user, environment variables, and the absence of privileged or socket access.
- C#, Python, and SQL runner contract tests use a recording fake engine to verify workspace generation, harness and test-manifest injection, per-language image/command/environment, path rejection before Docker is invoked, cleanup, and outcome mapping.
- Submission endpoint tests verify validation, harness injection, adapter selection by language, terminal results, unavailable execution, reserved filename rejection, and non-disclosure of harness content.
- SQL tests also verify the single-file limit, the built-in harness, schema/seed injection into server tests, query-plan validation, and that schema, seed, and hidden SQL data are absent from challenge responses.
- Assessment tests verify that Run selects only public tests, Submit selects public and hidden tests, hidden names and data remain server-side, scoring is deterministic, and every outcome is mapped.
- Challenge loader tests verify that harness content is absent from the serialized client projection, including Python and SQL harnesses.
- Recovery tests verify stale rows become `Interrupted`.
- Progress tests verify derived competency states and assessment history without exposing hidden test material.
- Examination tests verify server-selected variants, `Submit`-only attempts, challenge matching, expiry with a controllable clock, finish behavior, and the hint-free challenge projection.

Live C# container execution was also verified manually with Docker Desktop 29.9.2 and `mcr.microsoft.com/dotnet/sdk:10.0`:

- A correct `two-sum` solution reached `Succeeded` with `0,1` output.
- An unimplemented solution reached `RuntimeError` with exit code `134` and the captured exception.
- A malformed solution reached `CompilationError` with the compiler diagnostic.
- A probe submission confirmed that `/tmp` is writable, `/workspace` writes are blocked by the read-only mount, and outbound network access fails.
- No `psel.execution=true` container remained after the runs.

Live Python container execution was then verified manually with Docker Desktop 29.9.2 and `python:3.13-slim`:

- The `two-sum` reference solution reached `Succeeded` and `Accepted` with 4/4 tests, 2 public and 2 hidden.
- The unimplemented starter reached assessment `RuntimeError` with 0/4 tests while the harness continued through every test.
- A source file with an unclosed function header reached `CompilationError` and exposed the interpreter diagnostic.
- A C# `two-sum` reference submission still reached `Succeeded` and `Accepted` with 4/4 tests on the same running API.
- No `psel.execution=true` container or per-submission workspace remained after the runs.

The Python harnesses for all five executable challenges were also run locally with Python 3.12 against their reference solutions and passed every public and hidden test.

The SQL harness was validated directly with Python 3.12 and SQLite against the two repository SQL packages:

- The reference query passes result and column checks.
- The composite-index solution passes the `EXPLAIN QUERY PLAN` constraint.
- A query that wraps the indexed column in an expression still returns the right rows but fails the plan constraint, proving the performance check is active.
- A malformed statement is reported as `CompilationError` through `PSEL_SQL_COMPILE_ERROR`, and a read-only violation is rejected before execution.

Live SQL container execution was then verified with Docker Desktop 29.9.2, `python:3.13-slim`, and the running API:

- The `order-revenue` reference query reached `Succeeded` and `Accepted` with 5/5 tests, 2 public and 3 hidden.
- The `order-index-plan` reference query reached `Succeeded` and `Accepted` with 4/4 tests, including the index and no-scan plan assertions.
- A query that returned the right rows but wrapped the indexed column in an expression reached `WrongAnswer` with 0/4 tests, confirming the plan check cannot be bypassed by correct rows alone.
- A non-read-only statement reached `CompilationError` with `PSEL_SQL_COMPILE_ERROR`.

The live checks are operational verification, not part of the automated suite; the suite itself still uses a fake Docker engine so it can run anywhere.

## 14. Related records

- [ADR-0002: Code execution isolation](decisions/ADR-0002-code-execution-isolation.md)
- [ADR-0006: Submission queue and Docker runner](decisions/ADR-0006-submission-queue-and-docker-runner.md)
- [ADR-0007: Deterministic assessment](decisions/ADR-0007-deterministic-assessment.md)
- [ADR-0008: Python runtime](decisions/ADR-0008-python-runtime.md)
- [ADR-0009: Evidence-based progress and examinations](decisions/ADR-0009-evidence-based-progress-and-examinations.md)
- [ADR-0010: SQL execution and SQLite isolation](decisions/ADR-0010-sql-execution-and-sqlite-isolation.md)
- [PROJECT_SPEC.md](../PROJECT_SPEC.md), sections 10 and 13
