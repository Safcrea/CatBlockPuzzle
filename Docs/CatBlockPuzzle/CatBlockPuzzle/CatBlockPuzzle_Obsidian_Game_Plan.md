---
title: CatBlockPuzzle Game Plan
aliases:
  - Cat Block Puzzle Game Bible
  - CatBlockPuzzle Design Hub
tags:
  - game-design
  - gdd
  - obsidian
  - catblockpuzzle
status: draft
updated: 2026-07-15
project: CatBlockPuzzle
---

# CatBlockPuzzle Game Plan

## One-Line Pitch

CatBlockPuzzle is a cozy portrait-mobile puzzle game where players drag cat-shaped pieces into a target board, clear fast 10-20 second levels, and gradually turn empty rooms into safe forever homes for rescued cats.

## Product Summary

- Genre: casual mobile puzzle
- Platform: Unity, portrait-first mobile layout
- Session style: snackable, low-friction, high-feedback
- Campaign scope: 100 levels
- Meta structure: 10 story chapters / rooms, 10 levels per chapter
- Emotional tone: cozy, gentle, hopeful, tactile
- Core fantasy: solve cute cat puzzles to earn trust and build a home

## Vision

The game should feel fast, readable, and comforting. Each level is short enough to complete in seconds, but the player still feels steady forward motion because every few wins unlock more room decoration, more story, and a stronger sense that the cats are becoming safe.

The puzzle layer carries the moment-to-moment satisfaction. The room rescue layer carries the emotional reason to keep going.

## Player Promise

- I can understand a level at a glance.
- Dragging pieces feels direct and satisfying.
- Failure is light and recovery is fast.
- Progress always means something: more coins, more room pieces, more cat trust, more story.
- The game stays cozy instead of punitive.

## Core Pillars

### 1. Fast tactile puzzle play

The player should pick up a piece instantly, see clear valid/invalid feedback, and get satisfying snap-and-pop confirmation on correct placement.

### 2. Cozy rescue fantasy

Finishing puzzles is framed as helping shy cats settle into safe rooms instead of merely clearing abstract boards.

### 3. Visible progress every few minutes

Every chapter has ten levels and five decoration milestones, so the player gets a frequent sense of accomplishment.

### 4. Low-pressure completion

Hints, readable silhouettes, and generous pacing should reduce frustration and support broad casual appeal.

## Story Premise

A quiet house is being prepared room by room for rescued cats. Each chapter introduces a different cat with a specific fear, habit, or emotional need. Solving puzzles earns the coins and trust needed to furnish a room for that cat. By the end, the house becomes a shared forever home.

The story is not heavy drama. It is soft emotional progression: fear becomes curiosity, caution becomes trust, and empty space becomes belonging.

## Story Arc

### Act 1. Safety

The early cats need quiet, shelter, and predictable spaces. The game teaches the player that puzzle completion equals care.

### Act 2. Trust

Mid-game rooms become more personalized. The cats start to show preferences, playfulness, and attachment.

### Act 3. Home

Late-game rooms emphasize shared life, confidence, and permanence. The final chapter unites all cats in one completed home.

## Chapter Map

| Chapter | Levels | Room | Cat | Emotional Beat |
| --- | --- | --- | --- | --- |
| 1 | 1-10 | Welcome Nook | Mochi | Fear softens into first safety |
| 2 | 11-20 | Maker Corner | Bean | Human-made care becomes comfort |
| 3 | 21-30 | Reading Nest | Pickle | Quiet routine becomes rest |
| 4 | 31-40 | Seaside Dayroom | Nori | Curiosity replaces fear of openness |
| 5 | 41-50 | Dreamy Nursery | Miso | Sleep becomes peaceful |
| 6 | 51-60 | Celebration Craft Room | Waffle | Repair and play become welcome |
| 7 | 61-70 | Tea & Trust Lounge | Taffy | Solitude turns into shared ritual |
| 8 | 71-80 | Garden Sunroom | Sunny | Exploration becomes safe adventure |
| 9 | 81-90 | Moonlight Den | Pepper | Nighttime safety is learned |
| 10 | 91-100 | Forever Home Studio | All Cats | The house becomes a permanent home |

## Chapter Story Notes

### Chapter 1. Welcome Nook

- Cat: Mochi
- Start beat: frightened by every new sound
- End beat: has a truly safe sleeping place
- Role in campaign: tutorial for both puzzle play and emotional framing

### Chapter 2. Maker Corner

- Cat: Bean
- Start beat: hides beneath the craft desk
- End beat: learns that human hands can build comfort
- Role in campaign: expands world identity and reinforces care through making

### Chapter 3. Reading Nest

- Cat: Pickle
- Start beat: watches the doorway instead of resting
- End beat: quiet routine becomes enough to stay
- Role in campaign: deepens calm, cozy tone

### Chapter 4. Seaside Dayroom

- Cat: Nori
- Start beat: nervous near open windows
- End beat: curiosity begins to replace fear
- Role in campaign: first clear broadening of environmental confidence

### Chapter 5. Dreamy Nursery

