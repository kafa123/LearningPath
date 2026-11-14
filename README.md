# Personal Software Engineering Lab (PSEL)

> **Train. Build. Test. Understand.**

PSEL is a local-first web application for learning, practicing, and objectively assessing software engineering skills. This repository currently contains the **Phase 8 database engineering** layer plus post-MVP settings and runtime diagnostics on top of the Phase 7 progress and examination features: a running ASP.NET Core API, a React application, SQLite/EF Core initialization, five seeded learning paths with modules and Markdown lessons, eight validated filesystem challenge packages, a Monaco-based workspace, Docker-backed C#, Python, and SQL execution, public/hidden test assessment, deterministic scoring, submission persistence, server-run examination sessions, evidence-based competency progress, read-only runtime diagnostics, automated tests, and the project documentation baseline.

The canonical product and architecture specification is [PROJECT_SPEC.md](PROJECT_SPEC.md).

## Current status

| Phase | Scope | Status |
|---|---|---|
| 1 | Foundation, application shell, SQLite/EF Core, health check, baseline tests | Implemented |
| 2 | Learning domains, modules, materials, competencies, prerequisites, seed data | Implemented |
| 3 | Challenge packages, browsing, workspace | Implemented |
| 4 | Submission persistence and Docker execution | Implemented |
| 5 | Assessment engine | Implemented |
| 6 | Python runtime | Implemented |
| 7 | Progress and examination | Implemented |
| 8 | Database engineering challenges | Implemented |
| 9 (post-MVP) | Settings and read-only runtime diagnostics | Implemented |

Phase 4 adds submission persistence, a bounded in-memory execution queue, a background execution worker, a Docker-backed C# runner, server-side execution harnesses, resource limits, and cleanup. Submitted code is never compiled or run in the API process. When Docker is unavailable, the API still starts and submissions are recorded with an `ExecutionUnavailable` result rather than crashing.

Phase 5 adds public and hidden test execution, deterministic scoring, assessment persistence, and permitted result views. `Run` executes only public tests. `Submit` executes public and hidden tests and records an `Accepted`, `WrongAnswer`, `CompilationError`, `RuntimeError`, `TimeLimitExceeded`, `MemoryLimitExceeded`, `OutputLimitExceeded`, or `InternalRunnerError` outcome. Hidden test names, descriptions, identifiers, and data never reach the browser.

Phase 6 adds a second language runtime behind the existing `ILanguageRunner` boundary. The same challenge workflow now runs C# and Python where a package provides both implementations. Each language keeps its own server-side harness, image, command, and environment; the submission API, queue, worker, and assessment rules are language-neutral.

Phase 7 adds the `Psel.Modules.Progress` module. It derives competency states on read from append-only assessment evidence, exposes assessment history, and runs server-controlled examination sessions. A challenge maps to competencies through `competencySlugs`, and an optional `examination.variants` block supplies the variants and time limits the server selects from. Examination attempts use the existing submission pipeline with `purpose: "Submit"`; the server validates the session, records the attempt link, and returns a hint-free challenge projection. Competency states are `NotStarted`, `Learning`, `Practicing`, `AssessmentRequired`, and `Mastered`.

Phase 8 adds database engineering challenges without a second execution pipeline. A package gains an optional `sql` block that declares a schema file and an optional seed file, and the `sql` language is executable only when that block is valid. The server injects the schema and seed into each test's trusted data, supplies a built-in SQLite harness, and runs the learner's single `.sql` file in a disposable container against a fresh in-memory database. Assessment compares result rows, optional column names, ordering, and selected `EXPLAIN QUERY PLAN` constraints, so correctness and index-usage exercises share the same submission, worker, runner, and grading rules as C# and Python. The learner query runs with `PRAGMA query_only`, a read-only authorizer, and row, column, and time caps; the application's own SQLite database is never mounted or opened.

The web application exposes learning paths, the full challenge catalog, a practice page, examinations, progress, and a read-only Settings page. Practice combines the catalog with derived competency states and recent assessments, recommends challenges whose mapped competencies need attention, and hides packages that have no trusted execution harness. Every practice card opens the same workspace used for assessment.

