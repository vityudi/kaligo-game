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

**Currently in:** Phase 2 — A World to Walk In (next up).

### Sitting-level progress

- [x] **Phase 0 — Foundations.** Unity Hub, Unity 6 LTS, URP project, Git + LFS, `HelloKaligo.cs` running.
- [x] **Phase 1 — Third-Person Character Controller.** Shipped — character runs around an empty plane and feels good.
    - [x] **A — Asset import.** Mixamo X Bot + Sword & Shield animation pack (52 FBXs) under `Assets/Characters/XBot/` and `Assets/Animations/SwordAndShield/{Locomotion,Stance,Combat,Crouch,WeaponSwap,Death}/`. Rigs configured as Humanoid; animation rigs use Copy From Other Avatar (X Bot Avatar).
    - [x] **B — Animator Controller.** `Assets/Animations/Controllers/XBotAnimator.controller` with a 1D blend tree (`idle@0` ↔ `walk@2` ↔ `run@6`) driven by a `Speed` float parameter. Loop Time enabled on locomotion and stance clips.
    - [x] **C — PlayerController.** `Assets/Scripts/PlayerController.cs` — WASD movement, Left Shift sprint, camera-relative direction, smooth turning, Animator `Speed` driven by horizontal velocity. CharacterController for movement; Apply Root Motion disabled.
    - [x] **D — Cinemachine third-person camera.** Cinemachine 3.1 in `Packages/manifest.json`. `Assets/Scripts/CameraOrbitInput.cs` reads mouse delta (new Input System) and writes WORLD rotation to a `CameraTarget` child of the X Bot — world-rotation is critical because it breaks a feedback loop where parent (X Bot) rotation would otherwise cascade into CameraTarget and spin the character instead of moving it. `Assets/Editor/SittingDSetup.cs` wires the rig: `CinemachineBrain` on Main Camera, `Player Camera` GameObject with `CinemachineCamera` + `CinemachineThirdPersonFollow` tracking the CameraTarget; X Bot tagged `Player` so the rig's collision raycast ignores its own body. Free-aim only — lock-on lands in Phase 3 per VISION §5.
    - [x] **E — Feel tuning.** Hand-tuned acceleration, turn speed, sprint multiplier, mouse sensitivity, camera distance, shoulder offset against the inspector. Phase 1 exit criterion met. (The optional 7° shoulder-offset alignment fix in PlayerController was evaluated and skipped — drift wasn't perceptible in play. Re-open if Phase 3 combat surfaces it.)

### Picking up in a new session

Read this Status section, then open `ROADMAP.md` and skim **Phase 2 — A World to Walk In**. That's where the empty plane becomes a place: Synty / Quaternius / Kenney asset pack, ProBuilder blockout, baked GI, URP post-processing Volume (bloom, color grading, fog), ambient audio. It's a big visual jump for relatively little code. The Phase 1 character controller and camera carry forward unchanged.

Open design questions still parked in `VISION.md` §7 (worth chewing on between sessions):

- Stamina-based, mana-based, or both?
- Open world (one giant map) vs. zoned (RuneScape-style regions)?
- Progression model: level + stats / skill-based / gear-based / hybrid?

## How we're working

This project is being built with AI assistance. A few conventions that current and future sessions should respect:

- **Operating mode: hybrid + automated setup.** Code, configs, and folder structure are written directly via file tools. Repetitive Unity-Editor wiring (asset-import settings, scene component setup) is automated via Editor scripts under `Assets/Editor/`. Visual judgment, level layout, and "feel" tuning stay manual — the human plays the game and decides when it's right.
- **Editor scripts already in place:**
    - `Assets/Editor/FbxImportSettings.cs` — `AssetPostprocessor` that auto-configures Mixamo FBXs (humanoid rig, copy avatar from X Bot, loop time on Locomotion/Stance clips). Menu: `Kaligo → Reimport Animation Assets`.
    - `Assets/Editor/SittingCSetup.cs` — one-click scene wiring for Sitting C (PlayerController + CharacterController + ground + temporary static camera). Menu: `Kaligo → Setup → Sitting C - Wire PlayerController`.
    - `Assets/Editor/SittingDSetup.cs` — one-click scene wiring for Sitting D (CinemachineBrain on Main Camera, CameraTarget child on X Bot with `CameraOrbitInput`, Player Camera vcam with `CinemachineThirdPersonFollow`). Idempotent. Menu: `Kaligo → Setup → Sitting D - Wire Cinemachine Camera`.
- **Gameplay scripts in place:**
    - `Assets/Scripts/HelloKaligo.cs` — Phase 0 sanity check.
    - `Assets/Scripts/PlayerController.cs` — Sitting C; planar WASD movement with sprint and Animator drive. Reads `Camera.main` for camera-relative direction, so it works unchanged once Cinemachine takes over the Main Camera in Sitting D.
    - `Assets/Scripts/CameraOrbitInput.cs` — Sitting D; mouse → yaw/pitch on the CameraTarget transform. No Cinemachine dependency by design (camera state is debuggable as plain Transform euler angles).
- **Code style:** all gameplay code lives under the `Kaligo` namespace; editor tooling under `Kaligo.EditorTools`. SerializeField + tooltips on every tunable. Comments explain *why*, not *what*.
