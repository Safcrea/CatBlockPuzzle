# Cat Block Puzzle: Game Design Document

| | |
|---|---|
| **Working title** | Cat Block Puzzle (home screen logo: "Cat Blocks") |
| **Genre** | Casual drag-and-drop block / jigsaw puzzle |
| **Platforms** | Android (min API 25) and iOS (15+), portrait only |
| **Engine** | Unity 6 (6000.3), Universal Render Pipeline, uGUI |
| **Build version** | 0.2.0 |
| **Document status** | Current as of 2026-10-30, branch `HassamDev` |
| **Audience of this document** | Designers, QA, new developers, publishing and marketing |

How to read it: sections 1 to 3 explain the game to someone who has never seen it. Sections 4 to 12 are the
rules and numbers. Section 13 is the technical map. Section 14 lists every value you can tune and where it
lives. Section 15 is honest about what is unfinished.

Related, older, narrower documents: `CatBlockPuzzle_100_Level_GDD.md` (original level-design brief),
`Docs/LevelPlanV2/IMPLEMENTATION_SPEC.md` (the v2 level plan), `Agent_Handover_2026-09-30.md`
(engineering handover), `analytics-events.md` and `avn-event-coverage.md` (analytics).

---

## 1. Game overview

### 1.1 Pitch

A cozy portrait puzzle game. Each level is a board with a unique outline of empty cells. At the bottom of the
screen sits a shelf of cat-shaped block pieces. The player drags every piece onto the board until the outline is
completely filled, ideally before a clock runs out. Fill the board and the cats cheer. Run out of time (or
mistakes) and they cry.

### 1.2 Design pillars

1. **Cozy and warm.** Pastel art, soft animation, happy cats, no harsh failure screens.
2. **One-thumb and quick.** A level takes roughly 10 to 60 seconds. Drag and drop is the only skill.
3. **Learn one thing at a time.** The game introduces one new idea per stretch of levels, teaches it with a short
   card, and only later mixes ideas.
4. **Always a way out.** A struggling player gets Hint, Freeze, a free Hint after repeated fails, and replays that
   still pay coins.
5. **Short rewards, often.** Stars, coins, a daily reward and a daily puzzle give a reason to return every day.

### 1.3 Content at a glance

| | |
|---|---|
| Authored levels | **100**, then an endless loop of the same boards (section 7.4) |
| Board sizes | 2x3 up to 8x8 cells |
| Pieces per level | 2 to 8 (690 pieces across the campaign) |
| Piece shapes | 29 distinct cat shapes of 3 to 5 cells |
| Named cats | 50 (Mint, Ribbon, Peanut, Tuna, Pebble, Kiki, Poppy, Chili, Pudding, Bean, Pickle, Fig and more) |
| Level mechanics | 10 (bowls, three kinds of padlock, colour zones, spare cat, busy shelf, hearts, fixed cat, fog, drop limit) |
| Power-ups | 2 live: **Hint** and **Freeze** (a third, Rotate, is built but switched off) |
| Daily features | Daily Reward (7-day cycle) and Daily Puzzle |
| Currencies | Coins (soft currency), stars (progress), power-up charges |
| Settings | Music, Sound, Vibration |

---

## 2. Core gameplay loop

```
Home -> Play / Levels -> [level intro cards + tutorials] -> drag cats onto the board
     -> board full?  yes: Level Complete (stars, coins) -> Next level / Home
                     no and out of time or hearts: Level Failed -> Retry / Home
```

Minute to minute:

1. The board appears with its cells popping in one by one. The shelf shows the cats to place.
2. The clock starts (levels 11 and up).
3. The player lifts a cat from the shelf, carries it over the board and lets go.
4. A valid drop snaps the cat into place with a happy sound, a pop and a haptic tick. An invalid drop sends the cat
   gliding back to the shelf (and costs a heart on hearts levels).
5. When every cell is filled, the level is won immediately. Stars depend on speed (or drops, section 5.4).
6. Coins are paid, the result screen counts them up, and the player taps Next.

Session shape: a typical session is a handful of levels. The first 10 levels have no clock and teach the basics,
so the game starts gentle and tightens later.

---

## 3. Controls, feel and feedback

### 3.1 Controls

