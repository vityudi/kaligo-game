# Roadmap — Kaligo

> Learning-first milestones, staged from "Unity Hello World" to "persistent MMO." Each phase has a **goal**, **what you'll learn**, and an **exit criterion** — a concrete thing that, when working, means you can move on.
>
> Read `VISION.md` §2 first. The roadmap is split into two acts: **Act I — the single-player action RPG** (Phases 0–9), and **Act II — networked & MMO** (Phases 10–13). Act I is the actual game, played by one person. Act II layers networking onto something already fun.
>
> Time estimates assume evenings and weekends. Halve them if you go full-time, double them if life intrudes. **Be skeptical of all estimates.** Solo MMORPG-class projects routinely take many years — that's a feature of the genre, not a failure on your part.

---

# Act I — The Single-Player Action RPG

## Phase 0 — Foundations

**Goal:** Working dev environment and Unity's mental model in your head.

**What you'll learn:** Unity Editor layout, GameObjects vs. components, Prefabs, the Inspector, C# basics, MonoBehaviour lifecycle (`Awake` / `Start` / `Update`), the Input System, and how Unity organizes assets.

**Tasks:**

- Install **Unity Hub** and the latest **Unity LTS** release with the *Standard 3D* template.
- Install an IDE: **Visual Studio** (Windows) or **JetBrains Rider** (free for non-commercial / cheap for indies).
- Initialize a Git repo at the project root with a Unity-aware `.gitignore` (GitHub has an official one) and **Git LFS** configured for `.fbx`, `.png`, `.tga`, `.psd`, `.wav`, `.ogg`, `.mp4`, `.blend`. **Do this before your first commit** — Unity binary assets without LFS will wreck the repo within a month.
- Work through the **Unity Learn — Junior Programmer** pathway end-to-end. It's free and the fastest known way to internalize C# + Unity together.
- Read the docs sections on *GameObjects*, *Components*, *Prefabs*, and the *new Input System*.

**Exit criterion:** You can create a scene with a primitive, attach a C# script, and make it print to console on a key press. You can explain a GameObject, a component, and a prefab out loud.

**Estimated time:** 2–3 weeks.

---

## Phase 1 — Third-Person Character Controller

**Goal:** A character you can run around with that *feels good*.

**What you'll learn:** `CharacterController` vs. `Rigidbody`-based movement, **Cinemachine** third-person camera (FreeLook), the **Animator** state machine, **humanoid rig retargeting** (so a Mixamo animation works on any humanoid model), and the difference between *moving* and *moving well*.

**Tasks:**

- Drop a free Mixamo character (download as FBX with skin) into the project. Configure the rig as **Humanoid**.
- Download Mixamo idle / walk / run / jump / strafe animations and assign them to the humanoid rig.
- Build an **Animator Controller** with a 2D blend tree (idle ↔ walk ↔ run) driven by movement speed, plus a jump state.
- Implement WASD movement with sprint (Left Shift) and jump (Space) using Unity's new **Input System**.
- Camera: a **Cinemachine FreeLook** (or Cinemachine 3 `CinemachineCamera` with Orbital follow) configured for over-the-shoulder framing per VISION §1 — orbits the player on mouse move, handles wall collision automatically.
- Smooth turning — character rotates toward the movement direction (free-aim mode).
- Tune acceleration, friction, jump height, gravity *until it feels good*. This is not throwaway tuning; it's the foundation of how your game feels.

> **Lock-on staging:** per VISION §5, our targeting is hybrid (free-aim default, Tab to lock on). Phase 1 builds **only the free-aim camera**. The lock-on camera — a second Cinemachine virtual camera that activates when a target is acquired and the brain blends to it — is added in **Phase 3**, once enemies exist to lock onto. Don't try to build it now; there's nothing to point at.

**Exit criterion:** You can run around an empty plane and it feels satisfying. Friends who try it say "this feels good."

**Estimated time:** 2–3 weeks.

---

## Phase 2 — A World to Walk In

**Goal:** A small, hand-crafted 3D zone that establishes the visual language.

**What you'll learn:** **ProBuilder** (Unity's built-in blockout tool), Unity's lighting system, **URP** (Universal Render Pipeline) post-processing **Volumes**, atmosphere, the gap between a level and a *place*.

**Tasks:**

