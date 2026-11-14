# Personal Software Engineering Lab (PSEL)

> **Train. Build. Test. Understand.**  
> A local-first web application for learning, practicing, and objectively assessing software engineering skills.

## 1. Project Overview

Personal Software Engineering Lab (PSEL) is a personal learning and assessment platform. It is designed to help a learner develop practical software engineering competency through structured learning materials, coding exercises, technical simulations, and examinations.

PSEL is not intended to be a conventional inventory, finance, or CRUD application. Its primary purpose is to provide a structured environment in which the learner can:

- Study a software engineering concept.
- Complete a guided exercise.
- Solve an independent technical challenge.
- Submit code or technical work for evaluation.
- Review test results, errors, and feedback.
- Track competency development over time.

The core learning and grading workflow must work without an AI service. AI may be added later as an optional tutor, but it must not be required to run exercises or determine objective test results.

### 1.1 Product principles

1. **Practice over passive reading** — concepts should lead to hands-on work.
2. **Evidence-based assessment** — competency should be measured through submissions and observable results.
3. **Deterministic grading where possible** — automated tests and explicit rules take priority over subjective AI grading.
4. **Progressive difficulty** — learning should move from guided work to independent assessment.
5. **Language flexibility** — challenge content should not be tightly coupled to one programming language.
6. **Local-first operation** — the application should run on the learner's own machine.
7. **Secure execution** — submitted code is untrusted and must run outside the web application process.

## 2. Learning Domains

The initial platform should cover five major domains.

### 2.1 Programming Fundamentals

Topics include:

- Algorithms and problem solving.
- Complexity analysis and Big-O notation.
- Arrays, strings, linked lists, stacks, queues, hash maps, trees, and graphs.
- Searching and sorting.
- Recursion and iteration.
- Object-oriented programming.
- Functional programming concepts.
- Error handling and basic language/runtime concepts.

### 2.2 Database Engineering

Topics include:

- Relational database fundamentals.
- Data modeling and normalization.
- Primary keys, foreign keys, and constraints.
- SQL queries, joins, grouping, and subqueries.
- Indexes and query plans.
- Transactions and isolation levels.
- Locking, concurrency, and deadlocks.
- Query optimization and performance analysis.

### 2.3 Backend Engineering

Topics include:

- HTTP and REST API design.
- Request validation and error handling.
- Authentication and authorization.
- Session and token-based security.
- Middleware and dependency injection.
- Caching.
- Background jobs and asynchronous processing.
- Concurrency and race conditions.
- Logging, observability, and API testing.

### 2.4 Software Architecture

Topics include:

- Separation of concerns.
- Layered architecture.
- Modular monoliths.
- Clean Architecture concepts.
- Dependency inversion.
- Common design patterns.
- Domain modeling.
- Scalability and reliability trade-offs.
- Architecture decision records (ADRs).

### 2.5 Software Quality

Topics include:

- Unit, integration, and end-to-end testing.
- Test design and boundary-value analysis.
- Debugging and root-cause analysis.
- Refactoring.
- Code review.
- Static analysis and maintainability.
- Secure coding.
- OWASP Top 10 fundamentals.
- Performance testing and profiling.

## 3. Learning Experience

PSEL uses a hybrid learning model: theory, practical exercises, simulation, and examination.

### 3.1 Learning progression

The standard progression for a topic is:

1. **Learn** — read explanations, diagrams, examples, and key concepts.
2. **Guided Exercise** — complete a task with progressive hints or partial guidance.
3. **Independent Challenge** — solve a problem without step-by-step guidance.
4. **Practical Assessment** — complete an assessment under defined constraints.
5. **Competency Review** — inspect results, weaknesses, and recommended next topics.

Not every topic must use every stage, but the platform should support this progression.

### 3.2 Modes

#### Learning Mode

- Presents explanations and examples.
- Allows hints and guided steps.
- Shows learning objectives and prerequisites.
- May optionally provide an AI tutor in a future release.

#### Practice Mode

- Presents realistic tasks.
- Allows a limited hint system.
- Shows public test results and useful diagnostics.
- Does not reveal complete reference solutions by default.

#### Examination Mode

