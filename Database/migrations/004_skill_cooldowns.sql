CREATE TABLE IF NOT EXISTS skill_cooldowns (
    id           SERIAL      PRIMARY KEY,
    character_id UUID        NOT NULL REFERENCES characters(id) ON DELETE CASCADE,
    skill_id     TEXT        NOT NULL,
    ready_at_utc TIMESTAMPTZ NOT NULL,
    UNIQUE (character_id, skill_id)
);
