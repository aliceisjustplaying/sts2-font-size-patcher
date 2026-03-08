# STS2 Font Mod Master Notes

Last updated: 2026-03-08

## Goal

Make Slay the Spire 2 text significantly larger on Steam Deck with a generic patch that can be shared with other players, not a pile of one-off screen hacks.

## Current Status

- The game currently launches.
- Character select currently works.
- The patch is now mostly generic, not primarily screen-specific.
- Current deployed base scale factor is `1.20x`.
- Debug footer/version labels get a footer-only scale of `1.70x`.
- Patch notes body text gets a release-notes-only scale of `1.45x`.
- Patched `sts2.dll` and `GodotSharp.dll` were rebuilt on 2026-03-08 and copied to the Deck.
- The earlier character-select-only fix has been replaced by a broader `MegaLabel` / `MegaRichTextLabel` `_Ready()` patch.
- The debug footer/version display is now patched to show:
  - `[version + Font Patch 1.20x] [date]`
- The original stubborn serif text was identified as the character description text, including:
  - `"The last soldier of the Ironclads."`
- Additional remaining misses that revealed the pattern included:
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

- Before copying patched DLLs to the Steam Deck, first check whether STS2 is currently running.
- If the game is running, stop and ask the user before overwriting the live DLLs.
- If the game is not running, it is fine to copy directly.
- Suggested check:

```bash
./scripts/check-running.sh
```

## Key Paths

### Repo

- Working folder: `./`
- Local game DLL copies: `./game_dlls/`
- Patcher project: `./patcher/StsFontPatcher/`
- Main patcher source: `./patcher/StsFontPatcher/Program.cs`
- IL dump helper: `./findlabels/FL/`
- Environment example: `./.env.example`
- Helper scripts: `./scripts/`

### Steam Deck

- Put host/path values in `.env`, not in tracked files.
- Recommended variables:
  - `STS2_DECK_HOST`
  - `STS2_GAME_DLL_DIR`
  - `STS2_LOG_PATH`
  - `STS2_LOCAL_DLL_DIR`
  - `STS2_PATCH_SCALE`
  - `STS2_DEBUG_FOOTER_EXTRA_SCALE`
  - `STS2_PATCH_NOTES_EXTRA_SCALE`

## Game Architecture Findings

- Engine: Godot 4.5.1 Mono / C#
- App ID: `2868840`
- Main managed gameplay assembly: `sts2.dll`
- Godot managed runtime assembly: `GodotSharp.dll`
- The `.pck` is encrypted, so direct scene/theme/resource editing through asset extraction is not a practical path right now.

## Text Rendering Findings

The game uses several distinct text paths:

- `MegaCrit.Sts2.addons.mega_text.MegaLabel`
  - extends `Godot.Label`
  - has auto-sizing logic
  - has `SetTextAutoSize(...)`
  - has `AdjustFontSize()`
  - has `SetFontSize(int)`

- `MegaCrit.Sts2.addons.mega_text.MegaRichTextLabel`
  - extends `Godot.RichTextLabel`
  - also has auto-sizing logic
  - has `SetTextAutoSize(...)`
  - has deferred `AdjustFontSize()`
  - has `SetFontSize(int)`
  - can still shrink text to fit bounds even after other font scaling attempts

- `MegaCrit.Sts2.Core.Localization.LocTextLabel`
  - extends `RichTextLabel` directly
  - not the main culprit for the stubborn character-select serif text

- Plain `Godot.Label`
  - often scene/theme sized
  - may use `LabelSettings`

- Plain `Godot.RichTextLabel`
  - may use theme font sizes
  - may also use inline BBCode font sizing

## Exact Stubborn Text Investigation

The useful clue was the text:

- `"The last soldier of the Ironclads."`

That led to the following confirmed path:

- owning screen: `MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectScreen`
- field: `_description`
- node path: `InfoPanel/VBoxContainer/DescriptionLabel`
- type: `MegaRichTextLabel`

Relevant decompile finding:

- `NCharacterSelectScreen.SelectCharacter(...)` sets:
  - `_description.Text = new LocString("characters", characterModel.CharacterSelectDesc).GetFormattedText();`

So the stubborn serif text is the character bio/description label on character select, not a random unrelated UI path.

## Release Notes Text Investigation

The patch notes / release notes screen uses:

- screen: `MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NPatchNotesScreen`
- body field: `_patchText`
- body type: `MegaRichTextLabel`
- date field: `_dateLabel`
- date type: `MegaLabel`

Relevant managed behavior:

- `CreateNewPatchEntry(...)` binds `_patchText` from `Content/PatchText`
- `LoadPatchNoteText(...)` reads the patch note file and calls `_patchText.SetTextAutoSize(...)`
- `UpdateDateLabel(...)` sets `_dateLabel.SetTextAutoSize(...)`

Conclusion:

- yes, release notes were already on the generic scaled `MegaRichTextLabel` path
- an additional release-notes-only bump can therefore be applied generically at the screen level without chasing strings

## Important Decompiled Behavior

