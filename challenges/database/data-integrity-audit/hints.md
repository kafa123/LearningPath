# Hints

- Build each audit as its own `SELECT`, then combine the results with `UNION ALL`.
- `GROUP BY team_id, lower(email)` with `HAVING COUNT(*) > 1` finds duplicates without reporting the same email per row.
- `owner_email IS NULL OR trim(owner_email) = ''` covers both NULL and blank owners. `trim` of NULL is NULL, so keep the explicit NULL check.
- A `NOT EXISTS` subquery is a safe anti-join for orphans. A `LEFT JOIN ... WHERE t.id IS NULL` works too.
- The outer query only needs to sort; keep the column names stable so the union lines up.
