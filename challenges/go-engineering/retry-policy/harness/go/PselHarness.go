// Server-side execution harness for the retry policy challenge.
// It reads the server-written test manifest and emits only opaque test
// identifiers and pass/fail evidence. It is never sent to the browser.
package main

import (
	"encoding/json"
	"errors"
	"fmt"
	"os"
	"reflect"
	"time"
)

const testsPath = "/workspace/PselTests.json"

var (
	errTemporary = errors.New("temporary failure")
	errPermanent = errors.New("permanent failure")
)

type manifest struct {
	SchemaVersion int        `json:"schemaVersion"`
	Tests         []testCase `json:"tests"`
}

type testCase struct {
	ID   string          `json:"id"`
	Data json.RawMessage `json:"data"`
}

type testData struct {
	Cases []json.RawMessage `json:"cases"`
}

type retryCase struct {
	Policy struct {
		MaxAttempts int `json:"maxAttempts"`
		BaseDelayMs int `json:"baseDelayMs"`
		MaxDelayMs  int `json:"maxDelayMs"`
	} `json:"policy"`
	Outcomes []string `json:"outcomes"`
	Expected struct {
		Attempts  int    `json:"attempts"`
		Delays    []int  `json:"delays"`
		Error     string `json:"error"`
		ErrorText string `json:"errorText"`
	} `json:"expected"`
}

type resultRecord struct {
	ID      string `json:"id"`
	Passed  bool   `json:"passed"`
	Errored bool   `json:"errored"`
}

type reportView struct {
	Attempts int     `json:"attempts"`
	Delays   []int64 `json:"delays"`
}

type recordingSleeper struct {
	delays []time.Duration
}

func (s *recordingSleeper) Sleep(d time.Duration) {
	s.delays = append(s.delays, d)
}

func main() {
	raw, err := os.ReadFile(testsPath)
	if err != nil {
		return
	}

	var schedule manifest
	if err := json.Unmarshal(raw, &schedule); err != nil {
		return
	}

	results := make([]resultRecord, 0, len(schedule.Tests))
	for _, test := range schedule.Tests {
		record := resultRecord{ID: test.ID}

		func() {
			defer func() {
				if recover() != nil {
					record.Passed = false
					record.Errored = true
				}
			}()

			var data testData
			if err := json.Unmarshal(test.Data, &data); err != nil {
				record.Errored = true
				return
			}
			if len(data.Cases) == 0 {
				return
			}

			record.Passed = true
			for _, rawCase := range data.Cases {
				var spec retryCase
				if err := json.Unmarshal(rawCase, &spec); err != nil {
					record.Passed = false
					record.Errored = true
					return
				}
				if !runCase(spec) {
					record.Passed = false
					return
				}
			}
		}()

		results = append(results, record)
	}

	encoded, err := json.Marshal(results)
	if err != nil {
		return
	}

	fmt.Println("__PSEL_RESULTS_BEGIN__")
	fmt.Println(string(encoded))
	fmt.Println("__PSEL_RESULTS_END__")
}

func runCase(spec retryCase) bool {
	policy := Policy{
		MaxAttempts: spec.Policy.MaxAttempts,
		BaseDelay:   time.Duration(spec.Policy.BaseDelayMs) * time.Millisecond,
		MaxDelay:    time.Duration(spec.Policy.MaxDelayMs) * time.Millisecond,
	}
	sleeper := &recordingSleeper{}
	attempts := make([]int, 0, len(spec.Outcomes))

	operation := func(attempt int) error {
		attempts = append(attempts, attempt)
		index := attempt - 1
		if index < 0 || index >= len(spec.Outcomes) {
			return fmt.Errorf("unexpected attempt %d", attempt)
		}

		switch spec.Outcomes[index] {
		case "ok":
			return nil
		case "retryable":
			return RetryableError{Err: errTemporary}
		case "retryable-wrapped":
			return fmt.Errorf("query failed: %w", RetryableError{Err: errTemporary})
		case "fatal":
			return errPermanent
		case "fatal-wrapped":
			return fmt.Errorf("decode: %w", errPermanent)
		default:
			return fmt.Errorf("unknown outcome %q", spec.Outcomes[index])
		}
	}

	report, err := Run(sleeper, policy, operation)

	var delays []int64
	if report.Delays != nil {
		delays = make([]int64, len(report.Delays))
		for i, delay := range report.Delays {
			delays[i] = int64(delay / time.Millisecond)
		}
	}

	view := reportView{Attempts: report.Attempts, Delays: delays}
	if !jsonMatch(view.Attempts, spec.Expected.Attempts) {
		return false
	}
	if !jsonMatch(view.Delays, spec.Expected.Delays) {
		return false
	}

	if len(attempts) != spec.Expected.Attempts {
		return false
	}
	for i, attempt := range attempts {
		if attempt != i+1 {
			return false
		}
	}

	if !sameDurations(sleeper.delays, report.Delays) {
		return false
	}

	if spec.Expected.Error == "" {
		if err != nil {
			return false
		}
	} else {
		sentinel, ok := sentinelFor(spec.Expected.Error)
		if !ok || err == nil || !errors.Is(err, sentinel) {
			return false
		}
	}

	if spec.Expected.ErrorText != "" {
		if err == nil || err.Error() != spec.Expected.ErrorText {
			return false
		}
	}

	return true
}

func sentinelFor(name string) (error, bool) {
	switch name {
	case "retryable":
		return errTemporary, true
	case "fatal":
		return errPermanent, true
	default:
		return nil, false
	}
}

func sameDurations(left []time.Duration, right []time.Duration) bool {
	if len(left) != len(right) {
		return false
	}
	for i := range left {
		if left[i] != right[i] {
			return false
		}
	}
	return true
}

func jsonMatch(actual any, expected any) bool {
	actualJSON, err := json.Marshal(actual)
	if err != nil {
		return false
	}
	expectedJSON, err := json.Marshal(expected)
	if err != nil {
		return false
	}

	var actualValue any
	if err := json.Unmarshal(actualJSON, &actualValue); err != nil {
		return false
	}
	var expectedValue any
	if err := json.Unmarshal(expectedJSON, &expectedValue); err != nil {
		return false
	}

	return reflect.DeepEqual(actualValue, expectedValue)
}
