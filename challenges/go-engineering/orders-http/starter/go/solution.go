package main

import (
	"context"
	"encoding/json"
	"net/http"
	"strings"
)

// Order is the JSON shape used by both routes.
type Order struct {
	ID         string `json:"id"`
	TotalCents int    `json:"totalCents"`
}

// Store is the persistence port used by the handler.
type Store interface {
	Get(ctx context.Context, id string) (Order, bool, error)
	Save(ctx context.Context, order Order) error
}

// NewHandler builds the order routes. TODO: complete the contract.
func NewHandler(store Store) http.Handler {
	mux := http.NewServeMux()

	mux.HandleFunc("GET /orders/{id}", func(w http.ResponseWriter, r *http.Request) {
		order, found, err := store.Get(r.Context(), r.PathValue("id"))
		if err != nil {
			writeJSON(w, http.StatusInternalServerError, map[string]string{"error": "internal_error"})
			return
		}
		if !found {
			writeJSON(w, http.StatusNotFound, map[string]string{"error": "not_found"})
			return
		}
		writeJSON(w, http.StatusOK, order)
	})

	mux.HandleFunc("POST /orders", func(w http.ResponseWriter, r *http.Request) {
		var order Order
		if err := json.NewDecoder(r.Body).Decode(&order); err != nil {
			writeJSON(w, http.StatusBadRequest, map[string]string{"error": "invalid_request"})
			return
		}

		order.ID = strings.TrimSpace(order.ID)
		if order.ID == "" || order.TotalCents <= 0 {
			writeJSON(w, http.StatusBadRequest, map[string]string{"error": "invalid_request"})
			return
		}

		// TODO: reject trailing JSON values, propagate the request context,
		// and report where the new order can be read.
		if err := store.Save(context.Background(), order); err != nil {
			writeJSON(w, http.StatusInternalServerError, map[string]string{"error": "internal_error"})
			return
		}
		writeJSON(w, http.StatusCreated, order)
	})

	return mux
}

func writeJSON(w http.ResponseWriter, status int, value any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(value)
}
