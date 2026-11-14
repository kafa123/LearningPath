"""Server-side execution harness for the Discount Refactoring challenge.

It reads the server-written test manifest and emits only opaque test
identifiers and pass/fail evidence. It is never sent to the browser.
"""

import json
import math
from pathlib import Path

import solution

TESTS_PATH = Path("/workspace/PselTests.json")
TOLERANCE = 1e-9


def _close(actual: float, expected: float) -> bool:
    return math.isclose(actual, expected, rel_tol=TOLERANCE, abs_tol=TOLERANCE)


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
                call = case["call"]
                tier = case["tier"]
                expected = case["expected"]

                if call == "ApplyDiscount":
                    actual = solution.apply_discount(float(case["price"]), tier)

                    if not _close(actual, float(expected)):
                        passed = False
                        break
                elif call == "ApplyDiscountToAll":
                    prices = [float(value) for value in case["prices"]]
                    actual = [
                        float(value)
                        for value in solution.apply_discount_to_all(prices, tier)
                    ]
                    expected_prices = [float(value) for value in expected]

                    if len(actual) != len(expected_prices) or any(
                        not _close(actual_value, expected_value)
                        for actual_value, expected_value in zip(
                            actual, expected_prices, strict=True
                        )
                    ):
                        passed = False
                        break
                else:
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
