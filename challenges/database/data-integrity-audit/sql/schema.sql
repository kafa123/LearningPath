CREATE TABLE teams (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL,
  owner_email TEXT
);

CREATE TABLE members (
  id INTEGER PRIMARY KEY,
  team_id INTEGER NOT NULL REFERENCES teams(id),
  email TEXT NOT NULL,
  display_name TEXT NOT NULL
);

CREATE TABLE invitations (
  id INTEGER PRIMARY KEY,
  team_id INTEGER NOT NULL REFERENCES teams(id),
  email TEXT NOT NULL,
  invited_at TEXT NOT NULL
);
