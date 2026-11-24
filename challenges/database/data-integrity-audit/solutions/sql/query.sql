SELECT issue, entity_id, detail
FROM (
  SELECT 'duplicate_member_email' AS issue,
         m.team_id AS entity_id,
         lower(m.email) AS detail
  FROM members AS m
  GROUP BY m.team_id, lower(m.email)
  HAVING COUNT(*) > 1

  UNION ALL

  SELECT 'missing_owner',
         t.id,
         t.name
  FROM teams AS t
  WHERE t.owner_email IS NULL OR trim(t.owner_email) = ''

  UNION ALL

  SELECT 'orphan_member',
         m.id,
         m.email
  FROM members AS m
  WHERE NOT EXISTS (
    SELECT 1 FROM teams AS t WHERE t.id = m.team_id
  )

  UNION ALL

  SELECT 'orphan_invitation',
         i.id,
         i.email
  FROM invitations AS i
  WHERE NOT EXISTS (
    SELECT 1 FROM teams AS t WHERE t.id = i.team_id
  )
)
ORDER BY issue ASC, entity_id ASC, detail ASC;
