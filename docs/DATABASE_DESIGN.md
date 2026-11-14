# PSEL Database Design

## 1. Scope

This document describes the persistence design through Phase 8. It defines the learning schema, the submission schema, the assessment schema, and the progress/examination schema, and records that challenge content is deliberately filesystem-backed. SQL challenges execute against disposable in-memory SQLite databases and are not persisted as application tables.

## 2. Technology

- **Database engine:** SQLite
- **Access layer:** Entity Framework Core 10
- **Provider:** `Microsoft.EntityFrameworkCore.Sqlite`
- **Application context:** `PselDbContext` in `Psel.Infrastructure`

SQLite is appropriate for a local-first, single-user application. It requires no separate server process and keeps the application portable.

## 3. Database location

The default configuration is:

```json
{
  "Database": {
    "Path": "data/psel.db"
  }
}
```

Relative paths are resolved against the API content root. When the API is started from the repository with `dotnet run --project src/Psel.Api`, the default file is:

```text
src/Psel.Api/data/psel.db
```

The application creates the parent directory if needed. The database and its SQLite sidecar files (`-wal`, `-shm`, `-journal`) are ignored by Git.

Absolute paths may be configured for a different local location. Tests override the path with a unique temporary file.

## 4. Schema history

### Phase 1

The `InitialCreate` migration creates EF Core's migration history only. The migration pipeline, startup initialization, and database connectivity are exercised and tested without pre-empting domain design.

### Phase 2 — learning model

The `LearningModel` migration adds the learning catalog. All identifiers are GUIDs stored as SQLite `TEXT`; timestamps are intentionally absent because seeded catalog content is currently read-only.

| Table | Purpose | Keys and constraints |
|---|---|---|
| `LearningDomains` | Top-level learning areas | `Id` PK; unique `Slug`; `Name`, `Description`, `SortOrder` |
| `LearningModules` | Modules inside a domain | `Id` PK; `DomainId` FK → `LearningDomains` (cascade); unique `Slug`; `Title`, `Summary`, `Level`, `EstimatedMinutes`, `SortOrder`; index on `(DomainId, SortOrder)` |
| `LearningMaterials` | Markdown lessons inside a module | `Id` PK; `ModuleId` FK → `LearningModules` (cascade); unique `Slug`; `Title`, `Kind`, `EstimatedMinutes`, `BodyMarkdown`, `SortOrder`; index on `(ModuleId, SortOrder)` |
| `LearningObjectives` | Observable module outcomes | `Id` PK; `ModuleId` FK (cascade); `Text`, `SortOrder`; index on `(ModuleId, SortOrder)` |
| `Competencies` | Measurable skills | `Id` PK; `ModuleId` FK (cascade); unique `Slug`; `Name`, `Description`, `SortOrder`; index on `(ModuleId, SortOrder)` |
| `LearningModulePrerequisites` | Directed prerequisite links | Composite PK `(ModuleId, PrerequisiteModuleId)`; `ModuleId` FK (cascade); `PrerequisiteModuleId` FK (restrict) |

The restrict behavior on the prerequisite target prevents deleting a module that another module still depends on. Cascade behavior removes a module's own objectives, materials, competencies, and outgoing prerequisite links when the module is deleted.

### Seed content

`LearningDataSeeder` runs after migration and inserts the initial catalog once:

| Entity | Count |
|---|---|
| Domains | 5 |
| Modules | 12 |
| Materials | 12 |
| Competencies | 24 |
| Objectives | 36 |
| Prerequisite links | 7 |

The seeder is idempotent by checking whether any domain exists. It is deliberately simple for the local single-user application; a future authoring feature will need explicit upsert and versioning rules rather than a count check.

The database health check verifies that EF Core can open a connection to the configured SQLite database.

### Phase 3 — challenges remain filesystem-backed

Phase 3 adds no migration and no tables. Challenge packages are authored as files under `challenges/` and validated at startup. Keeping them out of SQLite has deliberate consequences:

- The catalog is versionable and reviewable as ordinary files.
- Hidden test data and reference solutions never enter a queryable table that a careless endpoint could expose. Phase 5 later stores hidden test names and statuses only as assessment evidence, never their data.
- No migration is required to add or edit a challenge; restarting the API reloads the catalog.
- The database stays focused on mutable learner state (submissions, progress, assessment), which arrives in later phases.

