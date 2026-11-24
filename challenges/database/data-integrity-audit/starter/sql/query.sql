-- Report every integrity issue. Each row is (issue, entity_id, detail).
SELECT issue, entity_id, detail
FROM (
  SELECT 'duplicate_member_email' AS issue,
         team_id AS entity_id,
         lower(email) AS detail
  FROM members
  GROUP BY team_id, lower(email)
  HAVING COUNT(*) > 1
);