The post-MVP Settings page reports API health plus the safe subset of execution configuration: whether execution is enabled, the image for each language, and the time, memory, CPU, process, output, and queue limits. `GET /api/runtime` never returns Docker executable paths, workspace roots, container users, or database locations, and there is no endpoint that mutates configuration. Server-owned settings still change through `appsettings` or environment variables followed by an API restart.

The interface ships with dark and light themes. A toggle in the sidebar and an Appearance section in Settings switch the palette, and the choice is stored in the browser. The light theme remaps the same Tailwind color tokens, so every page, card, and status badge follows without a second component tree; Monaco uses a matching light editor theme.

## Technology baseline

- **Backend:** ASP.NET Core minimal APIs on .NET 10 (LTS)
- **Persistence:** EF Core 10 with SQLite
- **Frontend:** React 19 + TypeScript + Vite
- **Editor:** Monaco Editor, bundled locally (no CDN)
- **Styling:** Tailwind CSS 4
- **Backend tests:** xUnit v3
- **Frontend tests:** Vitest + Testing Library + jsdom
- **Execution sandbox:** Docker Engine (Docker Desktop with WSL2 on Windows)

Version choices and their rationale are recorded in [ADR-0003](docs/decisions/ADR-0003-runtime-and-stack-baseline.md).

## Prerequisites

| Tool | Required version | Notes |
|---|---|---|
| .NET SDK | 10.0.403 or later 10.0.x | `global.json` pins 10.0.403 with `latestPatch` roll-forward |
| Node.js | 26.x LTS | Required by Vite 8, Vitest 5, and jsdom 31 |
| npm | Bundled with Node 26 | Lockfile is committed |
| Git | Any recent version | Repository management |
| Docker Desktop + WSL2 | Required for code execution | The API and tests run without Docker; submissions report `ExecutionUnavailable` until an engine is available |

### Install on Windows

```powershell
winget install --id Microsoft.DotNet.SDK.10 --exact --accept-package-agreements --accept-source-agreements
winget install --id OpenJS.NodeJS.LTS --exact --accept-package-agreements --accept-source-agreements
```

Verify:

```powershell
dotnet --version   # 10.0.403 or later 10.0.x
node --version     # v26.x
npm --version
```

If an older Node installation shadows the new one, check `where.exe node` and adjust `PATH`.

## Run locally

Open two terminals from the repository root.

Terminal 1 — backend:

```powershell
dotnet tool restore
dotnet restore
dotnet run --project src/Psel.Api
```

The API listens on `http://localhost:5080`. On startup it creates the SQLite database and applies migrations.

Terminal 2 — frontend:

```powershell
cd client
npm ci
npm run dev
```

Open `http://localhost:5173`. The Vite development server proxies `/api` requests to the backend, so the browser does not need CORS access.

On first startup the API applies migrations and seeds five learning domains containing twelve modules, twelve Markdown lessons, twenty-four competencies, thirty-six objectives, and seven prerequisite links. The seeder is idempotent for the local single-user database.

Startup also discovers every `challenge.json` under `challenges/`, validates each package strictly, and loads the catalog. An invalid package or an empty challenge directory fails startup with a field-level error. Challenge content changes require an API restart.

Health check:

```powershell
Invoke-RestMethod http://localhost:5080/api/health
```

Learning catalog:

```powershell
Invoke-RestMethod http://localhost:5080/api/domains
Invoke-RestMethod http://localhost:5080/api/learning-paths
Invoke-RestMethod http://localhost:5080/api/learning-paths/programming-fundamentals
Invoke-RestMethod http://localhost:5080/api/modules/complexity-analysis
Invoke-RestMethod http://localhost:5080/api/materials/complexity-analysis-lesson
```

Challenge catalog:

```powershell
Invoke-RestMethod http://localhost:5080/api/challenges
Invoke-RestMethod http://localhost:5080/api/challenges/two-sum
```

The challenge endpoints return public content only. Hidden tests, reference solutions, and execution harnesses are validated on disk but never returned by the API.

Progress and examination endpoints:

