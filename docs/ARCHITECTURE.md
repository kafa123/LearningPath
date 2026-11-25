# PSEL Architecture

## 1. Context

PSEL is a local-first learning and assessment platform. The core learning and grading workflow must work without an AI service. Submitted code is untrusted and must never execute inside the ASP.NET Core process or with the application's normal operating-system privileges.

This document describes the Phase 8 architecture and the boundaries that later phases must preserve.

## 2. Architecture style: modular monolith

The application is a **modular monolith**, not a set of microservices. This keeps local deployment simple while preserving explicit domain boundaries.

### Phase 8 physical structure

```text
Psel.Api ──────────► Psel.Infrastructure
    │                       │
    │                       └── EF Core / SQLite (learning + submissions + assessment + progress)
    ├──────────► Psel.Modules.Learning
    │                       └── domain, EF configs, seed, endpoints
    ├──────────► Psel.Modules.Challenges
    │                       └── contracts, strict loader, catalog, harness/test source, endpoints
    ├──────────► Psel.Modules.Execution
    │                       └── Docker engine boundary, C#, Python, SQL, and Go runners, test manifest, limits, cleanup
    ├──────────► Psel.Modules.Assessment
    │                       └── deterministic grader, assessment entities, result endpoint
    ├──────────► Psel.Modules.Submissions
    │                       └── submission entity, validation, queue, worker, endpoints
    └──────────► Psel.Modules.Progress
                            └── competency aggregation, assessment history, examination sessions
```

