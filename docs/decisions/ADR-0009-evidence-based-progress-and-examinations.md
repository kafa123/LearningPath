# ADR-0009: Evidence-Based Progress and Examinations

- **Status:** Accepted
- **Date:** 2026-11-12
- **Phase:** 7

## Context

Phase 7 must turn stored assessment results into competency progress and add examination mode with optional time limits and challenge variants. Phases 2–6 already provide a seeded competency model, filesystem challenge packages, a language-neutral submission pipeline, and append-only assessment evidence.

Several forces apply:

- Progress must reflect actual evidence, not page views, lesson opens, or self-reported completion.
- The client must not be able to grant itself a competency state, choose an examination variant, extend a time limit, or bypass hidden tests.
- Progress must not drift from the assessment history. A separately maintained progress row would need reconciliation and could disagree with the evidence.
- Examination attempts must use the existing execution and grading pipeline; a parallel pipeline would duplicate queue, worker, limit, and scoring behavior.
- Hidden tests, reference solutions, and harnesses must stay server-side, including during examinations.
- The modular-monolith boundaries must survive: progress should not reach into submission persistence, and submissions should not depend on progress internals.
- SQLite cannot `ORDER BY` a `DateTimeOffset` column, so newest-first history needs a deliberate approach.
- The learner still needs a clear next action for every competency and a useful assessment history.

## Decision

### 1. New `Psel.Modules.Progress` module

`Psel.Modules.Progress` owns read-side competency aggregation, assessment history, and examination sessions. It references `Psel.Infrastructure` for the shared `PselDbContext` and `Psel.Modules.Challenges` for catalog metadata. It does not reference submission entity types.

The host registers `ProgressModuleRegistration.ModelAssembly` with infrastructure, so the examination configurations join the shared model without infrastructure referencing the module.

### 2. Derive progress on read

Progress is not stored. `GET /api/progress` reads the seeded domains, modules, and competencies, the challenge catalog, append-only assessments, and examination attempt links, then derives every state on each request.

A challenge contributes evidence only when its `competencySlugs` includes the competency. The state rules are:

| State | Condition |
|---|---|
| `NotStarted` | No assessment evidence on any mapped challenge. |
| `Learning` | At least one assessment, but no `Submit`. |
| `Practicing` | At least one `Submit`, but none accepted. |
| `AssessmentRequired` | At least one accepted `Submit`, but no accepted examination submission or fewer than two distinct mapped challenges accepted. |
| `Mastered` | An accepted examination submission plus accepted `Submit` evidence on at least two distinct mapped challenges. |

Each competency returns a next action and up to five newest evidence entries. The summary counts competencies by state.

### 3. Assessment history is a read projection

`GET /api/assessments` returns newest-first assessment evidence with public/hidden counts and examination linkage. The link is resolved through `ExaminationAttempts`, so the assessment module remains unaware of examinations.

### 4. Server-controlled examination sessions

`POST /api/examinations` accepts only a challenge slug. The server validates that the challenge:

- exists;
- has at least one executable language;
- defines at least one hidden test;
- declares at least one examination variant (the loader supplies a default untimed `standard` variant when the manifest omits the block).

The server selects a variant with `Random.Shared` and computes `ExpiresAtUtc` from the variant's `timeLimitSeconds`. The response embeds the normal `ChallengeDetail` with `HintsMarkdown = null`.

`GET /api/examinations` and `GET /api/examinations/{id}` return sessions with status, remaining time, and attempt count. `POST /api/examinations/{id}/finish` completes an active session. Expiry is lazy: a session is transitioned to `Expired` when it is read or used after `ExpiresAtUtc`.

### 5. Reuse submissions through `IExaminationPolicy`

An examination attempt is a normal submission with `purpose: "Submit"` and an optional `examinationId`. Before queuing, `SubmissionService` calls `IExaminationPolicy`, which checks that the session exists, is `InProgress`, has not expired, and targets the same challenge. `Run` is rejected for an examination.

After the attempt is persisted, the policy records an `ExaminationAttempt` link in the same save. `IExaminationPolicy` is the only cross-module surface: submission details and hidden test material never cross into the progress module.

### 6. Hint-free examination projection