```powershell
Invoke-RestMethod http://localhost:5080/api/progress
Invoke-RestMethod http://localhost:5080/api/assessments
Invoke-RestMethod http://localhost:5080/api/examinations

$body = @{ challengeSlug = 'two-sum' } | ConvertTo-Json
$exam = Invoke-RestMethod http://localhost:5080/api/examinations `
  -Method Post -ContentType 'application/json' -Body $body

Invoke-RestMethod "http://localhost:5080/api/examinations/$($exam.id)"
Invoke-RestMethod "http://localhost:5080/api/examinations/$($exam.id)/finish" -Method Post
```

`GET /api/progress` derives competency states on read. `GET /api/assessments` returns recent assessment evidence, newest first. `POST /api/examinations` starts a session and returns a server-selected variant with a hint-free challenge projection; `POST /api/examinations/{id}/finish` closes it. An examination attempt is a normal submission that carries `examinationId` and `purpose: "Submit"`; `Run` is rejected for an examination.

Challenge packages live under `challenges/` and are documented in [challenges/README.md](challenges/README.md).

## Code execution

The execution subsystem requires a running Docker engine. On Windows this means Docker Desktop with the WSL2 backend. Pull the configured language images once:

```powershell
docker pull mcr.microsoft.com/dotnet/sdk:10.0
docker pull python:3.13-slim
```

The API starts and serves the learning and challenge catalogs without Docker. Submissions created while the engine is unavailable are persisted with an `ExecutionUnavailable` result and score zero.

List the server-configured languages:

```powershell
Invoke-RestMethod http://localhost:5080/api/languages
```

Submit the workspace content for the C# language. Use `"purpose": "Run"` for a public-test practice run or `"purpose": "Submit"` for a public and hidden assessment:

```powershell
$content = [string](Get-Content -Raw -LiteralPath 'challenges/programming/two-sum/starter/csharp/Solution.cs')
$body = @{
  challengeSlug = 'two-sum'
  languageId = 'csharp'
  purpose = 'Submit'
  files = @(@{ path = 'Solution.cs'; content = $content })
} | ConvertTo-Json -Depth 5

$created = Invoke-RestMethod http://localhost:5080/api/submissions `
  -Method Post -ContentType 'application/json' -Body $body

Invoke-RestMethod "http://localhost:5080/api/submissions/$($created.id)"
Invoke-RestMethod "http://localhost:5080/api/submissions/$($created.id)/results"
```

The POST returns `202 Accepted`. Poll the submission endpoint until `isTerminal` is `true`; the status distinguishes success, compilation errors, runtime errors, time limits, memory limits, output limits, unavailable execution, and internal errors. The response includes captured output and diagnostics but never the server-side harness or hidden material. The result endpoint returns the deterministic outcome, score, public/hidden pass counts, and public per-test statuses. Scores use equal weight per selected test and integer arithmetic.

The same workflow runs Python when the challenge provides a Python starter file and harness:

```powershell
$content = [string](Get-Content -Raw -LiteralPath 'challenges/programming/two-sum/starter/python/solution.py')
$body = @{
  challengeSlug = 'two-sum'
  languageId = 'python'
  purpose = 'Run'
  files = @(@{ path = 'solution.py'; content = $content })
} | ConvertTo-Json -Depth 5

$created = Invoke-RestMethod http://localhost:5080/api/submissions `
  -Method Post -ContentType 'application/json' -Body $body
```

SQL challenges accept exactly one query file. The schema, seed data, and harness are server-side package content, so the request carries only the learner query:

```powershell
$content = [string](Get-Content -Raw -LiteralPath 'challenges/database/order-revenue/starter/sql/query.sql')
$body = @{
  challengeSlug = 'order-revenue'
  languageId = 'sql'
  purpose = 'Submit'
  files = @(@{ path = 'query.sql'; content = $content })
} | ConvertTo-Json -Depth 5

$created = Invoke-RestMethod http://localhost:5080/api/submissions `
  -Method Post -ContentType 'application/json' -Body $body