- Uses a defined time limit when appropriate.
- Can randomize challenge variants.
- Uses hidden tests for coding tasks.
- Does not provide AI-generated solutions or hints during the exam.
- Produces an assessment result based on explicit criteria.

### 3.3 Competency states

Each competency can have one of these states:

- `NotStarted`
- `Learning`
- `Practicing`
- `AssessmentRequired`
- `Mastered`

A competency should only become `Mastered` when the learner has met its defined assessment criteria. Merely opening a lesson or completing one guided exercise is insufficient.

## 4. Challenge Types

The platform should support multiple challenge types. The architecture must allow new types to be added without rewriting the entire assessment system.

| Challenge type | Example task | Evaluation approach |
|---|---|---|
| Coding | Implement a data structure or algorithm | Compile/run + automated tests |
| SQL | Write a query or improve a slow query | Execute against controlled database + assertions |
| Debugging | Find and fix a defect | Tests + expected behavior |
| Refactoring | Improve structure without changing behavior | Tests + rubric/static analysis |
| API | Implement an endpoint with validation and authorization | Integration tests |
| Security | Identify or fix a vulnerability | Security assertions + rubric |
| Architecture | Design a component or explain a trade-off | Structured rubric and evidence |
| Performance | Optimize an implementation | Benchmark with defined constraints |

Some challenges cannot be fully graded through automated tests. Architecture, design, and code-quality tasks may require structured rubrics and manual review. The system must clearly distinguish automated results from rubric-based judgments.

## 5. Technical Architecture

### 5.1 Proposed technology stack

| Layer | Technology | Purpose |
|---|---|---|
| Frontend | React + TypeScript | User interface |
| Styling | Tailwind CSS | UI styling |
| Code editor | Monaco Editor | In-browser code editing |
| Backend | ASP.NET Core | Application services and API |
| ORM | Entity Framework Core | Data access |
| Database | SQLite | Local persistent storage |
| Code execution | Docker Engine | Isolated execution environment |
| Backend tests | xUnit | Unit and integration testing |
| Frontend tests | Vitest | UI and logic testing |

The initial implementation should favor technologies that are practical for a local development environment. The stack can evolve through documented architecture decisions rather than ad hoc replacement.

### 5.2 Architecture style

Use a **Modular Monolith** for the main application. Keep domain boundaries explicit and avoid premature microservices.

Suggested backend modules:

- Learning
- Challenges
- Submissions
- Execution
- Assessment
- Progress
- Languages

The execution subsystem should be separated behind interfaces so that Docker-based runners can be replaced or extended later.

### 5.3 High-level components

```text
┌─────────────────────────────────────────────────────────┐
│                    React Web Frontend                   │
│ Dashboard | Learning | Challenges | Editor | Progress   │
└───────────────────────────┬─────────────────────────────┘
                            │ HTTP / JSON
┌───────────────────────────▼─────────────────────────────┐
│                    ASP.NET Core                         │
│                                                         │
│ Learning │ Challenge │ Submission │ Assessment │Progress│
│                         │                               │
│                 Execution Manager                      │
│                         │                               │
│                  Runner Adapters                        │
└───────────────┬─────────────────────────┬───────────────┘
                │                         │
       ┌────────▼────────┐       ┌────────▼────────┐
       │     SQLite     │       │  Docker Engine  │
       │ App data/state │       │ Isolated runners│
       └─────────────────┘       └─────────────────┘
```

### 5.4 Execution flow

1. The frontend submits source code and challenge metadata.
2. The backend validates the request and creates a `Submission`.
3. The execution manager queues or starts an execution job.
4. A language-specific runner prepares an isolated workspace.
5. Docker executes the submitted code and test harness.
6. The runner collects exit code, standard output, standard error, timing, and resource information.
7. The assessment engine evaluates the result.
8. The submission and assessment are persisted in SQLite.
9. The frontend retrieves the execution status and results.

The first implementation may use polling for execution status. SignalR can be considered later if real-time updates materially improve the experience.

## 6. Language Support

The language system must be configurable and extensible.

### 6.1 Initial languages

Start with:

- **C#** — primary language for the first runner.
- **Python** — second language after the execution pipeline is stable.

Potential future additions:

- PHP
- Go
- Java
- JavaScript / TypeScript
- C or C++

Do not hard-code language-specific behavior throughout the application. Use a language runtime configuration and runner adapter abstraction.

