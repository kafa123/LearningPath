# Challenge Packages

Challenge content is **filesystem-backed**, not stored in the database. Each
directory below this folder is one challenge package. The API discovers every
`challenge.json` recursively, validates all packages strictly at startup, and
serves the validated catalog read-only.

A package that fails validation prevents the API from starting. Invalid
packages never become browsable and never partially load.

For the end-to-end workflow, including learning content, competency mapping, and
database resets, see [docs/ADDING_CONTENT.md](../docs/ADDING_CONTENT.md).

## Directory layout

```text
challenges/
  <domain>/<slug>/
    challenge.json     Required manifest, one per package
    statement.md       Challenge statement rendered as Markdown
    hints.md           Optional hints rendered as Markdown
    starter/<language>/...   Starter files edited in Monaco
    harness/<language>/...   Server-only execution harness; never sent to the browser
    sql/schema.sql           Required for SQL packages; server-only schema
    sql/seed.sql             Optional for SQL packages; server-only seed rows
    tests/
      public/...       Public test data; names/descriptions and statuses are exposed
      hidden/...       Hidden test data; only pass/total counts are exposed
    solutions/<language>/... Reference solutions; never read into API responses
```

Packages are grouped by domain directory for humans, but discovery is recursive
and does not depend on the folder name. The manifest's `domainSlug` is
authoritative. The filesystem path never doubles as the public slug.

## Manifest (`challenge.json`)

The manifest uses strict JSON: unknown fields, comments, and trailing commas are
rejected. `schemaVersion` must be `1`.

```json
{
  "schemaVersion": 1,
  "slug": "two-sum",
  "title": "Two Sum",
  "summary": "Return the indices of the two numbers that add up to a target value.",
  "domainSlug": "programming-fundamentals",
  "topic": "Arrays and hash maps",
  "difficulty": "Beginner",
  "type": "Coding",
  "estimatedMinutes": 30,
  "objectives": ["Translate a problem statement into a function contract."],
  "prerequisites": ["algorithm-foundations"],
  "competencySlugs": [
    "algorithm-decomposition",
    "correctness-reasoning",
    "structure-selection"
  ],
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
      "harness": "harness/csharp/PselHarness.cs",
      "starterFiles": [
        { "path": "Solution.cs", "source": "starter/csharp/Solution.cs" }
      ]
    },
    {
      "id": "python",
      "name": "Python",
      "editorLanguage": "python",
      "harness": "harness/python/PselHarness.py",
      "starterFiles": [
        { "path": "solution.py", "source": "starter/python/solution.py" }
      ]
    }
  ],
  "statement": "statement.md",
  "hints": "hints.md",
  "tests": {
    "public": [
      {
        "name": "Example 1",
        "description": "The first pair adds up to the target.",
        "source": "tests/public/example-1.json"
      }
    ],
    "hidden": [
      {
        "name": "Negative numbers",
        "description": "Pairs may include negative values.",
        "source": "tests/hidden/negative-numbers.json"
      }
    ]
  },
  "evaluation": {
    "kind": "AutomatedTests",
    "timeLimitSeconds": 5,
    "memoryLimitMb": 256
  },
  "examination": {
    "variants": [
      {
        "id": "standard",
        "label": "Standard examination",
        "timeLimitSeconds": 600
      },
      {
        "id": "extended",
        "label": "Extended examination",
        "timeLimitSeconds": 900
      }
    ]
  }
}
```

### Field rules

