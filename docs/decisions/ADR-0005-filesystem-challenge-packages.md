# ADR-0005: Filesystem Challenge Packages and Strict Startup Validation

- **Status:** Accepted
- **Date:** 2026-11-08
- **Phase:** 3

## Context

Phase 3 must let a learner browse challenges, read a statement, and edit starter code before execution exists. Challenge content includes statements, hints, starter files, public tests, hidden tests, and reference solutions.

The content has properties that differ from learner state:

- It is authored and reviewed like source, not created through the application.
- It must support C# and Python variants of the same challenge.
- Hidden tests and reference solutions must never reach the browser.
- Invalid content must not become partially browsable.

The Phase 2 database holds seeded catalog data and will later hold mutable learner state such as submissions and progress. Storing challenge definitions in SQLite would conflate authored content with learner state and would require migrations for every content edit.

## Decision

1. Introduce `src/Modules/Psel.Modules.Challenges` as the second physical module assembly.
2. Store challenge packages as directories under `challenges/`, discovered by searching recursively for `challenge.json`.
3. Use a versioned manifest (`schemaVersion: 1`) parsed with strict JSON settings: unknown members, comments, and trailing commas are rejected.
4. Validate every package **once at startup** through a singleton loader and hosted initializer. Any validation error fails startup with package- and field-level messages; valid packages never partially load.
5. Enforce:
   - required fields and supported enumerated values;
   - slug format and global slug uniqueness;
   - bounded list lengths and text lengths;
   - relative-path-only references, resolved inside the package directory;
   - per-file size limits;
   - unique language ids, starter paths, and test names;
   - public and hidden tests when evaluation is `AutomatedTests` or `Hybrid`.
6. Keep the catalog in memory and serve it read-only through `IChallengeCatalog`.
7. Enforce a non-disclosure boundary in the loader and contracts:
   - read only the manifest, statement, hints, starter files, and public-test metadata;
   - validate that hidden test files exist without reading their contents into the response model;
   - exclude reference solutions structurally: the manifest has no solution reference, so the loader never reads them;
   - never expose hidden tests, hidden paths, solutions, or solution paths from client APIs.
8. Do not create database tables or EF configurations for challenges.
9. Bundle Monaco Editor locally instead of loading it from a CDN. Lazy-load the workspace so the editor is not part of the initial catalog bundle.

## Alternatives considered

### Store challenges in SQLite

Rejected for Phase 3. Challenge definitions are authored files, not mutable application data. A filesystem source keeps them reviewable, diffable, and independent of schema migrations. It also keeps hidden material out of a queryable table that an endpoint could accidentally expose.

### Validate packages lazily on first request

Rejected. A broken package would surface as a runtime failure and could leave the catalog partially available. Startup validation makes content correctness a deployment precondition, which matches the trusted-authoring model.

### Parse the manifest leniently and default missing fields

Rejected. Silent defaults hide authoring mistakes. Strict parsing and explicit errors make the package contract unambiguous. `editorLanguage`, which legitimately defaults to the language id, is the only intended default.

### Read hidden tests and solutions into the catalog, then filter them out

Rejected. Filtering depends on every future endpoint and serializer remembering to omit the data. The safer design never loads hidden material into the response model, so it cannot leak through a new projection.

### Load Monaco from a CDN

Rejected. PSEL is local-first and must work without internet access. A locally bundled editor also avoids a third-party runtime dependency and keeps the security surface documented.

## Consequences

### Positive

- Challenge content is versionable and reviewable with ordinary file tools.
- Startup fails fast on invalid content; the served catalog is always valid.
- Hidden tests and reference solutions are absent from the response model by construction.
- Adding a challenge requires no migration.
- The module has no database dependency and preserves the modular-monolith boundary.

### Negative

- Catalog changes require an API restart.
- Packages are validated only at startup, so hot-reloading content is not supported.
- The API host must resolve `Challenges:Path` correctly in development, tests, and future deployment.
- Strict JSON means authors must fix unknown fields rather than relying on them being ignored.
- The editor adds a large lazy chunk and worker assets to the frontend build.

### Mitigations

- Document the schema, path rules, and hidden-material layout in [challenges/README.md](../../challenges/README.md).
- Cover loader, startup, and endpoint behavior with dedicated tests, including temporary package trees.
- Keep `Challenges:Path` configuration-driven so tests can isolate content.
- Restart the API after content changes and surface validation errors in startup logs.
- Keep the workspace lazy-loaded and verify the initial bundle does not include Monaco.
