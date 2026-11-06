# CatBlockPuzzle Changelog

## Unreleased - 2026-11-06

### Round 5: TangleRings-style tutorials, a more useful Hammer, hint fixes

- **Hint bug, found with a new audit tool.** `HintAudit` (`EditorTools`) plays every level the way a player would who only follows the Hint, also after a few random wrong placements. Following the hint from an empty board solved all 100 levels, but after wrong placements it pointed some cats at spots that could not take them (seen on levels 24, 26, 27, 30, 34, 44, 78, 79, 89 and 99). The cause: when a cat's spot was held by wrong cats the hint just said "move those", even when the spot was also padlocked or colour-zoned, so the cat still could not go there. Now the hint only points at a spot that would really be usable once cleared; otherwise it points at a wrong cat to lift (no spot shown unless it is free). After the fix six scrambles over all 100 levels (seeds 31-33 with 5-6 wrong cats, seeds 12, 21 and 22 with 3-4) found no problem.
- **A hint now always costs 1.** From difficulty 8 (44 levels, first seen on level 14) a use cost 2 hints, so a player holding one tapped the button and got a purchase popup with no explanation, which looked like the Hint was broken.
- **TangleRings-style power-up tutorials.** New `TutorialSpotlight`: the screen dims except a soft round hole over the button, a glowing ring pulses out of it, the button bounces, a bold outlined label sits next to it and a hand pokes it; taps anywhere else do nothing. Used for the Hint (level 4) and Freeze (level 13) introductions and the clock on the first timed level. The old fly-in icon prefab (`PowerUpUnlockTutorial`) is no longer used. The level timer is paused while any of these is open.
- **Level 1 text** is now bold white letters with a brown outline straight on the scene (no card), like the TangleRings tutorial text.
- **The Hammer is taught when it is needed, not at its unlock level.** The first time the player has lost a couple of drops (or stalled) with cats, padlocks or fog in the way, the spotlight lands on the Hammer button ("SOMETHING IN YOUR WAY? TAP HAMMER!"), the player is given free Hammers to try it, and the press swings it for real.
- **More useful Hammer.** It now clears every wrong cat (up to 5; cats on their own or a same-shape twin's spot are never touched; the ones holding other cats' spots go first), then breaks padlocks and blows fog away as before, and afterwards shows the Hint for what to place next, free. Twin cats count as correct on each other's spots.

## Unreleased - 2026-11-04

### Round 4: power-ups that help, no popups, calmer hearts

Tested: edit-mode suite 139 of 139; the random-play bot (levels 10, 12, 18, 25, 36, 46, 61, 100; 24 moves each, power-ups mashed, pauses at odd moments) finds no problems after the cancel fix. Not run: the rewritten Hammer play-mode tests (the runner hangs on this machine), phone or tablet checks, and the hearts pointer and heart-crack animation have only been written, not watched.

- **Hammer redesigned.** The old Hammer needed arming and then a tap on the board, and the only thing it could do to a cat was what dragging already does. It is now one tap that clears the way: it knocks back the cats that sit on another cat's spot (up to 3), breaks the padlocks that keep a waiting cat out (up to 3, blocking ones first) and blows all the fog away, each with its own swing. A cat on its own spot is never touched, and with nothing in the way it says "Nothing in the way! You're on track." and spends nothing. The tap layer and arming mode are gone.
- **Hints near the end.** When the next cat's spot was held by a wrong cat, and nothing else fit, the Hint found no target and did nothing. It now shows the spot and pulses the cat in the way; with everything on the board it points at a cat that is off its own spot. A Hint with truly nothing to say plays the "no hint" sound.
- **Stuck nudge is a pointer only.** The message toast is gone ("Stuck? Tap Hint!" and "Try this cat in the glowing spot!"): only the glow and a bigger tapping hand on the right power-up button, as in TangleRings. Cats on the wrong spots now also point the player at the Hammer after two wrong drops.
- **Hearts.** The modal "Hearts!" card is gone; the first level with hearts makes the heart row glow with a tapping hand for a few seconds instead. Losing a heart now cracks it: pink shards and a row shake. The last heart beats until the level ends.
- **Shelf centred.** The cards sat high in the shelf because the cream panel behind them (`Pieces Panel`) never moved when I made the shelf area taller. The shelf area is back on the panel (centre -492) at 330 high, and card and cat sizes now follow the real room inside the shelf (`TrayLayout`), so a 4-row cat can no longer be sized for a taller card than it gets.
- **Hammer cancel fix (found by the bot).** Pausing mid-swing only removed the drawing; the swing sequence kept running behind the pause screen and drew more hammers. Cancel now stops the sequence and restores the board position.
- **Power-ups in unlock order.** The row reads Hint (level 4), Hammer (8), Freeze (13), Rotate (21).

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

