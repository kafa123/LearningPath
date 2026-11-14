# PSEL API Specification

## 1. Scope

This document defines the Phase 8 HTTP contract. The health endpoint, read-only learning catalog, read-only challenge catalog, language catalog, submission endpoints, assessment result endpoint, progress endpoints, and examination endpoints are implemented. SQL challenges reuse the language and submission contracts; there is no SQL-specific endpoint.

## 2. Conventions

| Item | Value |
|---|---|
| Base URL (development) | `http://localhost:5080` |
| Base path | `/api` |
| Content type | `application/json` |
| Error format | RFC 7807 ProblemDetails |
| API contract version | 1 |
| Time format | ISO 8601 UTC (`Z`) |

The `/api` base path is treated as contract version 1, matching `PROJECT_SPEC.md`. Breaking changes require a new base path (for example `/api/v2`) or a separately documented versioning mechanism. Additive, non-breaking changes may be made within version 1.

The development-only OpenAPI document is available at:

```text
http://localhost:5080/openapi/v1.json
```

## 3. Health

### `GET /api/health`

Returns the current application and database health.

**Request:** no parameters, no body.

**Response: 200 OK**

```json
{
  "status": "Healthy",
  "timestampUtc": "2026-11-07T12:00:00.0000000Z",
  "version": "0.1.0.0",
  "totalDurationMs": 1.42,
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "durationMs": 0.81
    }
  ]
}
```

**Response: 503 Service Unavailable**

The same shape is returned when a check is unhealthy or degraded. The overall `status` reflects the worst check status.

**Security constraints:**

- Do not include exception messages, stack traces, connection strings, file paths, or environment details.
- Do not expose the health endpoint beyond the local machine.

### Status values

`Healthy`, `Degraded`, `Unhealthy`.

## 4. Learning catalog

The learning endpoints are read-only and unauthenticated. Content is seeded on startup. Slugs are stable ASCII identifiers; clients must URL-encode them. Responses expose only public content and intentionally omit internal GUIDs and persistence details.

### `GET /api/domains`

Returns the learning domains ordered by catalog order, with a module count for browsing.

**Response: 200 OK**

```json
[
  {
    "slug": "programming-fundamentals",
    "name": "Programming Fundamentals",
    "description": "Algorithms, data structures, complexity, and the core techniques behind reliable programs.",
    "moduleCount": 3
  }
]
```

### `GET /api/learning-paths`

Returns every learning path with its modules and prerequisite slugs. The response is ordered by domain order and module order.

**Response: 200 OK**

```json
[
  {
    "slug": "programming-fundamentals",
    "name": "Programming Fundamentals",
    "description": "Algorithms, data structures, complexity, and the core techniques behind reliable programs.",
    "modules": [
      {
        "slug": "complexity-analysis",
        "title": "Complexity Analysis",
        "summary": "Compare algorithms by how their work grows as the input grows.",
        "level": "Intermediate",
        "estimatedMinutes": 50,
        "prerequisiteSlugs": ["algorithm-foundations"]
      }
    ]
  }
]
```

### `GET /api/learning-paths/{slug}`

Returns one learning path in the same shape as an item from `/api/learning-paths`.

