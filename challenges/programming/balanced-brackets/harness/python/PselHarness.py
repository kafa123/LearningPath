"""Server-side execution harness for the Balanced Brackets challenge.

It reads the server-written test manifest and emits only opaque test
identifiers and pass/fail evidence. It is never sent to the browser.
"""

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
            text = data["input"]["text"]
            expected = bool(data["expected"])

            passed = bool(solution.is_balanced(text)) == expected
        except Exception:
            errored = True

        results.append({"id": test_id, "passed": passed, "errored": errored})

    print("__PSEL_RESULTS_BEGIN__")
    print(json.dumps(results))
    print("__PSEL_RESULTS_END__")


if __name__ == "__main__":
    main()
