# Kaligo

A 3D fantasy MMORPG with skill-based real-time combat, built solo from scratch in Unity.

> *Kaligo* — from Latin *caligo* meaning fog, darkness, dim sight. The K-spelling makes the name ours: distinct from the 2017 Steam title *Caligo*, equally readable in English and Portuguese, and visually a touch more striking. See `VISION.md` §1 for the full naming rationale.

## Project Documents

- [`VISION.md`](./VISION.md) — what the game is, what it isn't, design pillars, open questions.
- [`ROADMAP.md`](./ROADMAP.md) — staged milestones from "install Unity" to "persistent MMO." Read VISION first.

## Repository Structure

The Unity project sits at the repo root — no nested project folder. Docs live alongside the Unity-managed directories.

```
kaligo-game/
├── README.md            ← you are here
├── VISION.md            ← the design north star
├── ROADMAP.md           ← phased milestones (Act I single-player → Act II MMO)
├── .gitignore           ← Unity-aware ignores
├── .gitattributes       ← Git LFS rules for binary assets
├── Assets/              ← Unity assets, scripts, scenes — the game lives here
├── Packages/            ← Unity Package Manager manifest (committed)
├── ProjectSettings/     ← Unity project settings (committed)
├── kaligo-game.slnx     ← Visual Studio / Rider solution file (committed)
├── Library/             ← Unity-generated, gitignored
├── Logs/                ← Unity-generated, gitignored
├── Temp/                ← Unity-generated, gitignored
└── UserSettings/        ← per-user IDE state, gitignored
```

## Tech Stack (locked in)

- **Engine:** Unity (latest LTS) with **URP** (Universal Render Pipeline)
- **Language:** C#
- **3D modeling:** Blender
- **Animations:** Mixamo (free, humanoid)
- **Placeholder art:** Synty POLYGON Fantasy Kingdom (paid, optional) / Quaternius / Kenney (free)
- **Database:** PostgreSQL 16 (local via Docker; same engine in Act II production)
- **DB driver:** Npgsql + Dapper (installed via NuGetForUnity — see below)
- **Networking (Act II):** Mirror or FishNet — decided in Phase 10
- **Version control:** Git + Git LFS

## Database Setup

The game uses PostgreSQL locally via Docker. Start it once before entering Play mode:

```bash
docker-compose up -d        # starts Postgres, applies migrations automatically
docker-compose down         # stop (data persists in Docker volume)
docker-compose down -v      # nuke data and start fresh
```

**First-time Unity setup (install Npgsql + Dapper):**
1. Install [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) via Package Manager (git URL).
2. In Unity: NuGet → Manage NuGet Packages → install `Npgsql` and `Dapper`.
3. Enter Play mode — `Bootstrap.cs` will connect and log `[Database] Connected to PostgreSQL.`

Connection string (local): `Host=localhost;Port=5432;Database=kaligo_dev;Username=kaligo;Password=localdev`

## Status

**Currently in:** Phase 5 — Progression (not started).

### Sitting-level progress

- [x] **Phase 0 — Foundations.** Unity Hub, Unity 6 LTS, URP project, Git + LFS, `HelloKaligo.cs` running.
- [x] **Phase 1 — Third-Person Character Controller.** Shipped — character runs around an empty plane and feels good.
    - [x] **A — Asset import.** Mixamo X Bot + Sword & Shield animation pack (52 FBXs) under `Assets/Characters/XBot/` and `Assets/Animations/SwordAndShield/{Locomotion,Stance,Combat,Crouch,WeaponSwap,Death}/`. Rigs configured as Humanoid; animation rigs use Copy From Other Avatar (X Bot Avatar). All 51 clips renamed from `"mixamo.com"` to their FBX filename via `ModelImporter.clipAnimations`.
    - [x] **B — Animator Controller.** `Assets/Animations/Controllers/XBotAnimator.controller`. Upgraded in Phase 3 to a **9-clip `FreeformDirectional2D` 2D blend tree** (`VelocityX` / `VelocityZ` params) covering forward walk/run, backward walk/run, and left/right strafe walk/run. Combat states added: `LightAttack1/2/3`, `HeavyAttack`, `BlockIdle`, `Dodge`, `Jump`.
    - [x] **C — PlayerController.** `Assets/Scripts/PlayerController.cs` — WASD + camera-relative direction, sprint (Shift), smooth turning. Feeds `VelocityX`/`VelocityZ` to animator. Exposes `ApplyJumpImpulse`, `ApplyDodgeImpulse`, `MovementMultiplier` (consumed by skill system). `OnAnimatorMove` applies root-motion rotation during attacks only — this is what makes spinning slash animations match their preview.
    - [x] **D — Cinemachine third-person camera.** `CameraOrbitInput.cs` writes world-rotation to a `CameraTarget` child. `CinemachineThirdPersonFollow` tracks it. Free-aim only; lock-on in Phase 3E.
    - [x] **E — Feel tuning.** Acceleration, turn speed, sprint, mouse sensitivity, camera distance hand-tuned.
