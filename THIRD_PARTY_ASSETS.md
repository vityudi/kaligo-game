# Third-Party Assets

These packages are **not committed to git** (large binaries, license restrictions).
Every developer must import them manually after cloning.

## Required Packs

| Pack | Source | Import path |
|------|--------|-------------|
| POLYGON Fantasy Kingdom v1.01 | [Unity Asset Store](https://assetstore.unity.com/packages/3d/environments/fantasy/polygon-fantasy-kingdom-low-poly-3d-art-by-synty-164532) | `Assets/PolygonFantasyKingdom/` |
| POLYGON Dungeons *(Goblin/Rat mobs)* | [Unity Asset Store](https://assetstore.unity.com/packages/3d/environments/dungeons/polygon-dungeons-low-poly-3d-art-by-synty-102677) | `Assets/PolygonDungeon/` |
| POLYGON Fantasy Hero Characters *(player model)* | [Unity Asset Store](https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/polygon-fantasy-hero-characters-low-poly-3d-art-by-synty-86193) | `Assets/PolygonFantasyHeroCharacters/` |
| POLYGON Animals *(needed for Deer/Wolf/Bear/Sheep/Chicken mobs)* | [Unity Asset Store](https://assetstore.unity.com/packages/3d/characters/animals/polygon-animals-low-poly-3d-art-by-synty-111395) | `Assets/PolygonAnimals/` |

## Import Instructions

1. Open Unity with this project.
2. Download the pack from the Unity Asset Store (or the URL above).
3. Import it into `Assets/ThirdParty/<PackName>/` — **do not move it outside that folder** or the `.gitignore` won't protect it.
4. The `Assets/ThirdParty/` folder is git-ignored so none of the binary files will be tracked.

## Why not Git LFS?

Git LFS is an option if the whole team needs the assets in version history (e.g. for CI builds).
For now we keep it simple: assets live outside git, documented here.
