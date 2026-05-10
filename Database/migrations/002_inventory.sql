DO $$ BEGIN
    CREATE TYPE equipment_slot AS ENUM (
        'Weapon', 'OffHand', 'Helmet', 'Chest', 'Legs', 'Boots', 'Ring1', 'Ring2'
    );
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

CREATE TABLE IF NOT EXISTS inventory (
    id           SERIAL         PRIMARY KEY,
    character_id UUID           NOT NULL REFERENCES characters(id) ON DELETE CASCADE,
    item_id      TEXT           NOT NULL,
    quantity     INT            NOT NULL DEFAULT 1 CHECK (quantity > 0),
    slot         equipment_slot,
    acquired_at  TIMESTAMPTZ    NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_inventory_character ON inventory(character_id);