- **One input: touch drag.** Press on a cat in the shelf, move, release. A piece lifts as soon as the finger goes
  down (no drag threshold).
- The shelf scrolls horizontally when there are more cats than fit; scrolling is disabled while a cat is held.
- **Placed cats can be moved.** Pick up a cat already on the board and drop it somewhere else. Picking up and
  dropping on a level with a drop limit counts as a drop.
- Buttons: pause, Hint, Freeze, plus the menu buttons.

### 3.2 Drag feel (what makes it feel good)

- The held cat floats above the finger (about 42 px on touch) so the finger never hides it.
- Reach grows slightly with upward finger travel so the player can reach the top of tall boards without leaving
  the thumb zone.
- The cat tilts and squashes a little ("jelly") while moving, and follows with a short delay.
- A **ghost preview** shows where the piece would land and whether that spot is valid (valid and invalid colours).
- **Snap:** inside a snap radius around a valid spot the piece locks to it. Landing on a new valid spot fires a
  light "selection" haptic.
- Tuned in `portrait_layout_profile.asset` (lift, reach, gain, snap padding, follow delay, tilt).

### 3.3 Feedback layer

- **Cat moods:** every cat on screen has expressions (neutral, happy, worried and so on) that react to placement.
- **Combo:** consecutive first-try placements build a combo. Badges and sparkles fire at combo 3, 5 and 8. A wrong
  drop resets it. The best combo of the attempt is reported on the result screen and in analytics.
- **Juice:** pop-in cells, piece pop on win, coin flight to the coin counter, board burst on completion.
- **Haptics** (section 11.2) and **audio** (section 11.1).
- **Reduced Motion** (a saved flag, `CatBlockPuzzle.Settings.ReducedMotion`) shortens or removes most animations.
  Every animated popup checks it.

---

## 4. Board, pieces and placement

### 4.1 Vocabulary

| Term | Meaning |
|---|---|
| **Board / grid** | The rectangle of slots (2x3 to 8x8). Not every slot is a board cell. |
| **Board cell** | A slot that must be filled to win. |
| **Piece / cat** | A group of 3 to 5 connected cells shaped like a cat tile. Each has a name and a colour. |
| **Shelf / tray** | The scrolling row of unplaced pieces under the board. |
| **Solution** | The authored way the pieces fit. Hints point at it. |
| **Bowl** | A decorative cat food bowl sitting in a slot that is not a board cell. |

### 4.2 Placement rule

A piece can be dropped when **every cell it covers** is:

1. inside the grid and a board cell (not a bowl, not outside the outline),
2. not occupied by another cat,
3. not locked (section 5), and
4. not reserved by a colour zone for a different cat.

The game accepts **any valid fit**, not only the authored solution. The win condition is simply
"every board cell is filled" (spare cats may be left on the shelf, section 5.5).

### 4.3 Shapes

29 shapes of 3 to 5 cells: straight (`i3`, `i4`, `i5`), L and J families (`l3`, `l4`, `l5`, `j3`, `j4`, their top and
corner variants), squares and Ts (`o4`, `t4`, `t5`), S and Z families (`s4`, `s5`, `z4`, `z5`), and `p5`, `u5`, `stair5`.
**Pieces never rotate** in normal play: a shape is placed in the orientation it is shown in. (A Rotate power-up
exists in code but is switched off, section 8.3.)

### 4.4 Level data

All 100 levels live in one file, `Assets/_GameData/System/Resources/CatBlockPuzzle/levels_100.json`. Per level:
`id`, `title` (named after the picture the board makes: "Hello Kitty Paws", "Cozy Box", "Paw Print"), `difficulty`
(1 to 10), `rows`, `cols`, `reward` (coins), `pieces[]` (shape, solution position, optional `fixed`), and optional
mechanics: `locked[]`, `zones[]`, `spares[]`, `blocked[]`, `fog[]`, `trayWindow`, `hearts`, `moves`.

A validator (`Gameplay/LevelValidator.cs`) runs at start-up and in the editor. It proves every level is solvable by
simulating placement honouring locks, keys and the shelf window, and checks that locks sit on solution cells, keys
name another piece, zones sit on their own piece, spares use a known shape, and so on.

---

## 5. Level mechanics