### 6.2 Runner abstraction

A runner should define the operations required to:

- Prepare a workspace.
- Place starter files and test files.
- Compile or interpret the submission.
- Execute tests.
- Enforce time and resource limits.
- Collect execution output and status.
- Clean up temporary resources.

A conceptual interface could look like:

```csharp
public interface ILanguageRunner
{
    string LanguageId { get; }

    Task<ExecutionResult> ExecuteAsync(
        ExecutionRequest request,
        CancellationToken cancellationToken);
}
```

This is an architectural illustration, not a requirement to use this exact interface without considering the implementation.

## 7. Challenge Content Structure

Challenge content should be stored independently from application business logic. This makes challenges easier to author, version, review, and extend.

Suggested structure:

```text
challenges/
  programming/
    two-sum/
      challenge.json
      statement.md
      hints.md
      starter/
        csharp/
        python/
      tests/
        public/
        hidden/
      solutions/
        csharp/
        python/
  database/
    ...
  backend/
    ...
```

### 7.1 Challenge metadata

A challenge definition should include, at minimum:

- Stable challenge ID.
- Title and description.
- Domain and topic.
- Difficulty.
- Learning objectives.
- Prerequisites.
- Supported languages.
- Estimated completion time.
- Challenge type.
- Starter-file references.
- Public-test references.
- Hidden-test references.
- Evaluation configuration.
- Optional rubric.

Use a versioned schema for challenge metadata. Invalid challenge packages should fail validation before they become available to learners.

### 7.2 Protecting hidden materials

Hidden tests and reference solutions must never be sent to the browser or exposed through ordinary challenge-detail API responses.

The execution subsystem may access them from trusted server-side storage. The frontend should receive only the challenge statement, permitted starter files, public tests (if intended), and allowed hints.

## 8. Data Model

The initial data model should include these core concepts:

| Entity | Responsibility |
|---|---|
| `LearningDomain` | Top-level learning area |
| `LearningModule` | Group of related lessons and competencies |
| `LearningMaterial` | Theory, examples, and learning resources |
| `Competency` | A skill or measurable learning objective |
| `Challenge` | A task assigned to the learner |
| `ChallengeTestCase` | Public or hidden test definition/reference |
| `LanguageRuntime` | Supported language and runtime configuration |
| `Submission` | A learner's submitted work |
| `TestResult` | Result of an individual test or test group |
| `Assessment` | Consolidated evaluation of a submission or exam |
| `UserProgress` | Learner progress against modules and competencies |

The schema should preserve submission history. New submissions must not overwrite previous attempts.

## 9. API Surface

Initial API endpoints:

| Method | Endpoint | Purpose |
|---|---|---|
| GET | `/api/domains` | List learning domains |
| GET | `/api/learning-paths` | List learning paths |
| GET | `/api/challenges` | Browse available challenges |
| GET | `/api/challenges/{id}` | Get challenge details safe for the client |
| GET | `/api/languages` | List enabled languages |
| POST | `/api/submissions` | Create a submission |
| GET | `/api/submissions/{id}` | Get submission status and summary |
| GET | `/api/submissions/{id}/results` | Get permitted execution results |
| GET | `/api/progress` | Get learner progress |
| GET | `/api/assessments` | List assessments |

The API contract should be documented and versioned. Validate all incoming data and avoid returning internal paths, hidden test data, reference solutions, or sensitive runner details.

## 10. Code Execution Security

Submitted code is untrusted input. Never execute it directly inside the ASP.NET Core process or with the application's normal operating-system privileges.

### 10.1 Minimum Docker controls

Each execution should use a short-lived isolated container with:

- Non-root execution user.
- Network disabled by default.
- CPU and memory limits.
- Execution timeout.
- Process-count limits where supported.
- Restricted filesystem access.
- Temporary, per-submission workspace.
- Read-only mounts where practical.
- Output-size limits.
- No access to application secrets or user files.
- No mount of sensitive host directories.
- Cleanup of containers, workspaces, and temporary artifacts.

Do not expose the Docker socket to submitted code. The execution manager may communicate with Docker, but the untrusted process must not gain Docker control.

### 10.2 Security limitations