See [challenges/README.md](../challenges/README.md) and [ADR-0005](decisions/ADR-0005-filesystem-challenge-packages.md).

### Phase 4 — submissions

The `SubmissionsModel` migration adds the execution-attempt table. A row represents one immutable attempt; a new Run creates a new row.

| Column | Type and rules | Purpose |
|---|---|---|
| `Id` | `TEXT` GUID, primary key | Attempt identifier exposed to the client |
| `ChallengeSlug` | `TEXT`, required, max 100 | Validated challenge package slug |
| `LanguageId` | `TEXT`, required, max 60 | Language selected for this attempt |
| `Purpose` | `TEXT`, required, max 20, default `Run` | `Run` (public tests) or `Submit` (public and hidden tests) |
| `Status` | `TEXT`, required, max 40 | Stored as the enum name |
| `SourceFilesJson` | `TEXT`, required | Serialized submitted files; server-side only |
| `CreatedAtUtc` | `TEXT`, required | Queue timestamp |
| `StartedAtUtc` | `TEXT`, nullable | Worker start timestamp |
| `CompletedAtUtc` | `TEXT`, nullable | Terminal timestamp |
| `DurationMs` | `INTEGER`, nullable | Runner-reported duration |
| `ExitCode` | `INTEGER`, nullable | Process exit code when available |
| `StandardOutput` | `TEXT`, required | Captured and size-limited output |
| `StandardError` | `TEXT`, required | Captured and size-limited diagnostics |
| `FailureMessage` | `TEXT`, nullable, max 1000 | Client-safe failure summary |
| `OutputTruncated` | `INTEGER` boolean, required | Whether output hit the limit |

Indexes exist on `CreatedAtUtc` and `(ChallengeSlug, LanguageId)` for future history views. Status transitions are `Queued → Running → terminal`; restart recovery converts stale `Queued`/`Running` rows to `Interrupted`. Terminal rows are never reopened.

`SourceFilesJson` is not included in any client response. Hidden tests, reference solutions, and execution harnesses are not stored in this table.

See [ADR-0006](decisions/ADR-0006-submission-queue-and-docker-runner.md).

### Phase 5 — assessment

The `AssessmentModel` migration adds the assessment tables and the `Submissions.Purpose` column. One assessment row is recorded for each submission attempt after execution finishes.

| Table | Purpose | Keys and constraints |
|---|---|---|
| `Assessments` | Deterministic grade for one submission | `Id` PK; unique `SubmissionId`; `ChallengeSlug`, `LanguageId`, `Purpose`, `Outcome`, `Score`, `PassedTests`, `TotalTests`, `PublicPassed`, `PublicTotal`, `HiddenPassed`, `HiddenTotal`, `CreatedAtUtc`; index on `CreatedAtUtc` and `(ChallengeSlug, CreatedAtUtc)` |
| `AssessmentTestResults` | Per-test evidence for one assessment | `Id` PK; `AssessmentId` FK → `Assessments` (cascade); `SortOrder`, `TestId`, `Name`, `Description`, `Visibility`, `Status`; index on `(AssessmentId, SortOrder)` |

`Assessments.SubmissionId` is intentionally unique but **not** a foreign key to `Submissions`. The assessment module owns this table and does not depend on the submissions module's persistence types; the API host composes both modules over one shared `PselDbContext` and transaction boundary. The worker records the assessment in the same `SaveChanges` call that marks the submission terminal.

`Purpose`, `Visibility`, `Outcome`, and `Status` are stored as enum names for readability. `AssessmentTestResults` persists hidden test names and statuses for server-side diagnosis, but the client projection filters to public rows and exposes only hidden pass/total counts. Hidden test data and reference solutions are never stored in the database.

### Phase 6 — Python runtime

Phase 6 adds no migration, table, or column. Python execution reuses the existing `Submissions.LanguageId` and `Assessments.LanguageId` values, which already store the language identifier as text. The queue, worker, assessment, and result projection are language-neutral; only the runner adapter and server-side challenge harness change. Challenge packages remain filesystem-backed, so adding a Python harness requires no database change.

### Phase 7 — progress and examination

The `ProgressAndExamination` migration adds two tables. Progress itself is **not** stored; competency state and assessment history are derived from `Assessments` and `ExaminationAttempts` at read time.

