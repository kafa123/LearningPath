# Hints

- Start by writing down the expected subtotal for one failing case, then compare it with the actual value. The difference usually points at one rule.
- Check the loop guard before the multiplication; a negative quantity subtracts from the subtotal.
- The threshold comparison is inclusive. Write the boundary value down explicitly: exactly 5000 and exactly 10000 must receive their discount.
- Percent discounts are integer arithmetic. `subtotal * 10 / 100` floors naturally in both languages; `Math.Round` and Python's `round` do not.
- Normalize the coupon once, at the top: trim first, then upper-case.
- Guard the empty cart by construction. A zero subtotal with a valid coupon still has nothing to discount.
