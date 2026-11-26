# Personal Software Engineering Lab (PSEL)

> **Train. Build. Test. Understand.**

PSEL is a local-first web application for learning, practicing, and objectively assessing software engineering skills. It ships with six learning paths, twenty-five validated challenge packages, a Monaco-based workspace, and a server-controlled Docker pipeline for C#, Python, SQL, and Go. Public and hidden tests produce deterministic assessment results, assessment evidence drives competency progress, and timed examinations reuse the same submission pipeline.

The canonical product and architecture specification is [PROJECT_SPEC.md](PROJECT_SPEC.md). Deeper material lives in [docs/](docs/).

## Contents

- [What PSEL does](#what-psel-does)
- [Learning tracks](#learning-tracks)
- [Supported languages](#supported-languages)
- [Technology stack](#technology-stack)
- [Prerequisites](#prerequisites)
- [Quick start](#quick-start)
- [Configuration](#configuration)
- [How a submission is assessed](#how-a-submission-is-assessed)
- [API overview](#api-overview)
- [Code execution](#code-execution)
- [Data and persistence](#data-and-persistence)
- [Project structure](#project-structure)
- [Tests and checks](#tests-and-checks)
- [Documentation](#documentation)
- [Security notes](#security-notes)
- [Status](#status)
- [Troubleshooting](#troubleshooting)

## What PSEL does

- **Learn.** Browse six learning paths with modules, illustrated Markdown lessons, objectives, competencies, and prerequisites. Lessons link to the challenges that assess their competencies.
- **Practice.** Filter the challenge catalog by difficulty and by the competencies that still need evidence. Every executable challenge opens the same workspace used for assessment.
- **Assess.** Run public tests for feedback or submit for a full public and hidden assessment. The server selects the tests, runs the code in an isolated container, and returns a deterministic score and outcome.
- **Progress.** Competency states are derived on read from append-only assessment evidence: `NotStarted`, `Learning`, `Practicing`, `AssessmentRequired`, and `Mastered`.
- **Examine.** Start a server-selected examination variant, solve the challenge under a time limit, and finish the session. Examination attempts cannot use practice runs or target a different challenge.
- **Review.** Add an advisory rubric self-review to a completed submission. The server recomputes the weighted score and band; the review never changes the assessment result.
- **Author.** Import a JSON learning pack or upload a ZIP challenge package from the local Authoring page. Both endpoints validate before writing.
- **Operate.** Inspect safe runtime diagnostics in Settings. The UI supports dark and light themes without a second component tree.

## Learning tracks

The seed catalog is synchronized as an upsert at startup. It currently contains six domains, nineteen modules, nineteen illustrated lessons, thirty-eight competencies, fifty-seven objectives, and thirteen prerequisite links.

| Track | Domain slug | Modules | Competencies | Challenge focus |
|---|---|---:|---:|---|
| Programming Fundamentals | `programming-fundamentals` | 3 | 6 | Algorithms, complexity, data structures |
| Database Engineering | `database-engineering` | 3 | 6 | Relational modeling, SQL, indexes and query plans |
| Backend Engineering | `backend-engineering` | 2 | 4 | HTTP API design, validation, error mapping |
| Software Architecture | `software-architecture` | 2 | 4 | Separation of concerns, ports and seams |
| Software Quality | `software-quality` | 2 | 4 | Testing foundations, debugging, root cause |
| Go Engineering | `go-engineering` | 7 | 14 | Errors, interfaces, concurrency, HTTP, generics, performance |

The challenge catalog contains twenty-five packages:

| Domain | Packages | Examples |
|---|---:|---|
| `programming-fundamentals` | 3 | Two Sum, Balanced Brackets, Word Frequency |
| `database-engineering` | 5 | Completed revenue per customer, Audit relational data integrity |
| `backend-engineering` | 3 | Add Request Validation to an Endpoint, Map Domain Errors to HTTP Responses |
| `software-architecture` | 2 | Extract a checkout notifier port, Inject a clock into pricing |
| `software-quality` | 3 | Debug an Off-by-One Sum, Debug cart totals and coupons |
| `go-engineering` | 9 | Normalize an inventory ledger, Build an LRU cache with generics, Serve orders with net/http |

Challenge content is filesystem-backed and documented in [challenges/README.md](challenges/README.md).

## Supported languages

The server owns the language catalog, image, command, environment, and test selection. The client can only choose a language that the challenge declares and that has a valid server-side harness.

| Language | ID | Extension | Runtime image | Execution model |
|---|---|---|---|---|
| C# | `csharp` | `.cs` | `mcr.microsoft.com/dotnet/sdk:10.0` | Generated console project, build, run |
| Python | `python` | `.py` | `python:3.13-slim` | Interpreter runs the harness module |
| SQL | `sql` | `.sql` | `python:3.13-slim` | One read-only query against an in-memory SQLite database |
| Go | `go` | `.go` | `golang:1.27-alpine` | `go build` followed by the compiled binary |

`GET /api/languages` returns the configured set. `GET /api/challenges/{slug}` reports `canExecute` per language; a language without a valid harness remains browsable and editable but cannot be submitted.

## Technology stack

- **Backend:** ASP.NET Core minimal APIs on .NET 10 (LTS), EF Core 10 with SQLite
- **Frontend:** React 19, TypeScript, Vite 8, Tailwind CSS 4
- **Editor:** Monaco Editor, bundled locally with no CDN dependency
- **Backend tests:** xUnit v3 on the Microsoft Testing Platform
- **Frontend tests:** Vitest 5, Testing Library, jsdom
- **Execution sandbox:** Docker Engine (Docker Desktop with the WSL2 backend on Windows)

Version choices and their rationale are recorded in [ADR-0003](docs/decisions/ADR-0003-runtime-and-stack-baseline.md).

## Prerequisites

| Tool | Required version | Notes |
|---|---|---|
| .NET SDK | 10.0.403 or later 10.0.x | `global.json` pins 10.0.403 with `latestPatch` roll-forward |
| Node.js | 26.x LTS | Required by Vite 8, Vitest 5, and jsdom 31 |
| npm | Bundled with Node 26 | The lockfile is committed |
| Git | Any recent version | Repository management |
| Docker Desktop + WSL2 | Required for code execution | The API and tests run without Docker; submissions report `ExecutionUnavailable` until an engine is available |

### Install on Windows

```powershell
winget install --id Microsoft.DotNet.SDK.10 --exact --accept-package-agreements --accept-source-agreements
winget install --id OpenJS.NodeJS.LTS --exact --accept-package-agreements --accept-source-agreements
```

Verify the tools:

```powershell
dotnet --version   # 10.0.403 or later 10.0.x
node --version     # v26.x
npm --version
```

If an older Node installation shadows the new one, check `where.exe node` and adjust `PATH`.

## Quick start

Open two terminals from the repository root.

### 1. Start the API

```powershell
dotnet tool restore
dotnet restore
dotnet run --project src/Psel.Api
```

The API listens on `http://localhost:5080`. On startup it creates the SQLite database, applies migrations, synchronizes the learning catalog, validates every challenge package, and loads the in-memory challenge catalog.

### 2. Start the frontend

```powershell
cd client
npm ci
npm run dev
```

Open `http://localhost:5173`. The Vite development server proxies `/api` to the backend, so the browser does not need CORS access.

### 3. Verify the installation

```powershell
Invoke-RestMethod http://localhost:5080/api/health
Invoke-RestMethod http://localhost:5080/api/domains
Invoke-RestMethod http://localhost:5080/api/challenges
```

`GET /api/challenges` must return twenty-five packages. If it returns fewer, check the API startup log for a package validation error.

## Configuration

Configuration is server-owned. Change `src/Psel.Api/appsettings.json` or set environment variables with the ASP.NET Core double-underscore syntax, for example `Execution__GoImage=golang:1.27-alpine`. Restart the API after changing configuration.

| Key | Default | Purpose |
|---|---|---|
| `Database:Path` | `data/psel.db` | SQLite file, relative to the API content root |
| `Challenges:Path` | `../../challenges` | Challenge package root |
| `Execution:Enabled` | `true` | Master switch for code execution |
| `Execution:DockerExecutable` | `docker` | Docker CLI used by the engine adapter |
| `Execution:CSharpImage` | `mcr.microsoft.com/dotnet/sdk:10.0` | C# runtime image |
| `Execution:PythonImage` | `python:3.13-slim` | Python runtime image |
| `Execution:SqlImage` | `python:3.13-slim` | SQL harness image, which includes SQLite |
| `Execution:GoImage` | `golang:1.27-alpine` | Go build-and-run image |
| `Execution:TimeLimitSeconds` | `60` | Wall-clock limit per container |
| `Execution:MemoryLimitMb` | `512` | Container memory limit |
| `Execution:CpuLimit` | `1.0` | CPU quota |
| `Execution:PidsLimit` | `128` | Process limit |
| `Execution:MaxOutputBytes` | `65536` | Captured stdout/stderr cap |
| `Execution:MaxConcurrentExecutions` | `2` | Parallel container limit |
| `Execution:MaxQueuedExecutions` | `64` | Queue depth |
| `Execution:ContainerUser` | `1000:1000` | Non-root container user |

## How a submission is assessed

1. The client posts a challenge slug, a language ID, a purpose (`Run` or `Submit`), and the source files.
2. The API validates the package, language, file count, paths, extensions, reserved filenames, and source size.
3. The submission is persisted and queued. A background worker executes one request at a time within the configured concurrency limit.
4. The language runner creates a temporary workspace with the submitted files, the trusted harness, any server-owned scaffolding, and a test manifest.
5. The container runs the fixed server command. The harness reads `/workspace/PselTests.json`, calls the learner's code, and emits only opaque per-test pass/fail observations.
6. The assessment engine checks completeness and maps the result to `Accepted`, `WrongAnswer`, `CompilationError`, `RuntimeError`, `TimeLimitExceeded`, `MemoryLimitExceeded`, `OutputLimitExceeded`, or `InternalRunnerError`. Missing Docker produces `ExecutionUnavailable`.
7. `Run` selects only public tests. `Submit` selects public and hidden tests. Hidden names, descriptions, identifiers, and data never reach the browser.
8. A terminal assessment appends evidence for the competencies mapped by the challenge, and `GET /api/progress` derives the new competency state on read.

Scores use equal weight per selected test and integer arithmetic. The workspace restores the latest attempt for the active language through `GET /api/submissions/latest`, and unsent edits are kept in browser `localStorage` per challenge. Examination workspaces start clean.

## API overview

The full contract, including request and response bodies, is in [docs/API_SPECIFICATION.md](docs/API_SPECIFICATION.md).

| Area | Endpoints |
|---|---|
| Health | `GET /api/health` |
| Learning catalog | `GET /api/domains`, `/api/learning-paths`, `/api/learning-paths/{slug}`, `/api/modules/{slug}`, `/api/materials/{slug}` |
| Challenge catalog | `GET /api/challenges`, `/api/challenges/{slug}` |
| Languages and runtime | `GET /api/languages`, `/api/runtime` |
| Submissions | `POST /api/submissions`, `GET /api/submissions/{id}`, `/results`, `/latest`, `GET/PUT /api/submissions/{id}/rubric` |
| Progress and history | `GET /api/progress`, `/api/assessments` |
| Examinations | `POST /api/examinations`, `GET /api/examinations`, `/api/examinations/{id}`, `POST /api/examinations/{id}/finish` |
| Authoring | `POST /api/authoring/learning-content`, `/api/authoring/challenge-packages` |

Examples:

```powershell
Invoke-RestMethod http://localhost:5080/api/learning-paths/programming-fundamentals
Invoke-RestMethod http://localhost:5080/api/challenges/two-sum
Invoke-RestMethod http://localhost:5080/api/progress

$body = @{ challengeSlug = 'two-sum' } | ConvertTo-Json
$exam = Invoke-RestMethod http://localhost:5080/api/examinations `
  -Method Post -ContentType 'application/json' -Body $body

Invoke-RestMethod "http://localhost:5080/api/examinations/$($exam.id)"
Invoke-RestMethod "http://localhost:5080/api/examinations/$($exam.id)/finish" -Method Post
```

`GET /api/runtime` exposes only safe diagnostics: execution status, the image per language, and the time, memory, CPU, process, output, and queue limits. It never returns Docker executable paths, workspace roots, container users, or database locations.

## Code execution

Docker is required for Run and Submit. Pull the configured images once:

```powershell
docker pull mcr.microsoft.com/dotnet/sdk:10.0
docker pull python:3.13-slim
docker pull golang:1.27-alpine
```

SQL reuses `python:3.13-slim`, which includes SQLite.

Every execution is a short-lived container with:

- network access disabled;
- a read-only workspace mount;
- a read-only root filesystem and `tmpfs` `/tmp`;
- a non-root user, dropped capabilities, and no-new-privileges;
- CPU, memory, process, wall-clock, and output limits;
- explicit removal after success, failure, timeout, or cancellation.

The API still starts and serves the catalog without Docker. Submissions created while the engine is unavailable are persisted with an `ExecutionUnavailable` result and score zero.

Submit the C# starter for a full assessment:

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

The same shape works for Go:

```powershell
$content = [string](Get-Content -Raw -LiteralPath 'challenges/go-engineering/inventory-ledger/starter/go/solution.go')
$body = @{
  challengeSlug = 'inventory-ledger'
  languageId = 'go'
  purpose = 'Submit'
  files = @(@{ path = 'solution.go'; content = $content })
} | ConvertTo-Json -Depth 5

$created = Invoke-RestMethod http://localhost:5080/api/submissions `
  -Method Post -ContentType 'application/json' -Body $body
```

Python and SQL requests follow the same contract. SQL accepts exactly one `.sql` file; its schema, seed data, and harness are server-side package content, so only the learner query is sent. The POST returns `202 Accepted`; poll the submission until `isTerminal` is `true`, then read the results endpoint.

See [docs/EXECUTION_SECURITY.md](docs/EXECUTION_SECURITY.md) for the threat model and isolation limits.

## Data and persistence

- The default database is `src/Psel.Api/data/psel.db`, relative to the API content root.
- The path is configurable through `Database:Path`.
- The database and SQLite sidecar files are ignored by Git.
- Migrations are applied automatically at startup.
- The learning catalog is synchronized as an upsert. Rows are matched by slug, and objectives by module and sort order. Submissions, assessments, progress evidence, and rubric reviews are preserved; rows removed from the seed are not deleted automatically.
- Challenge packages are validated at startup. Content changes require an API restart; the authoring ZIP endpoint reloads the in-memory catalog without one.
- To reset a development database, stop the API and delete the database files.

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

## Project structure

```text
src/
  Psel.Api/                    ASP.NET Core host, configuration, health endpoint
  Psel.Infrastructure/         EF Core context, migrations, database startup
  Modules/
    Psel.Modules.Learning/     Learning domain, EF configurations, seed data, endpoints
    Psel.Modules.Challenges/   Challenge contracts, strict package loader, catalog, endpoints
    Psel.Modules.Execution/    Docker boundary, C#/Python/SQL/Go runners, limits, cleanup
    Psel.Modules.Assessment/   Deterministic grader, assessment entities, result endpoint
    Psel.Modules.Submissions/  Submission entity, queue, worker, endpoints, rubric reviews
    Psel.Modules.Progress/     Competency progress, assessment history, examination sessions
tests/
  Psel.Api.Tests/              API, persistence, runner, and package-validation tests
client/
  src/app/                     Router, shell, layout, lazy pages
  src/features/                Dashboard, learning, challenges, practice, examinations, progress, authoring, settings
  src/shared/                  API client, Monaco setup, theme, shared components
  public/illustrations/        Generated lesson covers and diagrams
challenges/                    Validated challenge packages and authoring guide
docs/                          Architecture, database, API, security, roadmap, ADRs
```

The backend is a modular monolith. `Psel.Modules.Learning` owns the learning domain and its EF configurations; `Psel.Modules.Challenges` owns filesystem-backed packages but has no database tables; `Psel.Modules.Execution` owns runners and the Docker boundary, with one adapter per language behind `ILanguageRunner`; `Psel.Modules.Assessment` owns deterministic grading and result projection; `Psel.Modules.Submissions` owns the submission entity, queue, worker, HTTP contract, and rubric reviews; `Psel.Modules.Progress` owns read-side competency aggregation and examination sessions.

Execution depends on neither challenges nor submissions. Challenges expose harness and test material only through a trusted server-side interface. Submissions depend on Progress through the narrow `IExaminationPolicy` contract rather than its persistence. The host registers module assemblies with infrastructure so EF configurations join the shared model without infrastructure referencing the modules. See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) and the ADRs.

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

Current baseline:

- **195 backend tests** and **67 frontend tests**, all passing.
- Backend integration tests use temporary SQLite files and temporary challenge package trees and delete them after each run.
- Frontend tests mock HTTP calls and do not require the API to be running.
- Docker-backed execution is covered by runner contract tests with a fake Docker engine, so the suite never requires a live daemon.
- The SQL harness is additionally validated directly against Python 3.12 and SQLite for result correctness, query-plan pass and failure, and compile-error reporting.
- The nine Go packages were validated with Go 1.27 outside Docker: every reference solution passes all five tests, every starter fails at least one, and `go vet` is clean.
- Live Docker checks have covered C#, Python, and SQL success, compilation, runtime, isolation, and cleanup paths. Live Go container execution could not be re-verified in the current environment because the Docker engine was unavailable; the Go runner is covered by contract tests. The Go race detector was not run because it requires cgo, which the local toolchain lacks.

## Documentation

- [Adding content and data](docs/ADDING_CONTENT.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Database design](docs/DATABASE_DESIGN.md)
- [API specification](docs/API_SPECIFICATION.md)
- [Execution security](docs/EXECUTION_SECURITY.md)
- [Development roadmap](docs/DEVELOPMENT_ROADMAP.md)
- [Architecture decision records](docs/decisions/)
- [Challenge package authoring](challenges/README.md)

## Security notes

- The API binds to loopback only and has no authentication. Do not expose the development server or execution capability to an untrusted network.
- Submitted code runs only in a short-lived, restricted Docker container, never in the API process. Docker is a useful isolation layer, not an absolute security boundary.
- The client cannot select images, commands, mounts, environment variables, limits, examination variants, or time limits. These are server configuration or server-selected metadata.
- Challenge packages are trusted content validated at startup. Hidden tests, reference solutions, and execution harnesses are never returned by client API responses; keep them in `tests/hidden/`, `solutions/`, and `harness/`.
- The authoring endpoints are unauthenticated local tools. They share the loopback-only API and must not be exposed to a network. Uploaded challenge harnesses are trusted authoring input that later runs inside the Docker sandbox.
- Examination responses remove hints (`HintsMarkdown = null`), and examination submissions cannot use the `Run` purpose or target a different challenge.
- SQL submissions run against a fresh in-memory SQLite database built from server-owned schema and seed SQL inside the container. The application database file is never mounted. Learner queries are restricted to `SELECT`/`WITH`, `PRAGMA query_only` is enabled, and an authorizer denies writes, attachments, and pragmas.
- Learning material, challenge statements, and execution output are rendered as text or trusted Markdown. `react-markdown` does not render raw HTML by default; keep that default when user-authored content is introduced. Execution output is displayed in a `<pre>` element, not rendered as HTML.
- See [docs/EXECUTION_SECURITY.md](docs/EXECUTION_SECURITY.md).

## Status

All MVP phases and the post-MVP work are implemented:

| Area | State |
|---|---|
| MVP phases 1-8: foundation, learning catalog, challenges, execution, assessment, Python, progress and examinations, SQL | Implemented |
| Post-MVP: Settings and read-only runtime diagnostics | Implemented |
| Post-MVP: Rubric-based self-review | Implemented |
| Post-MVP: Local authoring for learning packs and challenge ZIPs | Implemented |
| Go engineering track and Go runtime | Implemented |

Known limits are documented rather than hidden: the queue is in-memory and single-process, there is no authentication, live Go container execution still needs a Docker-enabled environment, and the Go race detector is unavailable under the default `CGO_ENABLED=0` build. See [docs/DEVELOPMENT_ROADMAP.md](docs/DEVELOPMENT_ROADMAP.md) for phase history.

## Troubleshooting

| Symptom | Likely cause | Resolution |
|---|---|---|
| `No .NET SDKs were found` | SDK not installed or `PATH` stale | Install .NET SDK 10 and reopen the terminal |
| `global.json` SDK not found | Installed 10.0.x predates 10.0.403 | Install 10.0.403+ or update `global.json` deliberately |
| Vite or Vitest engine error | Node 20 or older is active | Install Node 26 LTS and verify `node --version` |
| `dotnet ef` not found | Local tools not restored | Run `dotnet tool restore` |
| Health check reports database unhealthy | Database path not writable or migration failed | Check API logs and `Database:Path` |
| Learning catalog is empty | Synchronizer failed before writing rows | Check API logs, fix the reported seed error, restart the API |
| API fails at startup with a challenge validation error | A manifest or referenced file is invalid | Fix the reported package and field; see [challenges/README.md](challenges/README.md) |
| Submission finishes with `ExecutionUnavailable` | Docker is not installed or running, or the image is missing | Start Docker Desktop, pull the language image (`mcr.microsoft.com/dotnet/sdk:10.0`, `python:3.13-slim`, or `golang:1.27-alpine`) |
| Assessment result returns `404` | The submission is still running, or assessment recording failed | Poll until `isTerminal`, then retry; check API logs |
| Go submission fails with a module download error | The container has no network and `GOPROXY=off` | Use only the standard library in Go challenges |
| Docker build fails because packages cannot be restored | The container has no network | Keep challenge code dependency-free and pull the image before submitting |
| Port already in use | Another process is listening on 5080 or 5173 | Stop the process or change the port in launch settings or Vite config |
