using Psel.Modules.Learning.Domain;

namespace Psel.Modules.Learning.Seed;

/// <summary>
/// Phase 2 seed content for the six learning domains. Keep this content
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

                    ![From problem statement to a verified algorithm](/illustrations/lessons/algorithm-foundations-lesson-diagram.svg)

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

                    ![Growth classes compared as input size increases](/illustrations/lessons/complexity-analysis-lesson-diagram.svg)

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

                    ![Choosing a structure by its dominant operations](/illustrations/lessons/data-structures-lesson-diagram.svg)

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

                    ![One fact, one place: keys, foreign keys, and normalization](/illustrations/lessons/relational-modeling-lesson-diagram.svg)

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

                    ![A query narrows rows stage by stage](/illustrations/lessons/sql-querying-lesson-diagram.svg)

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

                    ![Full table scan versus composite index search](/illustrations/lessons/indexes-and-query-plans-lesson-diagram.svg)

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

                    ![Resources, methods, and status codes as one contract](/illustrations/lessons/http-api-design-lesson-diagram.svg)

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

                    ![Validate at the boundary and fail with a safe response](/illustrations/lessons/validation-and-error-handling-lesson-diagram.svg)

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

                    ![Dependencies point inward; each layer has one reason to change](/illustrations/lessons/separation-of-concerns-lesson-diagram.svg)

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

                    ![One deployable with explicit module contracts](/illustrations/lessons/modular-monoliths-lesson-diagram.svg)

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

                    ![Tests as executable expectations across the pyramid](/illustrations/lessons/testing-foundations-lesson-diagram.svg)

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

                    ![Reproduce, isolate, falsify, fix](/illustrations/lessons/debugging-and-root-cause-lesson-diagram.svg)

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

        var goFoundations = CreateModule(
            "go-foundations",
            "Go Foundations",
            "Write Go programs with clear value semantics, slices, maps, and small packages.",
            "Beginner",
            50,
            1,
            [
                "Predict when Go copies a value and when two variables share backing storage.",
                "Use slices and maps without accidental aliasing or surprise capacity growth.",
                "Organize code into small packages with explicit, exported APIs."
            ],
            [
                (
                    "value-semantics",
                    "Value semantics",
                    "Explain how assignment, arguments, and receivers copy values, and where shared backing storage still leaks through."
                ),
                (
                    "slices-and-maps",
                    "Slices and maps",
                    "Choose and manipulate slices, maps, and structs without aliasing bugs or hidden capacity surprises."
                )
            ],
            [
                CreateMaterial(
                    "go-foundations-lesson",
                    "Values, slices, and maps",
                    "Lesson",
                    22,
                    1,
                    """
                    ## Values, slices, and maps

                    Go has no reference types in the Java sense. Every assignment, argument, and return copies the value. A slice or a map is itself a small value that points at shared backing storage, which is where most surprises come from.

                    ### Zero values and copies

                    Every type has a useful zero value: `0`, `""`, `false`, `nil`, and a struct whose fields are all zero. You rarely need constructors just to initialize memory.

                    ```go
                    type Account struct {
                        ID      string
                        Balance int64
                    }

                    func credit(a Account, amount int64) {
                        a.Balance += amount // mutates the copy only
                    }

                    func creditPtr(a *Account, amount int64) {
                        a.Balance += amount // mutates the caller's value
                    }
                    ```

                    Choose value receivers when the method only reads fields or when the type is small and immutable. Choose pointer receivers when the method mutates the receiver or the struct is large enough that copying is wasteful. Be consistent across a type: mixing pointer and value receivers makes interface satisfaction confusing.

                    A struct copy is shallow. Copying a struct that contains a slice copies the slice header, not the elements.

                    ![Value copy versus shared backing storage](/illustrations/lessons/go-foundations-lesson-diagram.svg)

                    ### Slices share an array

                    A slice is three words: pointer, length, capacity. Two slices can point at the same array.

                    ```go
                    base := []int{1, 2, 3, 4}
                    head := base[:2]
                    tail := base[2:]

                    head[0] = 99        // base[0] is now 99
                    tail = append(tail, 5) // may write into base's spare capacity
                    ```

                    Use `append` only when you own the destination. When a function must not expose its backing array, return a copy:

                    ```go
                    func clone(in []int) []int {
                        out := make([]int, len(in))
                        copy(out, in)
                        return out
                    }
                    ```

                    Preallocate when the final size is known: `make([]T, 0, n)`. A full slice expression such as `base[:2:2]` caps capacity, forcing the next `append` to allocate instead of overwriting a neighbour's data.

                    ### Maps are shared hash tables

                    A map value points at a runtime hash table. Copying the map variable does not copy the entries.

                    ```go
                    counts := map[string]int{"go": 1}
                    alias := counts
                    alias["go"] = 2 // counts["go"] is 2
                    ```

                    Reading a missing key returns the zero value; use the comma-ok form when presence matters. Iteration order is deliberately randomized, so sort keys when output must be stable. A nil map is readable but panics on write; initialize with `make` or a literal before inserting.

                    ```go
                    value, ok := counts["rust"]
                    if !ok {
                        value = 0
                    }
                    ```

                    ### Packages and exported names

                    Keep packages small and named after what they provide, not where they sit. An identifier is exported only when it starts with a capital letter. Export the minimum surface, document it, and prefer returning concrete types while accepting interfaces. That keeps callers flexible without leaking implementation details.
                    """
                )
            ]);

        var goErrorsAndInterfaces = CreateModule(
            "go-errors-and-interfaces",
            "Errors and Interfaces",
            "Model failures as values and depend on small interfaces at the consumer.",
            "Intermediate",
            55,
            2,
            [
                "Wrap errors with enough context while keeping errors.Is and errors.As working.",
                "Design minimal interfaces that describe what a consumer needs.",
                "Use interfaces as test seams instead of hiding behavior behind globals."
            ],
            [
                (
                    "error-wrapping",
                    "Error wrapping",
                    "Add context to failures without destroying the error chain or leaking diagnostics."
                ),
                (
                    "interface-contracts",
                    "Interface contracts",
                    "Define small, consumer-owned interfaces that are easy to implement and test."
                )
            ],
            [
                CreateMaterial(
                    "go-errors-and-interfaces-lesson",
                    "Errors are values, interfaces are contracts",
                    "Lesson",
                    24,
                    1,
                    """
                    ## Errors are values, interfaces are contracts

                    Go treats failures as ordinary values and behavior as an implicit contract. Both choices push design decisions to the call site instead of a framework.

                    ### Errors are values

                    An error is any value with an `Error() string` method. Return `nil` for success and a non-nil value for failure.

                    ```go
                    var ErrNotFound = errors.New("not found")

                    type ValidationError struct {
                        Field string
                    }

                    func (e ValidationError) Error() string {
                        return "invalid field: " + e.Field
                    }
                    ```

                    Sentinels such as `ErrNotFound` identify a condition. Custom types carry structured detail. Callers should not compare strings; they should ask the chain.

                    ```go
                    order, err := repo.Load(id)
                    if errors.Is(err, ErrNotFound) {
                        return Result{}, ErrInvalidRecipient
                    }

                    var invalid ValidationError
                    if errors.As(err, &invalid) {
                        log.Printf("field %s failed validation", invalid.Field)
                    }
                    ```

                    ### Wrap with context, not noise

                    Use `fmt.Errorf` with `%w` to add a layer while preserving the chain.

                    ```go
                    if err := notifier.Send(message); err != nil {
                        return Result{}, fmt.Errorf("send confirmation for %s: %w", order.ID, err)
                    }
                    ```

                    The message should describe the operation that failed. Avoid repeating "error" or embedding secrets. A wrap chain answers two questions: what was attempted, and what was the underlying cause. `errors.Join` combines independent failures when one operation genuinely has several causes.

                    Wrapping is not the same as logging. Log once at the boundary that owns the decision, not at every layer that passes the error along.

                    ### Interfaces belong to the consumer

                    An interface should describe the smallest set of methods a consumer calls. Go satisfies interfaces implicitly, so the producer does not need to know which interfaces exist.

                    ```go
                    type OrderStore interface {
                        Load(ctx context.Context, id string) (Order, bool, error)
                    }

                    func Fulfill(ctx context.Context, store OrderStore, id string) error {
                        order, found, err := store.Load(ctx, id)
                        if err != nil {
                            return fmt.Errorf("load order %s: %w", id, err)
                        }
                        if !found {
                            return ErrNotFound
                        }
                        _ = order
                        return nil
                    }
                    ```

                    Accept interfaces, return concrete types. Keep interfaces near the code that consumes them, not next to the implementation. One-method interfaces such as `io.Reader` compose better than broad "service" interfaces that force every fake to implement unrelated methods.

                    A nil interface is not a nil concrete value. If a function returns a typed nil pointer as an interface, the interface itself is non-nil and a nil check will fail. Return an explicit `nil` interface when there is no value.

                    ### Interfaces as test seams

                    The same small interface lets a test replace time, storage, or delivery with a deterministic fake. That is why the boundary matters: the consumer decides which dependency to inject, so the test can observe behavior without patching globals.

                    ![Errors and interfaces at a boundary](/illustrations/lessons/go-errors-and-interfaces-lesson-diagram.svg)

                    Prefer an explicit injected dependency over a package-level variable. It keeps tests parallel-safe and makes the production wiring visible in one place.
                    """
                )
            ]);

        var goConcurrency = CreateModule(
            "go-concurrency",
            "Concurrency",
            "Coordinate goroutines with channels, contexts, and bounded worker pools.",
            "Intermediate",
            60,
            3,
            [
                "Choose an owner for every goroutine and every channel it reads.",
                "Stop work reliably when a context is canceled.",
                "Bound concurrency and preserve output order under load."
            ],
            [
                (
                    "goroutine-lifecycle",
                    "Goroutine lifecycle",
                    "Start, cancel, and wait for goroutines so no work outlives its request."
                ),
                (
                    "channel-coordination",
                    "Channel coordination",
                    "Use channels and select to hand off work, close cleanly, and collect results in order."
                )
            ],
            [
                CreateMaterial(
                    "go-concurrency-lesson",
                    "Goroutines, channels, and cancellation",
                    "Lesson",
                    26,
                    1,
                    """
                    ## Goroutines, channels, and cancellation

                    A goroutine is cheap, but a goroutine that nobody waits for is a leak. Concurrency in Go is mostly about ownership: who starts the work, who stops it, and who closes the channel.

                    ### Every goroutine needs an owner

                    The function that starts a goroutine should also arrange for it to finish. `sync.WaitGroup` is the simplest tool when the number of goroutines is known.

                    ```go
                    var wg sync.WaitGroup
                    for _, key := range keys {
                        wg.Add(1)
                        go func(key string) {
                            defer wg.Done()
                            results <- fetch(key)
                        }(key)
                    }
                    wg.Wait()
                    close(results)
                    ```

                    Capture loop variables explicitly on older toolchains, and prefer the per-iteration semantics of modern Go. Never close a channel from the receiving side: the sender closes, and only after it has stopped sending.

                    A background goroutine that blocks forever on a channel keeps its stack, its references, and any locks it holds. Tests can detect the accumulation with the race detector and `runtime.NumGoroutine`, but the fix is ownership, not tuning.

                    ### Channels coordinate, they do not store

                    An unbuffered channel synchronizes sender and receiver. A buffered channel decouples them up to its capacity. Anything larger is usually a queue with its own lifetime and backpressure requirements.

                    ```go
                    select {
                    case result := <-results:
                        handle(result)
                    case <-ctx.Done():
                        return ctx.Err()
                    }
                    ```

                    `select` is how a goroutine stays responsive while waiting. Use `range` over a channel to receive until it closes, and use the comma-ok form when a closed channel must be distinguished from a zero value.

                    Closing a channel is a broadcast to every receiver. It does not flush pending work; the sender must finish first. Sending on a closed channel panics, so ownership must be exclusive.

                    ![Bounded fan-out with cancellation](/illustrations/lessons/go-concurrency-lesson-diagram.svg)

                    ### Context is the cancellation contract

                    `context.Context` carries a deadline, a cancellation signal, and request-scoped values. Pass it as the first parameter and never store it in a struct.

                    ```go
                    ctx, cancel := context.WithCancel(ctx)
                    defer cancel()
                    ```

                    Cancellation is cooperative. Every blocking operation must select on `ctx.Done()` or use an API that does. Check `ctx.Err()` after a failed operation to distinguish cancellation from a real failure.

                    ### Bound the fan-out

                    A worker pool limits how much work runs at once. A semaphore channel of size `n` is the smallest version:

                    ```go
                    slots := make(chan struct{}, workers)
                    for _, key := range keys {
                        slots <- struct{}{}
                        go func(key string) {
                            defer func() { <-slots }()
                            fetch(key)
                        }(key)
                    }
                    ```

                    The caller still needs a `WaitGroup` and a policy for the first error. Bounded concurrency protects downstream services and keeps memory predictable. Unbounded goroutine-per-item designs look simple until the input grows.

                    ### Race detector, not guesswork

                    Run `go test -race` and `go run -race` around concurrent code. A data race is undefined behavior even when it appears to work. Prefer message passing for ownership transfer, and mutexes for short critical sections that guard state.
                    """
                )
            ]);

        var goHttpServices = CreateModule(
            "go-http-services",
            "HTTP Services",
            "Serve JSON resources with net/http, propagate request context, and keep handlers testable.",
            "Intermediate",
            55,
            4,
            [
                "Model resources with HTTP methods, status codes, and a stable JSON error shape.",
                "Propagate request context and deadlines into every dependency.",
                "Test handlers with httptest instead of a live network."
            ],
            [
                (
                    "http-handlers",
                    "HTTP handlers",
                    "Decode requests, validate input, and return the right status and representation."
                ),
                (
                    "request-context",
                    "Request context",
                    "Carry cancellation, deadlines, and request-scoped values through the call chain."
                )
            ],
            [
                CreateMaterial(
                    "go-http-services-lesson",
                    "HTTP services with net/http",
                    "Lesson",
                    25,
                    1,
                    """
                    ## HTTP services with net/http

                    The standard library is a complete HTTP toolkit. A service needs routing, decoding, validation, a consistent response shape, and context propagation. Frameworks add convenience, not different semantics.

                    ### Route by method and path

                    Modern Go patterns include the method, so a GET and a POST to the same path can have different handlers.

                    ```go
                    mux := http.NewServeMux()
                    mux.HandleFunc("GET /orders/{id}", getOrder)
                    mux.HandleFunc("POST /orders", createOrder)
                    ```

                    Use nouns for resources and let the method express the action. Return `405 Method Not Allowed` when the path exists but the method does not. Keep transport code thin: decode, validate, call the domain, encode.

                    ### Decode at the boundary

                    Decode exactly one JSON object into a typed request value. Reject unknown or trailing input when the contract is strict.

                    ```go
                    func decodeOrder(r *http.Request) (Order, error) {
                        r.Body = http.MaxBytesReader(nil, r.Body, 64<<10)
                        decoder := json.NewDecoder(r.Body)
                        decoder.DisallowUnknownFields()

                        var order Order
                        if err := decoder.Decode(&order); err != nil {
                            return Order{}, ErrInvalidRequest
                        }

                        if decoder.More() {
                            return Order{}, ErrInvalidRequest
                        }

                        return order, nil
                    }
                    ```

                    Validation is not the same as decoding. Check required fields, ranges, and cross-field rules before calling the store. Trim identifiers, reject empty strings, and keep numeric limits explicit.

                    ### One error shape

                    A stable error response lets clients react without parsing prose.

                    ```go
                    type ErrorResponse struct {
                        Error string `json:"error"`
                    }
                    ```

                    Return `400` for malformed or invalid input, `404` for a missing resource, `409` for a state conflict, and `500` for a server failure. Never put stack traces, SQL, or internal paths in the response; log them on the server and return an opaque code.

                    ### Context belongs to the request

                    `r.Context()` is canceled when the client disconnects or the server deadline expires. Pass it to every call that can block: database queries, outbound HTTP, queues.

                    ```go
                    func (h *Handler) getOrder(w http.ResponseWriter, r *http.Request) {
                        order, found, err := h.store.Get(r.Context(), r.PathValue("id"))
                        if err != nil {
                            writeError(w, http.StatusInternalServerError, "internal_error")
                            return
                        }
                        if !found {
                            writeError(w, http.StatusNotFound, "not_found")
                            return
                        }
                        writeJSON(w, http.StatusOK, order)
                    }
                    ```

                    Do not replace it with `context.Background()` inside a handler: that severs cancellation and hides slow dependencies. Request-scoped values are for tracing and identity, not for passing required parameters.

                    ![Request flow through a handler](/illustrations/lessons/go-http-services-lesson-diagram.svg)

                    ### Test with httptest

                    A handler is just an `http.Handler`. Call `ServeHTTP` with `httptest.NewRequest` and `httptest.NewRecorder` to assert status, headers, and body without opening a port. A fake store records the context it received, which proves propagation without a real database.

                    Keep response writing in one helper so every handler emits the same content type and newline handling. Small helpers remove a whole class of inconsistent responses.
                    """
                )
            ]);

        var goTestingAndTooling = CreateModule(
            "go-testing-and-tooling",
            "Testing and Tooling",
            "Write deterministic tests around explicit seams and use the Go toolchain with confidence.",
            "Intermediate",
            50,
            5,
            [
                "Introduce seams for time, randomness, and I/O instead of patching globals.",
                "Write table-driven tests that fail for one readable reason.",
                "Use go test, the race detector, vet, and coverage as one loop."
            ],
            [
                (
                    "testable-seams",
                    "Testable seams",
                    "Replace hidden dependencies with small injected interfaces that tests can control."
                ),
                (
                    "deterministic-behavior",
                    "Deterministic behavior",
                    "Remove wall-clock, randomness, and scheduling from logic so repeated runs agree."
                )
            ],
            [
                CreateMaterial(
                    "go-testing-and-tooling-lesson",
                    "Testable seams and deterministic tests",
                    "Lesson",
                    23,
                    1,
                    """
                    ## Testable seams and deterministic tests

                    A flaky test usually means the code depends on something the test does not control: the clock, a random source, a network, or a scheduler. A seam is the place where that dependency can be replaced.

                    ### Inject the dependency, do not patch it

                    Package-level variables and `time.Now` are easy to call and hard to control. Move the dependency behind a small interface at the boundary that needs it.

                    ```go
                    type Clock interface {
                        Now() time.Time
                    }

                    type Retryer struct {
                        clock   Clock
                        sleeper Sleeper
                    }

                    func (r *Retryer) Backoff(attempt int) {
                        delay := time.Duration(1<<attempt) * 100 * time.Millisecond
                        r.sleeper.Sleep(delay)
                    }
                    ```

                    A test supplies a fake clock and a recording sleeper. Production wires the real implementations once in `main`. Do not mock everything: a pure function needs no interface, and a small value type can be used directly.

                    ### Table-driven tests

                    Table tests keep the cases together and the assertion in one place.

                    ```go
                    func TestNormalizeBatchSize(t *testing.T) {
                        cases := []struct {
                            name    string
                            size    int
                            wantErr bool
                        }{
                            {name: "valid", size: 3},
                            {name: "zero", size: 0, wantErr: true},
                            {name: "negative", size: -1, wantErr: true},
                        }

                        for _, tc := range cases {
                            t.Run(tc.name, func(t *testing.T) {
                                _, err := Batch([]int{1, 2, 3}, tc.size)
                                if (err != nil) != tc.wantErr {
                                    t.Fatalf("Batch() error = %v, wantErr %v", err, tc.wantErr)
                                }
                            })
                        }
                    }
                    ```

                    One case, one reason to fail. Use `t.Helper()` in shared assertions and `t.Cleanup()` for teardown. Run subtests in parallel only when they share no mutable state.

                    ### Determinism rules

                    Logic should not call `time.Now`, `time.Sleep`, `rand`, or `os.Getenv` directly. Receive the value or the dependency instead. Delays belong in a scheduler or a sleeper that tests replace.

                    Concurrency tests should coordinate with channels and counters, not with sleeps. When a timeout is needed as a safety net, keep it generous and assert on state, not on elapsed time.

                    ![A testable seam around time](/illustrations/lessons/go-testing-and-tooling-lesson-diagram.svg)

                    ### One toolchain loop

                    - `go test ./...` runs the suite.
                    - `go test -race ./...` finds data races.
                    - `go vet ./...` catches suspicious constructs the compiler accepts.
                    - `gofmt -l .` (or `go fmt ./...`) keeps formatting mechanical.
                    - `go test -cover ./...` shows untested branches, not correctness.
                    - `go test -bench . -benchmem` measures allocations before optimizing.

                    A test that depends on real time is a test that will eventually fail on a slow machine. Replace the dependency, keep the assertion sharp, and let the toolchain do the repetition.
                    """
                )
            ]);

        var goGenerics = CreateModule(
            "go-generics",
            "Generics",
            "Use type parameters and constraints where they remove real duplication.",
            "Advanced",
            45,
            6,
            [
                "Write generic functions that preserve type safety and readable call sites.",
                "Choose constraints that describe the operations actually used.",
                "Recognize when a generic abstraction costs more than the duplication it removes."
            ],
            [
                (
                    "type-parameters",
                    "Type parameters",
                    "Instantiate generic functions and data structures without losing inference or clarity."
                ),
                (
                    "generic-constraints",
                    "Generic constraints",
                    "Express the exact operations a type argument must support."
                )
            ],
            [
                CreateMaterial(
                    "go-generics-lesson",
                    "Type parameters without over-abstraction",
                    "Lesson",
                    21,
                    1,
                    """
                    ## Type parameters without over-abstraction

                    Generics remove duplication when the algorithm is identical and only the element type changes. They add cost when the behavior differs by type, because the constraint has to grow until it describes a whole protocol.

                    ### Start from a real duplication

                    A batch helper is a good candidate: the logic is the same for any element type.

                    ```go
                    func Batch[T any](items []T, size int) ([][]T, error) {
                        if size <= 0 {
                            return nil, ErrInvalidBatchSize
                        }

                        batches := make([][]T, 0, (len(items)+size-1)/size)
                        for start := 0; start < len(items); start += size {
                            end := min(start+size, len(items))
                            batches = append(batches, items[start:end])
                        }

                        return batches, nil
                    }
                    ```

                    `T any` means the function may store, move, and return values but cannot compare or add them. That is usually enough for containers and pipelines.

                    ### Constraints describe operations

                    A constraint is an interface that lists what the type argument must support.

                    ```go
                    type Number interface {
                        ~int | ~int64 | ~float64
                    }

                    func Sum[T Number](values []T) T {
                        var total T
                        for _, value := range values {
                            total += value
                        }
                        return total
                    }
                    ```

                    The `~` token admits defined types whose underlying type matches, so `type Cents int64` works. Use `comparable` when a type must be a map key or support `==`. Compose constraints from smaller interfaces rather than repeating unions.

                    ```go
                    type Keyed[K comparable] interface {
                        Key() K
                    }
                    ```

                    A constraint with methods forces every type argument to implement them, which can be the right price for a generic index or cache. If the constraint keeps growing, the abstraction is probably describing behavior, not storage.

                    ![Generic containers and constraints](/illustrations/lessons/go-generics-lesson-diagram.svg)

                    ### Generic data structures

                    A cache needs a comparable key and an arbitrary value. The implementation is identical for every pair, so generics fit.

                    ```go
                    type Cache[K comparable, V any] struct {
                        entries map[K]*entry[K, V]
                    }
                    ```

                    Keep the zero value useful when possible, or provide a constructor that documents the required invariants. Methods on generic types cannot introduce new type parameters, so put helpers on free functions when they need one.

                    ### When not to use generics

                    - The algorithm branches on type behavior: an interface or separate functions are clearer.
                    - There is exactly one instantiation and no plan for a second.
                    - The constraint becomes a page of unions that no reader can hold in their head.
                    - Reflection is already required to inspect fields or tags.

                    Type inference removes most explicit type arguments at call sites. If callers must write `Batch[Order, ...]` everywhere, the signature is probably too clever. Prefer the boring function until duplication is real and stable.
                    """
                )
            ]);

        var goPerformance = CreateModule(
            "go-performance",
            "Performance and Profiling",
            "Reduce allocations and choose data structures by measured cost.",
            "Advanced",
            55,
            7,
            [
                "Measure allocations and CPU before changing code.",
                "Preallocate, reuse, and avoid accidental copies in hot paths.",
                "Choose a container from the operations that dominate the workload."
            ],
            [
                (
                    "allocation-discipline",
                    "Allocation discipline",
                    "Reduce avoidable allocations and copies without obscuring the code."
                ),
                (
                    "data-structure-costs",
                    "Data structure costs",
                    "Pick slices, maps, or linked structures from the operations and memory they actually cost."
                )
            ],
            [
                CreateMaterial(
                    "go-performance-lesson",
                    "Allocation discipline and data structure costs",
                    "Lesson",
                    24,
                    1,
                    """
                    ## Allocation discipline and data structure costs

                    Performance work starts with a measurement and ends with a simpler or smaller hot path. Optimizing by intuition usually moves cost instead of removing it.

                    ### Measure before changing

                    Benchmarks report time and allocations per operation.

                    ```go
                    func BenchmarkNormalize(b *testing.B) {
                        items := loadItems()
                        b.ReportAllocs()
                        b.ResetTimer()

                        for range b.N {
                            _, _ = NormalizeInventory(items)
                        }
                    }
                    ```

                    `go test -bench . -benchmem` shows `ns/op` and `allocs/op`. Use a CPU profile (`go test -cpuprofile cpu.out -bench .`, then `go tool pprof`) to find the function that dominates, and an allocation profile to see what is allocated. A profile without a benchmark is a guess.

                    ### Preallocate when the size is known

                    `append` grows a slice geometrically. That is fine when the size is unknown; it is waste when the count is already available.

                    ```go
                    result := make([]Item, 0, len(items))
                    for _, item := range items {
                        if item.Quantity > 0 {
                            result = append(result, item)
                        }
                    }
                    ```

                    The same idea applies to maps: `make(map[string]int, len(keys))` reserves buckets up front. Do not over-tune small inputs; readability wins below the hot path.

                    ### Strings and copies

                    String concatenation in a loop allocates a new string each time. `strings.Builder` grows one buffer.

                    ```go
                    var builder strings.Builder
                    builder.Grow(estimatedSize)
                    for _, part := range parts {
                        builder.WriteString(part)
                    }
                    return builder.String()
                    ```

                    Converting `[]byte` to `string` copies unless the compiler can prove the result does not escape. Keep the bytes when the value is only compared or written. Watch for hidden copies in `fmt.Sprintf`, interface boxing, and closures that capture large values.

                    ![Slice growth versus preallocation](/illustrations/lessons/go-performance-lesson-diagram.svg)

                    ### Choose a container from its operations

                    | Need | Container | Cost |
                    |---|---|---|
                    | Index by position | slice | O(1) access, O(n) insert/delete |
                    | Lookup by key | map | average O(1), no order |
                    | Move between neighbours | linked list | O(1) splice, poor cache locality |
                    | Ordered keys | sorted slice or tree | O(log n) search, O(n) insert |

                    An LRU cache combines a map for lookup with a linked list for recency, so eviction and refresh are O(1). A slice-only cache would scan on every hit.

                    ### Close the loop

                    Re-run the benchmark and the profile after each change. Keep the fastest version only if it stays readable and covered by tests. A performance budget makes regressions visible; an unmeasured micro-optimization is just a different bug.
                    """
                )
            ]);

        AddPrerequisite(goErrorsAndInterfaces, goFoundations);
        AddPrerequisite(goConcurrency, goFoundations);
        AddPrerequisite(goHttpServices, goErrorsAndInterfaces);
        AddPrerequisite(goTestingAndTooling, goErrorsAndInterfaces);
        AddPrerequisite(goGenerics, goFoundations);
        AddPrerequisite(goPerformance, goConcurrency);

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
                debuggingAndRootCause),
            CreateDomain(
                "go-engineering",
                "Go Engineering",
                "Idiomatic Go services: value semantics, errors, concurrency, HTTP, generics, and performance.",
                6,
                goFoundations,
                goErrorsAndInterfaces,
                goConcurrency,
                goHttpServices,
                goTestingAndTooling,
                goGenerics,
                goPerformance)
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
