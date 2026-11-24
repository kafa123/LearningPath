# Audit relational data integrity

The workspace database stores teams, their members, and pending invitations. Some rows violate the intended integrity rules, and the database does not enforce every relationship.

Write one query that reports every issue it can find.

## Output contract

Return exactly three columns in this order:

| Column | Meaning |
|---|---|
| `issue` | One of the issue identifiers below |
| `entity_id` | The id of the team, member, or invitation that owns the issue |
| `detail` | A short, deterministic description of the issue |

Order rows by `issue` ascending, then `entity_id` ascending, then `detail` ascending.

## Issue rules

| Issue | Condition | `entity_id` | `detail` |
|---|---|---|---|
| `duplicate_member_email` | Two or more members of the **same team** share an email, compared case-insensitively | `team_id` | The email in lower case |
| `missing_owner` | A team has `owner_email` NULL, empty, or only whitespace | `teams.id` | The team name |
| `orphan_member` | A member references a `team_id` that does not exist in `teams` | `members.id` | The member email |
| `orphan_invitation` | An invitation references a `team_id` that does not exist in `teams` | `invitations.id` | The invitation email |

Report a duplicate email **once per team**, not once per duplicate row. The same email in two different teams is valid and must not be reported.

## Example

Given a team 1 with members `Ada@example.com` and `ada@example.com`, the audit returns:

```text
issue                  | entity_id | detail
duplicate_member_email | 1         | ada@example.com
```

## Constraints

- Use only read-only SQL.
- Return no rows when the data is clean.
- Do not hard-code ids from the seed data; the audit must work on any data set.
- A team with no members and a valid owner is not an issue.
