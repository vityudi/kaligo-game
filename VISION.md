# Game Vision — Kaligo

> A living brainstorm document. We'll keep filling this in as the concept sharpens.
> Last updated: 2026-05-08

---

## 0. The Name

**Kaligo.** Two syllables, *ka-LI-go*, identical in English and Brazilian Portuguese.

The word comes from Latin **caligo** — fog, mist, darkness, dim sight, obscurity of vision. Same root the Romans used for the haze of dawn over a battlefield and the dimness of failing eyes. *Caligo* also names a real-world genus of butterflies — the **owl butterflies** — large, dusky-winged creatures with eyespots on their wings that watch you from the underbrush. The name brings fog, shadow, and a quiet predator with it for free.

We chose the K-spelling deliberately. There is already a 2017 atmospheric horror walking simulator on Steam called *Caligo* (Krealit, 979+ reviews, "Mostly Positive"). Same vibe, same name — a collision we'd lose. Switching to **Kaligo** sidesteps that conflict entirely (no game on Steam or itch.io carries this spelling), reads more like a fantasy-game brand (the K is sharper, more proprietary), and is genuinely available for trademark in IC 009 and IC 041. The other Kaligos in the world — a hotel-booking platform and a French handwriting ed-tech app — are in unrelated industries.

The name also fits the design without explaining it. Caligo is what dims the world the player walks through; it is what sits between the player and what's been forgotten. The combat is sharp and clear; the world around it is in fog. That's the game.

*Domain notes: `kaligo.com` belongs to the hotel platform. We'll launch on `kaligo.gg` or `playkaligo.com` and own the social handles early.*

---

## 1. What We Know So Far

| Decision | Choice | Notes |
|---|---|---|
| Platform | Desktop (Windows/Mac/Linux) | Native build |
| Genre | 3D fantasy MMORPG | Albion Online, RuneScape as reference points |
| Art style | Low-poly, stylized (non-realistic) | Closer to *A Short Hike*, *RuneScape 3*, or stylized-low-poly Zelda — readable shapes, low texture detail |
| Theme | Fantasy medieval | Knights, magic, ruins, woods, taverns |
| Combat | Real-time, action-based, **no auto-attack** | Every input is intentional — basic attacks, dodges, blocks, and spells are all "skills" |
| Camera | Third-person, over-the-shoulder | Cinemachine handles the heavy lifting; tunable distance/height/FOV |
| Targeting | **Hybrid** — free-aim default, optional tab lock-on | Elden Ring / New World model: hitbox-based combat as the foundation, lock-on as a layer on top |
| Scope | Learning-first, but with a long-haul ambition | Project sized to maximize what we learn while building toward the MMO vision |
| Developer background | Web dev (JS/TS, Python), no gamedev | Learning C# as part of the journey |

---

## 2. North Star vs. First Build

This is the most important section in the document. Read it carefully.

The **north star** is the full MMORPG: persistent world, hundreds of concurrent players, deep economy, guilds, full-loot or zone PvP, the works. That's the dream and we're not going to talk you out of it.

The **first build** is going to be a **single-player 3D action prototype with the same combat, world feel, and art style as the MMO**. Networking comes in stages, *after* the game is already fun for one person.

Why this staging is non-negotiable:

- The MMORPG is a server/networking/scale problem stacked on top of an action-RPG game. If the action-RPG underneath isn't fun for one player, no amount of multiplayer fixes it.
- Networking adds a multiplier of complexity to *every other system you build*. Building network code into systems that don't yet exist is the fastest known way to ship nothing.
- Every successful "indie MMO-ish" game (Valheim, Project Zomboid, Terraria, early Minecraft) was built this way — a great single-player game first, multiplayer layered on. Almost every "started as an MMO" project never ships.
- You will learn ten times faster on a single-player game where every bug is reproducible. Multiplayer bugs are non-deterministic and brutal to debug.

**The combat, art, world, and feel of the first build are not throwaway.** They are the actual MMO, played by one person. When networking goes in, what you've built carries forward.

---

## 3. Tech Stack