- Cat: Miso
- Start beat: sleeps lightly beside the exit
- End beat: can finally dream without preparing to run
- Role in campaign: midpoint emotional payoff

### Chapter 6. Celebration Craft Room

- Cat: Waffle
- Start beat: brings broken toys to the house
- End beat: the room becomes a welcome kit for future rescues
- Role in campaign: shifts the home from shelter to community

### Chapter 7. Tea & Trust Lounge

- Cat: Taffy
- Start beat: only eats alone
- End beat: mealtime becomes a calm family ritual
- Role in campaign: trust becomes social

### Chapter 8. Garden Sunroom

- Cat: Sunny
- Start beat: curious about nature but afraid to go outside
- End beat: gets a safe first adventure
- Role in campaign: hopeful expansion of the world

### Chapter 9. Moonlight Den

- Cat: Pepper
- Start beat: wakes at every nighttime noise
- End beat: learns the house is safe after dark
- Role in campaign: prepares final emotional closure

### Chapter 10. Forever Home Studio

- Cat: All Cats
- Start beat: every rescued cat helps finish the final room
- End beat: shared permanent home
- Role in campaign: finale and completion fantasy

## Core Gameplay Loop

1. Enter a level and read the board silhouette immediately.
2. Pick up one of the available cat-shaped pieces.
3. Drag it onto the board.
4. Read valid or invalid placement feedback.
5. Snap correct pieces into place.
6. Finish the board.
7. Collect reward, story progress, and decoration unlock progress.
8. Continue to the next level or room step.

## Puzzle Rules

- Pieces are pre-authored cat-shaped block arrangements.
- Rotation is currently disabled.
- The board has a target silhouette made of active cells.
- A level is complete when all active cells are correctly filled.
- Levels should feel visually solvable more often than logically exhaustive.

## Current Verified Gameplay Systems

These are already reflected in the current Unity implementation and should be treated as present features, not future ideas.

- 100-level campaign loaded from `levels_100.json`
- Portrait mobile-first layout
- 120-second level timer
- 3-star score result based on remaining time
- Coin rewards per level
- Combo tracking with celebratory feedback
- Sound toggle
- Haptics toggle
- Reduced motion toggle
- Hint flow
- Reset flow
- Win and fail overlays
- Chapter-based room progression
- Decoration unlock milestones at chapter levels 2, 4, 6, 8, and 10
- Meta hub and room-detail UI
- Cat trust / story gating between chapters

## Level Design Direction

The game should remain a fast-recognition puzzle rather than become a slow, punishing logic game. Difficulty should come from silhouette readability, piece count, and spatial ambiguity in a controlled way.

### Recommended difficulty levers

1. Increase piece count.
2. Increase board size.
3. Add more irregular silhouettes.
4. Introduce more similar piece profiles.
5. Use holes, corners, and corridors.
6. Reserve the biggest boards and heaviest ambiguity for the final chapters.

### Avoid

- Large plain rectangles too often
- Early levels with same-looking pieces
- Difficulty spikes from multiple levers increasing at once
- Levels that require hidden-rule reasoning

## 100-Level Campaign Plan

| Phase | Levels | Goal | Board Range | Pieces | Target Time |
| --- | --- | --- | --- | --- | --- |
| Tutorial Porch | 1-10 | Teach drag, snap, finish | 3x3 to 4x5 | 2-3 | 5-8 sec |
| Cozy Room | 11-30 | Build confidence and speed | 4x4 to 5x6 | 3-4 | 8-10 sec |
| Cat Cafe | 31-60 | Add medium friction | 5x5 to 6x7 | 4-6 | 10-14 sec |
| Moon Garden | 61-85 | Make players pause and plan | 6x6 to 7x8 | 5-7 | 14-18 sec |
| Rooftop Finale | 86-100 | Deliver final challenge | 7x7 to 8x8 | 6-8 | 18-25 sec |

## Recommended Grid Distribution

| Grid Size | Target Count |
| --- | ---: |
| 3x3 to 3x4 | 5 |
| 4x4 to 4x5 | 15 |
| 5x5 to 5x6 | 25 |
| 6x6 to 6x7 | 30 |
| 7x7 to 7x8 | 20 |
| 8x8 | 5 |

## Piece Vocabulary

### Current useful vocabulary

- 3-cell line
- 4-cell line
- 5-cell line
- L and J variants
- 5-cell L
- 2x2 square
- P piece
- T piece
- S/Z family

### Recommended additions for long-term variation

- 3-cell corner
- small 3-cell L
- 4-cell T
- compact 4-cell S/Z
- 5-cell U
- 5-cell short stair
- 5-cell plus, used sparingly

## Economy Plan

Coins should act as positive reinforcement and room-building fuel, not as a blocker that stops campaign progression.

### Current verified reward profile

- Level count: 100
- Reward range: 20 to 100 coins
- Average reward: 51.25 coins
- Early-game rewards alternate around 20-25 coins
- Late-game rewards ramp into 70-100 coins

### Current decoration unlock structure

- Each chapter has 5 decorations
- Decorations unlock after local chapter levels 2, 4, 6, 8, and 10
- Decoration prices increase across the campaign, from 35 coins in chapter 1 up to 165 coins in the final chapter