New mechanics are introduced one at a time. The first level to use a mechanic shows a one-time **"New!" card**
explaining it before the clock starts (section 10.3). After that, mechanics mix.

### 5.1 Mechanic reference

| Mechanic | What the player sees | Rule | Levels (examples) |
|---|---|---|---|
| **Cat bowls** | A pink food bowl in a grid slot | The slot is not a board cell. Nothing can be dropped there. Purely shapes the board. | 11-15, 17-20, 25, 28-29, 32, 35, 44-45, 47, 49-50, 52, 55-57, 62, 69, 72, 74-75, 77, 80-82, 91-93, 95-96, 100 |
| **Counter padlock** | Dimmed cell with a padlock and a number N | Opens after N cats have been placed on the board. | 16-17, 19-20, 22, 24, 29, 33, 37, 42, 49, 53, 57, 60, 63, 68, 70, 74, 79, 83, 89, 94, 97, 99-100 |
| **Neighbour padlock** | Padlock with no number | Opens once the free cells around it that belong to other cats are filled. | 26-30, 34, 44-45, 50, 54, 59, 65, 75, 78, 85, 90, 94-95, 99 |
| **Key padlock** | Padlock showing a small cat face and a key | Opens the moment that specific cat is placed. The key cat's shelf card carries a key tag. | 21, 23-25, 27, 29-30, 32, 35, 39-40, 43, 45, 49-50, 52, 55, 59-60, 62, 65, 69-70, 73, 75, 79-80, 82, 85, 89-92, 95, 97, 99-100 |
| **Colour zone** | A tinted cell with a faint cat face | Only that cat may sit there. | 36-40, 47, 58, 62-70, 72, 77, 80, 87, 91, 94, 98, 100 |
| **Spare (decoy) cat** | One extra cat on the shelf that fits nowhere | Not needed. The level is won when the board is full, even with it left over. Hints ignore it. | 41-45, 48, 50, 72-80, 88, 93, 96, 98 |
| **Busy shelf** | "+N cats waiting" under the shelf | Only the first 2 to 4 unplaced cats are visible. A new one appears when one is placed. | 31-32, 34-35, 40, 52, 54-60, 67, 84, 92, 97 |
| **Mistake hearts** | A row of 2 or 3 hearts under the timer | Releasing a cat over the board where it cannot land costs a heart. Releasing in empty space or returning it to the shelf is free. At 0 hearts the level is failed. | 46-50, 81-90, 93, 95, 98-100 |
| **Cat already placed (fixed)** | A cat sitting on the board at the start | It cannot be picked up, never appears on the shelf, and counts as placed for counter padlocks. | 51, 53, 57, 59, 64, 72, 86, 94, 98, 100 |
| **Fog** | Soft clouds hiding cells | A cloud hides whether a slot is board, bowl or empty. It fades once a cat lands next to it or on it. Clouds never block drops. | 61, 63, 67, 69, 76, 83, 87, 91, 97, 99 |
| **Drop limit** | "Drops 3/10" label instead of the clock stars | Stars come from the number of drops, not the timer (section 6.3). | 71, 74, 78, 81, 88, 92, 96, 100 |

### 5.2 Introduction order (the curve)

| Levels | Focus |
|---|---|
| 1-10 | Base game, **no timer**, 3 stars guaranteed. Level 1 is a hands-on tutorial. |
| 11-20 | Timer begins at level 11. Bowls appear. Counter padlocks start at 16. |
| 21-30 | Key padlocks (21), neighbour padlocks (26). |
| 31-40 | Busy shelf (31), colour zones (36). |
| 41-50 | Spare cats (41), hearts (46). |
| 51-60 | Fixed cats (51), larger boards, mixes. |
| 61-70 | Fog (61), many zones. |
| 71-80 | Drop limit (71), spares and zones together. |
| 81-100 | Everything mixed, hearts everywhere, difficulty 8 to 10. |

### 5.3 Difficulty rating

Each level has an authored `difficulty` from 1 to 10 (counts: 1:10, 2:9, 3:4, 4:7, 5:10, 6:12, 7:13, 8:16, 9:11,
10:8). It rises across the campaign with small dips so each new mechanic starts easy, and it drives the timer length,
star thresholds and hint behaviour (section 6.1).

### 5.4 Level 1 tutorial