- Switch the project to **URP** if you didn't pick it at creation — URP is the right pipeline for stylized low-poly.
- Bring in a low-poly asset pack: **Synty POLYGON Fantasy Kingdom** is the gold standard if you have ~$30 to spend; **Quaternius** and **Kenney** medieval kits are free alternatives.
- Block out one zone (~100m × 100m) using the asset pack: a forest clearing, a path, a ruined structure, a stream — places, not just geometry.
- Lighting: Directional Light as the sun, baked global illumination, a global mood (golden hour? overcast? dusk?).
- Add a **Volume** with post-processing: bloom, mild color grading, fog. Find a look you love and lock it in.
- Add ambient sound (wind, birds, water) via Audio Sources.

**Exit criterion:** You can walk into the zone and *feel* something. It looks like a place, not a level.

**Estimated time:** 2 weeks.

---

## Phase 3 — Combat Foundations: One Sword, One Enemy

**Goal:** Real-time melee combat against a single enemy, polished until it feels great. **This is the most important phase in the roadmap.**

**What you'll learn:** Hitboxes / hurtboxes (collider triggers), **animation events** to fire damage windows on specific frames, screen shake via **Cinemachine Impulse**, hit-stop, animation cancelling, the difference between combat that *works* and combat that *feels good*.

**Tasks:**

