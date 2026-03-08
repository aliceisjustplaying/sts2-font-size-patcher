# STS2 Runtime Mod Master Notes

Last updated: 2026-03-08

## Goal

Make Slay the Spire 2 text significantly larger on Steam Deck with a generic runtime mod that can live in the game's `mods/` folder.

## Repo Scope

This repo now keeps only the runtime-mod version:

- Harmony mod source
- mod `.pck` project files
- runtime-mod helper scripts
- readme / notes

The older direct-DLL patcher source has been archived outside the repo.

## Current Status

- The runtime mod loads through the `mods/` folder.
- The modded save namespace is now understood and documented.
- Current runtime-mod config target base scale factor is `1.20x`.
- Debug footer/version labels get a footer-only scale of `1.70x`.
- Patch notes body text gets a release-notes-only scale of `1.45x`.
- Preview-card description text gets a preview-only extra scale of `1.40x`.
- The original stubborn serif text was identified as the character description text:
  - `"The last soldier of the Ironclads."`
- Character-select starter-relic description text is now explicitly reapplied after `NCharacterSelectScreen.SelectCharacter(...)`.
- This shared character-select fix covers cases like:
  - `"At the start of each combat, draw 2 additional cards."`
- Additional probe strings that helped identify remaining paths were:
  - `"..arise.. my.. warrior..."`
  - `"Until next turn, prevents damage."`
  - `"Unlocked by"`

## Save Migration Findings

- The plain game save is still the authoritative non-modded profile.
- The `mods/` loader build uses a separate modded profile namespace.
- This namespace split is only applied to profile-scoped data, not account-scoped data.

Confirmed from managed save-path code:

- account-scoped root stays normal:
  - `user://steam/<steamid>/profile.save`
  - `user://steam/<steamid>/settings.save`
- profile-scoped root becomes modded when running mods:
  - `user://steam/<steamid>/modded/profile1/...`

In `UserDataPathProvider.GetProfileDir(int profileId)`:

- non-modded => `profile1`
- modded => `modded/profile1`

In `ProgressSaveManager.LoadProgress()`:

- the game only falls back to `ProgressState.CreateDefault()` when loading the modded `progress.save` fails or returns null
- it does not reject a modded save merely because it is in the modded namespace

Practical implication:

- if the modded `progress.save` is blank on launch, the real problem is usually sync/state mismatch, not the folder name itself

## Validated Save Migration Procedure

The migration that finally worked was:

1. Back up everything first.
2. Copy normal `profile1/` into both:
   - Steam cloud modded root
   - local synced modded root
3. Copy top-level `profile.save` and `settings.save` into the `modded/` subdirectory in both roots.
4. Verify hashes match before launching modded STS2.

Steam cloud root:

- `~/.local/share/Steam/userdata/58189749/2868840/remote/`

Local synced root:

- `~/.local/share/SlayTheSpire2/steam/76561198018455477/`

Safe migration commands:

```bash
remote_root="$HOME/.local/share/Steam/userdata/58189749/2868840/remote"
local_root="$HOME/.local/share/SlayTheSpire2/steam/76561198018455477"

stamp=$(date +%Y%m%d-%H%M%S)
backup_dir="$HOME/tmp/sts2-save-backups/modded-migrate-$stamp"

mkdir -p "$backup_dir"
cp -a "$remote_root/modded" "$backup_dir/remote-modded-before"
cp -a "$local_root/modded" "$backup_dir/local-modded-before"

mkdir -p "$remote_root/modded/profile1" "$local_root/modded/profile1"

rsync -a --delete "$remote_root/profile1/" "$remote_root/modded/profile1/"
rsync -a --delete "$remote_root/profile1/" "$local_root/modded/profile1/"

cp -a "$remote_root/profile.save" "$remote_root/profile.save.backup" "$remote_root/settings.save" "$remote_root/settings.save.backup" "$remote_root/modded/"
cp -a "$local_root/profile.save" "$local_root/profile.save.backup" "$local_root/settings.save" "$local_root/settings.save.backup" "$local_root/modded/"
```

Hash verification used after migration:

```bash
python3 - <<'PY'
from pathlib import Path
import hashlib

checks = [
    ("remote progress", Path.home()/".local/share/Steam/userdata/58189749/2868840/remote/profile1/saves/progress.save",
     Path.home()/".local/share/Steam/userdata/58189749/2868840/remote/modded/profile1/saves/progress.save"),
    ("local modded progress", Path.home()/".local/share/Steam/userdata/58189749/2868840/remote/profile1/saves/progress.save",
     Path.home()/".local/share/SlayTheSpire2/steam/76561198018455477/modded/profile1/saves/progress.save"),
]

for label, a, b in checks:
    ah = hashlib.sha256(a.read_bytes()).hexdigest()[:16]
    bh = hashlib.sha256(b.read_bytes()).hexdigest()[:16]
    print(label, ah == bh, a.stat().st_size, b.stat().st_size, ah, bh)
PY
```

