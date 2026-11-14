# PSEL Development Roadmap

This roadmap follows [PROJECT_SPEC.md](../PROJECT_SPEC.md). Build incrementally. Do not start a later phase until the current phase is implemented, tested, documented, and reviewed.

## Phase status

| Phase | Scope | Status |
|---|---|---|
| 1 | Foundation | Implemented |
| 2 | Learning management | Implemented |
| 3 | Challenge management | Implemented |
| 4 | Code execution | Implemented |
| 5 | Assessment engine | Implemented |
| 6 | Python runtime | Implemented |
| 7 | Progress and examination | Implemented |
| 8 | Database engineering | Implemented |

## Phase 1 — Foundation

### Delivered

- Repository structure, solution, and project references.
- ASP.NET Core API on .NET 10.
- React + TypeScript + Vite frontend shell.
- Tailwind CSS configuration.
- SQLite and EF Core with migrations.
- Startup database initialization.
- `GET /api/health` with database health.
- Baseline xUnit and Vitest tests.
- Architecture, database, API, security, roadmap, and ADR documentation.
- Local setup and test commands.

### Exit criteria

- Frontend and backend run locally.
- Database initializes.
- Health check works.
- Baseline tests pass.

### Out of scope

- Learning content and domain entities.
- Challenge packages and endpoints.
- Submissions and execution.
- Assessment and progress.
- Authentication.
- Production hosting.

## Phase 2 — Learning Management

### Delivered

- `Psel.Modules.Learning` module assembly with domain entities, EF Core configurations, endpoints, and seed catalog.
- `ModuleAssemblyRegistry` and module-contributed EF model configuration in the shared `PselDbContext`.
- Learning domains, modules, materials, objectives, competencies, and prerequisite relationships.
- Initial seed catalog: 5 domains, 12 modules, 12 materials, 24 competencies, 36 objectives, and 7 prerequisite links.
- Read-only API: `/api/domains`, `/api/learning-paths`, `/api/learning-paths/{slug}`, `/api/modules/{slug}`, `/api/materials/{slug}`.
- Learning-path, module, and Markdown lesson views in the React client.
- Backend endpoint/seed/migration tests and frontend browsing/render/error tests.
- Architecture, database, API, roadmap, README, and ADR updates.

### Exit criteria

- The learner can browse learning domains.
- The learner can open learning material and read it.
- The learner can see learning objectives, competencies, and prerequisites.

### Out of scope

- Learning-content authoring or editing.
- Progress tracking, completion state, or competency evidence.
- Challenge packages, submissions, execution, and assessment.

## Phase 3 — Challenge Management

### Delivered

- `Psel.Modules.Challenges` module assembly with contracts, filesystem loader, in-memory catalog, and endpoints.
- Versioned `challenge.json` schema (`schemaVersion: 1`) with strict JSON parsing.
- Startup validation of every package: required fields, supported values, supported difficulties/types/evaluation kinds, limits, duplicate slugs, unique test names, and relative path safety.
- Path traversal rejection, per-file size limits, and file-existence checks.
- Non-disclosure boundary: hidden test files are existence-checked; hidden content and reference solutions are never read into API responses. Solutions are excluded structurally.
- Six validated packages: `two-sum`, `balanced-brackets`, `word-frequency`, `debug-off-by-one`, `api-validation`, and `refactor-duplication`.
- C# and Python starter files and Monaco language mapping.
- Read-only API: `/api/challenges` and `/api/challenges/{slug}` with ProblemDetails 404 responses.
- Challenge catalog UI with search, domain, difficulty, and type filters; empty and error states.
- Lazy-loaded workspace with statement, objectives, constraints, examples, hints, public-test summaries, language tabs, file tabs, per-language drafts, dirty tracking, and reset controls.
- Monaco bundled locally with a custom theme and worker; no CDN dependency.
- Backend loader/startup/endpoint tests and frontend catalog/workspace tests.
- Architecture, database, API, roadmap, README, challenge authoring guide, and ADR updates.

### Exit criteria

- The learner can browse validated challenges.
- The learner can open a challenge and read its statement, examples, constraints, and public-test summaries.
- The learner can select a language and edit starter code in Monaco before execution exists.

### Out of scope

- Code execution, Run, and Submit.
- Submission persistence and results.
- Hidden-test execution and reference-solution use.
- In-app challenge authoring.
- Progress tracking.

### Notes

- Run and Submit were visible but disabled in Phase 3; Phase 4 enables Run, and Phase 5 enables Submit.
- Drafts are in-memory only; reloading or leaving the workspace discards them.
- Challenge catalog changes require an API restart.

## Phase 4 — Code Execution

### Delivered