### Hammer in rewards, bundles and sounds

- Daily Rewards: Day 5 now pays 2 Hammers (it paid 500 coins; change `DailyRewardConfig` if you want the coins back). The Day 5 card shows the hammer icon and "x2" (the scene's own icon image was switched, since `Use Authored Reward Icons` is on). Claims add `hammerCount` to the save and report it to analytics.
- Starter bundle now holds 3 Hints, 2 Freezes and 1 Hammer for the same 250 coins; the shop describes it as "... 1 Hammer". `PowerUpShopConfig` also has a Hammer price (150).
- Hammer sound: arming plays the booster sound, the strike plays `HammerHit`, a knocked cat adds its surprised meow, fog and cats add `HammerBreak`, putting the Hammer away plays the back sound. The booster sound no longer plays a second time at the end of a smash.

### Level 1 tutorial and tutorial text

- The Level 1 coach card moved above the board (it used to be wedged into the board's bottom edge, overlapping the board and the shelf, and shifted left). It is bigger, shows a title and one line of instructions by default (`Show Tutorial Captions` is now on) with the progress dots inside it.
- Soft text fixed: the card's detail line used best-fit sizing and was squeezed down to its 16 pt floor. Title and detail now use fixed 34 / 25 pt with room for the text. Copy shortened to fit one line ("Drag the glowing cat onto the green spaces.", "Green means it fits! Let go and the cat snaps into place.").
- The "Placed cats can move!" tip had the same problem (22 pt text in a small box, shrunk to 16): now a wider bubble with fixed 30 pt text, kept on screen near the edges.
- The message pill (stuck nudge, Hammer hints) has a bigger box and a 30 pt floor instead of 20 pt.

### Shop screen, power-up polish and more tutorial work

- **Shop:** new Hammer Pack card (150 coins, "x1 Hammer") next to Hint and Freeze, with matching art (`HammerPackCard.png`), its own Buy button wired through `ShopScreen` and `ProductionShopActions.BuyHammer`. The three cards now sit in one row (smaller, 326 wide) because two columns would not leave room for a third row on screen. The starter bundle line shrinks to fit ("3 Hints · 2 Freezes · 1 Hammer") instead of ending in "...".
- **Power-up polish:** using Hint, Freeze or Rotate punches the button (squash and stretch) and throws sparks in the power-up's colour; arming the Hammer does the same. Freeze also sprays ice sparks over the timer; a Hint pops gold sparks on the spot it points at. All of it is skipped with Reduced Motion.
- **Sharper tutorial icons:** the power-up unlock tutorial showed the Hint and Freeze icons (about 120 px and 128 px wide) at 230 units, a roughly 2.3x stretch that looked blurry. They now use 2x Lanczos-resampled versions (`DRP_Hint_icon_hd`, `DRP_Freeze_icon_hd`), which the theme points at.
- **Settings "?" button:** a round "?" next to the close button makes every tutorial and intro card show again (`TutorialReset.ResetAll`), then confirms with a small popup. Level 1's walkthrough shows the next time level 1 is opened. Covered by `TutorialResetTests`.

### Hammer sound effects and a Sleeping-cats intro

- Four new Hammer sounds taken from the TangleRings project (added to the config as `hammer_arm`, `hammer_hit`, `hammer_smash`, `hammer_lock_break`; the original 48 are untouched): the wind-up when the Hammer is armed (Spring), the strike (CandyBreak, 0.32 s, so it lands on contact), a cat or fog smash (Break Variant 2) and a padlock smash (Break Variant 1, which replaces the chest-open sound for Hammer-opened locks). The two clips over one second are flagged `review` like the other long ones. New events: `HammerArm`, `HammerLockBreak`; `HammerHit` and `HammerBreak` now play these clips.
- New intro card for levels with blocked (stone) cells, which had none: "Sleeping cats! Nothing fits on these spots. Build around them." It is included in the Settings "?" reset.
- The random-play bot now marks every tutorial and intro card as seen for the run (they wait for a tap it never makes); a first-run "input locked for 6 s" on level 100 may have been exactly such a card, but that is a guess.

### Test status of the shop / polish / Settings "?" / sounds batch

- Edit-mode suite: 140 of 140 pass, including `TutorialResetTests` and the updated `AudioConfigTests`. The random-play bot played levels 5, 12, 25, 46, 61 and 100 (24 random moves each, power-ups mashed) with no problems. Not run: the Hammer play-mode tests and any phone or tablet check.

### Still to do

- Checking all of the above on a phone and tablet.

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
