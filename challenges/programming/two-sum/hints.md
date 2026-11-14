# Hints

1. A nested loop is a valid first version. Write it down before optimizing.
2. What information do you need about the values you have already visited?
3. If the current value is `value` and the target is `target`, what value must you have seen earlier?
4. Store each value and its index in a dictionary as you scan. Check for the complement before inserting the current value so an element is never paired with itself.
