# ADR-0008: Python Runtime

- **Status:** Accepted
- **Date:** 2026-11-11
- **Phase:** 6

## Context

Phase 6 must add Python as a second executable language without changing the submission workflow, the assessment rules, or the hidden-material boundary. Phase 4 already established a server-controlled Docker boundary, and Phase 5 made grading language-neutral: a trusted harness reads `/workspace/PselTests.json` and emits opaque per-test observations.

Several forces apply:

- The submission API, queue, worker, and assessment already work for C#. Duplicating any of them for Python would create divergent behavior and two grading implementations.
- Submitted Python is untrusted and must never be interpreted in the ASP.NET Core process. The existing container controls must apply unchanged.
- The client must not select an image, command, harness, environment variable, or test set.
- Python and C# differ in how code is built and launched: C# needs a generated project and a build step, while Python needs an interpreter and a module import contract.
- Challenge packages already declare a language-specific `harness` path. The loader and catalog can carry a Python harness without new database tables.
- Containers have no network. Python challenges must therefore rely on the standard library only.
- Python has no separate compile step, but the assessment contract still needs a stable way to distinguish invalid source from a runtime failure.

## Decision

### 1. Add a Python adapter behind the existing runner boundary

`PythonDockerRunner` implements the existing `ILanguageRunner` contract and is registered beside `CSharpDockerRunner`. The submission service selects a runner by `LanguageId`; it has no Python-specific branch, and the worker still executes one request and persists one terminal outcome.

The runner owns only Python-specific details:

- image: `Execution:PythonImage`, default `python:3.13-slim`;
- command: `python3 -B /workspace/PselHarness.py`;
- accepted submitted files: `.py`;
- reserved server files: `PselHarness.py` and `PselTests.json`;
- diagnostics: `SyntaxError`, `IndentationError`, and `TabError` map to `CompilationError`; other non-zero exits map to `RuntimeError`.

### 2. Keep server-controlled isolation unchanged

The Python runner uses the same `IDockerEngine`, workspace rules, time, memory, CPU, process, output, and concurrency limits as C#. It does not add a privileged mode, a network path, a host mount, or a client-controlled argument. The container runs with the shared flags: no network, read-only root filesystem, `tmpfs` `/tmp`, dropped capabilities, no-new-privileges, non-root user, and a read-only workspace mount.

Python-specific environment values are server-generated:

```text
PYTHONDONTWRITEBYTECODE=1
PYTHONPYCACHEPREFIX=/tmp/pycache
PYTHONUNBUFFERED=1
PYTHONIOENCODING=utf-8
HOME=/tmp
```

Bytecode writes are disabled so the read-only workspace is not a problem and no `.pyc` files leak into the mounted source tree.

### 3. Use one harness protocol for every language

The loader already treats `languages[].harness` as server-only content. A Python harness is a `PselHarness.py` file that:

- imports the submitted module with `import solution`;
- reads `/workspace/PselTests.json`;
- runs every supplied test inside `try/except`, so one failing case does not abort the remaining observations;
- emits one observation per test with the opaque `id`, `passed`, and `errored` values between `__PSEL_RESULTS_BEGIN__` and `__PSEL_RESULTS_END__`.

The assessment module does not know which language produced the block. Its parsing, completeness checks, scoring, and public projection are unchanged.

### 4. Extract shared runner helpers

Workspace creation, test-manifest writing, and Docker outcome mapping are now shared by both runners in `ExecutionWorkspace`, `TestManifestWriter`, and `RunnerOutcomeMapper`. The C# runner was refactored onto the same helpers. Language adapters differ only in image, command, environment, accepted extensions, reserved paths, optional scaffolding, and diagnostic detection.

This keeps a change to workspace layout, manifest shape, timeout mapping, or cleanup behavior from drifting between languages.

### 5. Add Python harnesses to existing packages

