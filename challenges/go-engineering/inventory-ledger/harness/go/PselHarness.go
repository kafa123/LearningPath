// Server-side execution harness for the inventory ledger challenge.
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

type pselCase struct {
	Input         []Item          `json:"input"`
	Expected      json.RawMessage `json:"expected"`
	ExpectedError string          `json:"expectedError"`
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
	original := pselCopyItems(testCase.Input)

	actual, callErr := NormalizeInventory(testCase.Input)

	if !reflect.DeepEqual(original, testCase.Input) {
		return false
	}

	if testCase.ExpectedError != "" {
		if callErr == nil || !errors.Is(callErr, ErrInvalidSKU) {
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

func pselCopyItems(items []Item) []Item {
	if items == nil {
		return nil
	}

	copied := make([]Item, len(items))
	for i, item := range items {
		copied[i] = Item{SKU: item.SKU, Quantity: item.Quantity}
		if item.Tags != nil {
			tags := make([]string, len(item.Tags))
			copy(tags, item.Tags)
			copied[i].Tags = tags
		}
	}

	return copied
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
