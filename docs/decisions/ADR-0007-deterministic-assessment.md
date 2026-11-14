# ADR-0007: Deterministic Assessment

- **Status:** Accepted
- **Date:** 2026-11-10
- **Phase:** 5

## Context

Phase 5 must grade a submission against public and hidden tests, record a deterministic assessment, and return a permitted result view without exposing hidden material. Phase 4 already executes untrusted code in a short-lived Docker container and persists one terminal `Submission` row per attempt.

Several forces apply:

- Grading must be reproducible: the same execution output and the same server-selected tests must always produce the same outcome and score.
- The client must not choose tests, see hidden test names or data, or influence scoring.
- Hidden tests, reference solutions, and harnesses are server-only.
- The submission worker already owns the execution lifecycle; grading must not hold a database transaction open during execution or duplicate execution logic.
- The assessment module must not depend on the submissions module, or the modular-monolith boundaries would collapse.
- A missing, duplicated, or malformed harness result must fail closed rather than invent a score.
- The learner still needs actionable feedback: which public tests passed and why the attempt failed.

## Decision

### 1. Separate assessment module

- `Psel.Modules.Assessment` owns deterministic grading, the `Assessment` and `AssessmentTestResult` entities, their EF Core configurations, and the result endpoint. It references `Psel.Infrastructure` for the shared `PselDbContext` and `Psel.Modules.Execution` for the execution status contract.
- It does not reference `Psel.Modules.Submissions` or `Psel.Modules.Challenges`. `Assessments.SubmissionId` is unique but intentionally **not** a foreign key to `Submissions`; the host composes both modules over one shared model and transaction boundary.
- The submission worker builds a plain `AssessmentInput` from the worker's execution outcome and the server-selected test plan, calls `AssessmentService.Record`, and saves the assessment and the terminal submission state together.

### 2. Purpose selects tests server-side

`Submission.Purpose` defaults to `Run`.

- `Run` selects only public tests.
- `Submit` selects public and hidden tests.

The challenge loader reads `tests/public` and `tests/hidden` into a server-only `ChallengeTestPlan`. The worker selects the subset; the client never sends test identifiers, filters, or counts. Challenge detail exposes public test names and descriptions but no hidden material and no test data.

### 3. Harness result protocol

The runner writes the selected tests as `/workspace/PselTests.json`. Each entry carries an opaque identifier, its visibility, and its data. The trusted harness must emit exactly one observation per supplied test:

```text
__PSEL_RESULTS_BEGIN__
[{"id":"public-1","passed":true,"errored":false}, ...]
__PSEL_RESULTS_END__
```

Only the opaque `id`, `passed`, and `errored` values cross back to the server. Test names, descriptions, and data stay in server memory. The markers and the JSON block are removed from execution output before it is persisted or returned to the client.

### 4. Deterministic scoring

- Every selected test has equal weight.
- `score = passed * 100 / total` using integer arithmetic and truncation.
- A non-successful execution scores `0` and marks every selected test `NotRun`.
- A successful execution whose result block is missing, duplicated, incomplete, extra, or malformed scores `0`, marks every test `NotRun`, and returns `InternalRunnerError`. No partial score is inferred.
- `Accepted` requires every selected test to pass.
- Any harness-reported per-test `errored` yields `RuntimeError` while still recording the observed pass count.
- Otherwise a successful execution with at least one failed test is `WrongAnswer`.

### 5. Outcome mapping

| Execution status | Assessment outcome |
|---|---|
| `Succeeded`, all tests passed | `Accepted` |
| `Succeeded`, a harness test errored | `RuntimeError` |
| `Succeeded`, otherwise | `WrongAnswer` |
| `CompilationError` | `CompilationError` |
| `RuntimeError` | `RuntimeError` |
| `TimeLimitExceeded` | `TimeLimitExceeded` |
| `MemoryLimitExceeded` | `MemoryLimitExceeded` |
| `OutputLimitExceeded` | `OutputLimitExceeded` |
| `ExecutionUnavailable`, `InternalError`, `Cancelled` | `InternalRunnerError` |