The embedded challenge projection is the same client contract used for practice, with `hintsMarkdown` removed. Hidden tests, reference solutions, and harnesses are not added. This keeps one challenge contract and makes accidental disclosure less likely than a separate examination-only DTO.

### 7. Manifest-declared variants and competency mapping

Each `challenge.json` may declare `competencySlugs` and an `examination.variants` block (`id`, `label`, optional `timeLimitSeconds` from 60 to 7200, at most five variants). The loader validates both. A default untimed `standard` variant is supplied when the block is absent.

### 8. In-memory ordering for `DateTimeOffset`

SQLite cannot `ORDER BY` a `DateTimeOffset` column. Assessment and examination list endpoints materialize rows, sort newest-first in memory, and then take the requested limit. Limits are clamped (1–100 for assessments, 1–50 for examinations). The local dataset is small, and deterministic ordering matters more than provider-specific SQL.

### 9. Deterministic tests with a controllable clock

Examination expiry uses `TimeProvider`. Tests register a `TestTimeProvider` and advance it explicitly, so no test depends on the wall clock. This also makes expiry and finish behavior reproducible.

## Alternatives considered

### Store a mutable `UserProgress` row per competency

Rejected. It would duplicate evidence, need reconciliation after every assessment, and could drift from history. Deriving on read makes the assessment history the single source of truth.

### Let the client declare competency completion

Rejected. Self-reported completion is not evidence. Accepted submissions and examination attempts are the only inputs.

### Let the client choose an examination variant or time limit

Rejected. Variant selection and limits are server policy. Client control would let a learner choose an easier or untimed variant.

### Build a separate examination submission pipeline

Rejected. A second queue, worker, runner selection, and grading path would duplicate behavior and create divergent hidden-test handling. The existing pipeline already supports the required `Submit` semantics.

### Enforce the time limit only in the browser

Rejected. The browser clock and page state are untrusted. The server checks expiry when a session is read or used, and rejects late attempts.

### Return a bare `slug`/`title` for the examination challenge

Rejected. The workspace needs the full statement, languages, starter files, and public tests. Reusing `ChallengeDetail` with hints removed is simpler and keeps one contract.

### Persist hidden test content with the examination

Rejected. Examination sessions store only the server-selected variant and time limit. Hidden tests remain in the filesystem package and the server-only execution plan.

### Add background jobs to expire sessions

Rejected for the local single-user product. Lazy expiry is sufficient and avoids a scheduler. The documented limitation is that an idle session remains `InProgress` until the next read or attempt.

## Consequences

### Positive

- Progress is always consistent with assessment history because it is derived from it.
- The client cannot grant itself progress, choose a variant, extend a limit, or bypass hidden tests.
- Examination mode reuses the existing submission, execution, and assessment pipeline.
- Hidden tests, reference solutions, and harnesses remain server-side.
- Competency evidence is traceable to specific assessments and challenges.
- The examination projection stays on one challenge contract.
- Expiry behavior is deterministic in tests.

### Negative

- Deriving progress reads all assessments and examination attempts on every request. This is acceptable locally but would need caching or projection tables at a larger scale.
- `Mastered` requires two mapped challenges plus an examination, which authors must plan for through `competencySlugs` and challenge coverage.
- Lazy expiry means an abandoned session is not closed until it is next observed.
- SQLite ordering is done in memory, which would not scale to a large history.
- The progress module reads assessment tables directly, creating a read-side coupling to the assessment model. It does not mutate them.

### Mitigations

- Keep progress derivation a pure read over append-only evidence and document the scaling limits in [ARCHITECTURE.md](../ARCHITECTURE.md) and [DATABASE_DESIGN.md](../DATABASE_DESIGN.md).
- Document `competencySlugs` and `examination.variants` in [challenges/README.md](../../challenges/README.md), including the two-challenge mastery rule.
- Test every state transition, examination lifecycle edge, and expiry path through the HTTP contract.
- Use a fake `TimeProvider` so expiry tests never depend on the wall clock.
- Keep the hint-free projection explicit and covered by tests that assert `HintsMarkdown` is `null`.
- Record the ordering constraint next to the in-memory sort so a future change does not reintroduce a failing SQLite query.