### `MegaRichTextLabel`

- `set_Text(...)` routes to `SetTextAutoSize(...)`
- `SetTextAutoSize(...)`:
  - sets text
  - installs rich text effects
  - if auto-size is enabled, sets `_needsResize = true`
  - defers `AdjustFontSize()`

- `AdjustFontSize()`:
  - measures text against the control bounds
  - binary-searches between min/max font size
  - can reduce the text back down to fit the panel

This explains why broad font patches changed some text but still failed to move the stubborn character-select bio in a satisfying way until the `MegaRichTextLabel` path itself was handled more directly.

## What Was Tried

### 1. `override.cfg`

Path:

- `/home/deck/.steam/steam/steamapps/common/Slay the Spire 2/override.cfg`

Attempted:

- `[display] window/stretch/scale=...`
- `[gui] theme/default_font_size=...`

Results:

- stretch scaling affected everything, not just text
- user did not want that path
- config was removed

### 2. Patch 1: Scale `SetFontSize(...)` in Mega labels

Implemented in `sts2.dll`:

- `MegaLabel.SetFontSize(int)`
- `MegaRichTextLabel.SetFontSize(int)`

Behavior:

- injected multiplier at method entry
- scale factor is controlled by the patcher argument and is currently deployed at `1.25x`

Result:

- worked for a lot of the already-auto-sized sans-serif text
- did not fully solve the stubborn serif text

### 3. Patch 2: Global node-based font scaler in `NGame`

Implemented in `sts2.dll`:

- hooks `SceneTree.NodeAdded` from `NGame._Ready`
- recursively scans existing subtree at startup

Behavior:

- scales plain `RichTextLabel` theme sizes
- scales plain `Label` theme size
- duplicates/scales `LabelSettings` font size, outline size, and shadow size
- intentionally skips `MegaLabel` / `MegaRichTextLabel` in the general scan to avoid bad double-scaling with Patch 1

Result:

- useful for plain Godot labels
- intentionally still skips `MegaLabel` / `MegaRichTextLabel`, because those are now handled by Patch 1B

### 4. Patch 3: Global `GodotSharp.dll` rich-text patch

Implemented in `GodotSharp.dll` directly:

- patched `Godot.RichTextLabel`
- no external helper DLL dependency in the final form

Injected helpers:

- `__StsScaleInt(int)`
- `__StsScaleMatch(Match)`
- `__StsScaleBbcode(string)`

Patched methods:

- `SetText(string)`
- `ParseBbcode(string)`
- `AppendText(string)`
- `PushFont(Font, int)`
- `PushFontSize(int)`
- `PushOutlineSize(int)`
- `PushDropcap(... size ..., ... outlineSize ...)`

Scaled inline BBCode patterns:

- `font_size=...`
- `outline_size=...`

Important failure that already happened and was fixed:

- the earlier helper-DLL approach caused `FileNotFoundException` for `StsFontRuntimeHelper`
- that build was removed
- the current Godot patch is self-contained

### 5. Investigation pattern: the remaining misses clustered on `MegaRichTextLabel`

Probe strings that were still too small:

- `"..arise.. my.. warrior..."`
- `"Until next turn, prevents damage."`
- `"Unlocked by"`

What was found:

- the literal strings were not present in accessible loose files or managed DLL string tables
- they most likely come from encrypted content, localization, or scene data
- the common rendering pattern was not the string data itself but the UI control type
- `label_callsites.txt` showed `87` distinct types using `MegaRichTextLabel`
- likely examples included:
  - hover tips
  - unlock / achievement UI
  - speech bubbles
  - thought bubbles

Conclusion:

- the right fix was to make `MegaRichTextLabel` scaling generic instead of continuing to chase individual screens

### 7. Patch 1D: Generic release-notes body bump on `NPatchNotesScreen`

Implemented in `sts2.dll`:

- `MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NPatchNotesScreen::_ApplyPatchNotesFont(...)`
- call injected into `CreateNewPatchEntry(...)`

Behavior:

- applies a release-notes-only rich-text size override to `_patchText`
- scales:
  - `normal_font_size`
  - `bold_font_size`
  - `italics_font_size`
  - `bold_italics_font_size`
  - `mono_font_size`
- uses `base scale + STS2_PATCH_NOTES_EXTRA_SCALE`

Result:

- release notes text keeps the generic path
- the extra bump is configurable in `.env`
- no string-specific patching is needed

### 6. Patch 1B: Generic `_Ready()` scaling for `MegaLabel` and `MegaRichTextLabel`

Implemented in `sts2.dll`:

- `MegaLabel._Ready()`
- `MegaRichTextLabel._Ready()`

Behavior:

- before each class calls `AdjustFontSize()`, inject scaled base theme overrides
- `MegaLabel._Ready()` now applies:
  - `font_size`
- `MegaRichTextLabel._Ready()` now applies:
  - `normal_font_size`
  - `bold_font_size`
  - `italics_font_size`
  - `bold_italics_font_size`
  - `mono_font_size`

Why it matters:

- this is a generic patch at the widget-class level
- it catches many UI paths that were previously unaffected even though they were all `MegaRichTextLabel` based
- it replaced the need for the old one-off `NCharacterSelectScreen` helper

### 7. Patch 1C: Debug footer/version label formatting

Implemented in `sts2.dll`:

- `MegaCrit.Sts2.Core.Nodes.Debug.NDebugInfoLabelManager::UpdateText`

Behavior:

- routes the footer text through `MegaLabel.SetTextAutoSize(...)` instead of plain `Label.set_Text(...)`
- formats the first line as:
  - `[version + Font Patch 1.20x] [date]`
- applies a footer-only font bump to the release/build labels

Why it matters:

- the main-menu / debug footer now reflects the installed font patch build
- it also uses the same scaled text path as the rest of the patch

## Critical Failures We Hit

### A. Helper-DLL engine patch broke the main menu

Actual log error:

- `FileNotFoundException` for `StsFontRuntimeHelper`

Cause:

- `GodotSharp.dll` was patched to call code from an extra helper assembly
- Godot did not reliably load that helper assembly at startup

Fix:

- removed the helper DLL approach entirely
- rewrote the `GodotSharp.dll` patch to be self-contained

### B. First targeted character-select patch broke the screen

Actual log error:

- `System.MethodAccessException`

Cause:

- `NCharacterSelectScreen._ConfigureDescriptionFont(...)` tried to call non-public `MegaRichTextLabel.SetFontSize(Int32)` directly

Impact:

- `_Ready()` failed
- character-select state partially initialized
- then `_Process(...)` and `InitializeSingleplayer()` produced cascading `NullReferenceException`s
- user could not reach the character select screen

Fix:

- rewrote the patch to avoid calling `MegaRichTextLabel.SetFontSize(...)`
- replaced it with public `Godot.Control.AddThemeFontSizeOverride(...)`
- later replaced the targeted helper entirely with the generic Patch 1B approach
- rebuilt from clean `.bak` DLLs
- redeployed corrected DLLs

User confirmed:

- the corrected build restored access to the character select screen
- later generic builds made the text sizing work in many more places

## Current Deployed Shape

Currently intended on Deck:

- patched `sts2.dll`
- patched `GodotSharp.dll`
- no helper DLL
- no `override.cfg`

Latest intended patch stack:

- Patch 1: Mega label `SetFontSize` multiplier
- Patch 1B: generic `MegaLabel` / `MegaRichTextLabel` `_Ready()` base-size overrides
- Patch 2: `NGame` global node scan / node-added scaler for plain Godot labels
- Patch 3: self-contained `GodotSharp.dll` rich-text scaling

## Useful Commands

### Rebuild / repatch locally

```bash
./scripts/rebuild.sh
```

Current deployed example:

```bash
./scripts/rebuild.sh
```

### Deploy to Steam Deck

```bash
./scripts/deploy.sh
```

### Fetch Deck log locally

```bash
./scripts/fetch-log.sh
```

### Quick log triage

```bash
rg -n "Exception|ERROR|MethodAccess|NullReference|CharacterSelect|DescriptionLabel" ./deck_godot.log
```

### Inspect patched assemblies

```bash
dotnet run --project ./findlabels/FL/FL.csproj -- ./game_dlls/sts2.dll
```

## Files of Interest

- Patcher source:
  - `./patcher/StsFontPatcher/Program.cs`

- Original backups:
  - `./game_dlls/sts2.dll.bak`
  - `./game_dlls/GodotSharp.dll.bak`

- Working local DLLs:
  - `./game_dlls/sts2.dll`
  - `./game_dlls/GodotSharp.dll`

- Helper investigation dump:
  - `./findlabels_dump.txt`

## What We Know For Sure

- The character-select serif bio text is not random UI chrome.
- It is the `_description` `MegaRichTextLabel` on `NCharacterSelectScreen`.
- Generic font scaling at only the plain-Godot level was not enough because `MegaRichTextLabel` has its own auto-fit path.
- Directly calling `MegaRichTextLabel.SetFontSize(...)` from another class caused a runtime access violation.
- Public theme font-size overrides are safe from that specific failure mode.
- The global `GodotSharp.dll` patch is self-contained and does not require an external helper assembly.
- The remaining unscaled examples shared a real pattern: they were predominantly `MegaRichTextLabel` consumers.
- Patching `MegaLabel` / `MegaRichTextLabel` at `_Ready()` is much more generic than chasing strings or screens.

## Remaining Open Questions

- Some rare text may still live outside the managed label pipeline.
- If a stubborn miss remains, the first question is now:
  - is it actually a `MegaRichTextLabel`, a plain Godot label, custom drawing, or a texture/image?
- If a future miss is still on character select specifically, the next most likely fallback patch point would be:
  - `NCharacterSelectScreen.SelectCharacter(...)`

## Recommended Next Step

Use the current `1.25x` build as the base shareable patch.

If another miss turns up later:

- identify the exact on-screen text
- identify which control class renders it
- only add a new patch if it truly falls outside the existing generic paths