- `Psel.Api` is the composition root. It owns hosting, middleware, configuration, and endpoint mapping.
- `Psel.Infrastructure` owns persistence concerns: the EF Core `DbContext`, migrations, database initialization, and the module assembly registry.
- `Psel.Modules.Learning` owns the learning domain entities, EF Core configurations, seed catalog, and HTTP endpoints.
- `Psel.Modules.Challenges` owns challenge package contracts, filesystem discovery and validation, the in-memory catalog, server-only harness lookup, and challenge HTTP endpoints. It has no EF entities and does not reference `Psel.Infrastructure`.
- `Psel.Modules.Execution` owns the language-runner contract, Docker process boundary, one runner per supported language (C#, Python, SQL, and Go), execution limits, and cleanup. It depends on no other module.
- `Psel.Modules.Submissions` owns the submission entity and EF configuration, validation against trusted challenge and language configuration, the in-memory queue, the execution worker, recovery on restart, and submission HTTP endpoints. It references infrastructure, assessment, challenges, and execution.
- `Psel.Modules.Assessment` owns deterministic grading, assessment and per-test evidence entities, their EF configurations, and the read-only result endpoint. It references infrastructure and the execution status contract. It does not reference submissions or challenges.
- `Psel.Modules.Progress` owns read-side competency aggregation, assessment history, and examination sessions. It reads the shared `PselDbContext` and challenge catalog, and exposes `IExaminationPolicy` as a narrow contract for submissions. It does not reach into submission types or expose hidden material.
- The learning, submissions, assessment, and progress modules reference `Psel.Infrastructure` for the shared `PselDbContext`; infrastructure does not reference any module.
- There is no reverse dependency from infrastructure or modules to the host.

### Module evolution

The specification suggests these modules:

- Learning
- Challenges
- Submissions
- Execution
- Assessment
- Progress
- Languages

Phases 2–8 introduced six physical modules:

```text
src/Modules/Psel.Modules.Learning/
  Contracts/        Client-facing DTOs
  Domain/           LearningDomain, LearningModule, material, objective, competency, prerequisite
  Endpoints/        /api/domains, /api/learning-paths, /api/modules, /api/materials
  Persistence/      Module-owned EF Core configurations
  Seed/             Initial catalog and upsert hosted-service synchronizer
  LearningModuleRegistration.cs

src/Modules/Psel.Modules.Challenges/
  Contracts/        ChallengeSummary, ChallengeDetail, and related client-facing records
  Endpoints/        /api/challenges, /api/challenges/{slug}
  Packages/         Manifest model, path resolver, strict loader, catalog, harness lookup, startup initializer
  ChallengeModuleRegistration.cs

src/Modules/Psel.Modules.Execution/
  Docker/           Narrow Docker process boundary and isolation flags
  Runners/          C#, Python, SQL, and Go Docker runners plus shared workspace, manifest, and outcome helpers
  ExecutionOptions.cs
  ExecutionFileRules.cs
  ILanguageRunner.cs
  LanguageCatalog.cs
  ExecutionModuleRegistration.cs

src/Modules/Psel.Modules.Assessment/
  Contracts/        Assessment input and permitted result DTOs
  Domain/           Assessment, per-test evidence, outcome, visibility, status
  Endpoints/        /api/submissions/{id}/results
  Persistence/      Assessment EF Core configurations
  Services/         Result protocol parser and deterministic grader
  AssessmentModuleRegistration.cs

src/Modules/Psel.Modules.Submissions/
  Contracts/        Submission request and result DTOs
  Domain/           Submission entity and status transitions
  Endpoints/        /api/submissions
  Persistence/      Submission EF Core configuration
  Services/         Validation, bounded queue, worker, restart recovery
  SubmissionModuleRegistration.cs

src/Modules/Psel.Modules.Progress/
  Contracts/        Progress, assessment history, and examination DTOs; IExaminationPolicy
  Domain/           ExaminationSession, ExaminationAttempt, ExaminationStatus
  Endpoints/        /api/progress, /api/assessments, /api/examinations
  Persistence/      Examination session and attempt EF Core configurations
  ProgressService.cs
  ExaminationService.cs
  ProgressModuleRegistration.cs
```

The challenge module is filesystem-backed. It has no database tables and contributes no EF configurations. It exposes harness content only through the trusted `IChallengeExecutionSource` interface, keyed by challenge slug and language ID; the harness is not part of the client-facing `ChallengeDetail` record.

Each module exposes composition methods such as:

```csharp
public static class LearningModuleRegistration
{
    public static Assembly ModelAssembly { get; }
    public static IServiceCollection AddLearningModule(this IServiceCollection services);
    public static IEndpointRouteBuilder MapLearningEndpoints(this IEndpointRouteBuilder endpoints);
}
```

The API host references modules and calls those methods. Modules must not depend on the API host. Cross-module calls should go through explicit application interfaces rather than directly reaching into another module's persistence. The challenge module exposes `AddChallengesModule` and `MapChallengeEndpoints` and is composed by the host in the same way as the learning module.

The host passes `LearningModuleRegistration.ModelAssembly`, `SubmissionModuleRegistration.ModelAssembly`, `AssessmentModuleRegistration.ModelAssembly`, and `ProgressModuleRegistration.ModelAssembly` to `AddInfrastructure`. `ModuleAssemblyRegistry` carries that list to `PselDbContext`, which applies configurations from each registered module assembly. This keeps `Psel.Infrastructure` independent of concrete modules while preserving one shared model and transaction boundary.

Creating the remaining module projects before they contain real behavior would add build and navigation overhead without enforcing meaningful boundaries. Each boundary becomes physical when its phase begins.

### Why not microservices?

- The product is local-first and single-user.
- Distributed deployment, service discovery, and network failure modes add no learning value in the current product stage.
- A modular monolith keeps transactions and local development simple.
- Modules can be extracted later if a documented need appears.

## 3. Backend layers and responsibilities

### `Psel.Api`

- Application startup and dependency composition.
- Configuration binding and validation.
- HTTP middleware and ProblemDetails error handling.
- Development-only OpenAPI document.
- Health endpoint.
- Binding to `localhost` only.
- No persistence implementation details beyond registering infrastructure services.

### `Psel.Infrastructure`

- `PselDbContext`.
- EF Core model configuration.
- SQLite provider setup.
- Migrations.
- Startup migration application.
- Database health check integration.
- `ModuleAssemblyRegistry`, which lets the host contribute module EF configurations without infrastructure referencing those modules.

### `Psel.Modules.Learning`

- Learning domain entities and invariants.
- Module-owned EF Core configurations.
- Seed catalog and upsert startup synchronizer.
- JSON content-pack import that reuses the same upsert path, validates references and parent moves, and never deletes rows.
- Read-only HTTP endpoints and client-facing contracts.
- No dependency on the API host or on other modules.

### `Psel.Modules.Challenges`

- Versioned challenge package contract (`schemaVersion: 1`).
- Recursive filesystem discovery under `Challenges:Path`.
- Strict manifest parsing: unknown fields, comments, and trailing commas are rejected.
- Path traversal protection and per-file size limits.
- Optional `sql` block that points at a schema file and an optional seed file. The loader reads both into server-only test data, validates result-row and query-plan expectations, and validates that `sql` and the `sql` language are declared together.
- SQL packages cannot supply their own harness; the loader assigns the built-in harness so an author cannot weaken the execution boundary.
- Non-disclosure boundary: hidden test files are existence-checked, but hidden content and reference solutions are never read into API response models. Solutions are excluded structurally because the manifest has no solution reference. SQL schema and seed content are injected into the server test plan and never appear in `ChallengeDetail`.
- In-memory catalog loaded at startup and swapped atomically after an authoring upload; invalid packages fail startup.
- Local authoring endpoint that extracts an upload to a temporary directory, validates it with the startup loader, and only then installs it under `Challenges:Path` with backup and rollback.
- Read-only HTTP endpoints and client-facing contracts.
- No database access, no code execution, and no dependency on the API host.

### `Psel.Modules.Execution`

- `ILanguageRunner` abstraction with one Docker implementation per supported language (`CSharpDockerRunner`, `PythonDockerRunner`, `SqlDockerRunner`, and `GoDockerRunner`).
- Server-configured image per language, command, environment, limits, and workspace root.
- A per-language submission file limit: C# and Python accept multiple source files, while SQL accepts exactly one query file.
- Narrow `IDockerEngine` boundary so runners can be tested without a live daemon.
- Docker flags: network disabled, read-only container filesystem, tmpfs `/tmp`, dropped capabilities, no-new-privileges, non-root user, read-only workspace mount, memory/swap, CPU, process, and output limits.
- Explicit container removal after success, failure, timeout, cancellation, and startup cleanup of stale labelled containers.
- Structured `ExecutionOutcome` mapping for success, compilation error, runtime error, time limit, memory limit, output limit, unavailable execution, cancellation, and internal error.
- Shared runner helpers for workspace creation, test-manifest writing, and Docker outcome mapping, so language adapters differ only in image, command, environment, accepted extensions, and diagnostic detection.
- No dependency on submissions or the API host.

### `Psel.Modules.Submissions`

- Append-only `Submission` entity with explicit status transitions and a `Run`/`Submit` purpose.
- Request validation against the trusted challenge catalog: challenge exists, language belongs to the challenge and is executable, the server supports the runner, file paths are safe, extensions are accepted, reserved runner filenames are rejected, and source-size limits are enforced.
- Bounded in-memory queue of submission identifiers; back-pressure returns `503` instead of growing without limit.
- Background worker with configurable concurrency, one database scope per execution, and short transactions around state changes.
- Server-side test selection: `Run` selects public tests, `Submit` selects public and hidden tests. Clients cannot select tests.
- Restart recovery marks stale queued/running rows as `Interrupted`.
- Submission HTTP endpoints and client-facing contracts. Submitted source and harness content are never returned in the result payload.
- Advisory rubric reviews for terminal submissions. `RubricReview` and `RubricCriterionScore` are owned by this module; points and note come from the learner, while score, band, and criterion metadata come from the trusted package rubric. Reviews are separate from assessment evidence and never mutate it.

### `Psel.Modules.Assessment`

- Deterministic grader independent of HTTP, Docker, and database access.
- Equal weight per selected test; integer scoring `passed * 100 / total`; non-successful execution scores zero.
- Outcome mapping: `Accepted`, `WrongAnswer`, `CompilationError`, `RuntimeError`, `TimeLimitExceeded`, `MemoryLimitExceeded`, `OutputLimitExceeded`, `InternalRunnerError`.
- Protocol parser reads the harness result block; the block is stripped before execution output is persisted for the client.
- Append-only `Assessment` and `AssessmentTestResult` entities.
- Read-only projection filters out hidden test names, descriptions, identifiers, and data; only hidden pass and total counts are exposed.

### `Psel.Modules.Progress`

- Read-side competency aggregation over seeded learning content, challenge `competencySlugs`, and append-only assessment evidence. No progress row is written; state is derived on every read so it cannot drift.
- Assessment history with examination linkage, newest first.
- Examination sessions that choose a variant and time limit from the challenge manifest, enforce expiry and completion, and record attempt links.
- `IExaminationPolicy` is the only cross-module surface used by submissions. It answers whether an attempt is allowed and records the link without leaking submission details into Progress.
- Client examination responses embed the normal `ChallengeDetail` with `HintsMarkdown` removed, and never include hidden tests, reference solutions, or harnesses.
- SQLite cannot order by a `DateTimeOffset` column, so assessment and examination list endpoints materialize rows and order the newest window in memory. The local dataset is small enough for this to remain cheap and deterministic.

### Module evolution rules

Each module owns its domain rules, application services, endpoint definitions, and persistence configuration. Later phases must build on the assessment and examination contracts rather than moving hidden material into client contracts or reading another module's persistence types.

## 4. Database and persistence

- SQLite is the local persistent store.
- A single `PselDbContext` remains the transaction boundary. Module-specific entity configurations are contributed through `ModuleAssemblyRegistry` and applied during model creation.
- The database path is configuration-driven, defaulting to `data/psel.db` under the API content root.
- Migrations are applied at startup for the local single-user scenario. This is not a multi-instance deployment strategy and is documented as such.
- Learning content is synchronized by an `IHostedService` registered after database migration. The synchronizer matches domains, modules, materials, and competencies by slug and objectives by module plus sort order. It inserts missing rows, updates changed scalar values, and re-creates missing prerequisite links without deleting learner data or rows removed from the seed. The authoring import builds the same seed graph and calls the same synchronizer after validating the whole pack.
- Challenge packages are not stored in the database. A hosted service validates the filesystem catalog after startup; the catalog is held in memory and served read-only. Uploads validate a temporary copy with the same loader, install it under `challenges/<domainSlug>/<slug>`, then swap the in-memory snapshot without restarting the process.
- Challenge package content is trusted authoring input, not user input, but still passes strict schema, path, size, and uniqueness validation.
- Submission history is append-only: a new attempt creates a new row, and previous attempts are never overwritten. Status and output fields are updated only within the lifecycle of that attempt.
- The submission worker does not hold a database transaction open while code executes. It marks the row running, releases the transaction, runs the container, then persists the terminal outcome and its assessment together.
- Assessment and per-test evidence are append-only and belong to exactly one submission.
- Rubric reviews are advisory and separate from assessments: at most one review per terminal submission, with one criterion-score snapshot per rubric criterion. Criterion title, maximum points, and weight are copied at save time so historical scores stay explainable; a unique index on `(RubricReviewId, SortOrder)` preserves criterion order.
- Examination sessions and their attempt links are append-only from the learner's perspective: a session transitions from `InProgress` to `Completed` or `Expired`, and attempts are never rewritten.
- Progress itself is not stored. It is derived from assessments and examination attempts at read time.
- Hidden tests, reference solutions, and execution harnesses are never exposed through client APIs.

See [DATABASE_DESIGN.md](DATABASE_DESIGN.md).

## 5. Frontend

- React + TypeScript application built by Vite.
- Tailwind CSS for styling.
- React Router for the application shell.
- `features/` contains feature-specific UI; `shared/` contains reusable API and UI utilities.
- The frontend talks to the backend through typed HTTP functions.
- In development, Vite proxies `/api` to `http://localhost:5080`. This avoids an open CORS policy.
- Phase 2 adds nested learning routes for domains, modules, and Markdown materials. Markdown is rendered by `react-markdown` with GitHub-flavored Markdown support and without raw HTML.
- Phase 3 adds challenge browsing with client-side filters, a challenge workspace with statement, hints, examples, constraints, and public-test summaries, and language/file tabs with per-file drafts.
- Phase 4 enables Run for languages marked `canExecute`, posts the active language's files to the submission API, polls the submission until it is terminal, and renders the structured status, output, diagnostics, duration, and exit code.
- Phase 5 enables Submit. The workspace sends `purpose: "Run"` or `purpose: "Submit"`, then loads the assessment result after the submission becomes terminal and renders the outcome, score, public/hidden counts, and public per-test statuses. Hidden test names and data are never rendered because the API does not return them.
- Phase 6 enables the same Run and Submit workflow for any challenge language marked `canExecute`, including Python. Language switching keeps per-language drafts, and the request carries the active language identifier and its own starter files. No client code selects an image, command, harness, or test set.
- Phase 7 adds the progress dashboard, assessment history, and examination workspace. The dashboard summarizes mastery, pass rate, assessment readiness, and activity streak, recommends the next competency, shows per-domain progress, recent evidence, the module to continue, focus areas, an active-examination banner, and local lab health. The examination workspace is reused in examination mode with hints removed, Run hidden, Submit labelled for assessment, and submissions disabled once the session is completed or expired. A countdown is rendered from the server-supplied expiry, but the server remains authoritative near submission time.
- Phase 8 adds SQL challenges through the existing workspace. The SQL language supplies its own editor language and starter `query.sql`, and Run/Submit use the same request, polling, and assessment components. No SQL-specific UI or client-selected schema is introduced.
- Phase 8 also completes the Practice route. It combines the challenge catalog, progress states, and recent assessments into a practice-ready list, recommends challenges whose mapped competencies need attention, and links each card to the existing workspace. Read-only packages stay in the full Challenges catalog instead of appearing as practice-ready.
- The post-MVP Phase 9 Settings page is read-only. It reuses the health hook for API status and loads `GET /api/runtime` for execution languages, images, and limits. The endpoint projects only server-owned diagnostics; Docker executable paths, workspace roots, container users, and database locations are not part of the contract. Configuration changes still happen through `appsettings` or environment variables and require a restart.
- Phase 10 adds a rubric self-review panel after a terminal submission when the challenge detail carries a rubric. The panel loads any existing review, submits one point value per criterion plus an optional note, and shows the server-computed weighted score and band only after saving. Criterion titles, maximum points, and weights are read from the challenge detail; the client never computes the final score.
- Local authoring adds an Authoring page with two upload forms: a JSON content pack for learning upserts and a ZIP challenge package with an explicit replace option. It renders only server results and validation messages; no filesystem path or hidden material is displayed.
- The client supports dark and light themes through a `ThemeProvider` that writes `data-theme` to the document root and persists the choice in `localStorage`. The light palette is implemented by remapping Tailwind color tokens under `html[data-theme='light']`, so the existing components do not need dark/light variants. Monaco registers a matching light editor theme, and an inline script in `index.html` applies the stored theme before first paint to avoid a flash.
- The workspace lazy-loads Monaco Editor. Monaco, its workers, and its theme are bundled locally rather than fetched from a CDN; the initial catalog bundle does not include the editor.
- Per-file drafts live in component state and are mirrored to `localStorage` per challenge so unsent edits survive reloads and navigation. The workspace restores the most recent saved attempt for the active challenge language through `GET /api/submissions/latest`, including its source files and, when terminal, its last assessment. Examination workspaces do not restore practice drafts. Execution output and assessment results are rendered as text, never as HTML.
- Production hosting is intentionally deferred; the Phase 8 exit criterion is that SQL challenges execute against isolated databases without the API ever exposing or opening its own database for learner queries.

The frontend must never receive hidden tests, reference solutions, harness source, internal file paths, runner commands, or sensitive configuration.

## 6. Health endpoint flow

```text
Browser / operator
      │ GET /api/health
      ▼
ASP.NET Core health checks
      │
      ├── database check (EF Core CanConnect)
      ▼
JSON status response
```

The response includes only status, timing, version, and check names/statuses. It must not include exception messages, connection strings, or file paths.

## 7. Execution boundary

Phase 8 extends the execution pipeline with a third language adapter while keeping assessment language-neutral:

```text
POST /api/submissions  { purpose: Run | Submit }
        │  validate challenge, language, files, and limits
        │  select public tests (Run) or public + hidden tests (Submit)
        ▼
Submission row (Queued) ──► bounded in-memory queue
        │
        ▼
SubmissionWorker (bounded concurrency, one DB scope per attempt)
        │  trust only server-side harness and test plan from IChallengeExecutionSource
        ▼
ILanguageRunner (C#, Python, SQL, or Go adapter selected by language)
        │  writes PselTests.json, the language harness, and submitted files
        ▼
IDockerEngine / Docker CLI
        │
        ▼
Short-lived isolated container ──► ExecutionOutcome
        │  harness emits opaque test observations
        ▼
AssessmentGrader ──► Assessment + test evidence ──► Submission row (terminal)
        │
        ▼
GET /api/submissions/{id}/results   (public projection only)
```

Rules that remain true:

- Submitted source code is never compiled or interpreted by the API process.
- The client cannot choose arbitrary images, commands, mounts, environment variables, or runner arguments.
- Challenge IDs and language IDs are validated against trusted server-side configuration.
- Containers run as a non-root user, without network access, with CPU/memory/process/time/output limits, a read-only workspace mount, and a per-submission workspace.
- The Docker socket is never exposed to submitted code.
- Cleanup runs after success, failure, timeout, and cancellation; labelled stale containers are removed at startup.
- If a runner cannot enforce the required controls or Docker is unavailable, execution fails closed with a structured status.
- The C# runner adds a server-generated project file and compiles in the container; the Python runner writes a server harness and executes it with a bytecode-disabled interpreter. Both share the same workspace, manifest, isolation flags, and outcome categories.
- Python syntax diagnostics map to `CompilationError`; non-zero interpreter exits map to `RuntimeError`. The assessment layer sees identical harness observations for both languages.
- The SQL runner accepts one `.sql` file and writes the built-in Python/SQLite harness. Schema, seed data, and expected rows stay inside `PselTests.json`; the learner query is executed per test against a fresh in-memory SQLite database. The application database is never mounted.
- SQL queries run under `PRAGMA query_only`, an authorizer that permits only read actions, a `SELECT`/`WITH` leading-keyword check, and row, column, and time caps. A syntax or read-only violation emits `PSEL_SQL_COMPILE_ERROR` and maps to `CompilationError`; a per-test semantic failure is a `RuntimeError` carried by the normal assessment protocol.

### SQL execution flow

```text
challenge package
  sql/schema.sql + sql/seed.sql ──► loader ──► per-test DataJson (server only)
  sql language without harness ───► loader assigns BuiltInSqlHarness.Source

POST /api/submissions { languageId: "sql", files: [query.sql] }
        │  validate one .sql file, executable language, server test plan
        ▼
SqlDockerRunner
        │  writes query.sql, PselHarness.py, PselTests.json
        ▼
isolated container: python3 -B /workspace/PselHarness.py
        │  per test: fresh :memory: SQLite
        │  schema → seed → PRAGMA query_only → authorizer → query
        ▼
row/column/ordering comparison + EXPLAIN QUERY PLAN constraints
        │
        ▼
__PSEL_RESULTS_* block ──► AssessmentGrader ──► Assessment
```

### Progress and examination flow

```text
POST /api/examinations { challengeSlug }
        │  validate executable challenge + hidden tests + variants
        │  server selects variant and time limit
        ▼
ExaminationSession (InProgress) ──► ExaminationSessions row

POST /api/submissions { purpose: Submit, examinationId }
        │  IExaminationPolicy validates session, challenge, and expiry
        │  normal submission queue / runner / assessment
        ▼
Submission + Assessment + ExaminationAttempt link

GET /api/progress
        │  read learning catalog + challenge competencySlugs
        │  read append-only assessments and examination attempts
        ▼
Derived competency states (NotStarted → Learning → Practicing → AssessmentRequired → Mastered)
```

Examination detail responses reuse `ChallengeDetail` but set `HintsMarkdown` to `null`. Hidden tests are still selected server-side for `Submit`; nothing about them is added to the examination contract.

See [EXECUTION_SECURITY.md](EXECUTION_SECURITY.md), [ADR-0002](decisions/ADR-0002-code-execution-isolation.md), [ADR-0006](decisions/ADR-0006-submission-queue-and-docker-runner.md), [ADR-0008](decisions/ADR-0008-python-runtime.md), [ADR-0009](decisions/ADR-0009-evidence-based-progress-and-examinations.md), and [ADR-0010](decisions/ADR-0010-sql-execution-and-sqlite-isolation.md).

## 8. Testing strategy

### Phase 1

- API integration tests use `WebApplicationFactory` and a temporary SQLite database.
- Persistence tests verify that migrations apply to a clean database and that initialization is repeatable.
- Frontend tests cover shell navigation, health loading/success/failure states, and API error handling.
- Tests must not require Docker, a network service, or a shared database.

### Phase 2

- Backend tests cover every learning endpoint, unknown-slug ProblemDetails responses, seed counts, idempotent upsert behavior, restoration of edited rows, re-creation of deleted child rows and prerequisites, and the applied migration set.
- Frontend tests cover path browsing, module details, Markdown rendering, loading states, error states, and retry behavior.
- The seed catalog is verified through the HTTP contract rather than by reaching into internal entity builders.

### Phase 3

- Backend loader tests cover valid projection, schema version, strict JSON, missing files, missing hidden/solution files, path traversal, duplicate slugs, slug validation, required lists, test requirements, and editor-language defaults.
- Startup tests verify that invalid packages and empty challenge roots fail fast.
- Endpoint tests verify catalog ordering, detail contracts, unknown-slug ProblemDetails, and non-disclosure of hidden-test and solution content.
- Loader and startup tests use disposable temporary package trees rather than the repository catalog.
- Frontend tests cover challenge listing, filtering, empty and error states, workspace rendering, editing and resets, language switching with per-language drafts, restoring the latest saved attempt and result, keeping unsent drafts across remounts, and retry. Workspace tests mock the editor component so Monaco never loads in jsdom.

### Phase 4

- Submission endpoint tests cover accepted requests, polling to a terminal state, server-supplied harness injection, unknown challenges, non-executable languages, unsafe paths, unsupported extensions, unavailable execution, runtime failures, latest-attempt restore with saved source files, missing history, and non-disclosure of harness content.
- Runner contract tests use a recording fake Docker engine to verify workspace generation, harness injection, project/command construction, path rejection before Docker is invoked, workspace cleanup, and mapping of success, timeout, compilation failure, and unavailable execution.
- Docker argument tests assert the exact isolation flags, read-only mount, non-root user, environment variables, and absence of privileged/socket access.
- Challenge loader tests cover harness presence, missing/empty/out-of-package harness files, and verify harness content is absent from the serialized client projection.
- Recovery tests verify that stale queued/running submissions become `Interrupted` after restart.
- The automated test suite does not require a live Docker daemon. Live container execution is an operational check performed on a machine with Docker installed.

### Phase 5

- Unit tests cover every assessment outcome, integer scoring, partial passes, harness errors, malformed/incomplete result blocks, duplicate observations, output sanitization, and deterministic re-grading.
- Integration tests verify that `Run` selects only public tests, `Submit` selects public and hidden tests, hidden test names and data never appear in responses, result endpoints return the permitted projection, non-successful execution scores zero, and invalid purposes or reserved filenames are rejected.
- Frontend tests cover the Run and Submit request bodies, assessment rendering, public test statuses, hidden counts, and error handling.
- The suite still does not require a live Docker daemon.

### Phase 6

- Python runner contract tests mirror the C# runner tests: server workspace generation, harness and manifest injection, Python image and command, bytecode-disabled environment, path and reserved-filename rejection before Docker, cleanup, and mapping of success, timeout, syntax error, runtime error, and unavailable execution.
- Shared runner helpers are covered through both adapters, so a change to workspace or manifest behavior is verified for C# and Python.
- Integration tests verify that a Python submission uses the Python adapter, receives the server-side Python harness and selected tests, and produces the same deterministic assessment contract as C#. The C# fake runner records no request for a Python submission.
- Repository package tests verify that every declared Python language has a harness, is executable, and imports the submitted `solution` module.
- Frontend tests cover Python language selection, its own starter file in the Run request body, and editable access now that Python is executable.
- The suite still does not require a live Docker daemon.

### Phase 7

- Backend tests cover every competency state transition through the HTTP contract: no evidence, Run-only, failed Submit, accepted Submit without examination, and accepted examination plus a second mapped challenge.
- Assessment history tests verify newest-first ordering and examination linkage.
- Examination tests verify server-selected variants and limits, unknown challenges, non-executable challenges, missing hidden tests, `Run` rejection, cross-challenge rejection, expiry via a controllable clock, finish behavior, and attempt counts.
- A fake `TimeProvider` makes expiry deterministic; no test depends on the wall clock.
- Frontend tests cover the progress dashboard, including key metrics, next-step recommendation, recent activity, and the active-examination banner, as well as competency evidence, assessment history, empty and error states, examination start, the examination workspace, countdown display, hint removal, Submit-only mode, finish, and expiry.

### Phase 8

- Loader tests verify that SQL packages receive the built-in harness, that schema and seed content is injected into server test data, and that schema, seed, and hidden SQL expectations are absent from the client projection.
- Loader validation tests reject `sql` without a `sql` language, a `sql` language without the block, an author-supplied SQL harness, tests without `expected` or `plan`, malformed plan entries, and non-scalar expected values.
- SQL runner contract tests mirror the other runners: server workspace generation with query, harness, and manifest; the configured SQL image and command; exactly one `.sql` file; reserved filenames and traversal rejection before Docker; cleanup; and compile-error, runtime-error, and unavailable-execution mapping.
- Submission integration tests verify that SQL uses the SQL adapter, receives the built-in harness and schema/seed-bearing tests, and returns the same assessment contract. A second SQL file is rejected, and the other fake runners record no request.
- Endpoint tests verify that SQL challenge details expose the SQL editor language and executability without exposing schema, seed, hidden test names, or hidden data.
- Frontend tests cover opening a SQL challenge, rendering the SQL editor language, and posting `query.sql` to the same submission and assessment flow.
- Practice-page tests cover progress-aware recommendations, exclusion of read-only packages, difficulty filtering, the no-executable-challenge state, and retrying a failed challenge request.
- Settings-page tests cover API health, runtime languages, execution status, limits, the disabled-execution state, and retrying a failed runtime request. Runtime endpoint tests assert the diagnostics and the absence of Docker paths, workspace roots, container users, and database locations.
- App shell tests cover switching between dark and light themes, persisting the choice, and updating the toggle label. Settings tests also cover the explicit appearance controls.
- The suite still does not require a live Docker daemon. The harness itself is additionally validated directly with Python and SQLite for correctness, plan pass/fail, and compile-error behavior.

### Phase 10

- Loader tests verify rubric projection, default weights, and the absence of hidden material, and reject a rubric on `AutomatedTests`, a missing rubric on `Rubric`, out-of-range points or weights, and duplicate criterion ids.
- Endpoint tests verify rubric detail projection and the `GET`/`PUT` review contract: weighted score, band, snapshotted criteria, upsert behavior, unknown submissions, non-terminal conflicts, challenges without rubrics, and invalid criteria.
- Frontend tests cover the review panel appearing only after a terminal submission on a rubric challenge, loading an existing review, submitting points and a note, and rendering the server-computed score and band.

### Authoring

- Learning import tests cover a new domain and module, in-place updates without duplicates, multipart JSON upload, unknown-prerequisite rejection without writes, and cross-parent move rejection.
- Challenge upload tests use a disposable challenge root: valid install and catalog reload, conflict without `replace`, replacement with `replace=true`, zip-slip rejection, multiple-package rejection, and non-disclosure of hidden tests, solutions, harnesses, and filesystem paths.
- Frontend tests cover form state, learning import success and server rejection, challenge install and replace, conflict messages, and `postForm` error handling.

## 9. Key decisions

| Decision | Record |
|---|---|
| Modular monolith | [ADR-0001](decisions/ADR-0001-modular-monolith.md) |
| Docker-based execution isolation | [ADR-0002](decisions/ADR-0002-code-execution-isolation.md) |
| .NET 10 / Node 26 / SQLite / TypeScript pin | [ADR-0003](decisions/ADR-0003-runtime-and-stack-baseline.md) |
| Learning module extraction and EF model registration | [ADR-0004](decisions/ADR-0004-learning-module-and-model-registration.md) |
| Filesystem challenge packages and strict startup validation | [ADR-0005](decisions/ADR-0005-filesystem-challenge-packages.md) |
| Submission queue, Docker runner, and server-side harnesses | [ADR-0006](decisions/ADR-0006-submission-queue-and-docker-runner.md) |
| Deterministic assessment and hidden-material boundary | [ADR-0007](decisions/ADR-0007-deterministic-assessment.md) |
| Python runtime behind the language-runner boundary | [ADR-0008](decisions/ADR-0008-python-runtime.md) |
| Evidence-based progress and server-controlled examinations | [ADR-0009](decisions/ADR-0009-evidence-based-progress-and-examinations.md) |
| SQL execution with an in-memory SQLite database | [ADR-0010](decisions/ADR-0010-sql-execution-and-sqlite-isolation.md) |

## 10. Known limitations (through Phase 10)

- No authentication or authorization. Authoring endpoints inherit this limitation and are intended for a trusted local machine only.
- API binds to loopback only; it is not designed for network exposure.
- Learning content can be imported from JSON, but imports never delete rows; removal still requires a manual cleanup or a database reset.
- Challenge content can be uploaded as a ZIP and reloaded without restart, but there is no in-app package editor or delete action.
- Workspace drafts are kept in browser `localStorage` and the last Run or Submit attempt is restored from the server. Clearing browser storage loses unsent drafts; server history is not deleted from the UI.
- The queue is in-memory and single-process. Queued attempts are lost on shutdown and recovered as `Interrupted` at the next startup. A durable queue is out of scope for the local single-user product.
- C#, Python, SQL, and Go execution are implemented. Challenges that declare a language without a harness remain browse-only, and `api-validation` is still C#-only because its executable scenario needs a host application rather than a single function harness.
- SQL challenges use SQLite and a fixed built-in harness. Packages cannot supply a custom harness, cannot use MySQL/PostgreSQL syntax, and cannot require extensions or third-party databases.
- SQL result comparison is exact after numeric tolerance: column names are optional, ordering is configurable per test, and types are compared as SQLite returns them. A query that produces extra or differently ordered rows fails unless the test opts into unordered comparison.
- Query-plan checks match `EXPLAIN QUERY PLAN` text fragments. They are deterministic for SQLite but are not a general cost-based performance assertion; an exercise should assert the intended access path rather than a timing threshold.
- SQLite's in-memory database is created per test in a single connection, so a query cannot affect another test. Progress evidence for SQL challenges uses the same assessment rules as every other language.
- Executable challenges must be dependency-free. Containers have no network, so neither .NET package restore nor Python package installation can fetch third-party libraries at execution time.
- Python challenges run through a single `solution` module; multi-module imports are supported only through submitted files that the harness explicitly imports.
- Run and Submit execute the server-selected tests and record an assessment. Progress is derived from that evidence on read; there is no mutable progress row, so progress can only change through new assessments.
- Competency state requires a server-controlled examination: `Mastered` needs an accepted examination submission plus an accepted `Submit` on a second mapped challenge. A challenge without `competencySlugs` or without examination variants cannot contribute the corresponding evidence.
- Examination variants and time limits come from the challenge manifest. The server chooses a variant at session creation; the client cannot request one.
- Rubric reviews are learner-authored self-assessments with no reviewer role, moderation, or separate persisted reviewer identity. They attach to an existing terminal submission, so rubric-only review without an executable submission is not implemented.
- Assessment and examination lists are ordered in memory because SQLite cannot `ORDER BY` a `DateTimeOffset` column. This is acceptable for the local dataset and is documented as a scaling limit.
- Hidden test data and reference solutions are loaded from the filesystem package and are never persisted in the database or returned by an endpoint. Hidden test names and per-test statuses are stored as server-side assessment evidence but filtered out of every client projection.
- Docker is required for live execution. Without it, submissions terminate with `ExecutionUnavailable` and the assessment scores zero.
- The automated suite verifies Docker arguments and runner behavior with a fake engine; it does not spin up real containers.
- No production hosting or reverse-proxy configuration.
- Automatic startup migrations are appropriate for a local single-user application, not a horizontally scaled deployment.
- The database is unencrypted at rest; protect the local machine and backups.