An animated hand drags the first two cats into their spaces with glowing targets and a small context card, while
the game restricts which piece can be lifted and where it may land. A separate one-time tip (`MovePieceTip`) shows
that placed cats can be moved, the first time a valid drop lands away from its solution cell.

---

## 6. Timer, stars, failing

### 6.1 Timer and difficulty bands

- **Levels 1-10: no timer** (the timer and the Freeze button are not usable). Finishing awards 3 stars.
- **Levels 11 and up: timed.** The clock starts after the board reveal, any tutorial and any "New!" card. It runs
  only during play (paused by pause, settings and popups).
- Every timed level can be failed when the clock reaches 0.

| Difficulty | Time | 3 stars if more than this share of time is left | 2 stars if more than | Hint shows | Hint cost |
|---|---|---|---|---|---|
| 1-2 | 60 s | 60 % | 25 % | full target shape | 1 |
| 3-4 | 75 s | 55 % | 25 % | full target shape | 1 |
| 5-6 | 90 s | 50 % | 20 % | anchor cell only | 1 |
| 7-8 | 110 s | 45 % | 20 % | anchor cell only | 2 from difficulty 8 |
| 9-10 | 135 s | 40 % | 15 % | anchor cell only | 2 |

(Source: `Gameplay/DifficultyProfile.cs`.) Otherwise 1 star.

The timer panel shows the countdown and three stars that drain from the right as time passes, reaching empty at the
scoring thresholds, so the player always sees which star they are on. The last 5 seconds tick with a haptic. The
clock turns red as it gets low.

### 6.2 Failing

A level fails when **time runs out** or **hearts reach zero**. The Level Failed popup offers **Retry** and **Home**.

- **Free hint after 3 fails.** Fails are counted per level number. After the 3rd fail the next attempt starts with
  one free hint (no charge spent), shown with the toast "Here's a free hint!". Winning clears the count.

### 6.3 Drop-limit levels

On a level with `moves: N` (currently N = 10) stars come from drops instead of time: **3 stars at N drops or
fewer, 2 stars up to N+3, otherwise 1**. A drop is one release of a cat that lands on the board (moving a placed cat
counts). This is a star threshold, not a hard limit: going over never fails the level.

### 6.4 Result screens

- **Level Complete:** cat panel, "Level Complete!", three stars (earned stars gold with a burst, missed stars
  greyed), a coin bar that counts up to the coins won, a caption, and **Next** and **Home** buttons.
- **Level Failed:** sad cat, "Level Failed", three brown stars, **Retry** and **Home**.

---

## 7. Progression

### 7.1 Levels and chapters

- The 100 levels are grouped into **10 chapters of 10 levels**.
- Inside the level list, levels open in order: a level is playable after the previous one is cleared.
- **Chapter gate:** the next chapter opens only when **every level in the current chapter has 3 stars**. This is a
  strict mastery gate. Because levels 1-10 are untimed and always 3 stars, chapter 2 opens naturally; from chapter 3
  onward the player may need replays to reach 3 stars before moving on. (This is a deliberate design lever; if it
  proves too harsh, the rule lives in `CatMetaProgressStore.HighestEarnedChapter`.)
- The level list shows 12 cards per page (3 columns), with stars, a locked state, a highlighted current level and
  page arrows ("1 / 9").
- **Play** on the home screen continues from the next unplayed level.

### 7.2 Stars

Best stars per level are saved. Stars matter for the chapter gate and as the player's mastery score.

### 7.3 Unlock schedule (what unlocks when)

| Feature | Unlocks | Where it is set |
|---|---|---|
| Timer | Level 11 (levels 1-10 untimed) | `DifficultyProfile.UntimedLevelCount` |
| Hint | Level 4 | Gameplay HUD, Hint Unlock Level |
| Freeze | Level 13 | Gameplay HUD, Freeze Unlock Level |
| Daily Puzzle | After winning level 10 | GameSystem, Daily Puzzle Unlock After Level |
| Daily Reward | From the player's second session | Built in (a brand-new install never gets a reward in its first session) |

### 7.4 Endless mode

After the last authored level the same 100 boards repeat while the level number keeps counting (101 plays board 1,
102 plays board 2, 201 plays board 1 again). Loop levels are always open. Each loop number pays its full reward once.

---

## 8. Power-ups

