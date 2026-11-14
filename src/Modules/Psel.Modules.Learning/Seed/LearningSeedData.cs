using Psel.Modules.Learning.Domain;

namespace Psel.Modules.Learning.Seed;

/// <summary>
/// Phase 2 seed content for the five learning domains. Keep this content
/// independent of persistence and HTTP concerns.
/// </summary>
internal static class LearningSeedData
{
    public static IReadOnlyList<LearningDomain> Create()
    {
        var algorithmFoundations = CreateModule(
            "algorithm-foundations",
            "Algorithm Foundations",
            "Turn a problem statement into a correct, readable sequence of steps.",
            "Beginner",
            45,
            1,
            [
                "Break a problem into inputs, outputs, constraints, and edge cases.",
                "Express an algorithm in pseudocode before committing to syntax.",
                "Justify correctness with a small example and boundary cases."
            ],
            [
                (
                    "algorithm-decomposition",
                    "Problem decomposition",
                    "Translate a requirement into explicit inputs, outputs, and boundary cases."
                ),
                (
                    "correctness-reasoning",
                    "Correctness reasoning",
                    "Explain why an algorithm produces the right result using examples and invariants."
                )
            ],
            [
                CreateMaterial(
                    "algorithm-foundations-lesson",
                    "From problem to algorithm",
                    "Lesson",
                    20,
                    1,
                    """
                    ## From problem to algorithm

                    An algorithm is a finite sequence of unambiguous steps that turns an input into a desired output. Before writing code, state four things:

                    - **Input:** what data arrives, and in what shape.
                    - **Output:** what must be produced.
                    - **Constraints:** size limits, allowed operations, and performance expectations.
                    - **Edge cases:** empty input, duplicates, negative values, and maximum sizes.

                    ### Start with pseudocode

                    ```text
                    findLargest(numbers):
                        if numbers is empty: return null
                        largest = numbers[0]
                        for each number in numbers[1..]:
                            if number > largest: largest = number
                        return largest
                    ```

                    Pseudocode lets you check the logic without language syntax. Walk through a small example by hand, then a boundary case. If the steps are correct, translating to code is mechanical.

                    ### Check correctness

                    A useful habit is to ask what must always be true at each step. In the example, `largest` is always the maximum of the elements seen so far. That invariant explains why the final value is correct.
                    """
                )
            ]);

        var complexityAnalysis = CreateModule(
            "complexity-analysis",
            "Complexity Analysis",
            "Compare algorithms by how their work grows as the input grows.",
            "Intermediate",
            50,
            2,
            [
                "Count the dominant operation in a loop or recursive call.",
                "Compare algorithms by asymptotic growth instead of raw timing.",
                "Recognize common complexity classes and their practical trade-offs."
            ],
            [
                (
                    "asymptotic-analysis",
                    "Asymptotic analysis",
                    "Estimate time and space growth using Big-O notation."
                ),
                (
                    "performance-tradeoffs",
                    "Performance trade-offs",
                    "Choose an approach based on input size, constants, and memory cost."
                )
            ],
            [
                CreateMaterial(
                    "complexity-analysis-lesson",
                    "Measuring growth with Big-O",
                    "Lesson",
                    20,
                    1,
                    """
                    ## Measuring growth, not seconds

                    Big-O describes how an algorithm's work grows as the input size grows. It ignores constant factors and machine speed, so it lets you compare algorithms independently of hardware.

                    ### Count the dominant operation

                    For a loop over `n` elements, the body runs `n` times: **O(n)**. A loop inside a loop over the same input is usually **O(n²)**. Binary search halves the search space each step: **O(log n)**.

                    - `O(1)` — constant: direct array access.
                    - `O(log n)` — logarithmic: binary search on sorted data.
                    - `O(n)` — linear: one pass over a collection.
                    - `O(n log n)` — sorting with comparison-based algorithms.
                    - `O(n²)` — nested scans over the same collection.

                    ### A practical warning

                    An `O(n²)` algorithm is not automatically wrong. For small, fixed inputs it may be simpler and faster. Choose based on measured input sizes and the cost of the operations you are counting.
                    """
                )
            ]);

        var dataStructures = CreateModule(
            "data-structures",
            "Core Data Structures",
            "Choose structures whose operation costs match the task.",
            "Intermediate",
            60,
            3,
            [
                "Choose a data structure from the operations a task needs.",
                "Describe the cost of common operations for arrays, maps, stacks, queues, and heaps.",
                "Implement a simple structure with a clear invariant."
            ],
            [
                (
                    "structure-selection",
                    "Data structure selection",
                    "Select a structure whose operation costs match the problem."
                ),
                (
                    "structure-implementation",
                    "Structure implementation",
                    "Implement a small structure while preserving its core invariant."
                )
            ],
            [
                CreateMaterial(
                    "data-structures-lesson",
                    "Choosing the right data structure",
                    "Lesson",
                    25,
                    1,
                    """
                    ## Choose the structure around the operations

                    A data structure is valuable because of the operations it makes cheap. Before choosing one, list what the code must do: index by position, look up by key, insert at the front, remove the largest item, or preserve order.

                    - **Array / list** — fast indexed access; insertion in the middle is O(n).
                    - **Hash map** — average O(1) lookup, insertion, and deletion by key.
                    - **Stack** — last-in, first-out; useful for parsing and backtracking.
                    - **Queue** — first-in, first-out; useful for breadth-first traversal and job processing.
                    - **Heap** — fast access to the smallest or largest item.

                    ### Map the operation to the cost

                    If the task is "count how often each word appears", a hash map gives average constant-time updates. If the task is "process tasks in arrival order", a queue expresses the intent directly.

                    Do not add a structure because it sounds advanced. Pick the one whose guarantees match the operations you actually need, then verify the complexity of those operations.
                    """
                )
            ]);

        var relationalModeling = CreateModule(
            "relational-modeling",
            "Relational Modeling",
            "Represent entities, relationships, and rules with tables and keys.",
            "Beginner",
            45,
            1,
            [
                "Model entities and relationships as tables with keys.",
                "Use primary keys, foreign keys, and constraints to encode rules.",
                "Apply normalization to remove duplicated facts."
            ],
            [
                (
                    "relational-design",
                    "Relational design",
                    "Turn a domain description into tables, keys, and relationships."
                ),
                (
                    "constraint-design",
                    "Constraint design",
                    "Use database constraints to enforce rules for every writer."
                )
            ],
            [
                CreateMaterial(
                    "relational-modeling-lesson",
                    "Tables, keys, and relationships",
                    "Lesson",
                    20,
                    1,
                    """
                    ## Tables, keys, and relationships

                    A relational model stores facts in tables. Each row has a stable identity, each column has a type, and relationships are expressed with keys rather than object references.

                    ### Keys and constraints

                    - A **primary key** uniquely identifies a row.
                    - A **foreign key** references a row in another table.
                    - `NOT NULL`, `UNIQUE`, and `CHECK` constraints encode rules the database can enforce.

                    Constraints are valuable because every writer must obey them, not only the current application path.

                    ### Normalize deliberately

                    Normalization removes duplicated facts. If a customer address is stored with every order, changing it requires updating many rows and invites inconsistency. Store the address once and reference it.

                    ### Review the model

                    For each table, ask:

                    1. What real-world fact does one row represent?
                    2. What makes it unique?
                    3. Which values are required?
                    4. Which relationship owns the foreign key?

                    A clear answer to these questions prevents ambiguity later.
                    """
                )
            ]);

        var sqlQuerying = CreateModule(
            "sql-querying",
            "SQL Querying",
            "Compose joins, grouping, and subqueries that return the intended rows.",
            "Intermediate",
            60,
            2,
            [
                "Compose joins that preserve the intended rows.",
                "Group and aggregate data with correct filtering order.",
                "Reason about NULL values and three-valued logic."
            ],
            [
                (
                    "sql-composition",
                    "SQL composition",
                    "Build understandable queries from joins, filters, and aggregates."
                ),
                (
                    "null-semantics",
                    "NULL semantics",
                    "Handle unknown values correctly in filters and expressions."
                )
            ],
            [
                CreateMaterial(
                    "sql-querying-lesson",
                    "Composing SQL queries",
                    "Lesson",
                    25,
                    1,
                    """
                    ## Compose queries from smaller parts

                    SQL is declarative: describe the result you want, then let the database choose the execution plan. Build complex queries in layers instead of writing one large statement.

                    ### Joins

                    - `INNER JOIN` keeps rows with matches on both sides.
                    - `LEFT JOIN` keeps all rows from the left side and fills missing right-side values with `NULL`.
                    - A filter in the join condition is not the same as a filter in `WHERE`; placing it in the wrong clause changes the semantics.

                    ### Grouping

                    `GROUP BY` collapses rows into groups. Aggregate functions then describe each group:

                    ```sql
                    SELECT customer_id, COUNT(*) AS order_count
                    FROM orders
                    GROUP BY customer_id;
                    ```

                    Filter groups with `HAVING`, not `WHERE`. `WHERE` runs before grouping; `HAVING` runs after.

                    ### Nulls

                    `NULL` means unknown, not zero or empty string. Use `IS NULL` and `IS NOT NULL`. Remember that `NULL = NULL` is not true.
                    """
                )
            ]);

        var indexesAndQueryPlans = CreateModule(
            "indexes-and-query-plans",
            "Indexes and Query Plans",
            "Read execution plans and make evidence-based indexing decisions.",
            "Advanced",
            55,
            3,
            [
                "Explain how a B-tree index supports equality and range lookups.",
                "Read a query plan and identify scans, seeks, and temporary sorts.",
                "Evaluate the read and write trade-offs of an index."
            ],
            [
                (
                    "index-design",
                    "Index design",
                    "Choose index columns and order based on real access patterns."
                ),
                (
                    "plan-interpretation",
                    "Query plan interpretation",
                    "Use execution plans to explain and improve query performance."
                )
            ],
            [
                CreateMaterial(
                    "indexes-and-query-plans-lesson",
                    "Indexes and query plans",
                    "Lesson",
                    25,
                    1,
                    """
                    ## Indexes trade space for access speed

                    An index is an auxiliary structure that lets the database find rows without scanning the whole table. The most common index is a B-tree, which supports equality and range lookups efficiently.

                    ### What to index

                    Index columns used by frequent filters, joins, and ordering. A composite index on `(customer_id, created_at)` supports queries that filter by customer and sort by time. Column order matters: the leftmost columns constrain the search.

                    Indexes are not free:

                    - They consume disk space.
                    - They slow down inserts, updates, and deletes.
                    - Unused indexes still need maintenance.

                    ### Read the query plan

                    Use `EXPLAIN QUERY PLAN` in SQLite or the equivalent in your database. Look for full table scans where you expected an index seek, and for temporary B-trees created by sorting or grouping.

                    Change one thing at a time, measure again, and keep the change only when the evidence supports it.
                    """
                )
            ]);

        var httpApiDesign = CreateModule(
            "http-api-design",
            "HTTP API Design",
            "Model resources and use HTTP semantics consistently.",
            "Beginner",
            50,
            1,
            [
                "Model resources and choose appropriate HTTP methods.",
                "Use status codes consistently as part of the contract.",
                "Design stateless requests that are safe to retry."
            ],
            [
                (
                    "resource-modeling",
                    "Resource modeling",
                    "Name resources and operations so the API contract is predictable."
                ),
                (
                    "http-semantics",
                    "HTTP semantics",
                    "Apply methods, status codes, and headers consistently."
                )
            ],
            [
                CreateMaterial(
                    "http-api-design-lesson",
                    "Designing HTTP resources",
                    "Lesson",
                    20,
                    1,
                    """
                    ## Model resources, then choose methods

                    HTTP APIs work best when URLs name resources and methods describe the operation.

                    - `GET /api/challenges` — list challenges.
                    - `GET /api/challenges/{id}` — retrieve one challenge.
                    - `POST /api/submissions` — create a submission.
                    - `PATCH /api/challenges/{id}` — partially update a challenge.

                    ### Status codes are part of the contract

                    Return `200` for a successful read, `201` when a resource is created, `204` for a successful operation with no body, `400` for invalid input, `404` for an unknown resource, `409` for a state conflict, and `500` only for an unexpected server failure.

                    ### Keep requests stateless

                    Each request should carry the information needed to process it. Authentication and authorization context can be derived from headers, but the server should not depend on hidden conversation state between calls.

                    Document the contract. If a response changes shape, clients need a versioning or migration path.
                    """
                )
            ]);

        var validationAndErrorHandling = CreateModule(
            "validation-and-error-handling",
            "Validation and Error Handling",
            "Reject bad input at the boundary and return safe, useful errors.",
            "Intermediate",
            45,
            2,
            [
                "Validate request structure and meaning at the API boundary.",
                "Return consistent ProblemDetails responses without leaking internals.",
                "Log diagnostic detail with a correlation identifier."
            ],
            [
                (
                    "input-validation",
                    "Input validation",
                    "Check required fields, formats, ranges, and relationships before domain logic."
                ),
                (
                    "safe-error-responses",
                    "Safe error responses",
                    "Return useful client errors without exposing implementation details."
                )
            ],
            [
                CreateMaterial(
                    "validation-and-error-handling-lesson",
                    "Validate at the boundary",
                    "Lesson",
                    20,
                    1,
                    """
                    ## Validate at the boundary, fail safely

                    Incoming data is untrusted. Validate it before it reaches domain logic, and return a response that helps the client correct the request without revealing internals.

                    ### Validate structure and meaning

                    Check required fields, lengths, formats, ranges, and relationships. Validation should produce a clear field-level error, not a generic failure.

                    ### Use a consistent error shape

                    ProblemDetails is a useful default:

                    ```json
                    {
                      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                      "title": "One or more validation errors occurred.",
                      "status": 400,
                      "errors": {
                        "languageId": ["The language is not supported."]
                      }
                    }
                    ```

                    ### Do not leak implementation details

                    Never return stack traces, SQL text, file paths, or exception messages to the client. Log the diagnostic details server-side with a correlation identifier, and return only the safe message and the identifier.

                    Validation is also a security control: reject unexpected input sizes and shapes early.
                    """
                )
            ]);

        var separationOfConcerns = CreateModule(
            "separation-of-concerns",
            "Separation of Concerns",
            "Assign clear responsibilities and keep dependencies pointing inward.",
            "Beginner",
            40,
            1,
            [
                "Assign a clear responsibility to each component.",
                "Identify dependency direction between core rules and delivery details.",
                "Evaluate cohesion and coupling in a small design."
            ],
            [
                (
                    "responsibility-boundaries",
                    "Responsibility boundaries",
                    "Keep HTTP, persistence, and domain rules in distinct places."
                ),
                (
                    "dependency-direction",
                    "Dependency direction",
                    "Make core rules independent of frameworks and delivery mechanisms."
                )
            ],
            [
                CreateMaterial(
                    "separation-of-concerns-lesson",
                    "One reason to change",
                    "Lesson",
                    20,
                    1,
                    """
                    ## Give each part one reason to change

                    Separation of concerns means assigning a clear responsibility to each component. The goal is not more layers; it is fewer hidden coupling points.

                    ### Ask three questions

                    1. **What does this code know?** HTTP details, persistence details, business rules, or presentation?
                    2. **Who changes it?** A change in the database schema should not force a change in the UI.
                    3. **Which direction do dependencies point?** Core rules should not depend on delivery or storage frameworks.

                    ### Cohesion and coupling

                    High cohesion means the parts of a component belong together. Low coupling means a change in one component rarely forces unrelated changes elsewhere. A small class with one focused job is easier to test and reason about than a large class that coordinates everything.

                    ### A practical boundary

                    Keep endpoint handlers thin: validate input, call application logic, and map the result. Put durable rules in the application or domain layer so they can be tested without HTTP or a database.
                    """
                )
            ]);

        var modularMonoliths = CreateModule(
            "modular-monoliths",
            "Modular Monoliths",
            "Keep one deployable while enforcing explicit module boundaries.",
            "Intermediate",
            55,
            2,
            [
                "Define a module contract with explicit public entry points.",
                "Keep module internals private from other modules.",
                "Decide when a separate service is justified."
            ],
            [
                (
                    "module-boundaries",
                    "Module boundaries",
                    "Expose a small contract and keep persistence details internal."
                ),
                (
                    "architecture-tradeoffs",
                    "Architecture trade-offs",
                    "Compare modular monoliths with distributed services using concrete costs."
                )
            ],
            [
                CreateMaterial(
                    "modular-monoliths-lesson",
                    "Boundaries inside one process",
                    "Lesson",
                    20,
                    1,
                    """
                    ## One deployable, explicit boundaries

                    A modular monolith runs as one process and one deployment unit, but internally it is divided into modules with explicit responsibilities. This keeps local operation simple while preventing the codebase from becoming an undifferentiated collection of classes.

                    ### Module contract

                    Each module should expose:

                    - A composition entry point for dependency registration.
                    - Endpoint mappings or a public application interface.
                    - A small set of contracts used by other modules.

                    Keep internal entities and persistence details private. Other modules should call the public interface instead of querying another module's tables directly.

                    ### When to split

                    A separate service becomes reasonable when a module needs independent scaling, deployment, or failure isolation, and the operational cost is justified. Until then, a module boundary plus a shared transaction is usually the simpler and more reliable choice.

                    ### Enforce the boundary

                    Architecture tests, project references, and code review can prevent accidental coupling. Document the allowed dependency direction so the boundary survives future changes.
                    """
                )
            ]);

        var testingFoundations = CreateModule(
            "testing-foundations",
            "Testing Foundations",
            "Write focused tests that describe observable behavior.",
            "Beginner",
            50,
            1,
            [
                "Write a test that describes observable behavior.",
                "Use arrange, act, assert to keep tests focused.",
                "Select boundary values that expose defects."
            ],
            [
                (
                    "test-design",
                    "Test design",
                    "Describe behavior with focused, independent, and readable tests."
                ),
                (
                    "boundary-analysis",
                    "Boundary analysis",
                    "Choose inputs at edges where defects are most likely."
                )
            ],
            [
                CreateMaterial(
                    "testing-foundations-lesson",
                    "Tests as executable expectations",
                    "Lesson",
                    20,
                    1,
                    """
                    ## Tests are executable expectations

                    A good test describes a behavior, sets up only the state it needs, performs one action, and checks a meaningful outcome.

                    ### Arrange, act, assert

                    1. **Arrange** the inputs and collaborators.
                    2. **Act** by calling the unit under test.
                    3. **Assert** the observable result.

                    ### Test behavior, not implementation

                    Assert what callers can observe: return values, persisted state, emitted events, or HTTP responses. Tests that assert private method calls become brittle when the implementation is refactored.

                    ### Boundary values

                    For a range `1..100`, test `0`, `1`, `100`, and `101`, not only `50`. Bugs cluster around boundaries and empty inputs.

                    ### Keep the pyramid balanced

                    Use many fast unit tests, fewer integration tests for real boundaries, and a small number of end-to-end tests for critical workflows. A test suite should fail for a real defect and stay quiet otherwise.
                    """
                )
            ]);

        var debuggingAndRootCause = CreateModule(
            "debugging-and-root-cause",
            "Debugging and Root Cause",
            "Investigate failures with evidence instead of guessing.",
            "Intermediate",
            45,
            2,
            [
                "Reproduce a defect with the smallest reliable input.",
                "Test one hypothesis at a time using evidence.",
                "Add a regression test after fixing the cause."
            ],
            [
                (
                    "systematic-debugging",
                    "Systematic debugging",
                    "Reproduce, isolate, and falsify hypotheses in a controlled loop."
                ),
                (
                    "regression-prevention",
                    "Regression prevention",
                    "Turn a fixed defect into a durable regression test."
                )
            ],
            [
                CreateMaterial(
                    "debugging-and-root-cause-lesson",
                    "Reproduce, isolate, falsify",
                    "Lesson",
                    20,
                    1,
                    """
                    ## Reproduce, isolate, and falsify

                    Debugging is a controlled investigation, not random editing. Start by making the failure repeatable.

                    ### A repeatable loop

                    1. **Reproduce** the defect with the smallest input that still fails.
                    2. **Observe** the actual behavior and compare it with the expected behavior.
                    3. **Form a hypothesis** about the cause.
                    4. **Test the hypothesis** with a log, a breakpoint, or a focused experiment.
                    5. **Fix the cause**, not the symptom.
                    6. **Add a regression test** before closing the issue.

                    ### Avoid guessing

                    Changing several things at once destroys evidence. Make one change, rerun the reproduction, and record the result.

                    ### Inspect boundaries

                    Most defects appear where assumptions cross a boundary: null values, time zones, encoding, numeric precision, or partial failure. Read the data at the boundary instead of assuming it has the shape you expect.

                    When the defect is fixed, ask why it was possible. A small design or test change may prevent the same class of failure.
                    """
                )
            ]);

        AddPrerequisite(complexityAnalysis, algorithmFoundations);
        AddPrerequisite(dataStructures, algorithmFoundations);
        AddPrerequisite(sqlQuerying, relationalModeling);
        AddPrerequisite(indexesAndQueryPlans, sqlQuerying);
        AddPrerequisite(validationAndErrorHandling, httpApiDesign);
        AddPrerequisite(modularMonoliths, separationOfConcerns);
        AddPrerequisite(debuggingAndRootCause, testingFoundations);

        return
        [
            CreateDomain(
                "programming-fundamentals",
                "Programming Fundamentals",
                "Algorithms, data structures, complexity, and the core techniques behind reliable programs.",
                1,
                algorithmFoundations,
                complexityAnalysis,
                dataStructures),
            CreateDomain(
                "database-engineering",
                "Database Engineering",
                "Relational modeling, SQL, indexes, transactions, and query performance.",
                2,
                relationalModeling,
                sqlQuerying,
                indexesAndQueryPlans),
            CreateDomain(
                "backend-engineering",
                "Backend Engineering",
                "HTTP APIs, validation, security, asynchronous work, and observability.",
                3,
                httpApiDesign,
                validationAndErrorHandling),
            CreateDomain(
                "software-architecture",
                "Software Architecture",
                "Boundaries, dependency direction, design patterns, and trade-off analysis.",
                4,
                separationOfConcerns,
                modularMonoliths),
            CreateDomain(
                "software-quality",
                "Software Quality",
                "Testing, debugging, refactoring, static analysis, and secure coding.",
                5,
                testingFoundations,
                debuggingAndRootCause)
        ];
    }