Docker provides useful isolation but is not an absolute security boundary. The implementation must document this limitation. Stronger isolation technologies such as gVisor or Firecracker may be considered in a later hardening phase.

The application should not claim that arbitrary code execution is perfectly safe merely because it runs in a container.

### 10.3 Operational safeguards

- Validate challenge IDs and language IDs against trusted configuration.
- Do not accept arbitrary image names or runner commands from the client.
- Use server-controlled command templates.
- Sanitize filenames and prevent path traversal.
- Limit concurrent executions.
- Handle cancellation and timeouts.
- Record execution metadata without logging secrets or excessive source content.
- Ensure failed executions still trigger cleanup.

## 11. Assessment Design

### 11.1 Deterministic assessment

For coding challenges, assessment should rely primarily on:

- Compilation or interpreter success.
- Public and hidden test results.
- Expected output or state assertions.
- Runtime and resource constraints.
- Explicit challenge-specific scoring rules.

The system should distinguish at least:

- Accepted
- Wrong Answer
- Compilation Error
- Runtime Error
- Time Limit Exceeded
- Memory Limit Exceeded
- Internal Runner Error

### 11.2 Rubric-based assessment

For tasks such as architecture design, refactoring quality, or code review, use explicit rubrics. A rubric may evaluate:

- Correctness.
- Separation of concerns.
- Maintainability.
- Security.
- Performance considerations.
- Clarity of reasoning.
- Trade-off analysis.

Do not present subjective or AI-generated judgments as deterministic test results. If AI assistance is introduced later, label it as advisory feedback.

### 11.3 Competency scoring

Competency status should be derived from defined evidence, such as:

- Completion of prerequisite learning.
- Results across multiple independent challenges.
- Performance in a practical assessment.
- Recency or consistency of demonstrated skill, if relevant.

Avoid assigning mastery based solely on a single easy exercise.

## 12. User Interface

The interface should prioritize clarity and focused practice.

Suggested main navigation:

- Dashboard
- Learning Paths
- Challenges
- Practice
- Examinations
- Progress
- Settings

### 12.1 Dashboard

Show:

- Current learning path.
- Competency overview.
- Recent submissions.
- Recommended next activity.
- Progress summary.

### 12.2 Challenge workspace

The workspace should provide:

- Challenge statement.
- Constraints and examples.
- Language selector.
- Monaco code editor.
- Run / Submit actions.
- Public test results.
- Execution status and diagnostics.
- Hints when permitted by the current mode.

The layout should remain usable on a desktop-sized browser. Responsive behavior can be improved after the core workflow is functional.

## 13. Testing Strategy

### Backend

- Unit tests for domain rules and assessment logic.
- Integration tests for API endpoints.
- Persistence tests for submission history and progress.
- Tests for challenge package validation.
- Tests for runner failure, timeout, cancellation, and cleanup.

### Frontend

- Unit tests for state and utility logic.
- Component tests for challenge and result views.
- Tests for loading, error, and execution-status states.

### Execution subsystem

- Known-good submissions.
- Compilation failures.
- Wrong answers.
- Runtime exceptions.
- Infinite loops and timeouts.
- Excessive output.
- Resource-limit enforcement.
- Attempts to access network or restricted files.
- Cleanup after both successful and failed runs.

## 14. Development Roadmap

Build incrementally. Do not attempt to implement the entire platform in one pass.

### Phase 1 — Foundation

- Inspect the existing repository before changing files.
- Establish the solution and project structure.
- Configure ASP.NET Core and React frontend.
- Configure SQLite and EF Core.
- Add health/status endpoint.
- Add basic application shell and navigation.
- Add automated test projects.
- Document architecture and local setup.

**Exit criteria:** frontend and backend run locally, database initializes, health check works, and baseline tests pass.

### Phase 2 — Learning Management

- Implement learning domains.
- Implement modules and materials.
- Implement competencies and prerequisites.
- Add initial seed data.
- Build learning-path and lesson views.

**Exit criteria:** the learner can browse domains, open learning material, and see learning objectives.

### Phase 3 — Challenge Management

- Define and validate challenge package schema.
- Load challenge metadata and starter files.
- Implement challenge browsing and detail endpoints.
- Build challenge workspace UI.
- Add Monaco Editor and language selection.

**Exit criteria:** the learner can open a challenge and edit starter code, even before code execution is available.