```

The SQL runner uses `Execution:SqlImage` (default `python:3.13-slim`, which includes SQLite) and the same container isolation as the other languages. A syntax error or a statement that is not a read-only `SELECT`/`WITH` query is reported as `CompilationError`; a per-test SQL failure is a `RuntimeError`.

Every execution is a short-lived container with network disabled, a read-only workspace mount, a non-root user, CPU/memory/process limits, a wall-clock timeout, an output cap, and explicit cleanup. See [docs/EXECUTION_SECURITY.md](docs/EXECUTION_SECURITY.md).

## Database

- Default location: `src/Psel.Api/data/psel.db` (relative to the API content root).
- The path is configurable through `Database:Path` in `appsettings.json` or environment variables.
- The database and SQLite sidecar files are ignored by Git.
- Migrations are applied automatically at startup in this local-first application.
- To reset a development database, stop the application and delete the database files.

Create a migration after changing the EF Core model:

```powershell
dotnet tool restore
dotnet ef migrations add <MigrationName> `
  --project src/Psel.Infrastructure `
  --startup-project src/Psel.Api `
  --output-dir Persistence/Migrations
```

Apply migrations manually (normally unnecessary because startup does it):

```powershell
dotnet ef database update `
  --project src/Psel.Infrastructure `
  --startup-project src/Psel.Api
```

Back up by copying the database while the application is stopped. See [docs/DATABASE_DESIGN.md](docs/DATABASE_DESIGN.md).

## Tests and checks

Backend:

```powershell
dotnet build PSEL.slnx
dotnet test PSEL.slnx
```

Frontend:

```powershell
cd client
npm run lint
npm run typecheck
npm run test:run
npm run build
```

The backend integration tests use temporary SQLite files and temporary challenge package trees, and delete them after each run. Frontend tests mock HTTP calls; they do not require the API to be running. At the Phase 8 and post-MVP Phase 9 baseline there are 154 backend tests and 51 frontend tests. Docker-backed execution is covered by runner contract tests with a fake Docker engine; the test suite never requires a live Docker daemon. Live Docker execution has also been verified manually: for C#, success, compilation error, runtime error, read-only workspace, disabled network, and cleanup; for Python, accepted, runtime-error, and compilation-error paths plus container and workspace cleanup; and for SQL, an accepted `order-revenue` submission at 5/5 tests, an accepted `order-index-plan` submission at 4/4, a wrong-query submission that failed only the plan constraints, and a `CompilationError` for a non-read-only statement. The SQL harness has also been validated directly against Python 3.12 and SQLite for result correctness, query-plan pass and failure, and compile-error reporting.

## Project structure

```text
src/
  Psel.Api/                  ASP.NET Core host, configuration, health endpoint
  Psel.Infrastructure/       EF Core context, migrations, database startup
  Modules/
    Psel.Modules.Learning/   Learning domain, EF configurations, seed data, endpoints
    Psel.Modules.Challenges/ Challenge contracts, strict package loader, catalog, endpoints
    Psel.Modules.Execution/  Docker boundary, C#, Python, and SQL runners, limits, cleanup
    Psel.Modules.Assessment/ Deterministic grader, assessment entities, result endpoint
    Psel.Modules.Submissions/ Submission entity, queue, worker, endpoints
    Psel.Modules.Progress/   Competency progress, assessment history, examination sessions
tests/
  Psel.Api.Tests/            API, persistence, and package-validation tests
client/                      React + TypeScript application
docs/                        Architecture, database, API, security, roadmap, ADRs
challenges/                  Validated challenge packages and authoring guide
```