**Response: 404 Not Found** when no path matches the slug. The body is `application/problem+json`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Learning path not found.",
  "status": 404,
  "detail": "No learning path matches the requested slug."
}
```

### `GET /api/modules/{slug}`

Returns a module with its learning objectives, materials, competencies, and prerequisites.

**Response: 200 OK**

```json
{
  "slug": "complexity-analysis",
  "title": "Complexity Analysis",
  "summary": "Compare algorithms by how their work grows as the input grows.",
  "level": "Intermediate",
  "estimatedMinutes": 50,
  "domain": {
    "slug": "programming-fundamentals",
    "name": "Programming Fundamentals"
  },
  "objectives": [
    "Count the dominant operation in a loop or recursive call."
  ],
  "materials": [
    {
      "slug": "complexity-analysis-lesson",
      "title": "Measuring growth with Big-O",
      "kind": "Lesson",
      "estimatedMinutes": 20
    }
  ],
  "competencies": [
    {
      "slug": "asymptotic-analysis",
      "name": "Asymptotic analysis",
      "description": "Estimate time and space growth using Big-O notation."
    }
  ],
  "prerequisites": [
    {
      "slug": "algorithm-foundations",
      "title": "Algorithm Foundations",
      "domainSlug": "programming-fundamentals"
    }
  ]
}
```

**Response: 404 Not Found** when no module matches the slug.

### `GET /api/materials/{slug}`

Returns one learning material, including its Markdown body and the module context needed for navigation.

**Response: 200 OK**

```json
{
  "slug": "complexity-analysis-lesson",
  "title": "Measuring growth with Big-O",
  "kind": "Lesson",
  "estimatedMinutes": 20,
  "module": {
    "slug": "complexity-analysis",
    "title": "Complexity Analysis",
    "domainSlug": "programming-fundamentals"
  },
  "bodyMarkdown": "## Measuring growth, not seconds\n\n..."
}
```

**Response: 404 Not Found** when no material matches the slug.

**Content and security rules:**

- Learning content is trusted seed data. Internal identifiers, hidden tests, and reference solutions are never part of these responses.
- The client renders `bodyMarkdown` with `react-markdown` and GitHub-flavored Markdown. Raw HTML rendering stays disabled.
- These endpoints are read-only; authoring and progress mutations belong to later phases.

## 5. Challenge catalog

Challenge endpoints are read-only and unauthenticated. Content comes from filesystem packages validated once at startup. Slugs are stable ASCII identifiers; clients must URL-encode them. The responses expose only public content.

### `GET /api/challenges`

Returns the validated challenge summaries ordered by title. Each summary omits statements, starter code, and test content.

**Response: 200 OK**

```json
[
  {
    "slug": "two-sum",
    "title": "Two Sum",
    "summary": "Return the indices of the two numbers that add up to a target value.",
    "domainSlug": "programming-fundamentals",
    "topic": "Arrays and hash maps",
    "difficulty": "Beginner",
    "type": "Coding",
    "estimatedMinutes": 30,
    "languages": [
      { "id": "csharp", "name": "C#" },
      { "id": "python", "name": "Python" }
    ],
    "prerequisiteSlugs": ["algorithm-foundations", "data-structures"],
    "competencySlugs": ["algorithm-decomposition", "correctness-reasoning"]
  }
]
```

### `GET /api/challenges/{slug}`

Returns one challenge with its statement, hints, objectives, constraints, examples, language starter files, public-test summaries, and evaluation limits.

**Response: 200 OK**

```json
{
  "slug": "two-sum",
  "title": "Two Sum",
  "summary": "Return the indices of the two numbers that add up to a target value.",
  "domainSlug": "programming-fundamentals",
  "topic": "Arrays and hash maps",
  "difficulty": "Beginner",
  "type": "Coding",
  "estimatedMinutes": 30,
  "objectives": [
    "Translate a problem statement into an explicit function contract."
  ],
  "prerequisiteSlugs": ["algorithm-foundations", "data-structures"],
  "competencySlugs": ["algorithm-decomposition", "correctness-reasoning"],
  "constraints": ["Each input has exactly one valid answer."],
  "examples": [
    {
      "input": "numbers = [2, 7, 11, 15], target = 9",
      "output": "[0, 1]",
      "explanation": "numbers[0] + numbers[1] = 2 + 7 = 9."
    }
  ],
  "languages": [
    {
      "id": "csharp",
      "name": "C#",
      "editorLanguage": "csharp",
      "canExecute": true,
      "starterFiles": [
        {
          "path": "Solution.cs",
          "content": "namespace Challenge;\n\npublic static class Solution\n{\n    ...\n}\n"
        }
      ]
    },
    {
      "id": "python",
      "name": "Python",
      "editorLanguage": "python",
      "canExecute": true,
      "starterFiles": [
        {
          "path": "solution.py",
          "content": "def two_sum(numbers: list[int], target: int) -> list[int]:\n    ...\n"
        }
      ]
    }
  ],
  "publicTests": [
    {
      "name": "Example 1",
      "description": "The first pair adds up to the target."
    }
  ],
  "evaluation": {
    "kind": "AutomatedTests",
    "timeLimitSeconds": 5,
    "memoryLimitMb": 256
  },
  "examination": {
    "variants": [
      { "id": "standard", "label": "Standard examination", "timeLimitSeconds": 600 },
      { "id": "extended", "label": "Extended examination", "timeLimitSeconds": 900 }
    ]
  },
  "statementMarkdown": "# Two Sum\n\nGiven an array of integers ...",
  "hintsMarkdown": "# Hints\n\n1. Try the single-element case."
}
```

`languages[].editorLanguage` is the Monaco language id. `starterFiles[].path` is a display path inside the package; `content` is the starter text. `languages[].canExecute` reports whether the server has a trusted execution harness for that language. `competencySlugs` maps the challenge to seeded competencies for progress derivation. `examination.variants` lists the server-selectable variants; a manifest without the block exposes one untimed `standard` variant. Response property names use camelCase. Harness source is never part of this response. A SQL challenge is a normal detail whose language has `"id": "sql"`, `"editorLanguage": "sql"`, and `"canExecute": true`; its SQL schema and seed are not returned.

**Response: 404 Not Found** when no challenge matches the slug. The body is `application/problem+json`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Challenge not found.",
  "status": 404,
  "detail": "No challenge matches the requested slug."
}
```

