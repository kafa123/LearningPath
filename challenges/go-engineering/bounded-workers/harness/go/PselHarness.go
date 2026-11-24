// Server-side execution harness for the bounded worker pool challenge.
//
// It reads the server-written test manifest, drives Process with a recording
// handler, and prints only opaque test identifiers and pass/fail evidence. It
// never reaches the browser and uses only the Go standard library.
package main

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"os"
	"reflect"
	"sync"
	"sync/atomic"
	"time"
)

const testsPath = "/workspace/PselTests.json"

const harnessSafety = 500 * time.Millisecond

var (
	errHarnessJob     = errors.New("job failed")
	errHarnessTimeout = errors.New("harness safety timeout")
)

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
	Jobs                 []Job               `json:"jobs"`
	Workers              int                 `json:"workers"`
	Values               map[string]int      `json:"values"`
	Canceled             bool                `json:"canceled"`
	CancelAfterStarted   int                 `json:"cancelAfterStarted"`
	HoldStarted          int                 `json:"holdStarted"`
	Reverse              bool                `json:"reverse"`
	ErrorJobID           string              `json:"errorJobId"`
	ErrorAfterAllStarted bool                `json:"errorAfterAllStarted"`
	BlockingIDs          []string            `json:"blockingIds"`
	Expected             harnessExpectations `json:"expected"`
}

type harnessExpectations struct {
	Results         json.RawMessage `json:"results"`
	Error           string          `json:"error"`
	ErrorText       string          `json:"errorText"`
	Calls           int             `json:"calls"`
	MaxConcurrency  int             `json:"maxConcurrency"`
	CanceledHandles int             `json:"canceledHandles"`
}

type harnessResult struct {
	ID      string `json:"id"`
	Passed  bool   `json:"passed"`
	Errored bool   `json:"errored"`
}

type outcomeView struct {
	ID    string `json:"id"`
	Value int    `json:"value"`
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

func runCase(spec harnessCase) (bool, error) {
	var inFlight int32
	var maxInFlight int32
	var started int32
	var calls int32
	var canceledHandles int32
	var finished int32

	var mu sync.Mutex
	perJob := map[string]int{}
	var cancelOnce sync.Once

	values := spec.Values
	if values == nil {
		values = map[string]int{}
	}

	blocking := make(map[string]bool, len(spec.BlockingIDs))
	for _, id := range spec.BlockingIDs {
		blocking[id] = true
	}

	hold := make(chan struct{})
	var holdOnce sync.Once
	if spec.HoldStarted <= 0 {
		close(hold)
	}

	allStarted := make(chan struct{})
	var allOnce sync.Once

	index := make(map[string]int, len(spec.Jobs))
	releases := make([]chan struct{}, len(spec.Jobs))
	for i, job := range spec.Jobs {
		if _, ok := index[job.ID]; !ok {
			index[job.ID] = i
		}
		releases[i] = make(chan struct{})
	}

	var ctx context.Context
	var cancel context.CancelFunc
	if spec.Canceled {
		ctx, cancel = context.WithCancel(context.Background())
		cancel()
	} else {
		ctx, cancel = context.WithCancel(context.Background())
	}
	defer cancel()

	handle := func(callCtx context.Context, job Job) (int, error) {
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
		perJob[job.ID]++
		mu.Unlock()

		if spec.CancelAfterStarted > 0 && int(startedNow) >= spec.CancelAfterStarted {
			cancelOnce.Do(cancel)
		}
		if spec.HoldStarted > 0 && int(startedNow) >= spec.HoldStarted {
			holdOnce.Do(func() { close(hold) })
		}
		if int(startedNow) >= len(spec.Jobs) {
			allOnce.Do(func() { close(allStarted) })
		}

		select {
		case <-hold:
		case <-callCtx.Done():
			atomic.AddInt32(&canceledHandles, 1)
			return 0, callCtx.Err()
		case <-time.After(harnessSafety):
			return 0, errHarnessTimeout
		}

		if spec.Reverse {
			if i, ok := index[job.ID]; ok {
				defer close(releases[i])
				if i+1 < len(spec.Jobs) {
					select {
					case <-releases[i+1]:
					case <-callCtx.Done():
						return 0, callCtx.Err()
					case <-time.After(harnessSafety):
						return 0, errHarnessTimeout
					}
				} else {
					// The last job releases the chain only once every job is in
					// flight, so the observed peak concurrency is deterministic.
					select {
					case <-allStarted:
					case <-callCtx.Done():
						return 0, callCtx.Err()
					case <-time.After(harnessSafety):
						return 0, errHarnessTimeout
					}
				}
			}
		}

		if blocking[job.ID] {
			select {
			case <-callCtx.Done():
				atomic.AddInt32(&canceledHandles, 1)
				return 0, callCtx.Err()
			case <-time.After(harnessSafety):
				return 0, errHarnessTimeout
			}
		}

		if job.ID == spec.ErrorJobID {
			if spec.ErrorAfterAllStarted {
				select {
				case <-allStarted:
				case <-callCtx.Done():
					return 0, callCtx.Err()
				case <-time.After(harnessSafety):
					return 0, errHarnessTimeout
				}
			}
			return 0, fmt.Errorf("lookup %s: %w", job.ID, errHarnessJob)
		}

		if value, ok := values[job.ID]; ok {
			return value, nil
		}
		return job.Payload * 10, nil
	}

	outcomes, err := Process(ctx, spec.Jobs, spec.Workers, handle)

	deadline := time.Now().Add(50 * time.Millisecond)
	for atomic.LoadInt32(&finished) < atomic.LoadInt32(&calls) && time.Now().Before(deadline) {
		time.Sleep(time.Millisecond)
	}

	var view []outcomeView
	if outcomes != nil {
		view = make([]outcomeView, len(outcomes))
		for i, item := range outcomes {
			view[i] = outcomeView{ID: item.ID, Value: item.Value}
		}
	}

	if !jsonMatches(view, spec.Expected.Results) {
		return false, nil
	}

	switch spec.Expected.Error {
	case "":
		if err != nil {
			return false, nil
		}
	case "context canceled":
		if err == nil || !errors.Is(err, context.Canceled) {
			return false, nil
		}
	case "job":
		if err == nil || !errors.Is(err, errHarnessJob) {
			return false, nil
		}
	default:
		return false, nil
	}

	if spec.Expected.ErrorText != "" {
		if err == nil || err.Error() != spec.Expected.ErrorText {
			return false, nil
		}
	}

	totalCalls := atomic.LoadInt32(&calls)
	if atomic.LoadInt32(&finished) != totalCalls {
		return false, nil
	}
	if int(totalCalls) != spec.Expected.Calls {
		return false, nil
	}
	if int(atomic.LoadInt32(&canceledHandles)) != spec.Expected.CanceledHandles {
		return false, nil
	}

	limit := spec.Workers
	if limit < 1 {
		limit = 1
	}
	if limit > len(spec.Jobs) {
		limit = len(spec.Jobs)
	}
	observed := int(atomic.LoadInt32(&maxInFlight))
	if len(spec.Jobs) > 0 && observed > limit {
		return false, nil
	}
	if spec.Expected.MaxConcurrency > 0 && observed != spec.Expected.MaxConcurrency {
		return false, nil
	}

	occurrences := make(map[string]int, len(spec.Jobs))
	for _, job := range spec.Jobs {
		occurrences[job.ID]++
	}

	mu.Lock()
	defer mu.Unlock()
	for id, count := range perJob {
		if count > occurrences[id] {
			return false, nil
		}
	}

	return true, nil
}

func jsonMatches(actual any, expected json.RawMessage) bool {
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