- Player wields a sword (placeholder model is fine; Synty or Mixamo's pack has plenty).
- **Light attack** (LMB): fast, ~0.3s recovery. Animation cancels into the next attack to allow a 3-hit combo.
- **Heavy attack** (RMB): slow, ~0.7s wind-up + recovery, ~2x damage.
- **Dodge** (Space or Shift): i-frames during the roll animation, stamina cost.
- **Block** (Q or hold RMB): reduces incoming damage, costs stamina.
- One enemy with a simple state machine: walks toward player, attacks when in range, telegraphs each swing.
- Stamina bar for player; health bar for both.
- **Lock-on layer (hybrid targeting):** add a second Cinemachine virtual camera that frames the locked target and a `Targeting` component that finds the best candidate via a forward sphere-cast. **Tab** toggles lock; while locked, movement becomes strafe-relative and dodges become directional relative to the target. Hit detection itself does not change — we still resolve hits via hitbox overlap, not by "current target."
- **Game feel pass:** hit-stop on impact (~80ms freeze using `Time.timeScale`), Cinemachine Impulse screen shake on heavy hits, hit sparks/particles, hit sound effects, knockback. *This is the lesson of the phase: the game is a different game with these vs. without.*

**Exit criterion:** Fighting one enemy is genuinely fun. You catch yourself fighting it again "just one more time" while testing.

**Estimated time:** 4–6 weeks. Don't rush this.

---

## Phase 4 — Combat Depth: The Skill Bar

**Goal:** A skill bar of active abilities that makes combat tactical.

**What you'll learn:** **ScriptableObjects** as data containers for skills, cooldowns, resource systems, UI for skill bars (uGUI to start), VFX systems.

**Tasks:**

- Create a `SkillData` ScriptableObject (name, icon, cooldown, resource cost, animation, damage, VFX prefab).
- 4 active skills on a hotbar (1–4 keys), each with cooldown and resource cost.
- Examples to start: a quick gap-closer (dash strike), an AOE swing, a defensive buff, a heal.
- Mana resource separate from stamina.
- Skill bar UI showing icons, cooldown swirls, resource cost.
- Each skill has a distinct animation, VFX, and sound — readable by an opponent at a glance.
- A second enemy type that requires different tactics than the first.

**Exit criterion:** Combat encounters require choices. You die when you press the wrong button. You feel clever when you press the right one.

**Estimated time:** 3–4 weeks.

---

## Phase 5 — Progression

**Goal:** Mechanical progression that makes the player stronger over time.

**What you'll learn:** Stat systems via ScriptableObjects, leveling curves, the math of progression, the design choice between "skill-based" and "gear/level-based."

> **Service layer is already wired.** `IProgressionService` / `LocalProgressionService` / `DatabaseService` exist in `Assets/Services/` and `Assets/Database/`. XP and level are persisted to the local PostgreSQL instance automatically. Start the database with `docker-compose up -d` before entering Play mode.

**Tasks:**

- Pick a progression model (see `VISION.md` §7 — RuneScape-style skill-per-activity, Albion-style gear-tier, traditional level + stats, or hybrid).
- Implement XP gain from defeated enemies (or per-skill activity if RuneScape-style) by calling `Services.Progression.GrantXP(amount)` on enemy death.
- Design the leveling curve in `Assets/Services/Local/XPTable.cs` — replace the placeholder quadratic with your real formula. Plot it in a spreadsheet first.
- Implement stat increases on level/skill-up via `IProgressionService.OnLevelUp` event.
- New abilities or stat boosts unlock at thresholds.

**Exit criterion:** Defeating an enemy gives a small dopamine hit because of the progression bar filling. You can describe your progression curve in one sentence.

**Estimated time:** 2 weeks.

---

## Phase 6 — Inventory, Loot, and Equipment

**Goal:** Find stuff, equip stuff, the stuff matters.

**What you'll learn:** ScriptableObjects for items, equipment slots, stat modifiers, drop tables, drag-and-drop UI in uGUI.

> **Service layer is already wired.** `IInventoryService` / `LocalInventoryService` persist inventory rows to PostgreSQL. Call `Services.Inventory.Add(itemId)` on loot pickup and `Services.Inventory.Equip(rowId, slot)` from the UI. Equipment slots are defined as a PostgreSQL enum in `Database/migrations/002_inventory.sql`.

**Tasks:**

- `ItemData` ScriptableObject: name, model prefab, icon, stats, equipment slot, rarity.
- Equipment slots already defined: Weapon, OffHand, Helmet, Chest, Legs, Boots, Ring1, Ring2 — matches the `equipment_slot` DB enum.
- Inventory screen with drag-and-drop equip.
- Loot drops from enemies (drop tables with rarity weights).
- Equipped gear visible on the character model (at least weapon and chest — use **socket** GameObjects on the rig as parent points).

**Exit criterion:** Killing an enemy, picking up loot, and equipping a slightly better sword feels good. You want to fight more enemies to get more loot.

**Estimated time:** 3 weeks.

---

## Phase 7 — NPCs, Dialogue, and a Quest

**Goal:** A reason to be in the world beyond combat.

**What you'll learn:** NPC behavior with simple state machines or **NavMesh** patrol, dialogue trees, quest state management, narrative as a delivery vehicle for content.

> **Service layer is already wired.** `IQuestService` / `LocalQuestService` persist quest flags to PostgreSQL using upsert. Call `Services.Quest.SetState("questId", QuestState.InProgress)` to start a quest and `QuestState.Complete` to finish it. State survives restarts automatically.

**Tasks:**

- An NPC standing in the world. They have a name and a problem.
- Dialogue UI with branching choices, typewriter text. Consider **Yarn Spinner** or **Ink** (both free Unity-friendly dialogue tools) once your needs grow past a custom system.
- A quest: NPC asks for something → player goes elsewhere, fights an enemy or finds an item → returns and resolves it. Rewards XP and loot via `Services.Progression.GrantXP` and `Services.Inventory.Add`.
- A quest log UI driven by `Services.Quest.GetState(questId)`.

**Exit criterion:** A stranger could play through the quest start-to-finish without you in the room.

**Estimated time:** 2–3 weeks.

---

## Phase 8 — Persistence

**Goal:** Save and load. The world remembers.

**What you'll learn:** PostgreSQL schema design for game state, what actually needs to persist (and what doesn't), wiring a title screen to a real database. The foundation built here is the same one Act II uses in production — this is not throwaway work.

> **The database is already running.** PostgreSQL via Docker, all migrations applied. `Bootstrap.cs` connects on startup. Services write to the DB on every state change — persistence is already happening passively. This phase is about surfacing it to the player with a proper title screen and character flow.

**Tasks:**

- Title screen with New Game / Continue / Quit.
  - New Game: create a `characters` row, store the UUID in `PlayerPrefs` as `ActiveCharacterId`, load the game scene.
  - Continue: read `ActiveCharacterId`, call `Services.Initialize(db, id)`, load the game scene.
- Save position on rest (campfire, inn): `UPDATE characters SET pos_x/y/z WHERE id = @id`.
- The rest — XP, inventory, quest flags, cooldowns — is already written live by the service layer. No explicit save step needed.
- Defeated unique enemies / opened chests: add a `world_flags` table (`character_id`, `flag_id`, `value`) following the same pattern as `quest_flags`.