    private static LearningDomain CreateDomain(
        string slug,
        string name,
        string description,
        int sortOrder,
        params LearningModule[] modules)
    {
        var domain = new LearningDomain
        {
            Slug = slug,
            Name = name,
            Description = description,
            SortOrder = sortOrder
        };

        foreach (var module in modules)
        {
            module.DomainId = domain.Id;
            module.Domain = domain;
            domain.Modules.Add(module);
        }

        return domain;
    }

    private static LearningModule CreateModule(
        string slug,
        string title,
        string summary,
        string level,
        int estimatedMinutes,
        int sortOrder,
        IReadOnlyList<string> objectives,
        IReadOnlyList<(string Slug, string Name, string Description)> competencies,
        IReadOnlyList<LearningMaterial> materials)
    {
        var module = new LearningModule
        {
            Slug = slug,
            Title = title,
            Summary = summary,
            Level = level,
            EstimatedMinutes = estimatedMinutes,
            SortOrder = sortOrder
        };

        for (var index = 0; index < objectives.Count; index++)
        {
            module.Objectives.Add(new LearningObjective
            {
                ModuleId = module.Id,
                Module = module,
                Text = objectives[index],
                SortOrder = index + 1
            });
        }

        for (var index = 0; index < competencies.Count; index++)
        {
            var competency = competencies[index];

            module.Competencies.Add(new Competency
            {
                ModuleId = module.Id,
                Module = module,
                Slug = competency.Slug,
                Name = competency.Name,
                Description = competency.Description,
                SortOrder = index + 1
            });
        }

        foreach (var material in materials)
        {
            material.ModuleId = module.Id;
            material.Module = module;
            module.Materials.Add(material);
        }

        return module;
    }

    private static LearningMaterial CreateMaterial(
        string slug,
        string title,
        string kind,
        int estimatedMinutes,
        int sortOrder,
        string bodyMarkdown)
    {
        return new LearningMaterial
        {
            Slug = slug,
            Title = title,
            Kind = kind,
            EstimatedMinutes = estimatedMinutes,
            SortOrder = sortOrder,
            BodyMarkdown = bodyMarkdown
        };
    }

    private static void AddPrerequisite(LearningModule module, LearningModule prerequisite)
    {
        module.Prerequisites.Add(new LearningModulePrerequisite
        {
            ModuleId = module.Id,
            Module = module,
            PrerequisiteModuleId = prerequisite.Id,
            PrerequisiteModule = prerequisite
        });
    }
}
