CREATE TABLE properties (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL
);

CREATE TABLE rooms (
  id INTEGER PRIMARY KEY,
  property_id INTEGER NOT NULL REFERENCES properties(id),
  code TEXT NOT NULL,
  capacity INTEGER NOT NULL CHECK (capacity > 0)
);

CREATE TABLE bookings (
  id INTEGER PRIMARY KEY,
  room_id INTEGER NOT NULL REFERENCES rooms(id),
  guest_email TEXT NOT NULL,
  party_size INTEGER NOT NULL,
  starts_at TEXT NOT NULL,
  ends_at TEXT NOT NULL,
  status TEXT NOT NULL CHECK (status IN ('held', 'confirmed', 'cancelled'))
);
