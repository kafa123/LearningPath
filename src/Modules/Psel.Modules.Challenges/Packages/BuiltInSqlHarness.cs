namespace Psel.Modules.Challenges.Packages;

/// <summary>
/// Trusted SQL execution harness owned by the server. SQL packages describe
/// their schema, seed data, and expected results; they never ship a harness
/// themselves. The harness creates an in-memory SQLite database per test and
/// executes exactly one read-only learner query against it.
/// </summary>
internal static class BuiltInSqlHarness
{
    public const string Source = """
        # Trusted PSEL SQL harness.
        #
        # For every server-selected test it builds a fresh in-memory SQLite
        # database from trusted schema and seed SQL, then runs the single
        # learner query in read-only mode. Only opaque test identifiers and
        # pass/fail evidence are returned.

        import json
        import math
        import re
        import sqlite3
        import sys
        import time
        from pathlib import Path

        TESTS_PATH = Path("/workspace/PselTests.json")
        WORKSPACE = Path("/workspace")
        MAX_ROWS = 1000
        MAX_COLUMNS = 64
        QUERY_TIME_LIMIT_SECONDS = 5.0
        COMPILE_ERROR_MARKER = "PSEL_SQL_COMPILE_ERROR"

        ALLOWED_ACTIONS = {
            sqlite3.SQLITE_SELECT,
            sqlite3.SQLITE_READ,
            sqlite3.SQLITE_FUNCTION,
            sqlite3.SQLITE_RECURSIVE,
        }

        FIRST_KEYWORD = re.compile(r"[A-Za-z_]+")


        def emit(results):
            print("__PSEL_RESULTS_BEGIN__")
            print(json.dumps(results))
            print("__PSEL_RESULTS_END__")


        def load_learner_query():
            candidates = sorted(WORKSPACE.glob("*.sql"))

            if len(candidates) != 1:
                raise ValueError("Submit exactly one .sql query file.")

            return candidates[0].read_text(encoding="utf-8").strip()


        def first_keyword(sql):
            without_block_comments = re.sub(r"/\*.*?\*/", " ", sql, flags=re.DOTALL)
            without_line_comments = re.sub(r"--[^\n]*", " ", without_block_comments)
            match = FIRST_KEYWORD.search(without_line_comments)
            return match.group(0).upper() if match else ""


        def authorize(action, arg1, arg2, dbname, trigger):
            return sqlite3.SQLITE_OK if action in ALLOWED_ACTIONS else sqlite3.SQLITE_DENY


        def normalize(value):
            if isinstance(value, bytes):
                return value.decode("utf-8", "replace")
            return value


        def scalar_matches(actual, expected):
            if isinstance(expected, bool) or isinstance(actual, bool):
                return (
                    isinstance(expected, bool)
                    and isinstance(actual, bool)
                    and actual == expected
                )

            if isinstance(expected, (int, float)) and isinstance(actual, (int, float)):
                if isinstance(expected, int) and isinstance(actual, int):
                    return actual == expected
                return math.isclose(
                    float(actual),
                    float(expected),
                    rel_tol=1e-9,
                    abs_tol=1e-9,
                )

            if expected is None:
                return actual is None

            return actual == expected


        def row_matches(actual, expected):
            return len(actual) == len(expected) and all(
                scalar_matches(a, e) for a, e in zip(actual, expected)
            )


        def execute_query(connection, query):
            started = time.monotonic()

            def progress():
                return 1 if time.monotonic() - started > QUERY_TIME_LIMIT_SECONDS else 0

            connection.set_progress_handler(progress, 10000)
            cursor = connection.execute(query)
            columns = [column[0] for column in cursor.description or []]

            if len(columns) > MAX_COLUMNS:
                raise ValueError(f"Query returned more than {MAX_COLUMNS} columns.")

            rows = cursor.fetchmany(MAX_ROWS + 1)

            if len(rows) > MAX_ROWS:
                raise ValueError(f"Query returned more than {MAX_ROWS} rows.")

            return columns, [tuple(normalize(value) for value in row) for row in rows]


        def plan_details(connection, query):
            cursor = connection.execute("EXPLAIN QUERY PLAN " + query)
            return [str(row[3]) for row in cursor.fetchall()]


        def check_columns(columns, expected_columns):
            if len(columns) != len(expected_columns):
                return "Column count did not match the expected result."

            actual = [str(column).lower() for column in columns]
            expected = [str(column).lower() for column in expected_columns]

            if actual != expected:
                return "Column names did not match the expected result."

            return None


        def check_rows(rows, expected_rows, ordered):
            if len(rows) != len(expected_rows):
                return "Row count did not match the expected result."

            expected = [tuple(row) for row in expected_rows]

            if ordered:
                pairs = zip(rows, expected)
            else:
                pairs = zip(
                    sorted(rows, key=repr),
                    sorted(expected, key=repr),
                )

            for index, (actual_row, expected_row) in enumerate(pairs):
                if not row_matches(actual_row, expected_row):
                    if ordered:
                        return f"Row {index + 1} did not match the expected result."
                    return "Result rows did not match the expected result."

            return None


        def check_plan(connection, query, plan):
            details = plan_details(connection, query)
            haystack = "\n".join(details).lower()

            for needle in plan.get("mustContain") or []:
                if str(needle).lower() not in haystack:
                    return "Query plan did not use the required access path."

            for needle in plan.get("mustNotContain") or []:
                if str(needle).lower() in haystack:
                    return "Query plan contained a prohibited access path."

            return None


        def run_test(test, query):
            data = test.get("data") or {}
            schema = data.get("schema") or ""
            seed = data.get("seed") or ""

            connection = sqlite3.connect(":memory:")

            try:
                connection.executescript(schema)

                if seed:
                    connection.executescript(seed)

                connection.execute("PRAGMA query_only = ON")
                connection.set_authorizer(authorize)

                columns, rows = execute_query(connection, query)

                expected_columns = data.get("columns")

                if expected_columns is not None:
                    problem = check_columns(columns, expected_columns)

                    if problem is not None:
                        return False, problem

                expected_rows = data.get("expected")

                if expected_rows is not None:
                    problem = check_rows(
                        rows,
                        expected_rows,
                        bool(data.get("ordered", True)),
                    )

                    if problem is not None:
                        return False, problem

                plan = data.get("plan")

                if plan is not None:
                    problem = check_plan(connection, query, plan)

                    if problem is not None:
                        return False, problem

                return True, None
            finally:
                connection.close()


        def fail_all(message):
            print(COMPILE_ERROR_MARKER, file=sys.stderr)
            print(message, file=sys.stderr)
            return 1


        def main():
            try:
                query = load_learner_query()
            except Exception as error:
                return fail_all(str(error))

            if not query:
                return fail_all("The submitted query is empty.")

            keyword = first_keyword(query)

            if keyword not in ("SELECT", "WITH"):
                return fail_all("Only read-only SELECT or WITH queries are allowed.")

            try:
                with TESTS_PATH.open(encoding="utf-8") as stream:
                    manifest = json.load(stream)
            except Exception as error:
                print(f"Could not read the server test manifest: {error}", file=sys.stderr)
                return 1

            results = []

            for test in manifest.get("tests", []):
                test_id = test.get("id", "")
                passed = False
                errored = False

                try:
                    passed, problem = run_test(test, query)

                    if problem is not None:
                        print(f"{test_id}: {problem}", file=sys.stderr)
                except sqlite3.OperationalError as error:
                    text = str(error)
                    lowered = text.lower()

                    if (
                        "syntax error" in lowered
                        or "incomplete input" in lowered
                        or "unrecognized token" in lowered
                    ):
                        return fail_all(f"{test_id}: {text}")

                    if "one statement at a time" in lowered:
                        return fail_all(f"{test_id}: {text}")

                    errored = True
                    print(f"{test_id}: {text}", file=sys.stderr)
                except sqlite3.ProgrammingError as error:
                    text = str(error)

                    if "one statement at a time" in text.lower():
                        return fail_all(f"{test_id}: {text}")

                    errored = True
                    print(f"{test_id}: {text}", file=sys.stderr)
                except Exception as error:
                    errored = True
                    print(f"{test_id}: {error}", file=sys.stderr)

                results.append(
                    {"id": test_id, "passed": passed, "errored": errored}
                )

            emit(results)
            return 0


        if __name__ == "__main__":
            sys.exit(main())
        """;
}
