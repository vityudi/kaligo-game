DO $$ BEGIN
    CREATE TYPE quest_state AS ENUM ('NotStarted', 'InProgress', 'Complete');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

CREATE TABLE IF NOT EXISTS quest_flags (
    id           SERIAL      PRIMARY KEY,
    character_id UUID        NOT NULL REFERENCES characters(id) ON DELETE CASCADE,
    quest_id     TEXT        NOT NULL,
    state        quest_state NOT NULL DEFAULT 'NotStarted',
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (character_id, quest_id)
);

CREATE INDEX IF NOT EXISTS idx_quests_character ON quest_flags(character_id);