**Content and security rules:**

- Hidden test content, hidden test names and paths, reference solutions, and solution paths are never returned.
- Hidden test files are existence-checked at startup but excluded from the response model. Reference solutions are excluded structurally because the manifest has no solution reference.
- Execution harnesses are loaded and validated server-side but are not part of the client-facing contract. Only the boolean `canExecute` is exposed.
- Challenge content is trusted authoring input, but responses still expose only the client-safe projection.
- The workspace renders `statementMarkdown` and `hintsMarkdown` with `react-markdown`; raw HTML stays disabled.
- These endpoints are read-only. Submission endpoints are documented below.

## 6. Languages

### `GET /api/languages`

Returns the languages the server is configured to execute. This is server configuration, not a client-supplied value.

**Response: 200 OK**

```json
[
  { "id": "csharp", "name": "C#" },
  { "id": "python", "name": "Python" },
  { "id": "sql", "name": "SQL" }
]
```

The endpoint does not imply that every challenge supports every language. Use `languages[].canExecute` on the challenge detail.

## 7. Submissions

Submission endpoints are unauthenticated, like the rest of the Phase 8 API, and are intended for the local machine only.

### `POST /api/submissions`

Validates and queues one execution attempt. The server validates the challenge, the language, the server-side harness, file paths, file extensions, and source-size limits before persisting the attempt. Clients cannot supply an image, command, mount, environment variable, harness, test selection, or limit.

**Request**

```json
{
  "challengeSlug": "two-sum",
  "languageId": "csharp",
  "purpose": "Submit",
  "examinationId": "b7c1f0a2-4d3e-4f6a-9b8c-2d1e0f3a4b5c",
  "files": [
    {
      "path": "Solution.cs",
      "content": "namespace Challenge;\n\npublic static class Solution\n{\n    ...\n}\n"
    }
  ]
}
```

`examinationId` is optional. Omit it for practice runs and normal submissions. When present, the server validates the examination before queuing the attempt.

The same request for Python:

```json
{
  "challengeSlug": "two-sum",
  "languageId": "python",
  "purpose": "Run",
  "files": [
    {
      "path": "solution.py",
      "content": "def two_sum(numbers: list[int], target: int) -> list[int]:\n    ...\n"
    }
  ]
}
```

The same request for SQL. Only the learner query is sent; the schema, seed data, and harness come from the challenge package:

```json
{
  "challengeSlug": "order-revenue",
  "languageId": "sql",
  "purpose": "Submit",
  "files": [
    {
      "path": "query.sql",
      "content": "SELECT c.name, COUNT(o.id) AS completed_orders, ... FROM customers AS c ...;\n"
    }
  ]
}
```

Rules:

- `challengeSlug` must match a validated package.
- `languageId` must be part of the challenge and have `canExecute: true`.
- `purpose` is `Run` or `Submit`. It is case-insensitive and defaults to `Run` when omitted.
- `examinationId` is optional. When present, the examination must exist, be `InProgress`, not expired, and target the same `challengeSlug`. An examination attempt must use `purpose: "Submit"`; `Run` is rejected. The server records the attempt link with the submission.
- `Run` selects only public tests. `Submit` selects public and hidden tests.
- At least one file and at most 10 files are accepted. SQL accepts exactly one file.
- Paths are relative, may contain subdirectories, and must not be absolute or contain `.`/`..` segments.
- Server-reserved runner filenames (`Submission.csproj`, `PselHarness.cs`, `PselHarness.py`, `PselTests.json`) are rejected.
- File extensions must be accepted by the selected runner (`.cs` for C#, `.py` for Python, `.sql` for SQL).
- Each file is limited to 128 KiB and the submission to 256 KiB of source text.
- SQL content must be a single read-only `SELECT` or `WITH` statement. Syntax errors and non-read-only statements return `CompilationError`; per-test SQL failures are reported as `RuntimeError`.
- Apply execution isolation server-side: SQL runs in a disposable container against a fresh in-memory SQLite database, not the application database.

**Response: 202 Accepted**

```json
{
  "id": "0f1d2b5e-6f5a-4a0e-9a4b-1c2d3e4f5a6b",
  "status": "Queued",
  "createdAtUtc": "2026-11-08T10:15:30.0000000Z"
}
```

The `Location` header points to the submission resource. Execution is asynchronous; poll the resource until `isTerminal` is `true`.

**Response: 400 Bad Request**

The body is `application/problem+json` with a client-safe `detail`, for example:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Submission rejected.",
  "status": 400,
  "detail": "This challenge cannot be executed with the selected language."
}
```

**Response: 503 Service Unavailable**

Returned when the bounded execution queue is full. The attempt is persisted as unavailable and the client may retry later.

### `GET /api/submissions/{id}`

Returns one execution attempt.

**Response: 200 OK**

```json
{
  "id": "0f1d2b5e-6f5a-4a0e-9a4b-1c2d3e4f5a6b",
  "challengeSlug": "two-sum",
  "languageId": "csharp",
  "status": "Succeeded",
  "isTerminal": true,
  "createdAtUtc": "2026-11-08T10:15:30.0000000Z",
  "startedAtUtc": "2026-11-08T10:15:30.2500000Z",
  "completedAtUtc": "2026-11-08T10:15:36.7500000Z",
  "durationMs": 6500,
  "exitCode": 0,
  "standardOutput": "all tests ran\n",
  "standardError": "",
  "failureMessage": null,
  "outputTruncated": false,
  "purpose": "Submit"
}
```

`status` is one of:

- `Queued`, `Running` — non-terminal.
- `Succeeded`.
- `CompilationError`.
- `RuntimeError`.
- `TimeLimitExceeded`.
- `MemoryLimitExceeded`.
- `OutputLimitExceeded`.
- `ExecutionUnavailable` — the server cannot execute, for example because Docker is not installed or running.
- `InternalError`.
- `Interrupted` — the process restarted or the execution was cancelled before completion.

**Response: 404 Not Found** when no submission matches the id.

**Content and security rules:**

- The response never includes submitted source text, the execution harness, hidden tests, reference solutions, the container image, the command, environment variables, or workspace paths.
- `standardOutput` and `standardError` are capped at the configured server limit; `outputTruncated` reports truncation.
- Execution output is untrusted text. Clients must render it as text, never as HTML.
- The harness result block is removed from `standardOutput` before it is persisted or returned.
- `status` reports the execution lifecycle. Deterministic grading is reported separately by the result endpoint.

### `GET /api/submissions/{id}/results`

Returns the deterministic assessment recorded for one submission attempt. Exactly one assessment exists per submission; the endpoint returns `404` until an assessment has been recorded.

**Response: 200 OK**

```json
{
  "submissionId": "0f1d2b5e-6f5a-4a0e-9a4b-1c2d3e4f5a6b",
  "challengeSlug": "two-sum",
  "languageId": "csharp",
  "purpose": "Submit",
  "outcome": "WrongAnswer",
  "isAccepted": false,
  "score": 75,
  "passedTests": 3,
  "totalTests": 4,
  "publicPassed": 2,
  "publicTotal": 2,
  "hiddenPassed": 1,
  "hiddenTotal": 2,
  "publicTests": [
    {
      "name": "Example 1",
      "description": "The first pair adds up to the target.",
      "status": "Passed"
    },
    {
      "name": "Example 2",
      "description": "The matching pair is not at the start of the array.",
      "status": "Passed"
    }
  ],
  "createdAtUtc": "2026-11-08T10:15:36.7500000Z"
}
```

`outcome` is one of:

- `Accepted` — every selected test passed.
- `WrongAnswer` — execution succeeded but at least one selected test failed.
- `CompilationError`.
- `RuntimeError` — execution failed, or the harness reported a test-level exception.
- `TimeLimitExceeded`.
- `MemoryLimitExceeded`.
- `OutputLimitExceeded`.
- `InternalRunnerError` — the runner failed or produced an unreadable result block.

Scoring rules:

- Every selected test has equal weight.
- `score` is `passedTests * 100 / totalTests` using integer truncation.
- A non-successful execution scores `0` and marks every selected test `NotRun`.
- `Run` assesses only public tests. Hidden counts are `0`.
- `Submit` assesses public and hidden tests.

**Response: 404 Not Found** when no assessment exists for the submission.

**Content and security rules:**

- `publicTests` contains only public test names, descriptions, and statuses.
- Hidden test names, descriptions, identifiers, and data are never returned. Only the hidden pass and total counts are exposed.
- Reference solutions and execution harnesses are never returned.
- Submitted source text is never returned.

## 8. Progress

Progress endpoints are unauthenticated and local-only, like the rest of the API. Progress is derived on read from append-only assessment evidence and examination attempts; there is no mutable progress row.

### `GET /api/progress`

Returns every seeded domain, module, and competency with its derived state and recent evidence.

**Request:** no parameters, no body.

**Response: 200 OK**

```json
{
  "summary": {
    "total": 24,
    "notStarted": 20,
    "learning": 2,
    "practicing": 1,
    "assessmentRequired": 1,
    "mastered": 0
  },
  "domains": [
    {
      "slug": "programming-fundamentals",
      "name": "Programming Fundamentals",
      "description": "Algorithms, data structures, and the core techniques behind reliable programs.",
      "sortOrder": 1,
      "modules": [
        {
          "slug": "algorithm-foundations",
          "title": "Algorithm Foundations",
          "level": "Beginner",
          "sortOrder": 1,
          "competencies": [
            {
              "slug": "algorithm-decomposition",
              "name": "Problem decomposition",
              "description": "Break a requirement into inputs, outputs, and edge cases.",
              "state": "Learning",
              "nextAction": "Submit a mapped challenge to run the hidden assessment tests.",
              "evidence": [
                {
                  "challengeSlug": "two-sum",
                  "challengeTitle": "Two Sum",
                  "purpose": "Run",
                  "outcome": "Accepted",
                  "score": 100,
                  "passedTests": 2,
                  "totalTests": 2,
                  "isExamination": false,
                  "createdAtUtc": "2026-11-11T10:00:00Z"
                }
              ]
            }
          ]
        }
      ]
    }
  ]
}
```

**State rules:**

| State | Evidence |
|---|---|
| `NotStarted` | No assessment evidence on any challenge mapped to the competency. |
| `Learning` | At least one assessment, but no `Submit` attempt. |
| `Practicing` | At least one `Submit`, but none accepted. |
| `AssessmentRequired` | An accepted `Submit`, but no accepted examination submission or fewer than two distinct mapped challenges accepted. |
| `Mastered` | An accepted examination submission plus accepted `Submit` evidence on at least two distinct mapped challenges. |

Evidence is newest first and capped at five entries per competency. A challenge contributes evidence only when its `competencySlugs` includes the competency. `summary` counts competencies by state and `total` is their sum.

## 9. Assessment history

### `GET /api/assessments`

Returns recent assessment evidence newest first, including whether each attempt came from an examination.

**Query parameters:**

| Name | Type | Default | Rules |
|---|---|---|---|
| `limit` | integer | 20 | Clamped to 1–100. |

**Response: 200 OK**

```json
[
  {
    "submissionId": "0f1d2b5e-6f5a-4a0e-9a4b-1c2d3e4f5a6b",
    "challengeSlug": "two-sum",
    "challengeTitle": "Two Sum",
    "languageId": "csharp",
    "purpose": "Submit",
    "outcome": "Accepted",
    "isAccepted": true,
    "score": 100,
    "passedTests": 4,
    "totalTests": 4,
    "publicPassed": 2,
    "publicTotal": 2,
    "hiddenPassed": 2,
    "hiddenTotal": 2,
    "isExamination": true,
    "examinationId": "b7c1f0a2-4d3e-4f6a-9b8c-2d1e0f3a4b5c",
    "examinationVariant": "Standard examination",
    "createdAtUtc": "2026-11-11T10:00:00Z"
  }
]
```

The endpoint never returns hidden test names, descriptions, identifiers, or data. It returns only hidden pass/total counts, matching the result projection.

## 10. Examinations

Examinations are server-controlled. The server chooses the variant and time limit from the challenge manifest, selects hidden tests through the normal submission pipeline, and removes hints from the challenge projection. Clients supply only the challenge slug and their source files.

### `POST /api/examinations`

**Request**

```json
{
  "challengeSlug": "two-sum"
}
```

**Response: 201 Created**

The `Location` header points to `/api/examinations/{id}`.

```json
{
  "id": "b7c1f0a2-4d3e-4f6a-9b8c-2d1e0f3a4b5c",
  "challengeSlug": "two-sum",
  "challengeTitle": "Two Sum",
  "variantId": "standard",
  "variantLabel": "Standard examination",
  "timeLimitSeconds": 600,
  "status": "InProgress",
  "isActive": true,
  "remainingSeconds": 598,
  "startedAtUtc": "2026-11-11T10:00:00Z",
  "expiresAtUtc": "2026-11-11T10:10:00Z",
  "completedAtUtc": null,
  "challenge": {
    "slug": "two-sum",
    "title": "Two Sum",
    "hintsMarkdown": null
  }
}
```

The `challenge` field is the full `ChallengeDetail` contract with `hintsMarkdown` set to `null`. Hidden tests, reference solutions, and harnesses are not added to it.

**Response: 400 Bad Request**

A ProblemDetails response is returned when the challenge does not exist, cannot be executed, has no hidden assessment tests, or has no examination variants.

### `GET /api/examinations`

Returns examination sessions newest first.

**Query parameters:**

| Name | Type | Default | Rules |
|---|---|---|---|
| `limit` | integer | 20 | Clamped to 1–50. |

**Response: 200 OK**

```json
[
  {
    "id": "b7c1f0a2-4d3e-4f6a-9b8c-2d1e0f3a4b5c",
    "challengeSlug": "two-sum",
    "challengeTitle": "Two Sum",
    "variantId": "standard",
    "variantLabel": "Standard examination",
    "timeLimitSeconds": 600,
    "status": "InProgress",
    "isActive": true,
    "remainingSeconds": 598,
    "startedAtUtc": "2026-11-11T10:00:00Z",
    "expiresAtUtc": "2026-11-11T10:10:00Z",
    "completedAtUtc": null,
    "attemptCount": 1
  }
]
```

`status` is `InProgress`, `Completed`, or `Expired`. `isActive` is true only while the session is `InProgress` and has not exceeded its limit. `remainingSeconds` is `null` when the variant has no time limit.

### `GET /api/examinations/{id}`

Returns one session and its hint-free challenge projection.

**Response: 200 OK** — the same shape as the create response.

**Response: 404 Not Found** — the session does not exist.

An expired session is transitioned to `Expired` when read, and the response reflects that state.

### `POST /api/examinations/{id}/finish`

Closes an active session. The request has no body.

**Response: 200 OK** — the updated session with `status: "Completed"`, `isActive: false`, and `completedAtUtc` set.

**Response: 404 Not Found** — the session does not exist.

Finishing an already completed or expired session returns its current state without reopening it. After a session is closed, submissions for that `examinationId` are rejected.

## 11. Error handling

Unhandled API errors are returned as ProblemDetails:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An unexpected error occurred.",
  "status": 500,
  "traceId": "..."
}
```

