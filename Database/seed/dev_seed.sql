-- Dev character — fixed UUID so it survives docker-compose down/up cycles
INSERT INTO characters (id, name, level, xp, max_hp, max_mana, pos_x, pos_y, pos_z)
VALUES ('00000000-0000-0000-0000-000000000001', 'TestPlayer', 1, 0, 100, 100, 0, 1, 0)
ON CONFLICT (id) DO NOTHING;
