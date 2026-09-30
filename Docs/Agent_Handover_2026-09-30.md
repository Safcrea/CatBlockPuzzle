# CatBlockPuzzle — agent handover (29–30 Sep 2026)

Context for the next AI agent or developer picking up this branch. It summarises what was changed on
`HassamDev` between commit `8533e51` and `370bd67`, how the new systems work, how to verify them, and
what is known to be unfinished. Read this before changing gameplay, levels or the scene.

## 1. Architecture: no more partial class

`CatBlockPuzzleGame` used to be one ~8,000-line class split over 22 `partial` files. It is now a thin
composition root (`Core/CatBlockPuzzleGame.cs`) over single-responsibility services. **The project owner
explicitly does not want partial classes. Add a new class per concern instead.**

- The root keeps the serialized fields, script GUID and every public method that `GameSystem`,
  `LevelSelectionScreen`, the baked `CatPuzzleUiAction` / `CatPuzzlePieceDragView` bindings and the
  editor menus call. `Compose()` builds the services in dependency order.
- Services talk to the scene through `IGameplayContext`, `IPuzzleConfig` and `ILevelFlow`
  (`Core/GameplayContracts.cs`), all implemented by the root. Cycles are broken with interfaces
  (`IPlacementGate`) and events (`PieceDragController.PiecePlaced` / `MissedDrop`, `LevelTimer.Expired`,
  `MistakeHearts.OutOfHearts`).
- `PuzzleSession` holds per-level runtime state. The field names are kept from the old class because
  the play-mode tests reflect into them by name. `PreparedScenePlayModeFixture.Field/Call/SetField`
  searches the whole service graph.
- Editor-only authoring lives in `EditorTools/` (`CatPuzzleUiAuthoring`, `CatPuzzleMetaAuthoring`,
  `CatPuzzleScenePreparer`, `CatPuzzleMetaCapture`, `CatPuzzleLevelPrefabExporter`, `CatPuzzleSpriteFactory`).

Key services: `BoardController` (layout, `IsCellFree`, `CanPlace`, `CellBaseColor`), `PieceDragController`,
`TrayLayout` (shelf, including the drip-fed window), `LevelFlowController` (load, win, fail), `LevelTimer`,
`HintPresenter`, `PlacementFeedback`, `CellLockController`, `CellZoneController`, `MistakeHearts`,
`MechanicIntroPresenter`, `TestLevelNavigator`, `PreparedLevelLoader` (instantiates level prefabs and
builds spare cats), `Services/HapticsController`.

## 2. Difficulty progression

| levels | mechanic introduced | intro level |
|---|---|---|
| 1–20 | base game (tutorial on level 1) | 1 |
| 21–40 | padlocks: counter and neighbour rules | 21 |
| 41–60 | key cats | 41 |
| 61–70 | busy shelf (drip-fed tray, 3–4 cats visible) | 61 |
| 71–80 | colour zones | 71 |
| 81–90 | spare (decoy) cat | 81 |
| 91–100 | mistake hearts (3) | 91 |

Each intro level uses only its new mechanic. Later levels mix mechanics. Rotation was considered and
deliberately **not** built, at the owner's request.

**Timer, stars and hints** scale with the authored `difficulty` (1–10) through
`Gameplay/DifficultyProfile.cs`:

| difficulty | time | 3★ at | 2★ at | can fail |
|---|---|---|---|---|
| level 1 | 120 s | 50 % | 20 % | no |
| 1–2 | 60 s | 60 % | 25 % | no |
| 3–4 | 75 s | 55 % | 25 % | yes |
| 5–6 | 90 s | 50 % | 20 % | yes |
| 7–8 | 110 s | 45 % | 20 % | yes |
| 9–10 | 135 s | 40 % | 15 % | yes |

Hints show the full target shape up to difficulty 4, then only the anchor cell. From difficulty 8 a
hint costs 2. On non-failable levels, running out of time costs stars but never fails the level.

## 3. Level data format (`Assets/_GameData/System/Resources/CatBlockPuzzle/levels_100.json`)

All new fields are optional per level:

```jsonc
"trayWindow": 3,                                      // busy shelf: cats visible at once, in piece order
"hearts": 3,                                          // mistake hearts for one attempt (0-5)
"locked": [ { "row": 2, "col": 1, "after": 3 },       // counter lock: opens after 3 cats are placed
            { "row": 2, "col": 3, "after": 0 },       // neighbour lock: opens when free neighbours of other cats are filled
            { "row": 0, "col": 0, "after": 0, "key": "l048_p4" } ], // key lock: opens when that cat is placed
"zones":  [ { "row": 6, "col": 1, "piece": "l071_p6" } ],          // only that cat may sit here
"spares": [ { "id": "l081_s1", "name": "Mochi", "shape": "u5", "row": 0, "col": 0 } ], // decoy cat
"blocked": [ { "row": 0, "col": 4 } ]                 // legacy "sleeping cat" cells; supported but unused
```

