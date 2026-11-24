INSERT INTO teams (id, name, owner_email) VALUES
  (1, 'Platform', 'ada@example.com'),
  (2, 'Growth', NULL),
  (3, 'Infra', '   '),
  (4, 'Legacy', 'old@example.com');

INSERT INTO members (id, team_id, email, display_name) VALUES
  (1, 1, 'ada@example.com', 'Ada'),
  (2, 1, 'ADA@example.com', 'Ada Duplicate'),
  (3, 2, 'grace@example.com', 'Grace'),
  (4, 3, 'linus@example.com', 'Linus'),
  (5, 99, 'orphan@example.com', 'Orphan');

INSERT INTO invitations (id, team_id, email, invited_at) VALUES
  (1, 1, 'new@example.com', '2026-01-02'),
  (2, 98, 'stray@example.com', '2026-01-03'),
  (3, 2, 'grace2@example.com', '2026-02-01');