**Exit criterion:** Quit, reopen, Continue — find the world exactly as you left it.

**Estimated time:** 1–2 weeks (persistence is already wired; this phase is mostly UI and the character-select flow).

---

## Phase 9 — Vertical Slice

**Goal:** One polished zone where every Act I system works together.

**What you'll learn:** Level design, pacing, playtesting, the gap between systems and a *game*.

**Tasks:**

- Expand the zone from Phase 2 into a full play space (~30 min of content).
- Populate with 3–5 enemy types in considered locations.
- 2–3 quests with NPCs that reference each other.
- A mini-boss at the end of the quest chain.
- Music. Sound effects on every meaningful action.
- Pause menu, options menu (volume, fullscreen, sensitivity) — Unity has a built-in `Resolution` API.
- **Have at least three people play it. Watch them silently. Take notes. Don't help.**

**Exit criterion:** A stranger can pick it up, play 30 minutes, finish a quest chain, and have opinions. **You have just shipped a single-player action RPG. Stop and celebrate.**

**Estimated time:** 6–8 weeks.

> **Decision point at the end of Act I:** Is the game fun? Do you want to keep going? Is the MMO still the goal, or is "polish this single-player game and ship it" actually more interesting now? Both are great answers. If the single-player game is genuinely fun, you can ship Act I as a real game on itch.io / Steam and *then* decide about Act II.

---

# Act II — Networked & MMO

> Heads-up: every phase from here on is harder than every phase before it combined. Multiplayer programming is its own discipline. Consider working through Glenn Fiedler's *Gaffer on Games* articles (the canonical free starting point) and the **Mirror** or **FishNet** documentation start-to-finish before Phase 10.

## Phase 10 — Two Players, Same Map

**Goal:** A second player can connect over LAN and run around with you.

**What you'll learn:** Choosing between **Mirror** and **FishNet**, `NetworkBehaviour`, `NetworkTransform`, server/client RPCs, basic state synchronization, peer-to-peer hosting.

**Decision in this phase:** Mirror or FishNet?

- **Mirror** — older, larger community, more tutorials, used in commercial MMOs. Good default.
- **FishNet** — newer, faster, much better client-side prediction out of the box, also free. Worth picking if you're confident.

**Tasks:**

- Install your chosen networking library via Package Manager.
- One player hosts (acts as server). Another joins by IP.
- Both characters visible to each other; animations and movement sync smoothly on the remote view (interpolation handled by `NetworkTransform`).
- Basic chat box.
- *No combat sync yet.* Just movement.

**Exit criterion:** You and a friend can run around the same world over LAN.

**Estimated time:** 3–4 weeks.

---

## Phase 11 — Authoritative Server & Combat Sync

**Goal:** A dedicated server is the source of truth. Combat is synced and cheat-resistant.

**What you'll learn:** Authoritative server architecture, **client-side prediction**, **server reconciliation**, lag compensation. **This is the genuinely hard part of multiplayer.**

**Tasks:**

- Headless **dedicated server build** (Unity supports this as an export target — runs without a window).
- Client connects to server; server runs the world simulation.
- Combat is fully server-authoritative — clients don't decide if they hit, the server does.
- Client-side prediction so movement feels responsive even with latency.
- Two players can fight an enemy together and see consistent damage numbers.

**Exit criterion:** Two players over the internet (not LAN) can fight an enemy together and the result is consistent for both.

**Estimated time:** 8–12 weeks. Be patient.

---

## Phase 12 — Persistence at Scale

**Goal:** A real persistent server you can leave running.

**What you'll learn:** Account/session management, cloud hosting (a small VPS to start: Hetzner, DigitalOcean, Linode), zone/region partitioning.

> **The schema is already designed and battle-tested.** `Database/migrations/` contains the exact SQL that runs in production. The C# row types and Dapper queries in `Services/Local/` work unchanged against a cloud PostgreSQL instance — only the connection string changes.

**Tasks:**

- Provision a cloud PostgreSQL instance (Supabase free tier, Railway, or a self-hosted VPS). Run the same migration files.
- Add accounts table + login flow (server-side hashing — `BCrypt.Net-Next`). Characters are now per-account, not per-device.
- Implement `Services/Networked/` implementations (stubs since Phase 4). Each method sends a server RPC instead of writing the DB directly; the server runs the same Dapper queries.
- Headless Unity server runs 24/7 on the VPS.
- Basic logging and crash recovery.