Two power-ups are live. Both are consumables stored in the player's bag. The bag count shows under each button.

### 8.1 Hint

- Highlights where a cat should go. On difficulty 1 to 4 it shows the full target shape; from 5 up it shows only the
  anchor cell. It ignores spare, locked and off-shelf cats.
- Costs 1 charge (2 charges on difficulty 8 and above).
- Unlocks at level 4.

### 8.2 Freeze

- Stops the clock for **15 seconds** (`freezeDurationSeconds`). Usable once per attempt. The timer turns icy blue.
- Costs 1 charge. Unlocks at level 13. Because levels 1-10 have no clock, the button shows **"No timer"** there.

### 8.3 Rotate (not in the game)

A Rotate booster (turn one shelf cat 90 degrees) was designed and built, then switched off at the owner's request.
Its button was removed from the HUD, so it never unlocks; the code is left in place but unused.

### 8.4 How power-ups appear and unlock

- Both buttons are **always on screen**. Until their level is reached they are **greyscale with the unlock level
  ("Lv 13") in the count pill**. Tapping a locked button opens a small popup ("Unlocks at level 13") and pauses the
  level behind it.
- Unlocking is by **furthest level reached**, so replaying an earlier level does not lock them again.
- **Unlock tutorial** (first time only): the screen dims, the power-up icon pops in the middle with a short message
  ("Stuck? Tap Hint!"), flies onto its button and a hand tells the player to tap it. That tap demonstrates the
  effect and **costs nothing**.
- **Free on unlock:** each power-up gives **2 free charges** the moment it unlocks (Inspector value *Free On Unlock*).
- Tapping an unlocked power-up with an empty bag opens the **purchase popup** (section 9.3).

### 8.5 Timer tutorial

On the first timed level (level 11), the clock icon pops in with "Beat the clock! Stars fade as time runs out", flies
up onto the timer, and after two seconds the level carries on by itself. No tap needed.

---

## 9. Economy and shop

### 9.1 Coins (soft currency)

**Earned from**

| Source | Amount |
|---|---|
| Level first clear | The level's `reward`: 20 to 100 coins (20/25 early, rising to 100 at level 100). All 100 levels total 5,125 coins. |
| Level replay | 50 % of the reward, minimum 5 |
| Endless loop level (first time that number is won) | Full reward |
| Daily Reward | See 10.1 |
| Daily Puzzle | Random reward, 50 coins when it rolls coins |
| Shop coin packs | Not live yet (no store, section 15) |

Levels opened with the editor test bar pay nothing.

**Spent on**: Hint and Freeze charges and the Starter Bundle.

### 9.2 Shop

The shop screen has a header (back left, title centre, coin balance right) and these offers, all driven by one
asset, `PowerUpShopConfig.asset`:

| Offer | Price | Gives |
|---|---|---|
| Hint Pack | 100 coins | 1 Hint |
| Freeze Pack | 150 coins | 1 Freeze |
| Starter Bundle | 250 coins | 3 Hints and 2 Freezes (can also include bonus coins and Rotates) |
| Coin Pack, row 1 / 2 / 3 | $0.99 / $1.99 / $4.99 | 500 / 1,200 / 3,000 coins (display only for now) |

Messages such as "Not enough coins" or "Hints added!" appear as a large centred toast for about two seconds.
The coin pack rows have an *Available* testing switch that grants coins for free; leave it off until a real store is
connected.

### 9.3 Power-up purchase popup (in a level)

Tapping Hint or Freeze with none left opens a small cat popup: the power-up icon, a quantity badge ("x1"), a green
coin button with the price, and a red X. If the player cannot afford it the button dims, shakes when tapped and the
popup says "Need N more". A purchase bursts sparkles and closes itself. The level is paused while it is open.

---

## 10. Retention and onboarding

### 10.1 Daily Reward

A 7-day cycle, claimable once per UTC day from the Daily Rewards icon (a red dot shows when one is waiting):

| Day | Reward |
|---|---|
| 1 | 100 coins |
| 2 | 2 Hints |
| 3 | 250 coins |
| 4 | 2 Freezes |
| 5 | 500 coins |
| 6 | 750 coins |
| 7 | 1,000 coins, 1 Hint and 1 Freeze |

