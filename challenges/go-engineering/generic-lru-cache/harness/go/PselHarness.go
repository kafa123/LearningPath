// Server-side execution harness for the generic LRU cache challenge.
//
// It reads the server-written test manifest, replays every operation sequence,
// and writes only opaque test identifiers and pass/fail evidence. It is never
// sent to the browser and uses only the Go standard library.
package main

import (
	"encoding/json"
	"fmt"
	"os"
	"reflect"
)

const pselTestsPath = "/workspace/PselTests.json"

type pselOp struct {
	Op    string          `json:"op"`
	Key   json.RawMessage `json:"key"`
	Value json.RawMessage `json:"value"`
}

type pselCase struct {
	Capacity  int             `json:"capacity"`
	KeyType   string          `json:"keyType"`
	ValueType string          `json:"valueType"`
	Ops       []pselOp        `json:"ops"`
	Expected  json.RawMessage `json:"expected"`
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

type pselPoint struct {
	X int `json:"X"`
	Y int `json:"Y"`
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
	var observations []any
	var err error

	switch testCase.KeyType + "/" + testCase.ValueType {
	case "string/int":
		observations, err = pselRunCache(testCase.Capacity, testCase.Ops, pselDecodeStringKey, pselDecodeIntValue)
	case "int/string":
		observations, err = pselRunCache(testCase.Capacity, testCase.Ops, pselDecodeIntKey, pselDecodeStringValue)
	case "string/point":
		observations, err = pselRunCache(testCase.Capacity, testCase.Ops, pselDecodeStringKey, pselDecodePointValue)
	default:
		panic(fmt.Sprintf("unknown key/value types %q/%q", testCase.KeyType, testCase.ValueType))
	}

	if err != nil {
		panic(err)
	}

	equal, err := pselJSONEqual(observations, testCase.Expected)
	if err != nil {
		panic(err)
	}

	return equal
}

func pselRunCache[K comparable, V any](
	capacity int,
	ops []pselOp,
	decodeKey func(json.RawMessage) (K, error),
	decodeValue func(json.RawMessage) (V, error),
) ([]any, error) {
	cache := NewCache[K, V](capacity)
	observations := make([]any, 0, len(ops))

	for _, op := range ops {
		switch op.Op {
		case "put":
			key, err := decodeKey(op.Key)
			if err != nil {
				return nil, err
			}
			value, err := decodeValue(op.Value)
			if err != nil {
				return nil, err
			}
			cache.Put(key, value)
			observations = append(observations, map[string]any{"op": "put"})

		case "get":
			key, err := decodeKey(op.Key)
			if err != nil {
				return nil, err
			}
			value, found := cache.Get(key)
			observations = append(observations, map[string]any{
				"op":    "get",
				"found": found,
				"value": value,
			})

		case "remove":
			key, err := decodeKey(op.Key)
			if err != nil {
				return nil, err
			}
			cache.Remove(key)
			observations = append(observations, map[string]any{"op": "remove"})

		case "keys":
			observations = append(observations, map[string]any{
				"op":   "keys",
				"keys": cache.Keys(),
			})

		case "len":
			observations = append(observations, map[string]any{
				"op":  "len",
				"len": cache.Len(),
			})

		default:
			return nil, fmt.Errorf("unknown op %q", op.Op)
		}
	}

	return observations, nil
}

func pselDecodeStringKey(raw json.RawMessage) (string, error) {
	var key string
	err := json.Unmarshal(raw, &key)
	return key, err
}

func pselDecodeIntKey(raw json.RawMessage) (int, error) {
	var key int
	err := json.Unmarshal(raw, &key)
	return key, err
}

func pselDecodeIntValue(raw json.RawMessage) (int, error) {
	var value int
	err := json.Unmarshal(raw, &value)
	return value, err
}

func pselDecodeStringValue(raw json.RawMessage) (string, error) {
	var value string
	err := json.Unmarshal(raw, &value)
	return value, err
}

func pselDecodePointValue(raw json.RawMessage) (pselPoint, error) {
	var value pselPoint
	err := json.Unmarshal(raw, &value)
	return value, err
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