- `Psel.Modules.Execution` and `Psel.Modules.Submissions` module assemblies with explicit boundaries.
- Append-only `Submission` entity, EF Core configuration, and `SubmissionsModel` migration.
- Submission validation against trusted challenges and languages: executable harness, runner availability, safe relative paths, accepted extensions, duplicate paths, 10-file limit, 128 KiB per file, and 256 KiB total.
- Bounded in-memory queue with back-pressure (`503` when full) and configurable concurrency.
- Background execution worker with one database scope per attempt, no long-held transaction, and terminal outcome persistence.
- Restart recovery that marks stale `Queued`/`Running` submissions as `Interrupted`.
- Docker process boundary with server-controlled image, command, environment, limits, and mounts.
- C# runner that generates a project and workspace, injects the server-side harness, builds and runs the submission, and maps outcomes to structured statuses.
- Isolation controls: network disabled, read-only root filesystem, tmpfs `/tmp`, dropped capabilities, no-new-privileges, non-root user, read-only workspace, memory/swap, CPU, process, output, and wall-clock limits.
- Explicit container removal on all paths plus startup cleanup of labelled stale containers.
- Server-only `languages[].harness` package support with strict path/size validation; harness content is excluded from client contracts.
- `GET /api/languages`, `POST /api/submissions`, and `GET /api/submissions/{id}`.
- Workspace Run action with polling, status display, output, diagnostics, duration, exit code, and error handling. Submit remains disabled until Phase 5.
- Backend tests for submissions, runner contracts, Docker flags, harness non-disclosure, and recovery; frontend tests for run, polling, results, and rejection errors.
- Architecture, database, API, security, roadmap, README, challenge authoring, and ADR updates.

### Exit criteria

- A C# submission is persisted and queued.
- The worker executes it in an isolated Docker container with server-controlled harness and limits.
- The client can poll and receive a structured, reliable terminal result with diagnostics.
- Docker being unavailable degrades to `ExecutionUnavailable` instead of crashing the API.

### Out of scope

- Public and hidden test execution.
- Deterministic scoring, accepted/wrong-answer classification, and result views.
- Python execution.
- Submission history UI.
- Authentication and multi-user isolation.

### Notes

- The automated suite uses a fake Docker engine and never requires a live daemon. Live Docker execution was verified manually with Docker Desktop and the SDK image: success, compilation error, runtime error, read-only workspace, disabled network, writable `/tmp`, and cleanup.
- Only C# has an execution harness; Python starter files remain editable but not executable.
- Run is a diagnostic execution of the challenge harness, not assessment. Assessment arrives in Phase 5.
- The queue is in-memory and single-process; a restart marks unfinished attempts `Interrupted`.

## Phase 5 — Assessment Engine

### Delivered

- `Psel.Modules.Assessment` module assembly with deterministic grading, assessment entities, EF Core configurations, and the result endpoint.
- `Purpose` on submissions: `Run` selects public tests; `Submit` selects public and hidden tests. The purpose defaults to `Run`.
- Server-only challenge test plans loaded from package `tests/public` and `tests/hidden` JSON files; clients cannot select tests.
- Harness contract that reads `/workspace/PselTests.json` and emits opaque test observations inside `__PSEL_RESULTS_BEGIN__` / `__PSEL_RESULTS_END__` markers.
- Deterministic scoring: equal weight per selected test, integer `passed * 100 / total`, zero for non-successful execution.
- Outcomes: `Accepted`, `WrongAnswer`, `CompilationError`, `RuntimeError`, `TimeLimitExceeded`, `MemoryLimitExceeded`, `OutputLimitExceeded`, and `InternalRunnerError`.
- `Assessment` and `AssessmentTestResult` persistence with the `AssessmentModel` migration and a unique submission identifier.
- Result block removed from persisted execution output.
- `GET /api/submissions/{id}/results` with a public-only test projection and hidden pass/total counts.
- Workspace Submit action, assessment rendering, public test statuses, hidden counts, and error handling.
- Backend unit and integration tests for every outcome, scoring, test selection, non-disclosure, reserved filenames, and result endpoints; frontend tests for Run/Submit request bodies and assessment rendering.
- Architecture, database, API, security, roadmap, README, challenge authoring, and ADR updates.

### Exit criteria

- Run executes public tests; Submit executes public and hidden tests.
- The server records a deterministic assessment with a score and outcome.
- The learner can inspect permitted result views without hidden material.
- Every assessment outcome is covered by automated tests.

### Out of scope

- Progress aggregation and competency evidence.
- Submission history UI.
- Exam mode and time-limited assessments.
- Python execution and assessment.
- Authentication and multi-user isolation.

### Notes