| Field | Rule |
|---|---|
| `schemaVersion` | Must be `1`. |
| `slug` | Lowercase letters, numbers, and single hyphens; unique across all packages. |
| `title` | Required, at most 150 characters. |
| `summary` | Required, at most 400 characters. |
| `domainSlug` | Slug identifying the owning learning domain; authors should use a seeded domain slug. |
| `topic` | Required, at most 100 characters. |
| `difficulty` | `Beginner`, `Intermediate`, or `Advanced`. |
| `type` | `Coding`, `SQL`, `Debugging`, `Refactoring`, `API`, `Security`, `Architecture`, or `Performance`. |
| `estimatedMinutes` | Integer from 5 to 600. |
| `objectives` | One to 20 entries, each at most 300 characters. |
| `prerequisites` | Zero to 20 unique module slugs. |
| `competencySlugs` | Zero to 20 unique seeded competency slugs. A challenge contributes assessment evidence only to the competencies it lists. |
| `constraints` | Zero to 20 entries, each at most 300 characters. |
| `examples` | Zero to 10 entries; `input` and `output` required (at most 500 characters each), `explanation` optional (at most 1000 characters). |
| `languages` | At least one entry; language `id` unique within the package. |
| `languages[].name` | Required, at most 80 characters. |
| `languages[].editorLanguage` | Optional Monaco language id; defaults to `id`. |
| `languages[].harness` | Optional relative path to a server-only execution harness. When present and valid, the language is executable (`canExecute: true`). For `sql`, this field must be absent; the server supplies the harness. |
| `languages[].starterFiles` | At least one entry; unique `path` values. |
| `sql` | Optional block for SQL challenges. Requires a `schema` path and an optional `seed` path; requires a matching `languages` entry with `id: "sql"`. |
| `sql.schema` | Required inside `sql`; relative path to a non-empty SQL file, at most 128 KiB. |
| `sql.seed` | Optional inside `sql`; relative path to a SQL file, at most 128 KiB. |
| `statement` | Required relative path to a Markdown file, at most 256 KiB. |
| `hints` | Optional relative path to a Markdown file, at most 256 KiB. |
| `tests.public` | Required for `AutomatedTests` and `Hybrid`; test names unique. Each `source` must be a JSON object. Standard packages require `expected`, `cases`, or `expectedStatus`; SQL packages require `expected` or `plan`. |
| `tests.hidden` | Required for `AutomatedTests` and `Hybrid`; test names unique. Uses the same JSON shape rules as public tests. |
| `evaluation.kind` | `AutomatedTests`, `Rubric`, or `Hybrid`. |
| `evaluation.timeLimitSeconds` | Integer from 1 to 120. |
| `evaluation.memoryLimitMb` | Integer from 32 to 4096. |
| `examination` | Optional. When absent, the challenge exposes one default `standard` variant with no time limit. |
| `examination.variants` | Zero to 5 entries when present. Each variant has a unique `id` (max 60), a `label` (max 120), and an optional `timeLimitSeconds` from 60 to 7200. The server selects a variant; clients cannot request one. |
| `rubric` | Optional. Allowed only when `evaluation.kind` is `Rubric` or `Hybrid`; required for `Rubric` and rejected for `AutomatedTests`. |
| `rubric.criteria` | One to 12 unique criteria. Each has an `id` slug (max 60), a `title` (max 120), an optional `description` (max 500), `maxPoints` from 1 to 100, and an optional `weight` from 1 to 10 (default `1`). |

### Rubric criteria

A package that combines automated tests with reflective review can declare a rubric:

```json
{
  "evaluation": {
    "kind": "Hybrid",
    "timeLimitSeconds": 5,
    "memoryLimitMb": 256
  },
  "rubric": {
    "criteria": [
      {
        "id": "behavior-preservation",
        "title": "Behavior preservation",
        "description": "Public behavior stays identical for every tier and price.",
        "maxPoints": 10,
        "weight": 3
      },
      {
        "id": "clarity",
        "title": "Clarity",
        "description": "Names and structure make the rule easy to read.",
        "maxPoints": 10,
        "weight": 2
      }
    ]
  }
}
```

