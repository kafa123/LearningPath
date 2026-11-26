# ADR-0011: Go Runtime

- **Status:** Accepted
- **Date:** 2026-11-25
- **Phase:** Post-MVP

## Context

The learning catalog gained a Go engineering track with seven modules, seven
lessons, and fourteen competencies. Those competencies can only reach `Mastered`
through the existing evidence pipeline, so Go needs executable challenges and a
server-controlled runtime.

The submission, queue, worker, assessment, and Docker boundaries already work for
C#, Python, and SQL. Go differs from the interpreted languages because the
submitted files must be compiled before they run, and it differs from C# because
the toolchain, module rules, and diagnostics are different.

Additional forces:

- Submitted Go is untrusted and must never be compiled or executed in the
  ASP.NET Core process.
- The client must not choose the image, command, environment, harness, or tests.
- Containers have no network. Go challenges must use the standard library only,
  and no module download may occur at run time.
- The harness must share the submitted package so it can call the learner's
  exported identifiers without an import path.
- A Go build failure should be reported as `CompilationError`, not as a generic
  runtime failure.

## Decision

### 1. Add a Go adapter behind `ILanguageRunner`

`GoDockerRunner` is registered beside the existing runners. The submission
service still selects a runner by `LanguageId`, and the queue, worker,
assessment, and persistence paths are unchanged.

The runner owns only Go-specific details:

- image: `Execution:GoImage`, default `golang:1.27-alpine`;
- command: `cd /workspace && go build -trimpath -o /tmp/psel-submission . && /tmp/psel-submission`;
- accepted submitted files: `.go`;
- reserved server files: `PselHarness.go`, `go.mod`, and `PselTests.json`;
- server-written module: `module psel/submission` with `go 1.27`;
- diagnostics: `# psel/submission`, `syntax error`, `undefined:`,
  `declared and not used`, `imported and not used`, `missing return`, and
  `cannot use` map to `CompilationError`; other non-zero exits map to
  `RuntimeError`.

### 2. Keep the container controls unchanged

The Go runner uses the same `IDockerEngine`, workspace rules, limits, and cleanup
as the other runners: no network, read-only root filesystem, `tmpfs` `/tmp`,
dropped capabilities, no-new-privileges, non-root user, and a read-only workspace
mount. The client supplies only validated source files.

Go-specific environment values are server-generated:

```text
HOME=/tmp
GOCACHE=/tmp/gocache
GOPATH=/tmp/go
GO111MODULE=on
GOTOOLCHAIN=local
GOPROXY=off
CGO_ENABLED=0
```

`GOPROXY=off` and `GOTOOLCHAIN=local` make module resolution fail closed instead
of reaching the network. `CGO_ENABLED=0` keeps builds reproducible in the Alpine
image without a C toolchain.

### 3. Use the shared harness protocol

A Go harness is a `package main` file that lives in the same package as the
submission. It reads `/workspace/PselTests.json`, recovers panics per test, and
emits one observation per test with the opaque `id`, `passed`, and `errored`
values between `__PSEL_RESULTS_BEGIN__` and `__PSEL_RESULTS_END__`.

The assessment module does not know which language produced the block. Parsing,
completeness checks, scoring, and the public projection are unchanged.

### 4. Add Go packages to the existing catalog

Nine Go packages cover value semantics, error wrapping, interface seams,
concurrency, HTTP handlers, middleware, generics, and allocation costs. Each
declares a Go language entry, a server-only harness, two public and three hidden
test files, and `AutomatedTests` evaluation. The packages reuse the existing
filesystem-backed catalog and need no database change.

### 5. No persistence change

No migration, table, or column is added. `Submissions.LanguageId` and
`Assessments.LanguageId` already store the language identifier as text, and the
learning seed is synchronized as an upsert.

## Alternatives considered

### Interpret or compile Go inside the API process

Rejected. Submitted code is untrusted and must stay inside the Docker boundary.

### Use a separate Go submission pipeline

Rejected. A second queue, worker, endpoint, and grading path would duplicate
behavior. The existing pipeline is language-neutral; only the adapter belongs
behind `ILanguageRunner`.

### Download modules inside the container

Rejected. The container has no network, and module downloads would make execution
non-deterministic and enlarge the attack surface. Go challenges use only the
standard library.

### Run `go run` instead of building first

Rejected. A separate build step gives a clear compile failure, avoids compiling on
every test invocation, and keeps the binary in `/tmp` outside the read-only
workspace.

### Put the harness in a separate package

Rejected. A separate import path would require exported symbols, a module layout,
and more authoring rules. Sharing `package main` keeps the harness contract small
and lets it call the learner's API directly.

### Enable cgo or the race detector by default

Rejected. cgo requires a C toolchain that the default Alpine image does not
provide, and the race detector needs cgo. Challenges must be deterministic and
portable, so the default build stays `CGO_ENABLED=0`.

## Consequences

### Positive

- Go reuses the existing submission, queue, worker, assessment, and client
  projection.
- The client cannot influence the Go image, command, environment, harness, or
  tests.
- A compile failure is reported as `CompilationError` with the Go diagnostic.
- Adding Go required no database migration and no client production change; the
  workspace enables Run automatically when `canExecute` is true.
- The seeded Go track gives the fourteen Go competencies a path to `Mastered`
  through the same assessment and examination rules.

### Negative

- The default Go image is an additional image to pull and keep available.
- Go challenges cannot use third-party modules because the container has no
  network.
- The harness shares the submission package, so a conflicting `main` or duplicate
  symbol is an authoring error rather than a separate namespace.
- The race detector is unavailable under the default `CGO_ENABLED=0` build.

### Mitigations

- Document the Go harness contract, reserved filenames, and no-network limit in
  [challenges/README.md](../../challenges/README.md).
- Cover the Go runner with contract tests for workspace generation, module and
  manifest injection, path rejection, cleanup, compile errors, runtime errors,
  timeouts, and unavailable Docker.
- Verify every Go package outside Docker with the Go 1.27 toolchain: reference
  solutions pass all tests, starters fail at least one, and `go vet` is clean.
- Treat live container execution as an operational check and repeat it when the
  image or runner changes.
