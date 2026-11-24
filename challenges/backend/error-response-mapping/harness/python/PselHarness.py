"""Server-side execution harness for the error response mapping challenge.

It reads the server-written test manifest and emits only opaque test
identifiers and pass/fail evidence. It is never sent to the browser.
"""

import json
from pathlib import Path

import solution

TESTS_PATH = Path("/workspace/PselTests.json")


def _matches(actual: dict, expected: dict) -> bool:
    return (
        actual["status"] == expected["status"]
        and actual["code"] == expected["code"]
        and actual["title"] == expected["title"]
        and actual["detail"] == expected["detail"]
        and actual["retryable"] == expected["retryable"]
    )


def main() -> None:
    results = []

    with TESTS_PATH.open(encoding="utf-8") as stream:
        manifest = json.load(stream)

    for test in manifest["tests"]:
        test_id = test.get("id", "")
        passed = False
        errored = False

        try:
            cases = list(test["data"]["cases"])
            passed = len(cases) > 0

            for case in cases:
                actual = solution.map_failure(
                    case.get("errorCode"),
                    case.get("internalDetail"),
                )

                if not _matches(actual, case["expected"]):
                    passed = False
                    break
        except Exception:
            passed = False
            errored = True

        results.append({"id": test_id, "passed": passed, "errored": errored})

    print("__PSEL_RESULTS_BEGIN__")
    print(json.dumps(results))
    print("__PSEL_RESULTS_END__")


if __name__ == "__main__":
    main()
