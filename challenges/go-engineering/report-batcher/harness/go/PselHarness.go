// Server-side execution harness for the report batcher challenge.
//
// It reads the server-written test manifest, runs every case, and writes only
// opaque test identifiers and pass/fail evidence. It is never sent to the
// browser and uses only the Go standard library.
package main

import (
	"encoding/json"
	"errors"
	"fmt"
	"os"
	"reflect"
)

const pselTestsPath = "/workspace/PselTests.json"

type pselRow struct {
	Name  string `json:"Name"`
	Score int    `json:"Score"`
}

type pselCase struct {
	Op               string          `json:"op"`
	Type             string          `json:"type"`
	Items            json.RawMessage `json:"items"`
	Size             int             `json:"size"`
	KeyMode          string          `json:"keyMode"`
	Kind             string          `json:"kind"`
	Values           json.RawMessage `json:"values"`
	Expected         json.RawMessage `json:"expected"`
	ExpectedError    string          `json:"expectedError"`
	ExpectedKeyCalls *int            `json:"expectedKeyCalls"`
}

type pselTestData struct {
	Cases []pselCase `json:"cases"`
}

type pselTest struct {
	ID   string       `json:"id"`
	Data pselTestData `json:"data"`
}

type pselManifest struct {
	SchemaVersion int        `json:"schemaVersion"`
	Tests         []pselTest `json:"tests"`
}

type pselResult struct {
	ID      string `json:"id"`
	Passed  bool   `json:"passed"`
	Errored bool   `json:"errored"`
}

func main() {
	results := make([]pselResult, 0)

	raw, err := os.ReadFile(pselTestsPath)
	if err == nil {
		var manifest pselManifest
		if err := json.Unmarshal(raw, &manifest); err == nil {
			for _, test := range manifest.Tests {
				passed, errored := pselRunTest(test)
				results = append(results, pselResult{ID: test.ID, Passed: passed, Errored: errored})
			}
		}
	}

	encoded, err := json.Marshal(results)
	if err != nil {
		encoded = []byte("[]")
	}

	fmt.Println("__PSEL_RESULTS_BEGIN__")
	fmt.Println(string(encoded))
	fmt.Println("__PSEL_RESULTS_END__")
}

func pselRunTest(test pselTest) (passed bool, errored bool) {
	passed = true
	defer func() {
		if recover() != nil {
			passed = false
			errored = true
		}
	}()

	if len(test.Data.Cases) == 0 {
		return false, false
	}

	for _, testCase := range test.Data.Cases {
		if !pselRunCase(testCase) {
			return false, false
		}
	}

	return true, false
}

func pselRunCase(testCase pselCase) bool {
	switch testCase.Op {
	case "batch":
		return pselRunBatch(testCase)
	case "group":
		return pselRunGroup(testCase)
	case "sum":
		return pselRunSum(testCase)
	default:
		panic(fmt.Sprintf("unknown op %q", testCase.Op))
	}
}

func pselRunBatch(testCase pselCase) bool {
	switch testCase.Type {
	case "string":
		var items []string
		if err := json.Unmarshal(testCase.Items, &items); err != nil {
			panic(err)
		}
		actual, err := Batch(items, testCase.Size)
		return pselCheck(actual, err, testCase)
	case "row":
		var items []pselRow
		if err := json.Unmarshal(testCase.Items, &items); err != nil {
			panic(err)
		}
		actual, err := Batch(items, testCase.Size)
		return pselCheck(actual, err, testCase)
	default:
		panic(fmt.Sprintf("unknown batch element type %q", testCase.Type))
	}
}

func pselRunGroup(testCase pselCase) bool {
	var items []string
	if err := json.Unmarshal(testCase.Items, &items); err != nil {
		panic(err)
	}

	if testCase.KeyMode != "first-letter" {
		panic(fmt.Sprintf("unknown key mode %q", testCase.KeyMode))
	}

	calls := 0
	groups := GroupBy(items, func(item string) string {
		calls++
		if item == "" {
			return ""
		}
		return item[:1]
	})

	if testCase.ExpectedKeyCalls != nil && calls != *testCase.ExpectedKeyCalls {
		return false
	}

	return pselCheck(groups, nil, testCase)
}

func pselRunSum(testCase pselCase) bool {
	switch testCase.Kind {
	case "int":
		var values []int
		if err := json.Unmarshal(testCase.Values, &values); err != nil {
			panic(err)
		}
		return pselCheck(Sum(values), nil, testCase)
	case "int64":
		var values []int64
		if err := json.Unmarshal(testCase.Values, &values); err != nil {
			panic(err)
		}
		return pselCheck(Sum(values), nil, testCase)
	case "float64":
		var values []float64
		if err := json.Unmarshal(testCase.Values, &values); err != nil {
			panic(err)
		}
		return pselCheck(Sum(values), nil, testCase)
	default:
		panic(fmt.Sprintf("unknown numeric kind %q", testCase.Kind))
	}
}

func pselCheck(actual any, callErr error, testCase pselCase) bool {
	if testCase.ExpectedError != "" {
		if callErr == nil || !errors.Is(callErr, ErrInvalidBatchSize) {
			return false
		}
	} else if callErr != nil {
		return false
	}

	equal, err := pselJSONEqual(actual, testCase.Expected)
	if err != nil {
		panic(err)
	}

	return equal
}

func pselJSONEqual(actual any, expected json.RawMessage) (bool, error) {
	actualJSON, err := json.Marshal(actual)
	if err != nil {
		return false, err
	}

	var actualValue any
	if err := json.Unmarshal(actualJSON, &actualValue); err != nil {
		return false, err
	}

	var expectedValue any
	if err := json.Unmarshal(expected, &expectedValue); err != nil {
		return false, err
	}

	return reflect.DeepEqual(actualValue, expectedValue), nil
}