Python harnesses were added to the five challenges that already had Python starters and reference solutions: `two-sum`, `balanced-brackets`, `word-frequency`, `debug-off-by-one`, and `refactor-duplication`. Each manifest declares `harness/python/PselHarness.py`, so `canExecute` becomes true for Python.

`api-validation` remains C#-only and non-executable because it models an ASP.NET Core host rather than a single function. A language entry without a harness remains browsable and editable but cannot be submitted.

### 6. No persistence change

Phase 6 adds no migration, table, or column. `Submissions.LanguageId` and `Assessments.LanguageId` already store the language identifier as text, and challenge packages remain filesystem-backed. Adding a Python harness is an authoring change, not a schema change.

### 7. Reserve the Python harness filename

`PselHarness.py` joins `Submission.csproj`, `PselHarness.cs`, and `PselTests.json` in the reserved filename rules enforced by both the submission service and every runner. A client cannot overwrite the server harness or test manifest.

## Alternatives considered

### Run Python inside the API process

Rejected. Submitted code is untrusted. Interpreting it in-process would bypass the container boundary, expose application memory and files, and violate the non-negotiable execution rules.

### Add a separate Python submission pipeline

Rejected. A second queue, worker, endpoint, and grading path would duplicate behavior and make language-specific regressions likely. The existing pipeline is already language-neutral; only the adapter belongs behind `ILanguageRunner`.

### Compile Python to bytecode as a required step

Rejected. A bytecode step adds time and complexity without improving isolation. The interpreter's own syntax diagnostics are sufficient, and `-B` plus a temporary cache prefix avoids workspace writes.

### Map every Python non-zero exit to `RuntimeError`

Rejected. Syntax and indentation errors are authoring/source defects, not runtime behavior. Mapping them to `CompilationError` keeps the assessment outcome compatible with C# and gives the learner the right category.

### Install packages inside the container

Rejected. The container has no network, and allowing package installation would make execution non-deterministic and enlarge the attack surface. Python challenges must use the standard library.

### Use a full Python image with build tools

Rejected for the default. `python:3.13-slim` is smaller and sufficient for standard-library challenges. A future challenge that genuinely needs native tooling should introduce a separate, explicitly reviewed image rather than weakening the default.

### Share one harness file across languages

Rejected. C# compiles into an entry point and Python imports a module; a shared file would either be invalid in one language or hide language-specific test adaptation. Per-language harnesses are explicit and independently testable.

## Consequences

### Positive

- Python reuses the existing submission, queue, worker, assessment, and client projection.
- The client cannot influence the Python image, command, environment, harness, or tests.
- Isolation controls are identical for both languages and verified by shared Docker argument tests.
- Adding Python required no database migration and no client production change; the workspace enables Run automatically when `canExecute` is true.
- Syntax errors produce a meaningful `CompilationError` outcome.
- Shared helpers reduce drift between language adapters.
- Live Python container execution was verified with Docker Desktop 29.9.2 and `python:3.13-slim` for accepted, runtime-error, and compilation-error paths, with no residual container or workspace.

### Negative

- Each executable language needs its own harness and runner tests.
- Python challenges cannot use third-party packages because the container has no network and no installation step.
- The Python runner accepts a single root-level `solution` module import contract; multi-module layouts need explicit imports in the harness.
- The default Python image is an additional image to pull and keep available.

### Mitigations

- Document the Python harness contract and reserved filename in [challenges/README.md](../../challenges/README.md).
- Test the Python runner and endpoint with the same contract coverage as C#, including path rejection, reserved names, cleanup, syntax errors, runtime errors, timeouts, and unavailable Docker.
- Verify with a repository package test that every declared Python language has a harness and imports `solution`.
- Document the no-network and standard-library limitation in [EXECUTION_SECURITY.md](../EXECUTION_SECURITY.md) and the README.
- Repeat the live Python container check when the image or runner changes; treat it as an operational verification, not a suite dependency.