`Submission.Status` continues to describe the execution lifecycle; the assessment outcome is separate. A submission can be `Succeeded` while its assessment is `WrongAnswer` or `RuntimeError`.

### 6. Append-only assessment evidence

One `Assessment` row is written per submission attempt, with public/hidden pass and total counts and one `AssessmentTestResult` row per selected test in selection order. Test results persist visibility and status for server-side diagnosis but are never returned wholesale. Assessments are never recomputed in place.

### 7. Public-only result projection

`GET /api/submissions/{id}/results` returns the outcome, score, pass counts, and public test names, descriptions, and statuses. Hidden test results are filtered out; only `hiddenPassed` and `hiddenTotal` are exposed. Reference solutions, harness source, hidden data, and submitted source text are never returned. The endpoint returns `404` until an assessment exists.

### 8. Fail closed on evidence

If grading throws, the worker logs the failure and still persists the terminal submission state without an assessment rather than leaving the attempt stuck `Running`. The result endpoint then returns `404` for that attempt, which is observable and honest.

### 9. Reserved runner paths

`Submission.csproj`, `PselHarness.cs`, and `PselTests.json` are rejected by both the submission service and the runner, so a client cannot overwrite the project, harness, or test manifest.

## Alternatives considered

### Grade inside the submissions module

Rejected. It would mix execution lifecycle and grading rules, make deterministic scoring harder to test in isolation, and encourage the submissions module to own hidden-test projection. A separate module keeps the scoring contract narrow and independently testable.

### Let the client select tests or see hidden results

Rejected. Test selection and hidden material are trust-boundary inputs. Allowing either would let a client choose easier tests or learn the hidden suite.

### Fail the whole submission when grading fails

Rejected. The execution result is still valid evidence even when grading cannot be completed. Persisting the terminal state and returning `404` for the missing assessment avoids a stuck row and keeps the failure visible.

### Score by test category or difficulty

Rejected for Phase 5. Equal weight per selected test is simple, explainable, and reproducible. Weighted scoring needs a product decision that is not in scope.

### Parse free-form harness output

Rejected. Natural-language or ad-hoc output cannot be validated reliably. A delimited JSON block with opaque identifiers makes completeness checkable and lets the server reject ambiguous evidence.

### Persist the assessment through a cross-module foreign key

Rejected. A foreign key would couple the assessment schema to the submissions module and undermine the module boundary. The unique `SubmissionId` plus the shared transaction satisfies the one-assessment-per-attempt rule without the coupling.

## Consequences

### Positive

- Grading is deterministic and independent of HTTP, Docker, and database access.
- The client cannot select tests, influence scoring, or observe hidden material.
- Hidden tests, reference solutions, and harnesses stay server-side.
- A malformed result fails closed instead of producing a misleading score.
- Public test feedback remains actionable while hidden counts stay aggregate.
- The assessment module can be unit-tested without a database or container.
- The submission worker records the assessment in the same save as the terminal state.

### Negative

- The harness must be authored to the result protocol; non-conforming harnesses yield `InternalRunnerError`.
- Test data is visible to submitted code at runtime through `/workspace/PselTests.json`, because the harness must read it. A determined submission can inspect inputs and special-case them.
- The score is coarse: integer truncation and equal weights can hide per-test importance.
- History, progress aggregation, and exam mode are still out of scope.

### Mitigations

- Document the harness protocol and result rules in [challenges/README.md](../../challenges/README.md).
- Test every outcome, scoring edge, malformed block, duplicate/extra observation, sanitization, and deterministic re-grade in the automated suite.
- Verify with integration tests that `Run` selects only public tests, `Submit` selects public and hidden tests, and hidden names and data never appear in responses.
- Keep the client projection explicit: only public test rows and hidden counts are mapped.
- Document the `PselTests.json` visibility limitation in [EXECUTION_SECURITY.md](../EXECUTION_SECURITY.md).