Criteria are server-owned and returned with the challenge detail. A learner may later score a terminal submission against them; the server computes the weighted 0–100 score and band, and the review never changes the automated assessment. Criteria ids are unique case-insensitively. Use `Hybrid` when a challenge has automated tests plus reflective review. A `Rubric` package must declare a rubric; stand-alone rubric-only review without an executable submission is not implemented yet.

### Path rules

Every `source` and `path` value is relative to the package directory.

- Absolute paths, drive/URL prefixes, `.`/`..` segments, and empty segments are
  rejected.
- A referenced file must exist and must resolve inside the package directory.
- Starter files are limited to 128 KiB; manifests and Markdown files to 256 KiB;
  harness files to 512 KiB; SQL schema and seed files to 128 KiB; test data files
  to 64 KiB.
- A `harness` file must exist, resolve inside the package directory, and contain
  non-whitespace text.
- Path traversal is rejected during validation, not at request time.

## Execution harness

`languages[].harness` points to a server-only source file that the execution
subsystem runs together with the learner's submitted files. It is the challenge
author's contract between the public starter API and a runnable program.

Four languages are executable:

- **C#** — the runner writes the submitted `.cs` files plus the harness into a
  generated console project, builds it, and runs it in an isolated container.
  The harness must contain the program entry point and call the learner's public
  API.
- **Python** — the runner writes the submitted `.py` files plus the harness and
  runs `python3 -B /workspace/PselHarness.py` in an isolated container. The
  harness imports the submitted solution module with `import solution`; the
  starter file must therefore be named `solution.py` at the package root of the
  workspace.
- **Go** — the runner writes the submitted `.go` files, a fixed `go.mod`, and the
  harness into one package, then runs `go build -trimpath -o /tmp/psel-submission .`
  followed by the binary inside `golang:1.27-alpine`. The container has no
  network, `GOPROXY=off`, `GOTOOLCHAIN=local`, and `CGO_ENABLED=0`, so harnesses
  may import only the standard library. The harness must be `package main` with a
  `func main()` and call the learner's public API.
- **SQL** — the package declares a `sql` block, and the loader supplies the
  server's built-in harness. The runner writes the single submitted `.sql` file
  and runs the same `python3 -B /workspace/PselHarness.py` command against a
  disposable in-memory SQLite database. Authors never write a SQL harness.

No harness may depend on network access, package installation, or reference
solutions at runtime.

For assessment, the runner also writes `/workspace/PselTests.json`. The harness
must read that file, run every supplied test, and emit a machine-readable block:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Challenge;

public static class Program
{
    private const string TestsPath = "/workspace/PselTests.json";

    public static void Main()
    {
        var results = new List<object>();
        using var document = JsonDocument.Parse(File.ReadAllText(TestsPath));

        foreach (var test in document.RootElement.GetProperty("tests").EnumerateArray())
        {
            var id = test.GetProperty("id").GetString() ?? string.Empty;
            var passed = false;
            var errored = false;

            try
            {
                var input = test.GetProperty("data").GetProperty("input");
                var expected = test.GetProperty("data").GetProperty("expected");
                // Read input, call Solution, and compare with expected.
                passed = /* comparison result */ true;
            }
            catch (Exception)
            {
                errored = true;
            }

            results.Add(new { id, passed, errored });
        }

        Console.WriteLine("__PSEL_RESULTS_BEGIN__");
        Console.WriteLine(JsonSerializer.Serialize(results));
        Console.WriteLine("__PSEL_RESULTS_END__");
    }
}
```

The equivalent Python harness:

```python
import json
from pathlib import Path

import solution

TESTS_PATH = Path("/workspace/PselTests.json")


def main() -> None:
    results = []

    with TESTS_PATH.open(encoding="utf-8") as stream:
        manifest = json.load(stream)

    for test in manifest["tests"]:
        test_id = test.get("id", "")
        passed = False
        errored = False

        try:
            data = test["data"]
            # Read input, call solution, and compare with data["expected"].
            passed = True
        except Exception:
            errored = True

        results.append({"id": test_id, "passed": passed, "errored": errored})

    print("__PSEL_RESULTS_BEGIN__")
    print(json.dumps(results))
    print("__PSEL_RESULTS_END__")