| Table | Purpose | Keys and constraints |
|---|---|---|
| `ExaminationSessions` | One server-created examination attempt container | `Id` PK; `ChallengeSlug` (max 100), `VariantId` (max 60), `VariantLabel` (max 120), `TimeLimitSeconds`, `Status` (stored as text), `StartedAtUtc`, `ExpiresAtUtc`, `CompletedAtUtc`; indexes on `StartedAtUtc` and `(ChallengeSlug, StartedAtUtc)` |
| `ExaminationAttempts` | Links one submission to an examination session | `Id` PK; `ExaminationId` FK → `ExaminationSessions` (cascade); unique `SubmissionId`; `CreatedAtUtc`; index on `CreatedAtUtc` |

`ExaminationAttempts.SubmissionId` is intentionally **not** a foreign key to `Submissions`; the progress module stores the identifier so it does not depend on the submissions module's persistence types. The submission service validates and records the link through `IExaminationPolicy`, and the attempt row is saved in the same call that persists the submission.

`Status` is stored as the enum name (`InProgress`, `Completed`, `Expired`). A session expires lazily: when it is read or used for a submission after `ExpiresAtUtc`, it transitions to `Expired` and its `CompletedAtUtc` is set to the expiry time. Finishing an active session sets `Completed`. Neither transition reopens the session.

SQLite cannot `ORDER BY` a `DateTimeOffset` column. `GET /api/assessments` and `GET /api/examinations` therefore materialize the rows, sort newest-first in memory, and then apply the requested limit. This keeps ordering deterministic without a provider-specific conversion and is acceptable for the local single-user dataset.

### Phase 8 — SQL challenge execution

Phase 8 adds no migration, table, or column. SQL submissions reuse `Submissions.LanguageId = "sql"`, and their assessments reuse the existing `Assessments` and `AssessmentTestResult` records. The execution database is **not** the application database:

| Concern | Storage |
|---|---|
| Challenge schema | `challenges/<domain>/<slug>/sql/schema.sql`, loaded by the package loader |
| Challenge seed rows | Optional `sql/seed.sql`, or a per-test `seed` override |
| Per-test expectations | `tests/public/...` and `tests/hidden/...` JSON, merged into `PselTests.json` |
| Learner query | Submission source file, persisted like any other submission |
| Execution database | Fresh SQLite `:memory:` instance created per test inside the container; discarded when the test ends |
| Application data | `src/Psel.Api/data/psel.db`; never mounted or opened by the SQL runner |

Because the schema and seed are trusted package content, the loader validates them at startup and injects them into each selected test's `DataJson`. They are not part of `ChallengeDetail`, and hidden test expectations remain filtered exactly like hidden tests for other languages.

The SQL runner passes only `query.sql`, the server-resolved harness, and the server-selected test manifest into the workspace. `PRAGMA query_only` and the harness authorizer prevent the query from modifying the execution database; the container has no network and no mount of the application database, so the query has no path to application data even if those controls were bypassed.


## 5. Migration workflow

Local tooling is pinned in `.config/dotnet-tools.json`.

```powershell
dotnet tool restore
```

Create a migration after changing the model:

```powershell
dotnet ef migrations add <MigrationName> `
  --project src/Psel.Infrastructure `
  --startup-project src/Psel.Api `
  --output-dir Persistence/Migrations
```

Apply migrations manually:

```powershell
dotnet ef database update `
  --project src/Psel.Infrastructure `
  --startup-project src/Psel.Api
