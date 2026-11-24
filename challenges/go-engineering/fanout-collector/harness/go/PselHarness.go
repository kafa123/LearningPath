// Server-side execution harness for the fan-out collector challenge.
// It reads the server-written test manifest and emits only opaque test
// identifiers and pass/fail evidence. It is never sent to the browser.
package main

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"os"
	"reflect"
	"runtime"
	"sync"
	"sync/atomic"
	"time"
)

const testsPath = "/workspace/PselTests.json"

const harnessSafety = 500 * time.Millisecond

var (
	errUpstream       = errors.New("upstream unavailable")
	errHarnessTimeout = errors.New("harness safety timeout")
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

type fetchCase struct {
	Keys                 []string          `json:"keys"`
	Workers              int               `json:"workers"`
	Canceled             bool              `json:"canceled"`
	HoldStarted          int               `json:"holdStarted"`
	Reverse              bool              `json:"reverse"`
	ErrorKey             string            `json:"errorKey"`
	ErrorAfterAllStarted bool              `json:"errorAfterAllStarted"`
	BlockingKeys         []string          `json:"blockingKeys"`
	Values               map[string]string `json:"values"`
	Expected             struct {
		Results         json.RawMessage `json:"results"`
		Error           string          `json:"error"`
		ErrorText       string          `json:"errorText"`
		Calls           int             `json:"calls"`
		MaxConcurrency  int             `json:"maxConcurrency"`
		CanceledFetches int             `json:"canceledFetches"`
	} `json:"expected"`
}

type resultRecord struct {
	ID      string `json:"id"`
	Passed  bool   `json:"passed"`
	Errored bool   `json:"errored"`
}

type resultView struct {
	Key   string `json:"key"`
	Value string `json:"value"`
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
				var spec fetchCase
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

func runCase(spec fetchCase) bool {
	var inFlight int32
	var maxInFlight int32
	var started int32
	var calls int32
	var canceledFetches int32
	var finished int32

	var mu sync.Mutex
	perKey := map[string]int{}

	values := spec.Values
	if values == nil {
		values = map[string]string{}
	}

	blocking := make(map[string]bool, len(spec.BlockingKeys))
	for _, key := range spec.BlockingKeys {
		blocking[key] = true
	}

	hold := make(chan struct{})
	var holdOnce sync.Once
	if spec.HoldStarted <= 0 {
		close(hold)
	}

	allStarted := make(chan struct{})
	var allOnce sync.Once

	index := make(map[string]int, len(spec.Keys))
	releases := make([]chan struct{}, len(spec.Keys))
	for i, key := range spec.Keys {
		if _, ok := index[key]; !ok {
			index[key] = i
		}
		releases[i] = make(chan struct{})
	}

	fetch := func(ctx context.Context, key string) (string, error) {
		defer atomic.AddInt32(&finished, 1)

		current := atomic.AddInt32(&inFlight, 1)
		defer atomic.AddInt32(&inFlight, -1)
		for {
			previous := atomic.LoadInt32(&maxInFlight)
			if current <= previous || atomic.CompareAndSwapInt32(&maxInFlight, previous, current) {
				break
			}
		}

		startedNow := atomic.AddInt32(&started, 1)
		atomic.AddInt32(&calls, 1)

		mu.Lock()
		perKey[key]++
		mu.Unlock()

		if spec.HoldStarted > 0 && int(startedNow) >= spec.HoldStarted {
			holdOnce.Do(func() { close(hold) })
		}
		if int(startedNow) >= len(spec.Keys) {
			allOnce.Do(func() { close(allStarted) })
		}

		select {
		case <-hold:
		case <-ctx.Done():
			atomic.AddInt32(&canceledFetches, 1)
			return "", ctx.Err()
		case <-time.After(harnessSafety):
			return "", errHarnessTimeout
		}

		if spec.Reverse {
			if i, ok := index[key]; ok {
				defer close(releases[i])
				if i+1 < len(spec.Keys) {
					select {
					case <-releases[i+1]:
					case <-ctx.Done():
						return "", ctx.Err()
					case <-time.After(harnessSafety):
						return "", errHarnessTimeout
					}
				} else {
					// The last key releases the chain only once every key is in
					// flight, so the observed peak concurrency is deterministic.
					select {
					case <-allStarted:
					case <-ctx.Done():
						return "", ctx.Err()
					case <-time.After(harnessSafety):
						return "", errHarnessTimeout
					}
				}
			}
		}

		if blocking[key] {
			select {
			case <-ctx.Done():
				atomic.AddInt32(&canceledFetches, 1)
				return "", ctx.Err()
			case <-time.After(harnessSafety):
				return "", errHarnessTimeout
			}
		}

		if key == spec.ErrorKey {
			if spec.ErrorAfterAllStarted {
				select {
				case <-allStarted:
				case <-ctx.Done():
					return "", ctx.Err()
				case <-time.After(harnessSafety):
					return "", errHarnessTimeout
				}
			}
			return "", fmt.Errorf("lookup %s: %w", key, errUpstream)
		}

		if value, ok := values[key]; ok {
			return value, nil
		}
		return "value-of-" + key, nil
	}

	goroutinesBefore := runtime.NumGoroutine()

	var ctx context.Context
	var cancel context.CancelFunc
	if spec.Canceled {
		ctx, cancel = context.WithCancel(context.Background())
		cancel()
	} else {
		ctx, cancel = context.WithTimeout(context.Background(), 2*time.Second)
	}
	defer cancel()

	results, err := FetchAll(ctx, spec.Keys, spec.Workers, fetch)

	deadline := time.Now().Add(50 * time.Millisecond)
	for atomic.LoadInt32(&finished) < atomic.LoadInt32(&calls) && time.Now().Before(deadline) {
		time.Sleep(time.Millisecond)
	}
	goroutinesAfter := runtime.NumGoroutine()
	_ = goroutinesBefore
	_ = goroutinesAfter

	var view []resultView
	if results != nil {
		view = make([]resultView, len(results))
		for i, item := range results {
			view[i] = resultView{Key: item.Key, Value: item.Value}
		}
	}
	if !jsonEqualRaw(view, spec.Expected.Results) {
		return false
	}

	switch spec.Expected.Error {
	case "":
		if err != nil {
			return false
		}
	case "context canceled":
		if err == nil || !errors.Is(err, context.Canceled) {
			return false
		}
	case "context deadline exceeded":
		if err == nil || !errors.Is(err, context.DeadlineExceeded) {
			return false
		}
	case "fetch":
		if err == nil || !errors.Is(err, errUpstream) {
			return false
		}
	default:
		return false
	}

	if spec.Expected.ErrorText != "" {
		if err == nil || err.Error() != spec.Expected.ErrorText {
			return false
		}
	}

	totalCalls := atomic.LoadInt32(&calls)
	if atomic.LoadInt32(&finished) != totalCalls {
		return false
	}
	if int(totalCalls) != spec.Expected.Calls {
		return false
	}
	if int(atomic.LoadInt32(&canceledFetches)) != spec.Expected.CanceledFetches {
		return false
	}

	limit := spec.Workers
	if limit < 1 {
		limit = 1
	}
	observed := int(atomic.LoadInt32(&maxInFlight))
	if observed > limit {
		return false
	}
	if spec.Expected.MaxConcurrency > 0 && observed != spec.Expected.MaxConcurrency {
		return false
	}

	occurrences := make(map[string]int, len(spec.Keys))
	for _, key := range spec.Keys {
		occurrences[key]++
	}

	mu.Lock()
	defer mu.Unlock()
	for key, count := range perKey {
		if count > occurrences[key] {
			return false
		}
	}

	return true
}

func jsonEqualRaw(actual any, expected json.RawMessage) bool {
	if len(expected) == 0 {
		return false
	}

	actualJSON, err := json.Marshal(actual)
	if err != nil {
		return false
	}

	var actualValue any
	if err := json.Unmarshal(actualJSON, &actualValue); err != nil {
		return false
	}
	var expectedValue any
	if err := json.Unmarshal(expected, &expectedValue); err != nil {
		return false
	}

	return reflect.DeepEqual(actualValue, expectedValue)
}
