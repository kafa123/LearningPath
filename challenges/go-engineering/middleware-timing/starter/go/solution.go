package main

import (
	"net/http"
	"strconv"
	"time"
)

// Clock supplies the current time so the middleware stays deterministic.
type Clock interface {
	Now() time.Time
}

// Observe wraps next and reports how long it took in milliseconds.
func Observe(clock Clock, next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		start := clock.Now()
		next.ServeHTTP(w, r)
		elapsed := clock.Now().Sub(start)
		w.Header().Set("X-Elapsed-Ms", strconv.FormatInt(elapsed.Milliseconds(), 10))
	})
}