**Engine: [Unity](https://unity.com/) (LTS release)**, scripting in **C#**.

Why Unity is the right pick for this project:

- **3D-first.** Mature 3D rendering pipeline, lighting, post-processing, and physics that handle low-poly stylized rendering beautifully out of the box.
- **Cinemachine.** Unity's built-in camera system is best-in-class for third-person games. A polished third-person camera with collision avoidance, target framing, and smooth follow is configuration, not code.
- **Animation tooling.** Mecanim (Unity's animation state machine) plus humanoid rig retargeting means you can drop a Mixamo animation onto any humanoid character for free. This is a massive time-saver for solo dev.
- **The Asset Store ecosystem.** Synty Studios' POLYGON packs (Fantasy Kingdom, Knights, Dungeons) are the gold standard for low-poly fantasy and would otherwise take months to create. Quaternius and Kenney also publish free Unity-ready low-poly assets.
- **Networking story for Act II.** **Mirror** and **FishNet** — both free, open-source, MMO-grade — are mature, well-documented, and built specifically for the multiplayer use case. When we get to Phase 10+, we won't be reinventing wheels.
- **C# is a clean language to learn.** Coming from JS/TS the syntax will feel familiar; coming from Python you'll appreciate the static typing once the project gets big. Unity also has excellent debugging and profiling tools wired into the editor.
- **Tutorial volume.** Unity Learn is free, official, and structured. There are more MMORPG-specific tutorials, devlogs, and asset tutorials for Unity than for any other engine. When you get stuck (you will), you'll find someone who's been stuck on the same thing.
- **License.** Free for personal use up to $200k revenue/funding. Beyond that there's a per-seat fee, but we're nowhere near that and the licensing controversy of 2023 was reversed in 2024.

**Trade-off acknowledged:** Unity is heavier than the lightest engines and the editor takes a few weeks to feel comfortable. We accept that overhead because the long-term path (3D + MMO networking + asset ecosystem) is so much smoother.

**Tooling we'll use:**

- **Unity Hub** — manages Unity installs and projects.
- **Unity Editor (LTS)** — current LTS at project start.
- **Visual Studio** (Windows) or **JetBrains Rider** (Mac/Linux/Windows, paid but free for indies) for C# editing.
- **Cinemachine** + **Input System** + **TextMesh Pro** — installed via Package Manager.
- **Mirror** or **FishNet** (decision deferred to Phase 10) — networking library for Act II.
- **Blender** — low-poly modeling when we outgrow asset packs.
- **Mixamo** — free humanoid animations to drop on our characters.
- **Synty / Quaternius / Kenney** — placeholder and possibly final low-poly art.
- **Git** + **Git LFS** — version control. LFS is non-negotiable for a Unity project.

---

## 4. Design Pillars (to refine)

Pillars are the 3–4 sentences that, when you read them, immediately tell you what kind of game this is. Every later decision gets checked against these.

1. **Skill, not stats.** The better player wins. No auto-attack, no AFK grinding, no "my number is bigger than yours." Every input is intentional.
2. **Readable through silhouette.** Low-poly stylized art means clarity. You should know what something is from a hundred meters out.
3. **A world worth living in.** Even before there are other players, the world should feel inhabited, mysterious, and worth wandering. Atmosphere over content volume.
4. **Open hands.** The game tells you "you can do this" rarely; it shows you. Mechanics emerge from the world, not from tutorials.

*Sharpen, strike, or rewrite these as the concept evolves.*

---

## 5. Combat — The Heart of the Game

This is your strongest commitment so far, so it gets its own section.

**Core principle:** Every action is a deliberate input. There is no auto-attack. Standing still and pressing one button is not a viable strategy.

**Working model (to refine):**

- **Light attack** — fast, low damage, short cooldown. Basic skill 1.
- **Heavy attack** — slow, high damage, can be interrupted. Basic skill 2.
- **Dodge / roll** — i-frames during the animation, short stamina cost. Basic skill 3.
- **Block / parry** — directional, holds reduce stamina, perfect parry rewards big damage. Basic skill 4.
- **Class skills** — 4–6 active abilities tied to weapon/spec, on cooldowns. Skill bar.
- **Resource systems** — at least one (stamina). Possibly two (stamina + mana/focus).

**Targeting — hybrid model.** Free-aim is the default state. Every swing is a deliberate spatial choice; hits register against hitboxes in front of the player, not against a selected target. Players can press **Tab** to acquire a lock-on if there's a valid target in view — locked, the camera frames the target, movement becomes strafe-relative, and dodges are directional relative to the locked target. Press Tab again (or kill the target) to release.

Implications:

- Hit detection is fundamentally hitbox-based — there is no "current target" required for an attack to resolve.
- Lock-on is *only* a camera + movement layer. It does not change how damage is calculated.
- This means combat can be built free-aim-first (Phase 3), with lock-on added as a clean second layer once the basics feel good.

Reference points to study seriously: **Elden Ring's hybrid lock/free system** (closest match to our model), **Mortal Online 2's first-person combat**, **For Honor's directional system**, **Black Desert's action combat**, **New World's hybrid targeting**, **Soulslike combat in general**.

The goal is *reactive* combat. The player is constantly reading the enemy and choosing — not selecting from a menu, not waiting for a cooldown to be the only thing they can do.

**What to prototype first:** one-on-one melee combat against a single enemy, polished until it feels great. Everything else (multiplayer, classes, magic, ranged) layers onto a great melee feel.

---

## 6. World, Tone, Protagonist (placeholders)

| Question | Current answer | Status |
|---|---|---|
| Setting | Fantasy medieval | Provisional |
| Tone (cozy / eerie / heroic / melancholy / comedic) | TBD | Open |
| Who is the player character? | A wanderer in a world dimmed by *kaligo* — a fog of forgetting that hangs over the land | Open — the title now anchors this thread, but the specifics (named magic, lost name, etc.) are still up for grabs |
| What do they want? | TBD | Open |
| What's standing in their way? | TBD | Open |
| Is the world densely populated or eerily empty? | TBD | Open — has huge implications for content design |

**Worldbuilding hooks to consider** (pick one or two as anchors):

- A kingdom that fell, and the players are scavengers in its ruins.
- A "named" magic system where naming a thing gives power over it — and the player has no name.
- A continent only recently rediscovered; players are colonists/explorers.
- A pantheon of dead gods whose corpses are the dungeons.

---

## 7. Open Questions for Next Session

Pick a few to think on. We don't need every answer — just enough to commit to the **first prototype's specific shape**.

**Combat specifics:**

- ~~Third-person over-the-shoulder, isometric/top-down 3D, or first-person?~~ → **Third-person, over-the-shoulder.** *(Locked: 2026-05-08)*
- Stamina-based? Mana-based? Both?
- ~~Lock-on targeting (Zelda/Dark Souls) or free-aim (Elden Ring/Black Desert)?~~ → **Hybrid — free-aim default, Tab to lock on.** *(Locked: 2026-05-08. See §5 for the implementation model.)*

**The world:**

- Open world (one giant map) or zoned (RuneScape-style regions)? *(Recommendation: zoned — much easier to build incrementally and friendlier to MMO server design later.)*
- One biome to start (forest? ruins? plains?) or several?

**Progression:**

- Level + stats (RuneScape, traditional)?
- Skill-based — practice the thing to get better at the thing (RuneScape's other model, Mortal Online)?
- Gear-based — your power is what you wear (Albion)?
- Hybrid?

**Multiplayer flavor (for far-future planning):**

- PvE focus, PvP focus, or both?
- Full-loot (Albion-style), partial-loot, or no-loot PvP?
- Server model: shared persistent world (true MMO), private servers (Valheim-style), or both?

**Reference shelf:**

- Name 3 games whose *combat* you want yours to feel like.
- Name 3 games whose *world* you want yours to feel like.
- Name 1 thing from outside games (book, film, place) that should bleed into it.

---

## 8. Anti-Goals (what this game is *not*)

- Not realistic / AAA art — low-poly stylized only. No realism arms race.
- Not procedurally generated worlds — hand-crafted feel matters.
- Not a mobile port — desktop-native controls only.
- **Not an MMO before it's a fun single-player game.** The prime directive.
- Not pay-to-win or microtransactions. Ever.

---

## 9. How We'll Use This Document

- Update it whenever a major decision is made.
- Strike through (don't delete) ideas we're abandoning — the trail of crossed-out ideas is useful.
- When `ROADMAP.md` and `VISION.md` disagree, this one wins. The roadmap serves the vision, not the other way around.