**Exit criterion:** You and 3–5 friends can play on the same persistent server for a week and the world remembers everyone.

**Estimated time:** 6–10 weeks.

---

## Phase 13 — The MMO Layer

**Goal:** It feels like an MMO.

**What you'll learn:** The systems that make an MMO an MMO — and why each of them is a several-month project on its own.

**Areas to tackle, each as its own mini-phase:**

- Global and local chat (zone, party, whisper, guild).
- Parties / groups with shared XP and loot rules.
- Guilds with banks and ranks.
- Trading between players.
- A marketplace / auction house.
- PvP (zone-based or full-loot per `VISION.md`).
- Anti-cheat and rate limiting.
- Server scaling (when you have more than ~50 concurrent players, single-process won't cut it; look at zone-server splits).
- Content pipeline — adding new zones, enemies, quests in production without taking the server down.

**Exit criterion:** There's no real exit criterion. This is where MMOs live forever, with content patches, balance changes, and community management. Welcome to the long game.

**Estimated time:** Years. Honestly. With a team, faster.

---

## Guiding Principles

Rules to come back to when stuck:

1. **Finish each phase before starting the next.** Half-done systems compound badly.
2. **Placeholder art is fine.** Free Mixamo + Quaternius all the way through Act I if you want. Art is the easiest thing to upgrade later.
3. **Commit often.** Every working state goes into Git. Future-you will thank present-you.
4. **When in doubt, cut.** This roadmap will feel too big at some point. That's normal. Cut the slice smaller, never bigger.
5. **The MMO is the destination, not the journey.** The journey is a series of finished single-player builds, each better than the last.
6. **Ship Act I as a real game.** Even if Act II never happens, you'll have shipped something real. That changes everything about who you are as a developer.

---

## References & Study Material

Resources worth bookmarking now and revisiting at the right phase:

- [Unity Learn](https://learn.unity.com/) — official, free, structured. Start with the Junior Programmer pathway.
- [Catlike Coding](https://catlikecoding.com/unity/tutorials/) — deep, technical, free Unity tutorials. Excellent for the math-y bits.
- [Mirror Networking docs](https://mirror-networking.gitbook.io/docs/) — reference for Phase 10+.
- [FishNet docs](https://fish-networking.gitbook.io/docs/) — alternative for Phase 10+.
- [Gaffer on Games](https://gafferongames.com/) — Glenn Fiedler's articles on networking, the canonical free education on Phase 11 topics.
- [SlayHorizon/godot-tiny-mmo](https://github.com/SlayHorizon/godot-tiny-mmo) — different engine (Godot, 2D), but the **3-tier server architecture** (gateway → master → world) and SQLite persistence design are worth studying when planning Phase 12.

---

## Tracking

### Act I — Single-Player Action RPG

- [x] Phase 0 — Foundations
- [x] Phase 1 — Third-Person Character Controller
- [x] Phase 2 — A World to Walk In
- [x] **Phase 3 — Combat Foundations**
    - [x] 3A — Skill system core (SkillData / SkillBar / SkillExecutor), HitboxController, HealthSystem, 3-hit combo + heavy attack
    - [x] 3B — Block (hold, 80% DR), Jump, Dodge (i-frames), StaminaSystem, custom key-mapping API, animation cancel lock, movement multiplier during skills, camera-facing attack snap, 2D locomotion blend tree (9 clips)
    - [x] 3C — EnemyAI state machine (Idle → Chase → Attack → Dead), EnemyAnimator, world-space health bar
    - [x] 3D — Player HUD (HP bar + stamina bar)
    - [x] 3E — Lock-on targeting (Tab toggle, second Cinemachine vcam, strafe-relative movement)
    - [x] 3F — Game-feel pass (hit-stop, Cinemachine Impulse shake, hit particles + SFX, knockback)
- [x] **Phase 4 — Combat Depth**
    - [x] 4A — ManaSystem (100 mana, 5/s regen after 2s delay), manaCost on SkillData, per-skill independent cooldown tracking, mana bar in HUD
    - [x] 4B — New skill effects: DashStrikeEffect (gap-closer), AOESwingEffect (radius damage), DefensiveBuffEffect (timed DR), HealEffect
    - [x] 4C — Skill bar hotbar UI (4 slots, radial cooldown overlay, key hints, mana cost labels)
    - [x] 4D — 4 active skills on Key1-4: Dash Strike (20mp,6s), Whirlwind AOE (35mp,10s), Iron Skin buff (30mp,14s), Quick Mend heal (45mp,18s)
    - [x] 4E — Second enemy type: ShieldedEnemy (75% DR always, breaks on heavy hit for 3s stagger window)
- [x] **Phase 5 — Progression**
    - [x] 5A — XPTable: real leveling curve (floor(100 * n^1.85)), LevelProgress helper
    - [x] 5B — XP on kill: EnemyAI.xpReward (75 basic / 150 shielded), calls GameServices.Progression.GrantXP on death
    - [x] 5C — Stat scaling: PlayerStats component (+15 HP/level, +10 mana/level, +5% damage/level), SetMaxHealth/SetMaxMana on HealthSystem/ManaSystem
    - [x] 5D — Damage multiplier: HitboxController.Enable applies PlayerStats.DamageMultiplier to outgoing damage
    - [x] 5E — XP bar + level label HUD (XP_Fill, LevelLabel under HUDPanel), wired to PlayerHUD
    - [x] 5F — LevelUpNotification panel (fades in/out on level-up event)
- [x] **Phase 6 — Inventory & Equipment**
    - [x] 6A — `ItemRarity` enum, `EquipmentSlot` enum, `ItemData` ScriptableObject (id, name, description, icon, modelPrefab, slot, rarity, stat bonuses, stackable)
    - [x] 6B — `ItemRegistry` ScriptableObject (lookup by itemId); Bootstrap registers singleton at startup
    - [x] 6C — `LootTable` ScriptableObject (weighted entries, rollCount, dropChance); `LootDrop` component (subscribes to HealthSystem.OnDeath, rolls table, spawns pickups); `LootPickup` (trigger collider, floating label, rarity colour, `+Item` float text on pickup, calls Services.Inventory.Add)
    - [x] 6D — `EquipmentManager` component on player (subscribes to Inventory.OnChanged; sums HP/mana/damage bonuses; manages Weapon + Chest + optional sockets — instantiates item modelPrefab in socket on equip, destroys on unequip)
    - [x] 6E — `PlayerStats` updated: `RefreshStats()` public method; `ApplyStatsForLevel` layers EquipmentManager bonuses on top of level scaling
    - [x] 6F — Inventory UI (press I): bag grid (6×5), equipment panel (8 slots with correct positions), drag-and-drop equip, double-click equip/unequip, right-click unequip, hover tooltip, stat preview panel
    - [x] 6G — Item assets: Gold (Common, stackable 1-10, 9999 max stack), Rusty Sword (Uncommon, Weapon, +12% damage), Cloth Armor (Common, Chest, +20 HP +5 Mana); ItemRegistry asset wiring all three
    - [x] 6H — LootTable assets: BasicEnemyLootTable (85% drop, Gold×1-10 weight 10 / Rusty Sword weight 1), ShieldedEnemyLootTable (100% drop, 2 rolls, Gold×3-15 / Rusty Sword / Cloth Armor)
    - [x] 6I — Scene wired: Bootstrap GO added (ItemRegistry assigned); EquipmentManager on Player (ItemRegistry assigned); InventoryUI on HUD Canvas (ItemRegistry assigned, toggleKey=I); LootDrop on Enemy (BasicEnemyLootTable); LootDrop on ShieldedEnemy (ShieldedEnemyLootTable)
- [ ] Phase 7 — NPCs, Dialogue, Quest
- [ ] Phase 8 — Persistence
- [ ] Phase 9 — Vertical Slice

> **Infrastructure (cross-phase, done):** PostgreSQL via Docker, Npgsql + Dapper, service layer skeleton (`IProgressionService`, `IInventoryService`, `IQuestService`), `DatabaseService`, `Bootstrap`, migrations for all tables. See `VISION.md §3a` and `docker-compose.yml`.

### Act II — Networked & MMO

- [ ] Phase 10 — Two Players, Same Map
- [ ] Phase 11 — Authoritative Server & Combat Sync
- [ ] Phase 12 — Persistence at Scale
- [ ] Phase 13 — The MMO Layer
