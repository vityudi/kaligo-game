# Kaligo

A 3D fantasy MMORPG with skill-based real-time combat, built solo from scratch in Unity.

> *Kaligo* — from Latin *caligo* meaning fog, darkness, dim sight. The K-spelling makes the name ours: distinct from the 2017 Steam title *Caligo*, equally readable in English and Portuguese, and visually a touch more striking. See `VISION.md` §1 for the full naming rationale.

## Project Documents

- [`VISION.md`](./VISION.md) — what the game is, what it isn't, design pillars, open questions.
- [`ROADMAP.md`](./ROADMAP.md) — staged milestones from "install Unity" to "persistent MMO." Read VISION first.

## Repository Structure

```
kaligo/
├── README.md            ← you are here
├── VISION.md            ← the design north star
├── ROADMAP.md           ← phased milestones (Act I single-player → Act II MMO)
├── .gitignore           ← Unity-aware ignores
├── .gitattributes       ← Git LFS rules for binary assets
└── Kaligo/              ← Unity project (created in Phase 0)
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

Currently in **Phase 0 — Foundations**. See `ROADMAP.md` for the tracker.