Backend modules are separate assemblies introduced in the phase that gives them real behavior. `Psel.Modules.Learning` owns the learning domain and its EF configurations; `Psel.Modules.Challenges` owns filesystem-backed challenge packages but has no database tables; `Psel.Modules.Execution` owns runner and Docker concerns, with one adapter per language behind `ILanguageRunner`; `Psel.Modules.Assessment` owns deterministic grading, assessment persistence, and the result projection; `Psel.Modules.Submissions` owns the submission entity, queue, worker, and HTTP contract; `Psel.Modules.Progress` owns read-side competency aggregation and examination sessions. Execution depends on neither challenges nor submissions, and challenges expose harness and test material only through a trusted server-side interface. Submissions depend on Progress through the narrow `IExaminationPolicy` contract rather than its persistence. The host registers the learning, submissions, assessment, and progress assemblies with infrastructure so module EF configurations join the shared model without infrastructure referencing the modules. See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), [ADR-0004](docs/decisions/ADR-0004-learning-module-and-model-registration.md), [ADR-0005](docs/decisions/ADR-0005-filesystem-challenge-packages.md), [ADR-0006](docs/decisions/ADR-0006-submission-queue-and-docker-runner.md), [ADR-0007](docs/decisions/ADR-0007-deterministic-assessment.md), [ADR-0008](docs/decisions/ADR-0008-python-runtime.md), [ADR-0009](docs/decisions/ADR-0009-evidence-based-progress-and-examinations.md), and [ADR-0010](docs/decisions/ADR-0010-sql-execution-and-sqlite-isolation.md).

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [Database design](docs/DATABASE_DESIGN.md)
- [API specification](docs/API_SPECIFICATION.md)
- [Execution security](docs/EXECUTION_SECURITY.md)
- [Development roadmap](docs/DEVELOPMENT_ROADMAP.md)
- [Architecture decision records](docs/decisions/)

## Security notes

- The Phase 8 API binds to loopback only and has no authentication.
- Do not expose the development server or execution capability to an untrusted network.
- Submitted code runs only in a short-lived, restricted Docker container, never in the API process. Docker remains a useful isolation layer but not an absolute security boundary.
- The client cannot select images, commands, mounts, environment variables, limits, examination variants, or time limits. These are server configuration and server-selected metadata.
- Challenge packages are trusted content validated at startup. Hidden tests, reference solutions, and execution harnesses are never read into client API responses; keep them in `tests/hidden/`, `solutions/`, and `harness/`.
- Examination responses remove hints (`HintsMarkdown = null`) and examination submissions cannot use the `Run` purpose or target a different challenge.
- SQL submissions run against a fresh in-memory SQLite database built from server-owned schema and seed SQL inside the container. The application's own database file is never mounted. Learner queries are restricted to `SELECT`/`WITH`, `PRAGMA query_only` is enabled, and an authorizer denies writes, attachments, and pragmas.
- Learning material, challenge statements, and execution output are rendered as text or trusted Markdown. `react-markdown` does not render raw HTML by default; keep that default when user-authored content is introduced. Execution output is displayed in a `<pre>` element, not rendered as HTML.
- See [docs/EXECUTION_SECURITY.md](docs/EXECUTION_SECURITY.md).

## Troubleshooting

| Symptom | Likely cause | Resolution |
|---|---|---|
| `No .NET SDKs were found` | SDK not installed or `PATH` stale | Install .NET SDK 10, reopen the terminal |
| `global.json` SDK not found | Installed 10.0.x predates 10.0.403 | Install 10.0.403+ or update `global.json` deliberately |
| Vite/Vitest engine error | Node 20 or older is active | Install Node 26 LTS and verify `node --version` |
| `dotnet ef` not found | Local tools not restored | Run `dotnet tool restore` |
| Health check reports database unhealthy | Database path not writable or migration failed | Check API logs and `Database:Path` |
| Learning catalog is empty | Seeder failed or the database already contains partial data | Check API logs and delete the development database to reseed |
| API fails at startup with a challenge validation error | A package manifest or referenced file is invalid | Fix the reported package and field; see [challenges/README.md](challenges/README.md) |
| Submission finishes with `ExecutionUnavailable` | Docker CLI/engine is not installed or running, or the configured image is missing | Install Docker Desktop with WSL2, start the engine, and pull the language image (`mcr.microsoft.com/dotnet/sdk:10.0`, `python:3.13-slim`, or the configured `Execution:SqlImage`) |
| Assessment result returns `404` | The submission is still running, or assessment recording failed | Poll the submission until `isTerminal`, then retry; check API logs for assessment errors |
| Docker run fails or compilation cannot restore packages | The image is missing, or the code needs packages that cannot be fetched because the container has no network | Pull the language image before submitting and keep challenge code dependency-free |
| Port already in use | Another process is listening on 5080 or 5173 | Stop the process or change the port in launch settings/Vite config |