- [x] **Phase 2 — A World to Walk In.** *(Marked complete in roadmap prior to this session.)*
- [x] **Phase 3 — Combat Foundations.** Shipped.
    - [x] **3A — Skill system core.** Every action is a `SkillData` ScriptableObject with ordered `SkillStep[]`. Each step has an animator trigger, duration, combo-window, and a `[SerializeReference] List<SkillEffect>`. `SkillBar` maps input bindings (LMB/RMB/Space/1–5) to skills; `SkillExecutor` runs steps, manages combo chaining, `cancelLockDuration` interruption, `movementMultiplier`, `lockRotation`, and camera-facing snap at attack start. Default layout: LMB = SwordCombo (3-step), RMB = Block (hold), Space = Jump, Key1 = Dodge.
    - [x] **3A — Hitbox + Health.** `HitboxController` on weapon bone (right hand): trigger-based, per-swing hit-set prevents multi-tick. `HealthSystem` on any damageable object: events for UI and death. Block reduces damage via `SetBlockReduction`.
    - [x] **3B — Block / Jump / Dodge + Stamina.** `BlockEffect` sets `IsBlocking` animator bool and zeroes movement; `JumpEffect` calls `ApplyJumpImpulse`; `DodgeEffect` calls `ApplyDodgeImpulse` + i-frames. `StaminaSystem`: spend on activation, regen with configurable delay. Custom mapping API: `skillBar.AssignSkill / SwapSkills / GetSkill`.
    - [x] **3C — Enemy AI.** `EnemyAI` three-state machine (Idle → Chase → Attacking → Dead) on a red-tinted X Bot duplicate. Detects player by tag, walks toward them, attacks with a 2.33 s telegraphed swing (damage fires at 45% — dodge window). Respects player i-frames. `EnemyAnimator.controller` with Idle, Walk, Attack, HitReact, Death states. `EnemyHealthBar` world-space billboard canvas above the enemy's head, updates via `HealthSystem.OnHealthChanged`.
    - [x] **3D — Player UI.** HP bar + stamina bar + mana bar wired to `HealthSystem`, `StaminaSystem`, `ManaSystem` events.
    - [x] **3E — Lock-on targeting.** Tab to toggle; second Cinemachine vcam; strafe-relative movement while locked.
    - [x] **3F — Game-feel pass.** Hit-stop (~80 ms), Cinemachine Impulse screen shake, hit particles + SFX, knockback.
- [x] **Phase 4 — Combat Depth.** Shipped.
    - [x] **4A — ManaSystem.** 100 mana, 5/s regen after 2s delay, `manaCost` on `SkillData`, per-skill independent cooldown tracking, mana bar in HUD.
    - [x] **4B — New skill effects.** `DashStrikeEffect` (gap-closer), `AOESwingEffect` (radius damage + knockback), `DefensiveBuffEffect` (timed DR), `HealEffect` (HP restore + VFX).
    - [x] **4C — Skill hotbar UI.** 4 active slots (Key1–4), radial cooldown overlay, key hint labels, mana cost display.
    - [x] **4D — 4 active skills.** Dash Strike (20mp, 6s), Whirlwind AOE (35mp, 10s), Iron Skin buff (30mp, 14s), Quick Mend heal (45mp, 18s).
    - [x] **4E — ShieldedEnemy.** 75% DR always-on; breaks on heavy hit for a 3s stagger window.

