# CatBlockPuzzle Changelog

## Unreleased - 2026-11-04

### Hammer power-up (new)

- Added the **Hammer**: arm it, then tap the board to smash what is in the way. It opens a padlock, blows away fog (the tapped cloud and the ones touching it) or knocks a landed cat back onto the shelf. Pinned (settled) cats cannot be smashed.
- The Hammer is only spent when a smash actually lands. Arming it, tapping an empty cell, pressing the button again, pausing or leaving the level all cost nothing.
- It cannot be armed when there is nothing to smash (empty board, no padlocks, no fog), so one is never wasted.
- Swing animation (wind-up, strike, fade), board jolt, paw burst, and two new sound events (`HammerHit`, `HammerBreak`) mapped to existing clips in `audio_config.json`. Everything on the board that can be smashed pulses while the Hammer is armed.
- New HUD button next to Hint/Freeze (cloned from the Hint button, hammer icon from the TangleRings project). Unlocks at level 8 (`Hammer Unlock Level` on the Gameplay HUD), shows `Lv 8` in grey until then, and has its own unlock tutorial ("Something in the way? Tap Hammer!").
- Sold in the power-up purchase popup at 150 coins (`PowerUpShopConfig`), saved as `hammerCount`, counted in `LevelAttemptStats.hammersUsed`, and reported to analytics as `hammer`.
- The power-up row re-spaces itself when three buttons are shown.

### Cat shelf

- The shelf is taller (Tray Area 270 to 350, profile `TrayHeight` 292 to 380, `TrayCardHeight` 246 to 300) and cat blocks are slightly bigger (`TrayCatCellSize` 46 to 50), so cats no longer poke out of their cards while they bob, pop in or land.

### Testing tools

- `Cat Block Puzzle > Testing > Run Dumb Player Bot (Play Mode)`: a clumsy random player that drops cats anywhere, knocks them off, mashes every power-up, taps the Hammer board at random and pauses at odd moments, checking for errors, stuck input, board bookkeeping drift and cats outside their cards. Report: `Temp/DumbPlayerReport.txt`. It borrows and then restores the saved power-up counts.
- Added play-mode tests for the Hammer and a resolution/overflow test (`ResolutionLayoutPlayModeTests`).

### Dumb player results (editor, 2026-11-04)

- 14 levels (1, 4, 5, 9, 12, 18, 25, 36, 46, 61, 67, 76, 87, 100), 30 random moves each, Hammer/Hint/Freeze mashed, pauses mid-level: no console errors, no board bookkeeping drift, no cat outside its shelf card, Hammer layer always in step.
- One finding: on level 100 input stayed locked for 6 s once. Replaying levels 99-100 five times with another seed did not reproduce it, so it is unexplained. Watch for it on a phone (see the checklist).
- Edit-mode suite after these changes: 137 of 137 pass. Not yet run: the new `HammerPlayModeTests` (the Unity test runner hung on this machine), the resolution/overflow test (its first version measured nothing and is unreliable), 

### Stuck nudge (new)

- When a player is stuck (12 s with no move, or 3 wrong drops in a row) the game points at the power-up that helps: a gold glow pulsing behind the button, a hand tapping it and a one-line message. It also plays the hint chime.
- Picking: repeated padlock misses and a Hammer in stock point at the Hammer; low time with a Freeze in stock points at Freeze; otherwise the Hint (if none are left it says "Need a hint?" and the tap opens the purchase popup); otherwise a Hammer that has something to smash.
- It goes away the moment the player does anything, holds at most 8 s, waits 20 s before showing again and shows at most 3 times per level. Never while a popup, tutorial, pause, drag or Hammer is active. Nothing is shown before the first power-up unlocks (level 4).
- Logic lives in `Core/StuckAdvisor.cs`; the marker in `UI/PowerUpNudge.cs`. 8 edit-mode tests cover which power-up gets picked.

### Tutorial polish

- Before the Hint unlocks (levels 1-3) a stuck beginner gets the free glow: the next cat and its spot light up with "Try this cat in the glowing spot!" (same stuck rules, no power-up spent).
- The Hammer unlock tutorial now says what it does ("Smash a padlock, fog or cat! Tap Hammer!"). Its demo tap used to be wasted silently on an empty board; it now explains "Hammer ready! Place a cat, then tap Hammer to knock it off."
- The padlock, key-cat and fog intro cards mention the Hammer as a way out once it has unlocked.

### Still to do

- A "How to play" replay button in Settings (tutorials can only be replayed by clearing saves for now).

### Verify on a phone

See the checklist in `Docs/PhoneTestChecklist.md`.

## Version 0.1.0 - 2026-07-08 19:22:42 +05:00

### Touch Input Responsiveness

- Changed bottom cat pieces to start dragging immediately on pointer down instead of waiting for Unity's drag threshold.
- Disabled the UI drag threshold for cat piece interactions so short touch movement begins tracking faster.
- Added pointer-up handling so a piece drag can finish correctly even when the finger does not move far enough to trigger a normal drag end event.
- Added active pointer tracking so only the same finger or pointer that started a drag can move or release that piece.
- Removed cat-piece horizontal swipe detection that previously converted piece drags into tray scrolling.
- Disabled tray `ScrollRect` movement while a cat piece is actively being dragged, then re-enabled it after drop, cancel, or fail cleanup.
- Kept tray scrolling available from the tray background or empty tray area.
- Reduced touch visual lift from `70f` to `42f` so dragged pieces stay closer to the finger.
- Reduced mouse visual lift from `28f` to `22f`.
- Replaced delayed drag smoothing with direct target positioning so the dragged piece no longer lags behind the finger.
- Reduced drag tilt intensity from `15f` to `8f` and increased tilt response speed for a tighter feel.
- Ensured fail-state drag cleanup restores tray scrolling.

### Files Changed

- `Assets/Scripts/CatBlockPuzzle/CatBlockPuzzleGame.cs`
- `Assets/Scripts/CatBlockPuzzle/CatBlockPuzzleGame.Drag.cs`
- `Assets/Scripts/CatBlockPuzzle/CatBlockPuzzleGame.Pieces.cs`
- `Assets/Scripts/CatBlockPuzzle/CatBlockPuzzleGame.RuntimeState.cs`
- `Assets/Scripts/CatBlockPuzzle/CatBlockPuzzleGame.Feedback.cs`

### Verification

- `dotnet build Assembly-CSharp.csproj` completed successfully with `0` warnings and `0` errors.
- Unity MCP live Editor validation was not available because the local MCP endpoint timed out.