### Economy design rules

- Main campaign progression should not require grinding beyond normal first clears
- Decoration purchases should feel meaningful but reachable
- Coins should remain mostly for furnishing and light optional support features
- Hint pricing should be generous if monetized or retained as a coin sink

## Meta Progression Loop

1. Clear levels for first-clear rewards.
2. Unlock chapter decorations at milestone levels.
3. Spend coins to furnish the current room.
4. Trigger story beats for intro, trust, and room completion.
5. Unlock the next room and next cat.
6. Repeat until the final shared home is complete.

This loop is what turns the project from "cute block puzzle" into a sticky identity-driven game.

## UX and Feel

The game should be soft, readable, and touch-native.

### Required feel targets

- Immediate pickup response
- Strong snap satisfaction
- Clear valid and invalid feedback
- Minimal drag lag
- Compact but readable mobile UI
- Frequent visual celebration without long interruptions

### Current presentation direction

- Warm cream backgrounds and panels
- Rounded boards, trays, and buttons
- Kawaii cat-piece art style
- Paw-shaped particles and drag trails
- Cat mood feedback for placed and failed interactions
- Soft shadows and raised card presentation

## Audio and Haptics

Audio and haptics should support tactile confirmation rather than dominate the experience.

- Buttons should feel soft and clean
- Correct placement should have a satisfying snap
- Invalid placement should be noticeable but not harsh
- Win feedback should feel rewarding and brief
- Haptics should reinforce placement and success states
- Reduced-motion users should still get strong readability without heavy animation

## Accessibility and Comfort

- Portrait layout should remain touch-readable on small devices
- Reduced motion should preserve clarity while minimizing animation
- Sound and haptics toggles should remain persistent
- Difficulty should favor readability over trick design
- Failing a timer should recover quickly without emotional punishment

## Content Production Rules

- Every level should be solvable cleanly without rotation
- Early levels should avoid visual ambiguity
- Harder levels can introduce hesitation, not confusion
- Board silhouettes should feel thematic where possible: nook, cushion, shelf, moon, garden, window, fish, basket
- Each 10-level chapter should feel like a mini-arc with its own emotional mood
- Decoration art should visually reinforce each cat's personality and room identity

## Current Project State

Based on the current repository, the project is beyond prototype stage and already includes:

- A 100-level level pack
- A 10-chapter meta story catalog
- Authored room backgrounds and thumbnails
- Authored decoration assets for all 10 rooms
- Runtime-generated UI and puzzle board
- Meta hub / room flow
- Timer, stars, combo, rewards, and settings systems
- Recent touch responsiveness improvements

The main remaining work is likely polish, balance, QA, live feel tuning, and final content validation rather than fundamental feature invention.

## Risks

### Design risks

- If later levels become too slow, the cozy tone will collapse into fatigue.
- If decoration prices overshoot first-clear earnings, the rescue fantasy will stall.
- If silhouettes become too abstract, players lose the "cat cozy" flavor.

### Production risks

- Runtime-generated UI can slow down iteration compared with prefab-driven presentation.
- Balance risk is high across 100 levels if level validation remains mostly manual.
- Meta progression bugs can feel worse than puzzle bugs because they threaten player trust in reward flow.

## Recommended Next Production Steps

### Near term

- Playtest level completion times across the full 100-level campaign
- Verify economy affordability chapter by chapter
- Confirm every chapter has a satisfying intro, trust, and completion beat
- Review timer difficulty so stars reward speed without becoming stressful
- Run full-device touch QA across smaller phones

### Mid term

- Build a balancing sheet for level time, reward, and decoration affordability
- Tag levels by board theme and difficulty lever to prevent repetition
- Decide whether hints are free, coin-costed, or ad-supported
- Decide whether to preserve code-built UI long term or migrate selective areas to prefabs

### Polish pass

- Improve final chapter celebration
- Add more room-specific ambient audio identity
- Tighten fail-state pacing
- Add stronger visual identity to chapter transitions

## Obsidian Note Links

- [[CatBlockPuzzle_100_Level_GDD]]
- [[CatPuzzle_AgentRevamp_Report]]
- [[KawaiiPresentationLayer_Guide]]

## Source of Truth References

- Levels: `Assets/Resources/CatBlockPuzzle/levels_100.json`
- Meta chapters: `Assets/Resources/CatBlockPuzzle/meta_chapters.json`
- Runtime entry: `Assets/Scripts/CatBlockPuzzle/CatBlockPuzzleGame.cs`
- Meta UI flow: `Assets/Scripts/CatBlockPuzzle/CatBlockPuzzleGame.MetaUI.cs`
- Level model: `Assets/Scripts/CatBlockPuzzle/LevelModels.cs`
- Meta definitions: `Assets/Scripts/CatBlockPuzzle/CatMetaDefinitions.cs`

## Bottom Line

CatBlockPuzzle already has the structure for a strong cozy puzzle game: fast tactile solves, an authored 100-level campaign, and a meaningful rescue-home meta loop. The clearest product direction is to keep the puzzle layer simple and satisfying while using room decoration and cat trust as the main emotional retention driver.
