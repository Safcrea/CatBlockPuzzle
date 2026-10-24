# Level Plan v2: implementation spec

Approved by the owner on 2026-10-24. Work on branch HassamDev. Commit messages must not have any AI co-author or attribution trailer.

## 0. Ground rules
- Never run full "Prepare Game Scene" on the real scene. After changing grid sizes or cat counts, use **Cat Block Puzzle > Rebake Level Prefabs (Keep Scene)**.
- Keep the hand-authored HUD. Add new UI elements next to it with small editor scripts, the same way the level test bar was added in 7aab555.
- The baseline before this work: EditMode 2 of 95 failed, PlayMode 12 of 33 failed. No new failures are allowed. Add tests for the new rules.

## 1. Level data
Copy `Docs/LevelPlanV2/levels_v2.json` over `Assets/_GameData/System/Resources/CatBlockPuzzle/levels_100.json`. It holds exactly 100 levels. New optional fields:
- `pieces[].fixed: true`: the cat starts on the board at its solution spot (see 4).
- `fog: [{row,col}]`: foggy cells (see 5). They can include cells outside the board, to hide the outline.
- `moves: N`: move-limit stars (see 6).
- `blocked: [{row,col}]`: cat bowls (already parsed; see 3).
Level titles changed: every level is named after its board picture.

## 2. Seven new cat shapes (ShapeLibrary.cs)
```
{ "l3Top",      Cells((0,0),(0,1),(1,0)) },
{ "l3TopRight", Cells((0,0),(0,1),(1,1)) },
{ "j3",         Cells((0,1),(1,0),(1,1)) },
{ "l4TopLeft",  Cells((0,0),(0,1),(1,0),(2,0)) },
{ "j4Top",      Cells((0,0),(0,1),(1,1),(2,1)) },
{ "s4v",        Cells((0,0),(1,0),(1,1),(2,1)) },
{ "t4Left",     Cells((0,1),(1,0),(1,1),(2,1)) },
```
Check that anything else keyed on shape ids (portrait art, the prefab exporter, tests) handles them.

## 3. Draw the cat bowls (idea 5)
Every cell in `blocked` is inside the board's grid but not a board cell. Draw a small cat food bowl sprite in that grid slot, matching the board cell size, with no raycast target. Nothing can be dropped there (it is already not a board cell). Use an existing art sprite if one fits; otherwise draw a simple bowl procedurally, like CellLockArt draws padlocks.

## 4. A cat already on the board (idea 6)
A piece with `fixed: true` is placed on the board at its solution position when the level loads. It never appears in the tray or the busy-shelf window, cannot be picked up, and does not count against hearts or moves. It counts as placed for counter padlocks (so a sleepy cat with `after = cats - 1` opens when every other cat is down). Hints skip it. Give it a visible "settled" look, such as a small pin or a sleeping face badge. Add a one-time "New!" intro card (MechanicIntroPresenter) at its first level (51): "This cat is already napping in its spot. Build around it!"

## 5. Foggy cells (idea 8)
Cells in `fog` are covered by a soft cloud tile that hides whether the cell is board, a bowl or empty. A cloud clears (with a short fade) as soon as a cat is placed on any orthogonally neighbouring board cell. Clouds never block drops: a cat may be dropped onto foggy board cells, the player just can't see them. When a cloud clears, check the rest again, since clearing doesn't chain on its own. Intro card at level 61: "Foggy spots hide part of the board. Place a cat next to them to clear the fog."

## 6. Move-limit stars (idea 7)
On a level with `moves: N`, stars come from drops instead of the timer: 3 stars for N drops or fewer, 2 stars for up to N+3, otherwise 1 star. One drop is one release of a cat onto the board that lands. Picking up an already placed cat and dropping it again counts as another drop. Show a "Drops 3/10" counter where the stars are, and keep the timer only for failing, under the existing difficulty rules. Intro card at level 71: "Fewer drops, more stars! Solve it in N drops for 3 stars."

## 7. No timer in levels 1 to 10 (approved)
For level index 0 to 9, hide the timer label and the Freeze button, don't count down, and award 3 stars on completion. Hint still works.

## 8. Free hint after 3 fails (approved)
Count fails per level number: timer expiry and running out of hearts. Store the count in PlayerPrefs, keyed by level. When a level has failed 3 times, the next attempt gives one free hint that doesn't spend a charge, shown with a short toast: "Here's a free hint!" Reset the count when the level is won or the free hint is used.

## 9. Rotate booster from Chapter 3 (idea 9)
A new power-up, "Rotate", sits alongside Hint and Freeze. It unlocks at level 21 with the same tutorial flow as Hint and Freeze (PowerUpTutorialManager), and the tutorial grants one free charge. Using it puts the tray in rotate mode: tap one tray cat to turn it 90 degrees clockwise. You may turn it repeatedly for that use, and the mode ends when that cat is dropped. The win condition stays "board full", so a rotated cat that fills the board legitimately wins. Rotated shapes are computed at runtime from the offsets, normalised to a minimum row and column of 0. The card visual must rotate too. Add it to the power-up shop with the same pricing as Freeze. It is never used in levels 1 to 20 and never required by any level.

## 10. Daily hard puzzle (idea 10)
Add a "Daily Puzzle" button on the home screen, next to the existing daily reward, without regenerating the home UI. The daily level is chosen by date (UTC days since 2026-01-01, modulo 50) from levels 51 to 100. It plays as a preview-style level, so no progress or star changes, with one change: winning grants a bonus of 50 coins once per calendar day, through the existing coin wallet. Show a check mark when today's puzzle is done. It unlocks after level 20 is won.

## 11. Verify
- Compile with 0 errors. Validate Game Scene passes, and LevelValidator passes on all 100 levels (extend the validator: a fixed piece can't hold a lock or key; fog cells are inside the grid; moves ≥ the number of non-fixed cats).
- Rebake Level Prefabs (Keep Scene).
- With the level test buttons, play levels 1, 11, 16, 21, 31, 36, 41, 46, 51, 61, 71 and 100 in Play Mode. Check the new rules visually: screenshots at 1080x1920 go in TestArtifacts/v2_*.png. Check the Rotate tutorial at level 21 and the Daily Puzzle button.
- Run EditMode and PlayMode tests and compare them with the baseline.
- Commit in logical steps (data, shapes, rules, boosters, daily) with plain messages and no AI trailer. Don't push.