**Rules the validator enforces** (`Gameplay/LevelValidator.cs`, run by the game at start-up and by the
editor menus):
- Locks must sit on solution cells, and a key must name another piece.
- A greedy placement simulation (`CanComplete`) must be able to finish every level, honouring locks,
  keys and the shelf window. Opening is monotonic, so the greedy pass is exact.
- A zone must be on its own piece's solution cells.
- A spare must use a known shape no bigger than the largest baked card.
- `hearts` must be 0–5 and `trayWindow` must not be negative.

**Editing levels (important):**
1. The game reads a baked copy in `Assets/_GameData/Art/UI/CatPuzzleScene/Content.asset`, not the JSON.
2. After a JSON edit, run **Cat Block Puzzle → Refresh Level Data**. The pre-build check fails with
   "Prepared level data differs" until you do.
3. Refresh refuses changes to a level's grid size or piece count, because the 100 level prefabs bake
   one card per piece and one cell per grid slot. Those changes need a full
   **Cat Block Puzzle → Prepare Game Scene**. That regenerates the scene hierarchy, so avoid it unless
   you understand the authored "Reference UI" setup.
4. Spare cats deliberately avoid this: `PreparedLevelLoader.BuildSpare` clones a baked card with the
   same cat count at load time.
5. The lock, zone, spare and window data was generated with a seeded Python script and checked with a
   solver that mirrors the C# rules.

## 4. Mechanic details

- **Padlocks and keys (`CellLockController`, `CellLockArt`).** Locked cells are dimmed and wear a
  procedurally drawn padlock. Counter locks show a number. Key locks show the key cat's exact tray
  portrait plus a key, and the key cat's tray card gets a key tag. Dropping a cat onto its still-locked
  home wobbles the padlocks and pulses the key tag.
- **Placement rule.** `BoardController.IsCellFree(coord, piece)` combines board membership, occupancy,
  locks and zones. Both `CanPlace` and the drag's availability grid use it. An earlier bug let cats
  snap onto locks because the drag path had its own check.
- **Busy shelf (`TrayLayout.ApplyDripFeed`).** Shows the first `trayWindow` unplaced cats in piece order
  plus a "+N cats waiting" label. It runs at the start of every `RefreshTrayLayout`.
- **Colour zones (`CellZoneController`).** The tint and a faint face come from `PortraitColors`, which
  matches the portrait atlas order. `PuzzleStyle.PieceColors` does **not** match the portraits; don't
  use it to identify a cat.
- **Spare cats.** The win condition is now "board full" (`session.IsBoardFilled`). Hints and the
  move-piece tip ignore spares. Spares are never put on busy-shelf levels, because a decoy could fill
  the window.
- **Mistake hearts (`MistakeHearts`).** A heart is lost only when a cat is released **over the board** and
  can't land (`PieceDragController.MissedDrop`). Releasing in empty space, returning to the shelf, or a
  system cancel is free. The heart row sits under the timer panel.
- **Intro cards (`MechanicIntroPresenter` + `UI/MechanicIntroPopup`).** A one-time "New!" card appears per
  mechanic, before the timer starts. The card is the prefab `Assets/Resources/CatBlockPuzzle/MechanicIntroPopup.prefab`,
  built from the power-up purchase popup (same flowered panel, fonts and wooden button) by
  **Cat Block Puzzle → Build Mechanic Intro Popup Prefab**. Re-run that menu after restyling the purchase popup. Seen flags are PlayerPrefs `CatBlockPuzzle.Mechanic.<Padlocks|KeyCats|Shelf|ColourZones|SpareCats|Hearts>.v1`.
  The HUD's objective text is disabled in the authored scene, so it can't be used for hints.

## 5. Other changes

- **Power-up unlock tutorials (`UI/PowerUpTutorialManager` + `UI/PowerUpUnlockTutorial`).** Ported from the
  ZombieRush `PowerupUnlockTutorial` / `PowerupController` flow, without DOTween or Febucci (coroutines instead):
  1. The screen dims.
  2. The power-up icon pops in at the centre with a typewriter-bounce message.
  3. The icon flies onto the real Hint or Freeze button. This marks it seen and grants a free charge if the
     bag is empty.
  4. A hand points at the button while only that button can be tapped. The button is lifted above the dim
     with a temporary override-sorting `Canvas` + `GraphicRaycaster`, dropped again on finish or cancel.
  5. The tap is queued (`GameplayHudView.TryTakeTutorialUse`) and spent for real by `LevelFlowController`
     once the level starts.

  Hint unlocks on level 3 and Freeze on level 5 (`GameplayHudView.hintUnlockLevel/freezeUnlockLevel`); seen
  flags are `CatBlockPuzzle.Tutorial.Hint/Freeze`. The overlay is the prefab
  `Assets/Resources/CatBlockPuzzle/PowerUpUnlockTutorial.prefab`, built by
  **Cat Block Puzzle → Build Power Up Unlock Tutorial Prefab**. The old `HintTutorial`/`FreezeTutorial`
  card prefabs are no longer used.