```

Review every generated migration before committing it. SQLite has limited `ALTER TABLE` support, so EF Core may generate table-rebuild migrations. Back up development data before applying destructive changes.

`Psel.Infrastructure` has no design-time factory. The tooling uses `Psel.Api` as the startup project so the same configuration, module assembly registry, and SQLite provider are used at design time and runtime.

## 6. Startup initialization

`DatabaseInitializer` runs during application startup and:

1. Resolves and creates the database directory.
2. Calls `Database.MigrateAsync()`.
3. Logs successful initialization.
4. Fails startup with a clear error if the database cannot be initialized.

`LearningDataSeeder` is registered as a hosted service after the database initializer. It resolves a scope, checks for existing learning domains, and inserts the seed catalog only when the catalog is absent.

This is a deliberate local-first choice. It assumes one application instance. A multi-instance deployment should move migration execution to a controlled deployment step.

## 7. Data ownership

Module data ownership follows these rules:

- Each module owns its entities and EF Core configurations, as `Psel.Modules.Learning` does.
- Module configurations are applied through `ModuleAssemblyRegistry`, which the host populates with module assemblies. Infrastructure does not reference concrete modules.
- The single `PselDbContext` remains the transaction boundary for the modular monolith.
- Cross-module foreign keys should be deliberate; modules should not depend directly on each other's internal persistence types.
- `Psel.Modules.Challenges` owns no entities and contributes no configurations. Its catalog lifecycle is filesystem-to-memory, not database-backed. SQL schema and seed files are part of that catalog and are injected only into execution test data.
- `Psel.Modules.Submissions` owns the `Submission` entity and its configuration. It participates in the shared `PselDbContext` through the module assembly registry.
- `Psel.Modules.Assessment` owns the `Assessment` and `AssessmentTestResult` entities and their configurations. It participates in the shared `PselDbContext` through the module assembly registry.
- `Psel.Modules.Progress` owns the `ExaminationSession` and `ExaminationAttempt` entities and their configurations. It participates in the shared `PselDbContext` through the module assembly registry. It reads assessment evidence for derivation but does not own or mutate assessment rows.

## 8. Core entities

The specification identifies these eventual entities. The learning entities are implemented in Phase 2, `Submission` is implemented in Phase 4, the assessment entities in Phase 5, and the examination entities in Phase 7:

- `Challenge` and `ChallengeTestCase` — not database tables in the current design; challenge metadata and hidden cases are filesystem packages (Phase 3). SQL schemas and seeds are package files as well (Phase 8).
- `LanguageRuntime` — not a table; server configuration supplies the supported languages.
- `Submission` — implemented in Phase 4.
- `TestResult` — implemented as `AssessmentTestResult` in Phase 5.
- `Assessment` — implemented in Phase 5.
- `ExaminationSession` and `ExaminationAttempt` — implemented in Phase 7.
- No `UserProgress` table exists. Competency progress is derived on read from append-only assessment and examination evidence.

Any future need to query challenges relationally must be reconciled with the filesystem source of truth before adding tables.

## 9. History and immutability

Submission history is preserved. A new attempt creates a new row; previous attempts are never overwritten. Status and output columns are updated only within the lifecycle of that attempt, and terminal rows are not reopened. Restart recovery marks orphaned `Queued` or `Running` rows as `Interrupted` rather than deleting them.

An assessment is append-only evidence for one attempt. It is written once, is never recomputed in place, and is never returned for a different submission.

An examination session is created once by the server and moves forward through `InProgress` → `Completed` or `Expired`. Completed and expired sessions are closed: submissions are rejected and the session cannot be reopened. Attempt links are append-only, and each submission links to at most one examination.

## 10. Backup and recovery

For a local installation:

1. Stop the application.
2. Copy the database file to a safe location.
3. Restore by replacing the file while the application is stopped.

Do not copy an actively written database without accounting for SQLite's journal/WAL state. A future phase may add an application-level export or backup feature.

## 11. Concurrency notes

- SQLite serializes writers. The local single-user workload is acceptable.
- The submission worker does not hold a database transaction open during code execution. It marks the attempt running, releases the transaction, runs the container, then persists the terminal outcome and its assessment together.
- Submission state changes use short scopes and no long-lived tracked contexts.
- WAL mode may be considered when write concurrency increases; it is not enabled currently without a demonstrated need.

## 12. Security notes

- The database file is local and unencrypted. Protect the operating-system account and backups.
- Learning material is trusted seed data. When authoring or user-generated material is introduced, sanitize or render it without raw HTML and apply the same review to Markdown links and images.
- Hidden tests and reference solutions must remain server-side. They currently live in filesystem packages and are never read into API responses. Execution harnesses are server-side only as well.
- Examination sessions store the selected variant and time limit, not hidden test content. Examination detail responses remove hints and never add hidden material.
- SQL schema, seed, and hidden expectations are server-side package content. They are injected into execution tests but never returned by a challenge, submission, or result endpoint.
- SQL learner queries execute against a disposable in-memory database inside the container. The application's SQLite file is never mounted, attached, or opened by the harness.
- Submitted source text is stored for execution and is not returned by client API responses. Protect database backups accordingly.
- Never return raw database errors, file paths, or connection strings from the API.
