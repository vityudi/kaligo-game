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
- **Networking (Act II):** Mirror or FishNet — decided in Phase 10
- **Version control:** Git + Git LFS

## Status

**Currently in:** Phase 3 — Combat Foundations (in progress, ~75% done).

### Sitting-level progress

- [x] **Phase 0 — Foundations.** Unity Hub, Unity 6 LTS, URP project, Git + LFS, `HelloKaligo.cs` running.
- [x] **Phase 1 — Third-Person Character Controller.** Shipped — character runs around an empty plane and feels good.
    - [x] **A — Asset import.** Mixamo X Bot + Sword & Shield animation pack (52 FBXs) under `Assets/Characters/XBot/` and `Assets/Animations/SwordAndShield/{Locomotion,Stance,Combat,Crouch,WeaponSwap,Death}/`. Rigs configured as Humanoid; animation rigs use Copy From Other Avatar (X Bot Avatar). All 51 clips renamed from `"mixamo.com"` to their FBX filename via `ModelImporter.clipAnimations`.
    - [x] **B — Animator Controller.** `Assets/Animations/Controllers/XBotAnimator.controller`. Upgraded in Phase 3 to a **9-clip `FreeformDirectional2D` 2D blend tree** (`VelocityX` / `VelocityZ` params) covering forward walk/run, backward walk/run, and left/right strafe walk/run. Combat states added: `LightAttack1/2/3`, `HeavyAttack`, `BlockIdle`, `Dodge`, `Jump`.
    - [x] **C — PlayerController.** `Assets/Scripts/PlayerController.cs` — WASD + camera-relative direction, sprint (Shift), smooth turning. Feeds `VelocityX`/`VelocityZ` to animator. Exposes `ApplyJumpImpulse`, `ApplyDodgeImpulse`, `MovementMultiplier` (consumed by skill system). `OnAnimatorMove` applies root-motion rotation during attacks only — this is what makes spinning slash animations match their preview.
    - [x] **D — Cinemachine third-person camera.** `CameraOrbitInput.cs` writes world-rotation to a `CameraTarget` child. `CinemachineThirdPersonFollow` tracks it. Free-aim only; lock-on in Phase 3E.
    - [x] **E — Feel tuning.** Acceleration, turn speed, sprint, mouse sensitivity, camera distance hand-tuned.
- [x] **Phase 2 — A World to Walk In.** *(Marked complete in roadmap prior to this session.)*
- [ ] **Phase 3 — Combat Foundations.** In progress.
    - [x] **3A — Skill system core.** Every action is a `SkillData` ScriptableObject with ordered `SkillStep[]`. Each step has an animator trigger, duration, combo-window, and a `[SerializeReference] List<SkillEffect>`. `SkillBar` maps input bindings (LMB/RMB/Space/1–5) to skills; `SkillExecutor` runs steps, manages combo chaining, `cancelLockDuration` interruption, `movementMultiplier`, `lockRotation`, and camera-facing snap at attack start. Default layout: LMB = SwordCombo (3-step), RMB = Block (hold), Space = Jump, Key1 = Dodge.
    - [x] **3A — Hitbox + Health.** `HitboxController` on weapon bone (right hand): trigger-based, per-swing hit-set prevents multi-tick. `HealthSystem` on any damageable object: events for UI and death. Block reduces damage via `SetBlockReduction`.
    - [x] **3B — Block / Jump / Dodge + Stamina.** `BlockEffect` sets `IsBlocking` animator bool and zeroes movement; `JumpEffect` calls `ApplyJumpImpulse`; `DodgeEffect` calls `ApplyDodgeImpulse` + i-frames. `StaminaSystem`: spend on activation, regen with configurable delay. Custom mapping API: `skillBar.AssignSkill / SwapSkills / GetSkill`.
    - [x] **3C — Enemy AI.** `EnemyAI` three-state machine (Idle → Chase → Attacking → Dead) on a red-tinted X Bot duplicate. Detects player by tag, walks toward them, attacks with a 2.33 s telegraphed swing (damage fires at 45% — dodge window). Respects player i-frames. `EnemyAnimator.controller` with Idle, Walk, Attack, HitReact, Death states. `EnemyHealthBar` world-space billboard canvas above the enemy's head, updates via `HealthSystem.OnHealthChanged`.
    - [ ] **3D — Player UI.** Player HP bar + stamina bar (HUD). *(next)*
    - [ ] **3E — Lock-on targeting.** Tab to toggle; second Cinemachine vcam; strafe-relative movement while locked.
    - [ ] **3F — Game-feel pass.** Hit-stop (~80 ms), Cinemachine Impulse screen shake, hit particles, hit SFX, knockback.

### Picking up in a new session

Read this Status section. The next sitting is **3D — Player UI**: a HUD canvas with an HP bar and stamina bar wired to `HealthSystem.OnHealthChanged` and `StaminaSystem.OnStaminaChanged`. After that, 3E (lock-on) and 3F (game feel).

Key architecture decisions already locked in:
- **Everything is a skill.** Attacks, block, jump, dodge are all `SkillData` assets. New abilities = new ScriptableObjects dropped into `Assets/Data/Skills/`.
- **Root motion rotation during attacks only.** `PlayerController.OnAnimatorMove` applies `animator.deltaRotation` when `SkillExecutor.LockRotation` is true. Character snaps to camera-forward at attack start, then root motion plays the animation's baked spin naturally.
- **2D locomotion blend.** `VelocityX` / `VelocityZ` normalized to `runSpeed`. `FreeformDirectional2D` handles walk/run in the same direction correctly (unlike `SimpleDirectional2D`).

Open design questions still parked in `VISION.md` §7:
- Stamina-based, mana-based, or both? *(stamina exists; mana TBD in Phase 4)*
- Open world vs. zoned regions?
- Progression model?

## How we're working

This project is being built with AI assistance (Claude Code + Unity MCP). Conventions:

- **Operating mode: hybrid + automated setup.** Code and configs written via file tools. Unity-Editor wiring automated via Editor scripts or `script-execute` MCP calls. Visual judgment, level layout, and "feel" tuning stay manual.
- **Editor scripts in place:**
    - `Assets/Editor/FbxImportSettings.cs` — auto-configures Mixamo FBXs on import.
    - `Assets/Editor/SittingCSetup.cs` — wires PlayerController scene setup.
    - `Assets/Editor/SittingDSetup.cs` — wires Cinemachine rig.
- **Gameplay scripts:**
    - `Assets/Scripts/PlayerController.cs` — movement, root-motion hook, jump/dodge impulses.
    - `Assets/Scripts/CameraOrbitInput.cs` — mouse → CameraTarget rotation.
    - `Assets/Scripts/Skills/` — `SkillData`, `SkillStep`, `SkillEffect`, `SkillBar`, `SkillExecutor`, `InputBinding`, `SkillSlot`; effects: `HitboxEffect`, `BlockEffect`, `DodgeEffect`, `JumpEffect`.
    - `Assets/Scripts/Combat/` — `HealthSystem`, `HitboxController`, `StaminaSystem`, `EnemyAI`, `EnemyHealthBar`.
    - `Assets/Data/Skills/` — `SwordCombo.asset`, `HeavySlash.asset`, `BlockSkill.asset`, `DodgeSkill.asset`, `JumpSkill.asset`.
- **Code style:** `Kaligo` namespace for gameplay, `Kaligo.Skills` and `Kaligo.Combat` sub-namespaces. `[SerializeField]` + tooltips on all tunables. `[SerializeReference]` for polymorphic effect lists. Comments explain *why*, not *what*.