- Hidden test names, descriptions, identifiers, and data are never returned to the browser. Only hidden pass/total counts are exposed.
- Test data is available to submitted code inside the container at runtime because the harness must read it; this is documented as a limitation in [EXECUTION_SECURITY.md](EXECUTION_SECURITY.md).
- The assessment is recorded in the same transaction as the terminal submission state.
- The design is recorded in [ADR-0007](decisions/ADR-0007-deterministic-assessment.md).

## Phase 6 — Python Runtime

### Delivered

- `PythonDockerRunner` behind the existing `ILanguageRunner` boundary, registered beside the C# runner.
- `python` added to the server language catalog, so `GET /api/languages` reports both supported languages.
- Server-configured `Execution:PythonImage` (`python:3.13-slim`) and a fixed command, `python3 -B /workspace/PselHarness.py`.
- Python environment that disables bytecode writes (`PYTHONDONTWRITEBYTECODE`, `PYTHONPYCACHEPREFIX`) and fixes unbuffered UTF-8 I/O and `HOME`.
- Shared runner helpers for workspace creation, `PselTests.json` generation, and Docker outcome mapping; the C# runner was refactored onto the same helpers.
- `PselHarness.py` added to the reserved filename rules, so a submission cannot overwrite the server harness.
- Python harnesses for the five executable challenges that already provide Python starters and solutions: `two-sum`, `balanced-brackets`, `word-frequency`, `debug-off-by-one`, and `refactor-duplication`.
- Per-language `harness` entries in those challenge manifests; `canExecute` now reports true for their Python language.
- Python syntax diagnostics (`SyntaxError`, `IndentationError`, `TabError`) mapped to `CompilationError`; other non-zero exits mapped to `RuntimeError`.
- Same queue, worker, test selection, assessment, and client projection for both languages; no scoring or hidden-material rules changed.
- Backend runner, endpoint, and repository-package tests; frontend language-selection and Python Run tests.
- Architecture, database, API, security, roadmap, README, challenge authoring, and ADR updates.

### Exit criteria

- The same challenge workflow supports C# and Python where a challenge definition provides both implementations.
- A Python submission is validated, queued, executed in an isolated container with the server-side harness, and graded through the same deterministic assessment contract.
- Adding Python required no change to the submission API, queue, worker, assessment rules, or client result projection.
- The automated suite covers the Python adapter and still runs without a live Docker daemon.

### Out of scope

- Progress aggregation, assessment history, and exam mode.
- Additional languages beyond C# and Python.
- In-app challenge authoring.
- Third-party Python packages or package installation inside the execution container.
- Authentication and multi-user isolation.

### Notes

- `api-validation` remains C#-only and non-executable because its scenario needs a host application rather than a single Python function harness.
- Python challenges use standard-library-only modules imported by the server harness; the no-network container cannot install dependencies.
- Python syntax errors use the existing `CompilationError` status so assessment behavior stays language-neutral.
- Live Python container execution was verified manually with Docker Desktop 29.9.2 and `python:3.13-slim`: reference solution `Accepted` 4/4, unimplemented starter `RuntimeError` 0/4, malformed source `CompilationError`, and no residual container or workspace.
- The design is recorded in [ADR-0008](decisions/ADR-0008-python-runtime.md).

## Phase 7 — Progress and Examination

### Delivered

- `Psel.Modules.Progress` with read-side competency aggregation, assessment history, and examination sessions.
- `competencySlugs` in every `challenge.json`, validated by the loader and exposed by `ChallengeSummary` and `ChallengeDetail`; a challenge contributes evidence only to the competencies it declares.
- Optional `examination.variants` in challenge manifests, each with `id`, `label`, and optional `timeLimitSeconds`; the server randomly selects a variant when a session starts.
- `GET /api/progress`: domains, modules, and competencies with derived state, next action, and up to five recent evidence entries.
- `GET /api/assessments`: newest-first assessment history with examination linkage and public/hidden counts.
- `POST /api/examinations`, `GET /api/examinations`, `GET /api/examinations/{id}`, and `POST /api/examinations/{id}/finish`.
- Optional `examinationId` on submissions; `IExaminationPolicy` validates the session and challenge and records the attempt link. Examination attempts must use `purpose: "Submit"`.
- Hint-free examination projection: the embedded `ChallengeDetail` sets `HintsMarkdown` to `null` and never adds hidden tests, solutions, or harnesses.
- `ProgressPage`, `ExaminationsPage`, and `ExaminationWorkspacePage`; the existing `ChallengeWorkspace` is reused in examination mode with Submit-only controls, no hints, a countdown, and disabled submissions after completion or expiry.
- Dashboard progress and recent-assessment cards, navigation, routes, API clients, fixtures, and frontend tests.
- Two new tables through the `ProgressAndExamination` migration.
- Backend and frontend tests, plus README, architecture, database, API, security, roadmap, challenge authoring, and ADR updates.

