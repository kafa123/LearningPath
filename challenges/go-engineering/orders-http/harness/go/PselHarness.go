// Server-side execution harness for the orders-http challenge.
//
// It reads the server-written test manifest, drives NewHandler with a recording
// store, and prints only opaque test identifiers and pass/fail evidence. It is
// never sent to the browser and imports only the Go standard library.
package main

import (
	"bytes"
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"net/http/httptest"
	"os"
	"reflect"
	"strings"
	"sync"
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
	Store harnessStoreConfig `json:"store"`
	Cases []harnessCase      `json:"cases"`
}

type harnessStoreConfig struct {
	Orders      []Order  `json:"orders"`
	FailGetIDs  []string `json:"failGetIds"`
	FailSaveIDs []string `json:"failSaveIds"`
}

type harnessCase struct {
	Method       string        `json:"method"`
	Path         string        `json:"path"`
	Body         string        `json:"body"`
	ContextValue string        `json:"contextValue"`
	Expected     harnessExpect `json:"expected"`
}

type harnessExpect struct {
	Status      int             `json:"status"`
	Body        json.RawMessage `json:"body"`
	Location    string          `json:"location"`
	ContentType string          `json:"contentType"`
	StoreCalls  int             `json:"storeCalls"`
	ContextSeen bool            `json:"contextSeen"`
}

type harnessResult struct {
	ID      string `json:"id"`
	Passed  bool   `json:"passed"`
	Errored bool   `json:"errored"`
}

var errHarnessStore = errors.New("harness store failure")

type harnessStore struct {
	mu        sync.Mutex
	orders    map[string]Order
	failGet   map[string]bool
	failSave  map[string]bool
	want      string
	calls     int
	contextOK bool
}

func newHarnessStore(config harnessStoreConfig, want string) *harnessStore {
	store := &harnessStore{
		orders:    make(map[string]Order, len(config.Orders)),
		failGet:   make(map[string]bool, len(config.FailGetIDs)),
		failSave:  make(map[string]bool, len(config.FailSaveIDs)),
		want:      want,
		contextOK: true,
	}
	for _, order := range config.Orders {
		store.orders[order.ID] = order
	}
	for _, id := range config.FailGetIDs {
		store.failGet[id] = true
	}
	for _, id := range config.FailSaveIDs {
		store.failSave[id] = true
	}
	return store
}

func (s *harnessStore) Get(ctx context.Context, id string) (Order, bool, error) {
	s.mu.Lock()
	defer s.mu.Unlock()
	s.calls++
	s.checkContext(ctx)
	if s.failGet[id] {
		return Order{}, false, errHarnessStore
	}
	order, found := s.orders[id]
	return order, found, nil
}

func (s *harnessStore) Save(ctx context.Context, order Order) error {
	s.mu.Lock()
	defer s.mu.Unlock()
	s.calls++
	s.checkContext(ctx)
	if s.failSave[order.ID] {
		return errHarnessStore
	}
	s.orders[order.ID] = order
	return nil
}

func (s *harnessStore) checkContext(ctx context.Context) {
	value, _ := ctx.Value(harnessProbeKey{}).(string)
	if value != s.want {
		s.contextOK = false
	}
}

func (s *harnessStore) result() (calls int, contextSeen bool) {
	s.mu.Lock()
	defer s.mu.Unlock()
	return s.calls, s.calls > 0 && s.contextOK
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
		ok, err := runCase(data.Store, testCase)
		if err != nil {
			return false, true
		}
		if !ok {
			return false, false
		}
	}
	return true, false
}

func runCase(config harnessStoreConfig, testCase harnessCase) (bool, error) {
	store := newHarnessStore(config, testCase.ContextValue)
	handler := NewHandler(store)

	var body io.Reader
	if testCase.Body != "" {
		body = strings.NewReader(testCase.Body)
	}
	request := httptest.NewRequest(testCase.Method, testCase.Path, body)
	request = request.WithContext(
		context.WithValue(request.Context(), harnessProbeKey{}, testCase.ContextValue),
	)

	recorder := httptest.NewRecorder()
	handler.ServeHTTP(recorder, request)
	response := recorder.Result()
	defer response.Body.Close()

	actualBody, err := io.ReadAll(response.Body)
	if err != nil {
		return false, err
	}

	if response.StatusCode != testCase.Expected.Status {
		return false, nil
	}
	if response.Header.Get("Location") != testCase.Expected.Location {
		return false, nil
	}
	if testCase.Expected.ContentType != "" {
		if mediaType(response.Header.Get("Content-Type")) != testCase.Expected.ContentType {
			return false, nil
		}
	}
	if !bodyMatches(actualBody, testCase.Expected.Body) {
		return false, nil
	}

	calls, contextSeen := store.result()
	if calls != testCase.Expected.StoreCalls || contextSeen != testCase.Expected.ContextSeen {
		return false, nil
	}
	return true, nil
}

func bodyMatches(actual []byte, expected json.RawMessage) bool {
	trimmed := bytes.TrimSpace(expected)
	if len(trimmed) == 0 || bytes.Equal(trimmed, []byte("null")) {
		return true
	}
	return matchesJSON(json.RawMessage(actual), expected)
}

// matchesJSON marshals the actual value, then unmarshals both sides into any so
// object key order, whitespace, and numeric representation cannot change the
// comparison.
func matchesJSON(actual any, expected json.RawMessage) bool {
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

func mediaType(value string) string {
	if index := strings.IndexByte(value, ';'); index >= 0 {
		value = value[:index]
	}
	return strings.TrimSpace(value)
}
