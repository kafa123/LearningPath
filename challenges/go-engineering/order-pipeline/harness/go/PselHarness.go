// Server-side execution harness for the order pipeline challenge.
// It reads the server-written test manifest and emits only opaque test
// identifiers and pass/fail evidence. It is never sent to the browser.
package main

import (
	"encoding/json"
	"errors"
	"fmt"
	"os"
	"reflect"
)

const testsPath = "/workspace/PselTests.json"

var (
	errRepository = errors.New("storage offline")
	errNotifier   = errors.New("smtp rejected")
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

type orderCase struct {
	Order struct {
		ID         string `json:"id"`
		TotalCents int    `json:"totalCents"`
		Email      string `json:"email"`
	} `json:"order"`
	Repository struct {
		LoadError string `json:"loadError"`
		Wrapped   bool   `json:"wrapped"`
	} `json:"repository"`
	Notifier struct {
		SendError string `json:"sendError"`
		Wrapped   bool   `json:"wrapped"`
	} `json:"notifier"`
	Expected struct {
		Result struct {
			Sent    bool   `json:"sent"`
			Message string `json:"message"`
		} `json:"result"`
		Sends     []string `json:"sends"`
		Error     string   `json:"error"`
		ErrorText string   `json:"errorText"`
	} `json:"expected"`
}

type resultRecord struct {
	ID      string `json:"id"`
	Passed  bool   `json:"passed"`
	Errored bool   `json:"errored"`
}

type resultView struct {
	Sent    bool   `json:"sent"`
	Message string `json:"message"`
}

type fakeRepository struct {
	order Order
	err   error
}

func (f *fakeRepository) Load(id string) (Order, error) {
	if f.err != nil {
		return Order{}, f.err
	}
	return f.order, nil
}

type fakeNotifier struct {
	err      error
	messages []string
}

func (f *fakeNotifier) Send(message string) error {
	f.messages = append(f.messages, message)
	if f.err != nil {
		return f.err
	}
	return nil
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
				var spec orderCase
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

func runCase(spec orderCase) bool {
	repo := &fakeRepository{
		order: Order{
			ID:         spec.Order.ID,
			TotalCents: spec.Order.TotalCents,
			Email:      spec.Order.Email,
		},
	}

	switch spec.Repository.LoadError {
	case "":
	case "not_found":
		repo.err = ErrNotFound
	case "repository":
		repo.err = errRepository
	default:
		return false
	}
	if spec.Repository.Wrapped && repo.err != nil {
		repo.err = fmt.Errorf("database: %w", repo.err)
	}

	notifier := &fakeNotifier{messages: []string{}}
	switch spec.Notifier.SendError {
	case "":
	case "notifier":
		notifier.err = errNotifier
	default:
		return false
	}
	if spec.Notifier.Wrapped && notifier.err != nil {
		notifier.err = fmt.Errorf("transport: %w", notifier.err)
	}

	result, err := Fulfill(repo, notifier, spec.Order.ID)

	view := resultView{Sent: result.Sent, Message: result.Message}
	if !jsonMatch(view, spec.Expected.Result) {
		return false
	}
	if !jsonMatch(notifier.messages, spec.Expected.Sends) {
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
	case "err_not_found":
		return ErrNotFound, true
	case "err_invalid_recipient":
		return ErrInvalidRecipient, true
	case "err_below_minimum":
		return ErrBelowMinimum, true
	case "err_repository":
		return errRepository, true
	case "err_notifier":
		return errNotifier, true
	default:
		return nil, false
	}
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
