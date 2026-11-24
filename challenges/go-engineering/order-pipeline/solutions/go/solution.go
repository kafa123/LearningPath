package main

import (
	"errors"
	"fmt"
	"strings"
)

// Order is the purchase being fulfilled.
type Order struct {
	ID         string
	TotalCents int
	Email      string
}

// Repository loads orders by id.
type Repository interface {
	Load(id string) (Order, error)
}

// Notifier delivers a confirmation message.
type Notifier interface {
	Send(message string) error
}

// Result describes a successful fulfillment.
type Result struct {
	Sent    bool
	Message string
}

var (
	ErrNotFound         = errors.New("order not found")
	ErrInvalidRecipient = errors.New("invalid recipient")
	ErrBelowMinimum     = errors.New("order below minimum")
)

// Fulfill loads the order, applies the rules, and sends one confirmation.
func Fulfill(repo Repository, notifier Notifier, id string) (Result, error) {
	order, err := repo.Load(id)
	if err != nil {
		return Result{}, fmt.Errorf("load order %s: %w", id, err)
	}

	if strings.TrimSpace(order.Email) == "" {
		return Result{}, fmt.Errorf("order %s: %w", id, ErrInvalidRecipient)
	}

	if order.TotalCents < 1000 {
		return Result{}, fmt.Errorf("order %s: %w", id, ErrBelowMinimum)
	}

	message := fmt.Sprintf("Order %s confirmed: %d cents", id, order.TotalCents)
	if err := notifier.Send(message); err != nil {
		return Result{}, fmt.Errorf("send confirmation for %s: %w", id, err)
	}

	return Result{Sent: true, Message: message}, nil
}