After day 7 the cycle returns to day 1. The popup shows seven day cards, the current one highlighted, and a Claim
button. It opens automatically on start-up when a reward is waiting (`showDailyRewardOnStartup`). Values live in
`DailyRewardConfig.asset`.

### 10.2 Daily Puzzle

A bonus hard puzzle from the home screen (the calendar-cat icon next to Daily Rewards).

- **Which puzzle:** one of levels 51 to 100, chosen by the UTC date (days since 2026-01-01, modulo 50). Everyone
  gets the same puzzle on the same day.
- **Opens** after the player wins level 10 (an Inspector value).
- **It is a preview-style level:** it saves no stars and no progress.
- **Reward for winning:** one of three, chosen at random with equal chance: **50 coins**, **1 Hint** or **1 Freeze**.
  A small "Daily Reward!" popup shows the prize, then returns Home.
- **Locks again until the next day:** once won, the home icon turns greyscale with a countdown ("16:53:24"). Tapping
  it shows "Come back soon! Next puzzle in ...". It unlocks again at the next UTC midnight.
- **Locked by level:** tapping it earlier shows "Unlocked after level 10".
- A lost or abandoned attempt does not use up the day; the player can retry.

### 10.3 Teaching moments (every tutorial, in one place)

| Moment | Trigger | Shown once? |
|---|---|---|
| Level 1 hand tutorial | First level | Yes |
| "Move a placed cat" tip | First valid drop away from the solution | Yes |
| Timer tutorial | First timed level (11) | Yes |
| Hint unlock tutorial | Level 4 | Yes |
| Freeze unlock tutorial | Level 13 | Yes |
| "New!" mechanic cards | First level using padlocks, key cats, busy shelf, colour zones, spare cat, hearts, fixed cat, fog, or a drop limit | Yes, one per mechanic |
| Free hint toast | 3 fails on one level | Each time it triggers |

The "seen" flags are PlayerPrefs keys (`CatBlockPuzzle.Tutorial.*`, `CatBlockPuzzle.Mechanic.*.v1`).

---

## 11. Presentation

### 11.1 Audio

- **Music:** a gameplay track and a separate menu track, crossfaded when the screen changes. Music ducks under the
  win and fail stingers.
- **SFX** are looked up by id in `SoundBank.asset` (button, snap, wrong, win, coin, coin count tick, invalid
  placement and more). Win and fail have their own stingers.
- Music and Sound are separate switches in Settings.

### 11.2 Haptics

Named moments with their own feel: UI tap, selection (a cat lines up with a new valid spot), pickup, snap, combo, wrong
drop, lock open, heart lost, timer tick (last 5 s), level fail, level complete (a three-beat "ta-da").
Android uses composed primitives (API 30+), then predefined effects (API 29+), then amplitude pulses; iOS uses UIKit
feedback generators. Haptics follow the Vibration setting. The Android library must carry the `VIBRATE` permission;
this was fixed in the Gradle config on 2026-10-30. The first failed vibration is logged with the tag "[Haptics]".

### 11.3 UI structure

One portrait canvas, one scene (`GameScene`) holding every screen as a child of `MainUi`:

| Screen | Purpose |
|---|---|
| Loading | Splash with progress bar |
| Home | Logo, Play, Levels, Shop, Settings, coin bar (top left), Daily Rewards and Daily Puzzle (top right) |
| Levels | Paged grid of level cards. Header: back (left), "Levels" title, coin bar (right) |
| Shop | Header like Levels, then the offers in section 9.2 |
| Gameplay HUD | Coins, level label, timer and stars, hearts row, drop counter, combo badge, board, shelf, Hint and Freeze, pause |
| Pause | Resume, Restart, Home, Settings |
| Settings | Three rows: Music, Sound, Vibration (toggle switches) and Back |
| Daily Reward | 7-day card popup with Claim |
| Level Complete / Level Failed | Section 6.4 |
| Info popup | Small cat popup used for locked and daily-puzzle messages and the daily reward |
| No Internet | Offline notice with Retry and Okay |
| Collection, Rooms | Placeholders, switched off (section 15) |

**Header standard:** Levels and Shop share the same header layout (back button left, title centre, coin bar right),
built from layout groups so spacing is consistent.