Internal exception details must not be returned to the client. Diagnostic details belong in server logs.

## 12. Planned endpoints

All MVP endpoints listed in [PROJECT_SPEC.md](../PROJECT_SPEC.md) are implemented through Phase 8. SQL challenges deliberately reuse `POST /api/submissions` and the existing result endpoints; there are no separate database-execution endpoints.

Submission responses distinguish automated results from rubric-based judgments. Automated assessment is implemented; rubric-based authoring and review remain out of scope.

## 13. Validation and safety rules

- Validate all incoming data.
- Validate challenge IDs and language IDs against trusted server-side configuration. This is implemented for submissions.
- Reject arbitrary image names, runner commands, mount paths, and environment variables from clients. This is implemented; these values are never part of the request contract.
- Sanitize filenames and prevent path traversal. This is implemented in submission validation and again in the runner.
- Limit request body sizes for source-code submissions. This is implemented with per-file and total source limits plus the bounded queue.
- Apply execution time, memory, process, and output limits server-side. This is implemented in the Docker runner.
- Do not log secrets or full submitted source content. Submission logs record identifiers and outcomes, not source text.
- Enforce examination rules server-side: reject `Run` for an examination, reject a mismatched challenge, reject closed or expired sessions, and remove hints from the examination projection. This is implemented in the progress module.
- Return only permitted results for the current mode (practice, exam, review). Practice and examination modes are implemented; a separate review mode is not.