### Phase 4 — Code Execution

- Implement submission persistence.
- Implement execution manager.
- Implement Docker integration.
- Implement C# runner.
- Add execution limits and cleanup.
- Return structured execution status and diagnostics.

**Exit criteria:** a C# submission can execute in an isolated container and return a reliable result.

### Phase 5 — Assessment Engine

- Implement public and hidden test execution.
- Implement deterministic scoring.
- Implement submission result views.
- Ensure hidden tests and reference solutions remain server-side.
- Add tests for all assessment outcomes.

**Exit criteria:** coding challenges can be graded and the learner can inspect permitted results.

### Phase 6 — Python Runtime

- Add Python runner adapter.
- Add language-specific starter templates.
- Add Python challenge examples.
- Verify that language behavior remains isolated behind the runner abstraction.

**Exit criteria:** the same challenge workflow supports C# and Python where challenge definitions provide both implementations.

### Phase 7 — Progress and Examination

- Implement competency progress.
- Implement assessment history.
- Add examination mode rules.
- Add optional time limits and challenge variants.
- Build progress dashboard.

**Exit criteria:** the learner can take an assessment and see evidence-based competency progress.

### Phase 8 — Database Engineering

- Add controlled SQL challenge environments.
- Support schema setup and data seeding.
- Execute learner queries against isolated databases.
- Evaluate query correctness.
- Add selected query-plan and performance exercises.

**Exit criteria:** SQL challenges can be executed without exposing the application's own database.

## 15. MVP Acceptance Criteria

The MVP is considered usable when:

1. The application runs locally through a documented setup process.
2. The five learning domains are represented.
3. Learning materials can be browsed.
4. Challenges can be browsed and opened.
5. The challenge workspace includes a code editor.
6. At least one language runner (C#) executes code in an isolated environment.
7. Automated tests can evaluate a submission.
8. Public and hidden test materials are separated.
9. Compilation errors, runtime errors, and timeouts are handled.
10. Submission history is persisted.
11. Assessment results are visible to the learner.
12. Basic competency progress is recorded.
13. Runner cleanup occurs after success, failure, timeout, and cancellation.
14. The application does not expose hidden tests or reference solutions to the client.
15. Security limitations and local execution requirements are documented.

## 16. Repository and Documentation Expectations

Maintain these documents as the project evolves:

```text
PROJECT_SPEC.md
README.md
docs/
  ARCHITECTURE.md
  DATABASE_DESIGN.md
  API_SPECIFICATION.md
  EXECUTION_SECURITY.md
  DEVELOPMENT_ROADMAP.md
  decisions/
    ADR-0001-modular-monolith.md
    ADR-0002-code-execution-isolation.md
```

Update documentation when implementation decisions materially change. Record significant architectural choices as ADRs, including context, decision, alternatives, and consequences.

## 17. Instructions for OpenCode

Use this specification as the product and architecture baseline.

Before making changes:

1. Inspect the repository structure and existing files.
2. Identify existing frameworks, dependencies, conventions, and implementation status.
3. Compare the current repository with this specification.
4. Report conflicts, risks, missing prerequisites, and recommended adjustments.
5. Propose a concrete implementation plan for **Phase 1 only**.

Do not overwrite existing work blindly. Do not generate the entire application in one pass. Do not proceed to later phases until the current phase is implemented, tested, documented, and explicitly reviewed.

For Phase 1:

- Implement only the foundation and application shell.
- Keep the backend modular and testable.
- Keep frontend and backend responsibilities clear.
- Configure SQLite and EF Core with a maintainable migration workflow.
- Add a health endpoint.
- Add baseline backend and frontend tests.
- Provide exact commands to run the application and tests.
- Update README and architecture documentation.

After implementation, report:

- Files created or modified.
- Architecture and important decisions.
- Commands to run the application.
- Tests executed and their results.
- Known limitations and unresolved risks.
- Recommended next step.

**Stop after Phase 1 and wait for approval.**

---

## 18. Initial Build Philosophy

PSEL should make the learner better at engineering, not merely faster at producing code.

The platform should reward understanding, correctness, sound design, debugging discipline, and the ability to explain trade-offs. AI assistance may support learning in the future, but the core system must remain useful, testable, and assessable without it.