if __name__ == "__main__":
    main()
```

For Python, the harness is the executable module and must call the submitted
`solution` module's functions. Wrap each test in `try/except` so one failing case
does not abort the whole run. Use `math.isclose` for floating-point comparisons.

The equivalent Go harness skeleton:

```go
package main

import (
	"encoding/json"
	"fmt"
	"os"
)

const testsPath = "/workspace/PselTests.json"

type result struct {
	ID      string `json:"id"`
	Passed  bool   `json:"passed"`
	Errored bool   `json:"errored"`
}

func main() {
	raw, err := os.ReadFile(testsPath)
	if err != nil {
		emit(nil)
		return
	}

	var manifest struct {
		Tests []struct {
			ID   string          `json:"id"`
			Data json.RawMessage `json:"data"`
		} `json:"tests"`
	}
	if err := json.Unmarshal(raw, &manifest); err != nil {
		emit(nil)
		return
	}

	results := make([]result, 0, len(manifest.Tests))
	for _, test := range manifest.Tests {
		passed := false
		errored := false

		func() {
			defer func() {
				if recover() != nil {
					errored = true
				}
			}()
			// Decode test.Data, call the learner's exported API, compare.
			passed = true
		}()

		results = append(results, result{ID: test.ID, Passed: passed, Errored: errored})
	}
	emit(results)
}

