CREATE TABLE IF NOT EXISTS characters (
    id         UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    name       TEXT        NOT NULL,
    level      INT         NOT NULL DEFAULT 1,
    xp         INT         NOT NULL DEFAULT 0,
    max_hp     REAL        NOT NULL DEFAULT 100,
    max_mana   REAL        NOT NULL DEFAULT 100,
    pos_x      REAL        NOT NULL DEFAULT 0,
    pos_y      REAL        NOT NULL DEFAULT 0,
    pos_z      REAL        NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