**Safe areas:** a `ProductionUiSafeArea` fits the 1080x1920 authored layout inside the device's safe area while
backgrounds stay full bleed.

**Style:** warm pastel, rounded panels, a "cat peeking over the panel" popup motif, one rounded display font
(Fredoka), outlined white text on dark scenes and brown text on cream panels.

---

## 12. Settings and saved data

### 12.1 Player-facing settings

Music on/off, Sound on/off, Vibration on/off (all default on). A Reduced Motion flag is stored but has no toggle in
the current Settings screen.

### 12.2 What is saved

Everything is stored in PlayerPrefs (no server). Main groups:

| Data | Notes |
|---|---|
| Coins, current level, best stars per level | `PuzzleSaveKeys` |
| Chapter progress, first clears | Meta progress store |
| Power-up bag, Daily Reward state | One JSON blob, `CatBlockPuzzle.DailyReward.State.v1` |
| Daily Puzzle | `CatBlockPuzzle.DailyPuzzle.LastWonDay` (UTC day number) |
| Fail streaks | `CatBlockPuzzle.FailStreak.<level>` |
| Endless mode | `CatBlockPuzzle.Endless.*` |
| Tutorials seen | `CatBlockPuzzle.Tutorial.*`, `CatBlockPuzzle.Mechanic.*.v1` |
| Settings | `CatBlockPuzzle.Settings.*` |

All "once per day" features compare **UTC** days, so they reset at UTC midnight regardless of timezone.

---

## 13. Technical overview

### 13.1 Architecture

- `CatBlockPuzzleGame` (`Core/`) is a thin composition root. It builds single-purpose services in dependency order
  and exposes them through three interfaces: `IGameplayContext`, `IPuzzleConfig`, `ILevelFlow`. The project owner
  does not want partial classes; add a class per concern.
- `GameSystem` is the singleton that owns screens, pause, navigation and cross-feature settings.
- Key services: `BoardController`, `PieceDragController`, `TrayLayout`, `LevelFlowController` (load, win, fail),
  `LevelTimer`, `HintPresenter`, `CellLockController`, `CellZoneController`, `FogController`, `MistakeHearts`,
  `DropCounter`, `ComboTracker`, `CatMoodPresenter`, `PowerUpController`, `MetaProgressionController`,
  `PreparedLevelLoader`, `HapticsController`, `SoundManager`.
- UI scripts live in `UI/`. Editor-only authoring tools live in `EditorTools/` and `Editor/`.

### 13.2 Level pipeline

1. Levels are edited in `levels_100.json`.
2. The game reads a **baked copy** (`Content.asset`) plus 100 level prefabs, not the JSON directly.
3. After a JSON edit run **Cat Block Puzzle > Refresh Level Data**. Grid or piece-count changes need
   **Cat Block Puzzle > Rebake Level Prefabs (Keep Scene)**. Avoid a full "Prepare Game Scene": it regenerates the
   hand-authored hierarchy.
4. A pre-build check fails if baked data and JSON disagree.

### 13.3 Platforms and services

- **Analytics:** AVN Analytics via `CatAnalytics` / `GameAnalytics` / `AvnEvents`. Level funnel, economy, power-up use,
  tutorials, navigation and daily puzzle events. See `analytics-events.md` and `avn-event-coverage.md`.
- **Ads:** `RewardedAds` is the single entry point. With the AVN ads plugin it plays real rewarded ads; without it the
  reward is granted instantly so flows can be tested. No ad placement is currently visible in the UI.
- **In-app purchases:** none yet. Coin pack rows are display-only.
- **Android:** Gradle project with a small haptics library (`CatBlockPuzzleHaptics.androidlib`) that supplies the
  VIBRATE permission. Min SDK 25. **iOS:** minimum 15; a native `.mm` file provides haptics.

### 13.4 Testing and QA tools

- **Tests:** 124 edit-mode tests (rules, difficulty bands, level validity, scene wiring) all pass; play-mode tests
  exist but some expect an older UI hierarchy (see `Agent_Handover_2026-09-30.md`).
- **Testing switches on `GameSystem`:** *Unlock All Levels* (every level and the Daily Puzzle open) and
  *Show Level Test Buttons* (previous/next/go-to-level bar, no rewards). Both must be **off** for a release.
- Builds log a warning if a testing switch is on.

---