Observed good state after fix:

- normal `progress.save`: `90063` bytes
- modded remote `progress.save`: `90063` bytes
- modded local `progress.save`: `90063` bytes

Why the earlier migration failed:

- only part of the modded namespace was copied at first
- the local modded profile tree was left inconsistent
- on the next modded launch, STS2 wrote a fresh blank modded `progress.save`

## Deployment Safety

- Before copying mod files to the Steam Deck, first check whether STS2 is currently running.
- If the game is running, stop and ask the user before overwriting the live mod files.
- If the game is not running, it is fine to copy directly.

Suggested check:

```bash
./scripts/check-running.sh
```

## Key Paths

### Repo

- Working folder: `./`
- Runtime mod source: `./runtime_mod/Sts2FontSizeMod/`
- Mod pack files: `./runtime_mod/pck/`
- Mod tools: `./runtime_mod/tools/`
- Environment example: `./.env.example`
- Helper scripts: `./scripts/`
- Generated runtime config after build: `./runtime_mod/build/font_size_config.json`

### Steam Deck

- Put host/path values in `.env`, not in tracked files.
- Recommended variables:
  - `STS2_DECK_HOST`
  - `STS2_MODS_DIR`
  - `STS2_LOG_PATH`
  - `STS2_GODOT_EXPORTER`
  - `STS2_PATCH_SCALE`
  - `STS2_DEBUG_FOOTER_EXTRA_SCALE`
  - `STS2_PATCH_NOTES_EXTRA_SCALE`
  - `STS2_PREVIEW_CARD_DESCRIPTION_EXTRA_SCALE`

## Game Architecture Findings

- Engine: Godot 4.5.1 Mono / C#
- App ID: `2868840`
- Main managed gameplay assembly: `sts2.dll`
- Godot managed runtime assembly: `GodotSharp.dll`
- The `.pck` is encrypted, so direct scene/theme/resource editing through asset extraction is not a practical path right now.

## Investigation History

### Early Direct-DLL Phase

- The project started as a direct patch of `sts2.dll`, then briefly also `GodotSharp.dll`.
- That approach proved the font-scaling concept, but it was a poor sharing format because it replaced core game DLLs directly.
- The old direct-DLL source was intentionally removed from the public repo after the runtime-mod port became viable.

### Failed Global Godot Helper Injection

- An early attempt patched `GodotSharp.dll` to call into a separate helper assembly.
- That failed at runtime because Godot's C# startup did not load the extra helper assembly reliably.
- Result: `FileNotFoundException` spam and unstable menus.
- Lesson: any engine-level patch must be self-contained, or better, moved into the runtime-mod/Harmony path.

### Why The Runtime Mod Became The Main Path

- The `mods/` loader path avoids replacing stock game DLLs.
- It is easier to share, easier to tweak, and safer to revert.
- The price is that modded runs use a separate save namespace, which had to be understood and documented.

### Save Namespace Learning

- The mod loader does not wipe the normal save.
- Instead, it switches profile-scoped data into `modded/profile1/...` while leaving account-scoped files at the normal root.
- The first failed migrations happened because only part of that modded namespace was copied.
- The durable lesson is: copy both the cloud and local modded profile trees, plus the top-level `profile.save` and `settings.save`.

### Autosizing Was The Real "Why Is This One Still Tiny?" Pattern

- Most of the remaining stubborn misses were not separate font systems.
- They were usually `MegaLabel` / `MegaRichTextLabel` nodes that still used autosizing.
- Scaling only the base font size was not enough for longer text, because autosizing could shrink it back toward the original bounds.
- The generic fix was to scale autosize bounds as well, not just the visible base size.

### Not Every Miss Was The Same UI Path

- The character-select stubborn serif text was a real targeted path and helped identify where generic scaling was missing.
- Character-select starter-relic text turned out to be a sibling shared label path, not the main bio label.
- Timeline unlock / inspect text turned out to involve dedicated timeline screens and a mix of text node types.
- Secondary preview-card text needed its own extra bump because the desired result was different from the main card presentation.
- Lesson: keep the base patch generic, but allow a small number of explicit extras for UX-sensitive screens.

### Rejected Paths

- Global display scaling via `override.cfg`
  - too blunt, affected the wrong things, and was not the right solution
- Relying on encrypted asset extraction
  - not practical because the game `.pck` is encrypted
- Shipping the direct-DLL patcher as the final public solution
  - workable locally, but a worse distribution format than the runtime mod

### Documentation Intent

- `README.md` is intentionally short and task-oriented.
- `GUIDE.md` is for user-facing detail like save migration and troubleshooting.
- This file keeps the higher-level technical lessons and rejected paths so the reasoning is not lost.