func emit(results []result) {
	if results == nil {
		results = []result{}
	}
	payload, err := json.Marshal(results)
	if err != nil {
		payload = []byte("[]")
	}
	fmt.Println("__PSEL_RESULTS_BEGIN__")
	fmt.Println(string(payload))
	fmt.Println("__PSEL_RESULTS_END__")
}
```

A Go harness lives beside the submission in the same package, so helper types
and functions do not need an import path. Recover from panics per test so one bad
case does not abort the run.

The manifest shape is:

```json
{
  "schemaVersion": 1,
  "tests": [
    {
      "id": "public-1",
      "visibility": "public",
      "data": { "input": { "numbers": [2, 7], "target": 9 }, "expected": [0, 1] }
    }
  ]
}
```

Rules:

- Place harness files under `harness/<language>/` inside the package.
- The harness is trusted authoring content, but the loader still enforces the
  relative-path, existence, non-empty, and size rules above.
- The harness is loaded into server memory only. It is never part of
  `ChallengeDetail`, never returned by `GET /api/challenges`, and never sent to
  the browser.
- The harness must emit exactly one observation for every supplied test, with the
  opaque `id` and boolean `passed`/`errored` values. Missing, duplicate, extra,
  or malformed observations produce `InternalRunnerError`.
- A challenge without a harness for a language remains browsable and editable,
  but `canExecute` is `false` and submissions are rejected for that language.
- `Run` supplies only public tests. `Submit` supplies public and hidden tests.
  The client never selects tests.
- SQL packages do not provide harness files: the loader assigns the built-in
  harness and injects `sql.schema`/`sql.seed` into the test data. An
  author-supplied harness for the `sql` language is rejected.
- Submitted files named `Submission.csproj`, `PselHarness.cs`, `PselHarness.py`,
  `PselHarness.go`, `go.mod`, or `PselTests.json` are rejected before execution.
- C# submissions must use `.cs` files; Python submissions must use `.py` files;
  SQL submissions must use exactly one `.sql` file; Go submissions must use `.go`
  files and remain in `package main`.
- Python harnesses must import the submitted `solution` module rather than
  reading source files directly. Only the standard library is available because
  the container has no network and no package installation step.
- Go harnesses are compiled with the submitted files in one package, so they must
  not declare a conflicting `main`, `go.mod`, or duplicate symbol. Only the
  standard library is available; module downloads fail because `GOPROXY=off`.
- A C# harness needs a program entry point; a Python harness is executed
  directly as `PselHarness.py` and should keep its work under `main()`.

`challenges/backend/api-validation` intentionally has no harness: its starter is
an ASP.NET Core web application that the current runners cannot execute.

## SQL challenge packages

A SQL package uses the normal manifest plus a `sql` block and a single `sql`
language entry. It does not provide a harness:

```json
{
  "schemaVersion": 1,
  "slug": "order-revenue",
  "title": "Completed revenue per customer",
  "summary": "Aggregate completed order revenue for every customer.",
  "domainSlug": "database-engineering",
  "topic": "Joins, grouping, and NULL semantics",
  "difficulty": "Intermediate",
  "type": "SQL",
  "estimatedMinutes": 30,
  "objectives": ["Use a LEFT JOIN to keep customers without orders."],
  "prerequisites": ["sql-querying"],
  "competencySlugs": ["sql-composition", "null-semantics"],
  "constraints": ["Return exactly one row per customer."],
  "examples": [
    {
      "input": "One completed order worth 2500 cents.",
      "output": "name = 'Maya', revenue = 25.0"
    }
  ],
  "languages": [
    {
      "id": "sql",
      "name": "SQL",
      "editorLanguage": "sql",
      "starterFiles": [
        { "path": "query.sql", "source": "starter/sql/query.sql" }
      ]
    }
  ],
  "statement": "statement.md",
  "hints": "hints.md",
  "sql": {
    "schema": "sql/schema.sql",
    "seed": "sql/seed.sql"
  },
  "tests": {
    "public": [
      { "name": "Standard set", "source": "tests/public/standard.json" }
    ],
    "hidden": [
      { "name": "No orders", "source": "tests/hidden/no-orders.json" }
    ]
  },
  "evaluation": {
    "kind": "AutomatedTests",
    "timeLimitSeconds": 10,
    "memoryLimitMb": 256
  }
}
```

Rules:

- `sql` and a `languages` entry with `id: "sql"` must appear together. A SQL
  package with no `sql` block, or a `sql` block without the language, fails
  startup validation.
- A `sql` language must not declare `harness`; the server injects its built-in
  harness. The starter file is normally `query.sql`, but the runner accepts any
  single `.sql` path.
- The loader reads `sql.schema` and `sql.seed` into server memory and copies them
  into every selected test's `data`. They are never returned by the challenge,
  submission, or result endpoints.
- The learner query must start with `SELECT` or `WITH`. Writes, pragmas,
  attachments, and multiple statements are rejected. Results are capped at 1000
  rows and 64 columns, and each test query has a 5-second budget.

### SQL test files

A SQL test file is a JSON object with `expected`, `plan`, or both:

```json
{
  "columns": ["name", "completed_orders", "revenue"],
  "expected": [
    ["Alice", 2, 250.0],
    ["Bob", 1, 100.0]
  ],
  "ordered": true,
  "plan": {
    "mustContain": ["USING INDEX idx_orders_customer_date"],
    "mustNotContain": ["SCAN orders"]
  }
}
```

- `expected` is an array of rows; each row is an array of strings, numbers,
  booleans, or `null`. Floating-point values are compared with a small tolerance.
- `columns` is optional. When present it must match the result column names,
  compared case-insensitively.
- `ordered` defaults to `true`. Set it to `false` when row order is not part of
  the requirement.
- `plan` is optional. `mustContain` and `mustNotContain` are case-insensitive
  substring checks against `EXPLAIN QUERY PLAN`; a plan test should assert the
  intended access path rather than a timing threshold.
- `schema` and `seed` are optional per-test string overrides. They are useful for
  boundary cases such as empty tables or customers without orders. An override
  replaces the package-level file for that test.
- At least one of `expected` or `plan` is required. A package that returns the
  right rows but violates its plan still fails the test.

See `challenges/database/order-revenue` for a correctness package and
`challenges/database/order-index-plan` for a query-plan exercise.

## Competency mapping and examinations


`competencySlugs` connects a challenge to the seeded competency model. Use slugs that already exist in the learning seed; the API derives progress by matching assessment evidence from this challenge to those competencies, and the learning pages use the same field to surface related practice.

- A challenge with no `competencySlugs` remains fully playable but contributes no competency evidence.
- Evidence is recorded only for `Run` and `Submit` assessments. Progress states are derived by the server, not written by the challenge.
- To support `Mastered`, a competency needs accepted evidence from at least two distinct mapped challenges, one of which was submitted during an examination.
- Prefer two to four focused competencies per challenge. Listing every competency dilutes the signal.

Examinations reuse the normal submission and assessment pipeline. The optional `examination.variants` block declares the choices the server may select:

- `id` is a slug, unique within the block.
- `label` is shown to the learner.
- `timeLimitSeconds` is optional. Omit it for an untimed variant. When present it must be 60–7200 seconds.
- If the block is absent, the loader supplies a single `standard` variant with no time limit.
- At most five variants are allowed.

An examination can start only when the challenge has an executable language and at least one hidden test. Authors do not write examination-specific tests: the normal hidden tests are used. The server removes hints from the examination projection, so `hints.md` is never shown during an examination.

## Hidden-material boundary

The loader reads:

- `challenge.json`
- the statement and hints Markdown
- starter file contents
- public and hidden test data into a server-only test plan
- the server-side harness source referenced by `languages[].harness`
- for SQL packages, the server-owned built-in harness and the `sql.schema` and
  `sql.seed` files, merged into the server-only test plan

Public test names and descriptions are part of the client-facing challenge
detail. Hidden test data is loaded into the trusted `IChallengeExecutionSource`
test plan so the worker can execute it, but hidden names, descriptions,
identifiers, and data are never placed in a client-facing contract. Reference
solutions are not part of the manifest, so the loader neither reads nor validates
them; they are excluded structurally.

`GET /api/challenges` and `GET /api/challenges/{slug}` cannot return hidden test
data, hidden test paths, reference solutions, solution paths, harness source, SQL
schema, or SQL seed rows.
`GET /api/submissions/{id}/results` returns only public test names,
descriptions, and statuses, plus hidden pass/total counts.

The harness and test plan are kept in separate server-only structures and
exposed through `IChallengeExecutionSource`. They are not properties of the
client-facing `ChallengeDetail` or `ChallengeLanguage` records, so they cannot be
serialized into a challenge response by accident. Keep executable test fixtures
and solutions in `tests/hidden/` and `solutions/`.

## Adding a package

1. Create `challenges/<domain>/<slug>/`.
2. Add `challenge.json`, `statement.md`, `hints.md`, starter files, public and
   hidden test data, and reference solutions. Each non-SQL test data file must be
   a JSON object containing `expected`, `cases`, or `expectedStatus`; each SQL test
   file must contain `expected` or `plan`.
3. To make a C#, Python, or Go language executable, add a harness under
   `harness/<language>/` and reference it from that language entry. Leave it out
   for editable-only languages. For SQL, add the `sql` block and the `sql`
   language entry instead; never add a SQL harness.
4. Use a new unique `slug`; do not reuse a path or slug.
5. Add `competencySlugs` for the seeded competencies the challenge should
   evidence, and an optional `examination.variants` block if the challenge should
   offer timed or multiple examination variants.
6. Start the API. Validation runs at startup and reports every offending
   package and field. Fix all reported errors before the catalog is served.
7. Confirm the package with:

   ```powershell
   Invoke-RestMethod http://localhost:5080/api/challenges
   Invoke-RestMethod http://localhost:5080/api/challenges/<slug>
   ```

The catalog is loaded once at startup. Restart the API after changing package
content.