- **Timer starts on first touch.** `LevelTimer.awaitingFirstTouch` holds the countdown at full time until the
  first cat pickup (`PieceDragController.PieceLifted`) or the first Hint or Freeze. The timer still counts as
  "running" for pause/resume; it just spends no time.
- **Coins on level complete** are only paid on a level's first clear in normal play
  (`MetaProgressionController.RecordMetaFirstClear`). Levels opened with the test navigation bar run in
  testing mode (`levelNavigationTesting`) and deliberately award nothing, so a "no coins" report while
  browsing with the bar is expected.

- **Haptics (`Services/HapticsController.cs`, iOS plugin `.mm`).**
  - Named moments via `HapticKind`: UiTap, Selection (the dragged cat lines up with a new valid spot),
    Pickup, Snap, Combo, Wrong, LockOpen, HeartLost, TimerTick (last 5 s of failable levels),
    LevelFail and LevelComplete.
  - Android tries composed primitives (API 30+), then predefined effects (API 29+), then amplitude
    pulses, sent with game usage attributes.
  - The Android and iOS code is inside platform `#if` blocks, so a normal Editor compile won't catch
    errors in it. Verify with `PlayerBuildInterface.CompilePlayerScripts` for Android and iOS.
- **Drag feel.** Reach is linear, gain 1.10, max extra reach 80 px, follow delay 0.11 s, with half the
  tilt and jelly. Values live in `Resources/CatBlockPuzzle/portrait_layout_profile.asset`.
- **Test level navigation bar (`TestLevelNavigator`).** The bar (<< < > >>) is controlled by the
  **Show Test Level Navigation** checkbox on the game component, in every build. **It is currently
  ticked. Untick it before shipping.** The build check logs a warning while it's on.
- **Hint count label** no longer truncates on tall phones (scene change).
- `BoardController.GetBoardTargetMaxSize` falls back to the authored rect when the canvas isn't laid
  out (editor preview scenes).

## 6. How this was verified (and how to re-verify)

A live Unity Editor can be driven through the Unity CLI (`com.unity.pipeline` is installed):

```bash
unity status
unity command editor_status --project-path "<project>"
unity command recompile --project-path "<project>"
unity command run_tests --mode editor --async_tests true --project-path "<project>"
unity command eval '<C#>' --project-path "<project>"
```

Pass `--caller plugin --skill unity-cli` on each `unity command` call. An `eval` must finish within
about 5 seconds, so keep scripts short. A backgrounded Editor doesn't tick frames, so stop the level's
`boardRevealRoutine` and step coroutines by hand. `capture_game_view --source camera` renders even
when the window isn't focused.

What was checked on 30 Sep:
- **All 100 levels** were solved in Play Mode using only moves that `CanPlace` allows. Locked cats
  were refused until open, zones refused other cats, the shelf never exceeded its window, and no spare
  was needed.
- **Hearts:** 3 → 0 fails the level, no negative count, retry refills, no hearts on other levels, and
  real drag gestures behave correctly (onto a cat costs a heart; in a corner is free).
- **Intro cards:** each intro level shows exactly its own card once. Hints never target spare,
  off-shelf or locked cats.
- **Console:** no errors or warnings.
- **Builds:** Android and iOS player scripts compile, and the iOS plugin passes an iOS SDK syntax check.

**Known failing tests (not caused by this work):**
- Edit mode, 75/77 pass: `CatPuzzlePreparedSerializationTests.SavedScene_ReopensWithAllLevelAssetsAndEventsAssigned`
  (buttons "Settings" / "Next Level" have no persistent listener in the scene) and
  `ReferenceGameUiTests.SavedGameScene_UsesAuthoredHudAndContainsAllLevelSlots` (NullReferenceException).
- Play mode, `ReferenceGameUiPlayModeTests` 8/12 pass. The daily reward, shop "coming soon" and main
  menu tests expect an older UI hierarchy (for example "Reference UI", now "MainUi"). The
  pause-during-start test resumes before the start sequence has restarted the timer.
- Other play-mode fixtures default to a scene named "CatBlockPuzzle", which isn't in Build Settings.

## 7. Open items

- `GameScene.unity` and `ProjectSettings/ProjectSettings.asset` have uncommitted local changes that
  aren't from this work. They are Unity-side re-serialisation: removed stale `LevelSelectionScreen`
  fields and an added `sequenceTimeScale`. Review before committing.
- `Packages/packages-lock.json` downgrades `com.unity.test-framework.performance` 3.5.0 → 3.4.0, and
  `Assets/Resources/PerformanceTestRun*.json` are deleted locally. These look accidental; left alone.
- Haptic strength, heart-row placement, intro-card layout and the spare card's look were checked in
  code and desktop renders only. They need on-device testing (primary test phone: Galaxy S22).
- The generated lock, zone and spare placement has not been playtested for fun. Tune per level in the
  JSON, then Refresh Level Data.