### Exit criteria

- The learner can start an examination, submit code under a server-selected variant and limit, and see the resulting assessment evidence.
- Competency progress is derived from append-only assessment and examination evidence and reflects every documented state: `NotStarted`, `Learning`, `Practicing`, `AssessmentRequired`, and `Mastered`.
- The frontend shows a progress dashboard with competency states and assessment history, and an examination workspace that enforces the server's rules.
- The automated suite covers progress and examination behavior without a live Docker daemon or wall-clock dependency.

### Out of scope

- Authentication and multi-user isolation.
- In-app challenge or examination authoring.
- Background expiry jobs; sessions expire lazily on read or attempt.
- Durable or distributed queues.
- Rubric-based review mode.
- Additional languages beyond C# and Python.

### Notes

- Progress is not stored. It is recomputed from assessments and examination attempts so it cannot drift.
- `AssessmentRequired` is intentionally stricter than "passed a challenge": mastery also requires accepted examination evidence and a second mapped challenge.
- An examination attempt uses the normal submission pipeline and hidden-test selection; the only differences are the session check, the attempt link, and the hint-free projection.
- SQLite cannot `ORDER BY` a `DateTimeOffset` column, so assessment and examination lists sort the newest window in memory. This is documented as a local-dataset choice.
- The backend uses a controllable `TimeProvider` in tests so expiry is deterministic.
- The design is recorded in [ADR-0009](decisions/ADR-0009-evidence-based-progress-and-examinations.md).

## Phase 8 — Database Engineering

### Delivered

- A `sql` language and `SqlDockerRunner` behind the existing `ILanguageRunner` boundary, with `MaxSubmissionFiles = 1` and a server-configured `Execution:SqlImage`.
- An optional package `sql` block with a required `schema` file and an optional `seed` file, validated at startup and injected into each selected test's server-only `DataJson`. Per-test `schema` and `seed` overrides are supported.
- A server-owned `BuiltInSqlHarness` for SQL packages. SQL packages cannot supply or override a harness; the loader rejects an author-supplied `sql` harness.
- Strict SQL test validation for `expected` row arrays, optional `columns`, optional `ordered`, optional `plan.mustContain`/`plan.mustNotContain`, and scalar-only expected values.
- The harness creates a fresh in-memory SQLite database per test, runs schema and seed SQL, enables `PRAGMA query_only`, installs a read-only authorizer, rejects non-`SELECT`/`WITH` queries, and caps rows, columns, and query time.
- Query-plan checks run against `EXPLAIN QUERY PLAN`; result comparison supports ordered or unordered rows and numeric tolerance.
- Compile-like SQL failures emit `PSEL_SQL_COMPILE_ERROR` and map to `CompilationError`; per-test SQL failures become errored observations and `RuntimeError`.
- Two repository packages: `order-revenue` for joins, aggregation, and NULL semantics, and `order-index-plan` for a composite-index query-plan exercise.
- SQL runner, loader, and submission endpoint tests; frontend SQL workspace coverage; README, architecture, database, API, security, roadmap, challenge authoring, and ADR updates.

### Exit criteria

- SQL challenges can be executed without exposing the application's own database.
- Package schema and seed data reach only the trusted execution test plan, never client challenge, submission, or result responses.
- Query correctness and selected query-plan/performance exercises are assessed through the existing submission, worker, runner, and grading pipeline.

### Out of scope

- MySQL, PostgreSQL, or server-based database engines.
- Arbitrary database extensions, external data imports, or network access from the execution container.
- Author-supplied SQL harnesses or author-controlled execution images.
- Timing-based performance grading; query-plan assertions are the supported performance exercise.
- A separate SQL submission endpoint or a SQL-specific assessment pipeline.

### Notes

- Reusing the submission and assessment pipeline keeps grading, progress evidence, examinations, and hidden-material filtering identical across languages.
- The execution database is a disposable `:memory:` SQLite instance created inside the container. The application database is never mounted or attached.
- SQL statements are restricted to a single read-only statement. A write attempt or a syntax error is a compilation-class failure, not a learner-visible database mutation.
- The design is recorded in [ADR-0010](decisions/ADR-0010-sql-execution-and-sqlite-isolation.md).

## MVP acceptance mapping

The MVP acceptance criteria in [PROJECT_SPEC.md](../PROJECT_SPEC.md) section 15 span Phases 2–8. Phases 1–6 established the runnable foundation, the learning catalog, the challenge workspace, isolated C# and Python execution with submission persistence, and deterministic assessment. Phase 7 adds server-run examinations, assessment history, and evidence-based competency progress. Phase 8 adds isolated SQL challenge execution through the same pipeline. All MVP acceptance criteria mapped to Phases 2–8 are now implemented and covered by the automated suites.
