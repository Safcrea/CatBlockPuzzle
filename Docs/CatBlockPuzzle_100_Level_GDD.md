# Cat Block Puzzle: 100-Level Game Design Document

## Design Target

Cat Block Puzzle is a cozy, fast-session mobile puzzle game where players drag cat-shaped block pieces into a target board. The 100-level campaign should last about 20 minutes total, which means each level should average about 12 seconds, including reading, dragging, celebration, and transition time.

Because the target playtime is short, this should be designed as a snackable puzzle game, not a deep logic puzzle. Most levels should be solvable by visual matching, with difficulty coming from board shape, piece count, rotation-free spatial recognition, and mild decoys.

## Recommended Scope

- Total levels: 100.
- Target total playtime: 20 minutes.
- Average level time: 12 seconds.
- Ideal level time range: 5-25 seconds.
- Recommended max grid: 8x8.
- Practical campaign max grid: 7x8 or 8x7 for most hard levels.
- Existing current max grid: 6x7.
- Pieces per level: 2-8.
- Cells per piece: 3-5 for most levels.
- Rotation: keep disabled unless a later update adds explicit rotation UI and level rules.

## Maximum Grid Size Recommendation

Use 8x8 as the technical maximum for this campaign, but do not use full 8x8 filled boards often. On a portrait mobile screen, an 8x8 board still leaves readable cat cells, but anything beyond 8x8 will make the cats too small, reduce touch comfort, and slow down recognition.

Difficulty should usually come from board silhouette and piece mix, not raw grid size. For a 20-minute campaign, the game should feel quick and satisfying. A 9x9 or 10x10 board would push the game toward a slower puzzle audience and would likely break the 12-second average.

Recommended limits:

| Phase | Level Range | Grid Sizes | Pieces | Target Time |
| --- | --- | --- | --- | --- |
| Tutorial | 1-10 | 3x3 to 4x5 | 2-3 | 5-8 sec |
| Easy | 11-30 | 4x4 to 5x6 | 3-4 | 8-10 sec |
| Medium | 31-60 | 5x5 to 6x7 | 4-6 | 10-14 sec |
| Hard | 61-85 | 6x6 to 7x8 | 5-7 | 14-18 sec |
| Expert | 86-100 | 7x7 to 8x8 | 6-8 | 18-25 sec |

## Core Player Experience

The player should feel like they are helping cats fit into cozy spaces. The game should be readable at a glance, tactile when dragging, and generous with feedback.

Core loop:

1. See a cute board silhouette.
2. Pick up a cat piece.
3. Drag it toward the board with delayed movement and tilt.
4. See valid or invalid placement feedback.
5. Snap the piece into place.
6. Complete the board and get a short celebration.
7. Move immediately to the next level.

The emotional tone should be cozy, clever, and low pressure. The player should rarely feel stuck for more than 20-30 seconds.

## Level Design Rules

Each level should have exactly one intended solution, or at least one very clear solution path. Since the game has no rotation system, every piece orientation must be pre-authored and readable.

Rules for good levels:

- The active board cells must be exactly covered by all placed pieces.
- No piece should be visually ambiguous in the first 20 levels.
- Avoid too many same-color or same-shape pieces in early levels.
- In harder levels, use similar silhouettes to create light hesitation, not frustration.
- Keep empty holes and notches meaningful; they should hint where pieces belong.
- Avoid giant rectangular boards filled edge to edge too often, because they feel generic.
- Prefer cat-themed board silhouettes: basket, window, cushion, box, paw, fish, moon, yarn, shelf, garden bed.

## Difficulty Levers

Use these in this order:

1. Increase piece count.
2. Increase board size.
3. Add irregular board silhouettes.
4. Use more similar pieces.
5. Use long pieces that constrain placement.
6. Use holes, corners, and narrow corridors.
7. Use 7-8 pieces only near the end.

Avoid increasing all difficulty levers at once. A level with a bigger board should often have simpler pieces. A level with many similar pieces should use a smaller board.

## 100-Level Progression

### Levels 1-10: Tutorial Porch

Goal: teach drag, snap, invalid placement, and board completion.

- Grid: 3x3, 3x4, 4x4, 4x5.
- Pieces: 2-3.
- Shapes: lines, small L, 2x2 square.
- Average time: 5-8 seconds.
- No misleading pieces.

Design beats:

- Level 1: two obvious pieces.
- Level 2: first vertical line.
- Level 3: first L shape.
- Level 4: first 2x2 square.
- Level 5: first board notch.
- Level 6-10: combine learned shapes.

### Levels 11-30: Cozy Room

Goal: build confidence and speed.

- Grid: 4x4 to 5x6.
- Pieces: 3-4.
- Shapes: i3, i4, L4, J4, O4, T5.
- Average time: 8-10 seconds.

Design beats:

- Introduce wider boards.
- Use simple silhouettes like window, cushion, shelf, box.
- Start using one visually similar pair per level after level 20.

### Levels 31-60: Cat Cafe

Goal: create medium puzzle friction without slowing the campaign.

- Grid: 5x5 to 6x7.
- Pieces: 4-6.
- Shapes: all current shapes plus S/Z/P pieces.
- Average time: 10-14 seconds.

Design beats:

- Use more irregular boards.
- Introduce narrow corridors.
- Place one long piece as a clear anchor.
- Add levels with 5-6 pieces where the first placement matters.

### Levels 61-85: Moon Garden

Goal: make players pause and plan before dragging.

- Grid: 6x6 to 7x8.
- Pieces: 5-7.
- Average time: 14-18 seconds.

Design beats:

- Use asymmetric silhouettes.
- Mix long pieces with compact pieces.
- Include more boards with interior holes.
- Let some levels have two plausible starts, but only one clean finish.

### Levels 86-100: Rooftop Finale

Goal: final challenge while staying within a short-session design.

- Grid: 7x7 to 8x8.
- Pieces: 6-8.
- Average time: 18-25 seconds.

Design beats:

- Use the full shape vocabulary.
- Include the largest boards sparingly.
- Make level 100 a memorable 8x8 board silhouette, not just a large rectangle.
- Reward completion with a stronger celebration, extra coins, or a new cat theme.

## Level Count By Grid Size

Recommended distribution for 100 levels:

| Grid Size | Count |
| --- | ---: |
| 3x3 to 3x4 | 5 |
| 4x4 to 4x5 | 15 |
| 5x5 to 5x6 | 25 |
| 6x6 to 6x7 | 30 |
| 7x7 to 7x8 | 20 |
| 8x8 | 5 |

This keeps the campaign feeling like it grows over time without making the final third too slow.

## Piece Vocabulary

Current pieces are enough for the first 60 levels. For 100 levels, add a few more 3-5 cell shapes to prevent repetition.

Existing useful shapes:

- 3-cell line.
- 4-cell line.
- 5-cell line.
- 4-cell L/J variants.
- 5-cell L.
- 2x2 square.
- P piece.
- T piece.
- S/Z pieces.

Recommended additions:

- 3-cell corner.
- 3-cell small L.
- 4-cell T.
- 4-cell S/Z.
- 5-cell U.
- 5-cell plus.
- 5-cell short stair.

Avoid pieces larger than 5 cells for this campaign. Large pieces solve too much of the board at once and reduce interesting choices unless the grid becomes much bigger.

## Economy And Rewards

The current game already has coins. Use coins as a light reward, not a blocker.

Suggested reward curve:

- Levels 1-10: 20-25 coins.
- Levels 11-30: 25-35 coins.
- Levels 31-60: 35-50 coins.
- Levels 61-85: 50-70 coins.
- Levels 86-100: 70-100 coins.

Coin sinks:

- Hint.
- Undo or return helper if added later.
- Cosmetic cat colors.
- Board frame themes.
- Celebration effects.

Do not require coins to continue the main 100-level campaign.

## Hint System

The game should support fast completion without frustration.

Recommended hints:

- First hint: pulse a piece that has an obvious anchor.
- Second hint: show the piece's target area.
- Third hint: place one piece automatically or preview its final position.

For a 20-minute campaign, hints should be generous. A stuck player should not quit on level 37 because one piece is unclear.

## UX And Feedback

Required feedback:

- Smooth drag delay.
- Tilt based on horizontal movement.
- Valid placement color.
- Invalid placement color and shake.
- Snap sound and pop animation.
- Win burst.
- Fast next-level flow.

Recommended additions:

- Level progress map or simple level number strip.
- Restart button.
- Optional one-tap hint.
- Short completion rating based on moves or time, but avoid harsh failure states.

## Success Metrics

Design targets:

- Level 1 completion: under 10 seconds.
- Level 10 completion: under 1.5 minutes total elapsed.
- Level 50 completion: around 8-10 minutes total elapsed.
- Level 100 completion: around 20 minutes total elapsed for a capable player.
- Average stuck time: under 30 seconds.
- Restart rate: low before level 60, acceptable after level 80.

## Production Plan

Build levels in batches of 10.

For each batch:

1. Define grid size and theme.
2. Choose 2-3 new difficulty ideas.
3. Author 10 levels.
4. Play through without hints.
5. Record average solve time.
6. Replace levels that are too slow or too obvious.

Each 10-level batch should have:

- 6 normal levels.
- 2 slightly easier relief levels.
- 1 harder level.
- 1 memorable shaped-board level.

## Final Recommendation

Design all 100 levels, but keep the maximum campaign grid at 8x8 and use it only for the final 5 levels. Most of the game should live between 5x5 and 7x8. That gives enough variety while preserving the 20-minute playtime and keeping the cats large enough to feel good on mobile.
