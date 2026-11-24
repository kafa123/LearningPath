package main

import (
	"errors"
	"fmt"
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
	var retryable RetryableError
	return errors.As(err, &retryable)
}

// Run executes the operation with the supplied retry policy.
func Run(sleeper Sleeper, policy Policy, operation func(attempt int) error) (Report, error) {
	maxAttempts := policy.MaxAttempts
	if maxAttempts < 1 {
		maxAttempts = 1
	}

	report := Report{Delays: []time.Duration{}}

	for attempt := 1; attempt <= maxAttempts; attempt++ {
		err := operation(attempt)
		report.Attempts = attempt
		if err == nil {
			return report, nil
		}
		if !IsRetryable(err) {
			return report, err
		}
		if attempt == maxAttempts {
			return report, fmt.Errorf("giving up after %d attempts: %w", attempt, err)
		}
		if policy.BaseDelay <= 0 {
			continue
		}

		delay := delayFor(policy, attempt)
		report.Delays = append(report.Delays, delay)
		sleeper.Sleep(delay)
	}

	return report, nil
}

func delayFor(policy Policy, attempt int) time.Duration {
	delay := policy.BaseDelay
	if policy.MaxDelay > 0 && delay > policy.MaxDelay {
		delay = policy.MaxDelay
	}
	for step := 1; step < attempt; step++ {
		if policy.MaxDelay > 0 && delay >= policy.MaxDelay {
			return policy.MaxDelay
		}
		delay *= 2
		if policy.MaxDelay > 0 && delay > policy.MaxDelay {
			return policy.MaxDelay
		}
	}
	return delay
}
