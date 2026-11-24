package main

import (
	"bytes"
	"net/http"
	"strconv"
	"time"
)

// Clock supplies the current time so the middleware stays deterministic.
type Clock interface {
	Now() time.Time
}

// bufferedResponse records what the downstream handler wrote so the elapsed
// header can still be set before the first byte reaches the client.
type bufferedResponse struct {
	header      http.Header
	statusCode  int
	wroteHeader bool
	body        bytes.Buffer
}

func newBufferedResponse() *bufferedResponse {
	return &bufferedResponse{header: make(http.Header)}
}

func (b *bufferedResponse) Header() http.Header {
	return b.header
}

func (b *bufferedResponse) WriteHeader(statusCode int) {
	if b.wroteHeader {
		return
	}
	b.wroteHeader = true
	b.statusCode = statusCode
}

func (b *bufferedResponse) Write(p []byte) (int, error) {
	if !b.wroteHeader {
		b.WriteHeader(http.StatusOK)
	}
	return b.body.Write(p)
}

// Observe wraps next and reports how long it took in milliseconds.
func Observe(clock Clock, next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		start := clock.Now()

		buffer := newBufferedResponse()
		next.ServeHTTP(buffer, r)

		elapsed := clock.Now().Sub(start)

		header := w.Header()
		for key, values := range buffer.header {
			for _, value := range values {
				header.Add(key, value)
			}
		}
		header.Set("X-Elapsed-Ms", strconv.FormatInt(elapsed.Milliseconds(), 10))

		statusCode := buffer.statusCode
		if statusCode == 0 {
			statusCode = http.StatusOK
		}
		w.WriteHeader(statusCode)
		_, _ = w.Write(buffer.body.Bytes())
	})
}