### Picking up in a new session

Read this Status section. Run `docker-compose up -d` before entering Play mode (database must be running). The next sitting is **Phase 5 — Progression**: pick a progression model (see `VISION.md §7`), implement XP gain from enemy deaths via `Services.Progression.GrantXP(amount)`, and design the leveling curve in `Assets/Services/Local/XPTable.cs`.

Key architecture decisions already locked in:
- **Everything is a skill.** Attacks, block, jump, dodge are all `SkillData` assets. New abilities = new ScriptableObjects dropped into `Assets/Data/Skills/`.
- **Root motion rotation during attacks only.** `PlayerController.OnAnimatorMove` applies `animator.deltaRotation` when `SkillExecutor.LockRotation` is true. Character snaps to camera-forward at attack start, then root motion plays the animation's baked spin naturally.
- **2D locomotion blend.** `VelocityX` / `VelocityZ` normalized to `runSpeed`. `FreeformDirectional2D` handles walk/run in the same direction correctly (unlike `SimpleDirectional2D`).
- **Service layer + PostgreSQL.** All game-state mutations go through `Services.Progression`, `Services.Inventory`, `Services.Quest`. Backed by local PostgreSQL (Docker) in Act I; swapped for networked implementations in Act II with a one-line change in `Bootstrap.cs`. See `VISION.md §3a`.

Open design questions still parked in `VISION.md` §7:
- Open world vs. zoned regions?
- Progression model? *(Phase 5 — decide before implementing XP)*

## How we're working

This project is being built with AI assistance (Claude Code + Unity MCP). Conventions:

- **Operating mode: hybrid + automated setup.** Code and configs written via file tools. Unity-Editor wiring automated via Editor scripts or `script-execute` MCP calls. Visual judgment, level layout, and "feel" tuning stay manual.
- **Editor scripts in place:**
    - `Assets/Editor/FbxImportSettings.cs` — auto-configures Mixamo FBXs on import.
    - `Assets/Editor/SittingCSetup.cs` — wires PlayerController scene setup.
    - `Assets/Editor/SittingDSetup.cs` — wires Cinemachine rig.
- **Gameplay scripts:**
    - `Assets/Characters/Player/PlayerController.cs` — movement, root-motion hook, jump/dodge impulses.
    - `Assets/Characters/Player/CameraOrbitInput.cs` — mouse → CameraTarget rotation.
    - `Assets/Skills/_Core/` — `SkillData`, `SkillStep`, `SkillEffect`, `SkillBar`, `SkillExecutor`, `InputBinding`, `SkillSlot`.
    - `Assets/Combat/` — `HealthSystem`, `HitboxController`, `StaminaSystem`, `ManaSystem`, `EnemyAI`, `EnemyHealthBar`, `CombatFeedback`, `Targeting`.
    - `Assets/Database/` — `DatabaseService`, schema row types (`CharacterRow`, `InventoryItemRow`, `QuestFlagRow`, `SkillCooldownRow`).
    - `Assets/Services/` — `IProgressionService`, `IInventoryService`, `IQuestService`, `Services` locator.
    - `Assets/Services/Local/` — `LocalProgressionService`, `LocalInventoryService`, `LocalQuestService`, `XPTable`.
    - `Assets/Services/Networked/` — stubs for Act II implementations.
    - `Assets/Bootstrap.cs` — connects to PostgreSQL and initializes all services on startup.
- **Code style:** `Kaligo` namespace for gameplay, `Kaligo.Skills` and `Kaligo.Combat` sub-namespaces. `[SerializeField]` + tooltips on all tunables. `[SerializeReference]` for polymorphic effect lists. Comments explain *why*, not *what*.
