package main

import (
	"errors"
	"time"
)

// Sleeper waits between retry attempts.
type Sleeper interface {
	Sleep(d time.Duration)
}

// Policy controls the retry budget and delay growth.
type Policy struct {
	MaxAttempts int
	BaseDelay   time.Duration
	MaxDelay    time.Duration
}

// Report records what the retry loop actually did.
type Report struct {
	Attempts int
	Delays   []time.Duration
}

// RetryableError marks a failure that may be retried.
type RetryableError struct {
	Err error
}

func (e RetryableError) Error() string {
	if e.Err == nil {
		return "retryable error"
	}
	return e.Err.Error()
}

func (e RetryableError) Unwrap() error {
	return e.Err
}

// ErrNotRetryable identifies a failure that must not be retried.
var ErrNotRetryable = errors.New("not retryable")

// IsRetryable reports whether err carries the retry marker.
func IsRetryable(err error) bool {
	return err != nil
}

// Run executes the operation with the supplied retry policy.
func Run(sleeper Sleeper, policy Policy, operation func(attempt int) error) (Report, error) {
	maxAttempts := policy.MaxAttempts
	var delays []time.Duration
	attempt := 0

	for attempt < maxAttempts {
		attempt++
		err := operation(attempt)
		if err == nil {
			return Report{Attempts: attempt, Delays: delays}, nil
		}
		if attempt == maxAttempts {
			return Report{Attempts: attempt, Delays: delays}, err
		}

		sleeper.Sleep(policy.BaseDelay)
		delays = append(delays, policy.BaseDelay)
	}

	return Report{Attempts: attempt, Delays: delays}, nil
}
