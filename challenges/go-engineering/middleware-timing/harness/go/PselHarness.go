// Server-side execution harness for the middleware timing challenge.
//
// It reads the server-written test manifest, drives Observe with a recording
// clock and a downstream handler, and prints only opaque test identifiers and
// pass/fail evidence. It never reaches the browser and uses only the Go
// standard library.
package main

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"net/http/httptest"
	"os"
	"time"
)

const testsPath = "/workspace/PselTests.json"

type harnessProbeKey struct{}

type harnessManifest struct {
	Tests []harnessTest `json:"tests"`
}

type harnessTest struct {
	ID   string          `json:"id"`
	Data json.RawMessage `json:"data"`
}

type harnessData struct {
	Cases []harnessCase `json:"cases"`
}

type harnessCase struct {
	Before       string        `json:"before"`
	After        string        `json:"after"`
	Status       int           `json:"status"`
	Body         string        `json:"body"`
	ContextValue string        `json:"contextValue"`
	Expected     harnessExpect `json:"expected"`
}

type harnessExpect struct {
	Status        int    `json:"status"`
	ElapsedHeader string `json:"elapsedHeader"`
	Body          string `json:"body"`
	ClockCalls    int    `json:"clockCalls"`
	ContextSeen   bool   `json:"contextSeen"`
}

type harnessResult struct {
	ID      string `json:"id"`
	Passed  bool   `json:"passed"`
	Errored bool   `json:"errored"`
}

type recordingClock struct {
	stamps []time.Time
	calls  int
}

func (c *recordingClock) Now() time.Time {
	if c.calls < len(c.stamps) {
		value := c.stamps[c.calls]
		c.calls++
		return value
	}
	c.calls++
	return time.Time{}
}

func main() {
	raw, err := os.ReadFile(testsPath)
	if err != nil {
		emit(nil)
		return
	}

	var manifest harnessManifest
	if err := json.Unmarshal(raw, &manifest); err != nil {
		emit(nil)
		return
	}

	results := make([]harnessResult, 0, len(manifest.Tests))
	for _, test := range manifest.Tests {
		passed, errored := runTest(test)
		results = append(results, harnessResult{
			ID:      test.ID,
			Passed:  passed,
			Errored: errored,
		})
	}
	emit(results)
}

func emit(results []harnessResult) {
	if results == nil {
		results = []harnessResult{}
	}
	payload, err := json.Marshal(results)
	if err != nil {
		payload = []byte("[]")
	}
	fmt.Println("__PSEL_RESULTS_BEGIN__")
	fmt.Println(string(payload))
	fmt.Println("__PSEL_RESULTS_END__")
}

func runTest(test harnessTest) (passed bool, errored bool) {
	defer func() {
		if recover() != nil {
			passed = false
			errored = true
		}
	}()

	var data harnessData
	if err := json.Unmarshal(test.Data, &data); err != nil {
		return false, true
	}
	if len(data.Cases) == 0 {
		return false, false
	}

	for _, testCase := range data.Cases {
		ok, err := runCase(testCase)
		if err != nil {
			return false, true
		}
		if !ok {
			return false, false
		}
	}
	return true, false
}

func runCase(testCase harnessCase) (bool, error) {
	before, err := time.Parse(time.RFC3339Nano, testCase.Before)
	if err != nil {
		return false, err
	}
	after, err := time.Parse(time.RFC3339Nano, testCase.After)
	if err != nil {
		return false, err
	}

	clock := &recordingClock{stamps: []time.Time{before, after}}

	contextSeen := false
	downstream := http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		value, _ := r.Context().Value(harnessProbeKey{}).(string)
		contextSeen = value == testCase.ContextValue

		w.Header().Set("X-Downstream", "present")
		w.WriteHeader(testCase.Status)
		if testCase.Body != "" {
			_, _ = w.Write([]byte(testCase.Body))
		}
	})

	request := httptest.NewRequest(http.MethodGet, "/observed", nil)
	request = request.WithContext(
		context.WithValue(request.Context(), harnessProbeKey{}, testCase.ContextValue),
	)

	recorder := httptest.NewRecorder()
	Observe(clock, downstream).ServeHTTP(recorder, request)
	response := recorder.Result()
	defer response.Body.Close()

	if recorder.Code != testCase.Expected.Status {
		return false, nil
	}
	if response.Header.Get("X-Elapsed-Ms") != testCase.Expected.ElapsedHeader {
		return false, nil
	}
	if response.Header.Get("X-Downstream") != "present" {
		return false, nil
	}
	if recorder.Body.String() != testCase.Expected.Body {
		return false, nil
	}
	if clock.calls != testCase.Expected.ClockCalls {
		return false, nil
	}
	if contextSeen != testCase.Expected.ContextSeen {
		return false, nil
	}

	return true, nil
}
