# ADR-0010: SQL Execution and SQLite Isolation

- **Status:** Accepted
- **Date:** 2026-11-14
- **Phase:** 8

## Context

Phase 8 must add controlled SQL challenge environments: schema setup, data seeding, execution of learner queries against isolated databases, query-correctness evaluation, and selected query-plan/performance exercises. The exit criterion is that SQL challenges execute without exposing the application's own database.

Phases 4–7 already provide a submission queue, a worker, a Docker-backed runner boundary, server-side harnesses, deterministic assessment, and hidden-material filtering. Several forces apply:

- SQL must not create a second submission, execution, or grading pipeline. A parallel path would duplicate queue, worker, limits, artifact handling, scoring, progress evidence, and examination rules.
- The application database contains submissions, assessments, and examination evidence. A learner query must never be able to read or modify it.
- Schema and seed data are trusted authoring content, but they must not leak into client responses.
- A SQL package must not be able to replace the harness with something weaker, because the harness enforces read-only execution and result limits.
- Query correctness needs rows and optional column names; performance exercises need a deterministic signal that does not depend on wall-clock timing.
- The same container isolation, cleanup, and fail-closed behavior used for C# and Python must apply to SQL.
- The solution must stay local-first and work without a server-based database engine.

## Decision

### 1. Reuse the existing submission pipeline

SQL is a language behind `ILanguageRunner`, not a separate feature. A SQL submission goes through the same validation, queue, worker, execution, assessment, progress, and examination paths as C# and Python. Only three things differ:

- `SqlDockerRunner` accepts exactly one `.sql` file.
- The challenge package declares a `sql` block and a server-owned harness is used.
- The harness executes SQL against an in-memory SQLite database instead of importing a submitted program.

`SqlDockerRunner` uses `Execution:SqlImage` (default `python:3.13-slim`, which includes the SQLite standard-library module) and the fixed command `python3 -B /workspace/PselHarness.py`.

### 2. Package-owned schema and seed, server-only harness

A package declares:

```json
"languages": [
  { "id": "sql", "name": "SQL", "editorLanguage": "sql", "starterFiles": [...] }
],
"sql": { "schema": "sql/schema.sql", "seed": "sql/seed.sql" }
```

The loader:

- requires the `sql` block and the `sql` language to appear together;
- reads the schema and optional seed into memory with the same path, existence, and size validation as other package files;
- rejects an author-supplied `harness` for the `sql` language;
- assigns `BuiltInSqlHarness.Source` as the harness;
- injects the schema and seed into each selected test's server-only `DataJson`, allowing a per-test override.

The schema, seed, harness, and expectations are never added to `ChallengeDetail`. `GET /api/challenges` and `GET /api/challenges/{slug}` expose only the normal language projection with `editorLanguage: "sql"` and `canExecute: true`.

### 3. Disposable in-memory SQLite database

For each selected test, the harness opens a fresh `sqlite3.connect(":memory:")` connection, runs the trusted schema and seed, then executes the learner query on that connection. The connection is closed at the end of the test, so no state is shared between tests.

The application database is not mounted into the container and is not attached by the harness. The container has no network, so the query has no path to a remote database either.

### 4. Read-only execution controls

Before running the learner query, the harness:

- sets `PRAGMA query_only = ON`;
- installs an SQLite authorizer that permits only `SQLITE_SELECT`, `SQLITE_READ`, `SQLITE_FUNCTION`, and `SQLITE_RECURSIVE`;
- rejects a query whose first keyword, after stripping comments, is not `SELECT` or `WITH`;
- installs a progress handler that aborts after 5 seconds;
- caps output at 1000 rows and 64 columns.

These controls are defense in depth. The primary boundary is still the short-lived container with no application database mount.

### 5. Deterministic correctness and plan checks

A SQL test file contains `expected` (an array of rows of scalars), optional `columns`, optional `ordered` (default `true`), optional `plan`, and optional per-test `schema`/`seed` string overrides. The harness compares:

- column names case-insensitively when `columns` is supplied;
- row count and values exactly, with tolerance for floating-point numbers;
- row order when `ordered` is true;
- `EXPLAIN QUERY PLAN` substrings for `plan.mustContain` and `plan.mustNotContain`.

A plan check asserts an access path rather than a duration. Timing thresholds would be noisy on a developer machine and would make grading non-deterministic.

### 6. Compile-style failures versus per-test failures

A syntax error, incomplete statement, unrecognized token, empty query, multiple statements, or non-read-only leading keyword emits `PSEL_SQL_COMPILE_ERROR` and maps to `CompilationError`. A failure that is specific to one test, such as a missing table or a type error, marks that test as errored and produces `RuntimeError` through the normal assessment protocol.

This keeps the learner-facing categories consistent with C# and Python: malformed input is a compilation error, and a test-time failure is a runtime error.

### 7. Two authored packages

- `order-revenue` exercises joins, grouping, NULL semantics, and ordering with correct result rows.
- `order-index-plan` exercises a composite index and asserts that the plan uses `idx_orders_customer_date` and does not scan the table.

Both use the same harness, runner, and assessment path.

## Alternatives considered

### Add a separate SQL endpoint and execution pipeline

Rejected. It would duplicate validation, queueing, worker behavior, container lifecycle, assessment scoring, hidden-test selection, progress evidence, and examination checks. Reusing the pipeline keeps one contract and one set of security controls.

### Run SQL inside the API process with an in-memory SQLite connection

Rejected. It violates the rule that untrusted code never executes in the API process and would give the query the option to attach or read application files if a control failed.

### Mount a copy of the application database and restrict it with SQLite authorizers

Rejected. A copy still contains real submissions and assessment evidence, and any mistake in authorization or attachment handling would expose it. A fresh schema and seed eliminate the data exposure rather than relying on query filtering.

### Use the application's SQLite file directly with `query_only`

Rejected. `query_only` is a connection-level pragma, not a security boundary, and the file would be readable by the container. The application database must never be mounted.

### Let authors supply a SQL harness

Rejected. A harness controls the read-only authorizer, result limits, protocol output, and database lifecycle. Allowing an author to replace it would make package validation unable to guarantee those properties.

### Grade query performance by elapsed time

Rejected. Wall-clock timing is noisy and non-deterministic across machines. `EXPLAIN QUERY PLAN` assertions are stable and directly express the intended access path.

### Add SQL exercises as a new assessment type

Rejected. The existing harness protocol already supports opaque per-test observations. SQL correctness is just another observation, and reusing it keeps scoring, hidden tests, progress, and examinations unchanged.

## Consequences

### Positive

- SQL challenges use the same queue, worker, container isolation, cleanup, and scoring as other languages.
- The application database is never mounted or opened by learner queries.
- Schema and seed data stay server-side and are validated at startup.
- Authors cannot weaken the harness.
- Query-plan exercises are deterministic and explainable.
- Hidden tests and SQL expectations never enter client responses.
- The client needs no SQL-specific submission or result contract.

### Negative

- SQL challenges are SQLite-specific and cannot use MySQL or PostgreSQL syntax or extensions.
- A SQL submission is limited to one query file, which rules out some multi-statement authoring patterns.
- Exact row comparison can be sensitive to implicit type differences; authors must write expectations that match SQLite's returned values.
- The harness itself must exist in the container and can read the test manifest, just like other trusted harnesses.
- A query that creates large intermediate results can still consume container resources until the outer limits stop it.

### Mitigations

- Document the supported SQL dialect and single-file rule in [challenges/README.md](../../challenges/README.md).
- Validate SQL test shapes strictly at startup and reject non-scalar expected values.
- Keep the read-only controls layered: query-only pragma, authorizer, leading-keyword check, container isolation, and no application database mount.
- Assert access paths in plan tests instead of elapsed time.
- Cover the harness with direct Python/SQLite validation in addition to runner contract tests.
- Record the SQL boundary in [EXECUTION_SECURITY.md](../EXECUTION_SECURITY.md) and [DATABASE_DESIGN.md](../DATABASE_DESIGN.md).