## 14. Tuning cheat sheet: where to change things

| I want to change... | Where |
|---|---|
| Which level the Hint / Freeze unlock at, free amount on unlock | Scene > `Gameplay Screen` > Gameplay HUD component > *Power Up Unlock Levels* |
| Hint / Freeze price and amount, Starter Bundle contents, coin pack rows | `Resources/CatBlockPuzzle/PowerUpShopConfig.asset` |
| 7-day daily reward | `Resources/CatBlockPuzzle/DailyRewardConfig.asset` |
| Daily Puzzle unlock level and rewards | Scene > `GameSystem` > *Daily Puzzle* |
| Freeze length (15 s) | Scene > game component > `freezeDurationSeconds` |
| Timer lengths, star thresholds, hint cost per difficulty | `Gameplay/DifficultyProfile.cs` |
| Untimed levels (1-10) | `DifficultyProfile.UntimedLevelCount` |
| Level boards, mechanics, rewards | `levels_100.json`, then Refresh Level Data |
| Drag feel | `portrait_layout_profile.asset` |
| Sounds and music | `Resources/Sound/SoundBank.asset` |
| Replay reward share (50 %) | `MetaProgressionController` (`ReplayRewardShare`, `MinimumReplayReward`) |
| Free hint after N fails (3) | `LevelFailStreak.FreeHintAfter` |
| Popup and toast timing | Component fields (for example the shop *Toast Seconds*) |

---

## 15. Known gaps and open questions

Honest list of what is unfinished or a judgement call:

1. **Collection and Rooms** (decorating cat rooms with unlocked items) are built in code and content but **switched
   off** (`collectionEnabled` / `roomsEnabled` = false). Their home buttons show a "coming soon" state.
2. **No real-money purchases.** The Coin Pack rows are display-only; a store SDK is not connected.
3. **Chapter gate is strict** (3 stars on every level of a chapter). Needs playtesting; it may block players who
   clear levels without perfect speed.
4. **Rotate** is built but disabled.
5. **Double Coins (watch an ad) and Skip Level** existed on the result screens and were removed from the new result
   layouts. The code remains if monetisation wants them back.
6. **Fail messages are baked art**: the Level Failed popup no longer explains "out of hearts" versus "time up".
7. **On-device testing is thin.** Haptic strength, popup spacing and animation feel were verified in the Editor, not
   on phones (primary test phone: Galaxy S22). The Android vibration fix needs a device check.
8. **Level fun is unplaytested at scale.** All 100 levels are proven solvable by the validator, but difficulty
   curve and lock/zone placement were generated and solver-checked, not hand-tuned by playtests.
9. **Reduced Motion has no Settings toggle.**
10. **The Editor writes Unity-version churn** into `ProjectSettings` and `packages-lock.json` when opened with a
    different Unity patch version than `ProjectVersion.txt`; do not commit those accidentally.

---

## 16. Appendix

### 16.1 Glossary

| Term | Meaning |
|---|---|
| Charge | One use of a power-up |
| Bag | The player's stored Hint and Freeze charges |
| Drop | One release of a cat onto the board that lands |
| Preview level | A level that saves no progress (daily puzzle, editor test bar) |
| UTC day | Calendar day in UTC; the unit for daily features |
| Attempt | One try at a level, from load to win, fail or quit |

### 16.2 Level economy snapshot

First-clear rewards by stretch: levels 1-10 pay 20-25; 11-25 pay 25-35; 26-45 pay 35-50; 46-65 pay 45-60;
66-85 pay 55-75; 86-100 pay 70-100. A player who clears all 100 levels once earns 5,125 coins before daily bonuses.

### 16.3 One-page flow

```
 Launch -> Loading -> Home
 Home --Play--> next level --win--> Level Complete --Next--> next level
                                  \-fail-> Level Failed --Retry--> same level
 Home --Levels--> level list --tap card--> level
 Home --Shop--> offers --buy--> toast
 Home --Daily Rewards--> 7-day popup --Claim--> bag / coins
 Home --Daily Puzzle--> (locked popup | countdown popup | puzzle --win--> reward popup --> Home)
 Level --Hint / Freeze--> effect (or purchase popup if empty, or info popup if locked)
 Level --Pause--> Resume | Restart | Home | Settings
```
