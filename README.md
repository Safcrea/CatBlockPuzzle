# CatBlock

> A cozy mobile block-placement puzzle where players arrange groups of colorful cat tiles to complete each board before time runs out.

<p align="center">
  <img src="docs/media/CatBlock_prototype.gif" alt="CatBlock gameplay prototype" width="360" />
</p>

<p align="center">
  <strong>Relaxing presentation · Quick puzzle sessions · Cat-themed pieces · Mobile-first controls</strong>
</p>

---

## About the Game

**CatBlock** is a portrait-mode puzzle game set inside warm, handcrafted rooms. Each level presents a board with a unique arrangement of empty cells and a selection of cat-shaped tile groups. The player must place every group correctly to complete the board.

The game combines simple drag-and-drop interaction with light time pressure, star-based scoring, hints, resets, collectible coins, and themed level environments.

## Core Gameplay

1. Choose a cat-piece group from the tray at the bottom of the screen.
2. Drag it onto a valid group of empty board cells.
3. Continue placing pieces until the board is complete.
4. Finish quickly and efficiently to earn up to three stars.
5. Use **Hint** when stuck or **Reset** to restart the current puzzle.

## Features

- Cozy pastel visual style
- Portrait mobile layout
- Simple drag-and-drop controls
- Hand-authored puzzle boards
- Multiple cat colors and piece formations
- Timed level objectives
- Three-star performance rating
- Hint and reset systems
- Coin-based progression hooks
- Themed rooms, chapters, and level labels
- Lightweight interface designed for short play sessions

## Current Prototype UI

The prototype currently demonstrates:

- Level and chapter information
- Coin counter
- Pause and settings controls
- Countdown timer
- Three-star target display
- Main puzzle board
- Four selectable cat-piece groups
- Hint, reset, previous, and next controls

## Player Controls

| Action | Mobile | Editor / Desktop |
|---|---|---|
| Select a piece | Tap | Left click |
| Move a piece | Drag | Click and drag |
| Place a piece | Release over valid cells | Release over valid cells |
| Cancel placement | Release outside the board | Release outside the board |
| Request help | Tap **Hint** | Click **Hint** |
| Restart the puzzle | Tap **Reset** | Click **Reset** |

## Win and Scoring Rules

A level is completed when all required board cells are filled with valid cat pieces.

Suggested star thresholds:

- **3 Stars:** Complete within the best-time target
- **2 Stars:** Complete within the standard-time target
- **1 Star:** Complete before the timer expires

The exact timing thresholds should remain configurable per level rather than being hard-coded.

## Level Data

Each level should be data-driven so designers can create and balance puzzles without changing gameplay code.

Recommended level data fields:

```text
Level ID
Chapter / Room ID
Board width and height
Active board cells
Blocked cells
Available piece formations
Piece colors
Time limit
1-star, 2-star, and 3-star thresholds
Hint sequence
Coin reward
Background theme
Difficulty rating
```

For Unity, this data can be stored using **ScriptableObjects**, JSON, or a hybrid system. ScriptableObjects are convenient for editor workflows, while JSON is useful for remote balancing and content updates.

## Recommended Unity Project Structure

```text
Assets/
├── Art/
│   ├── Backgrounds/
│   ├── Cats/
│   ├── UI/
│   └── VFX/
├── Audio/
│   ├── Music/
│   └── SFX/
├── Materials/
├── Prefabs/
│   ├── Board/
│   ├── Pieces/
│   └── UI/
├── Scenes/
│   ├── Boot.unity
│   ├── MainMenu.unity
│   └── Gameplay.unity
├── Scripts/
│   ├── Core/
│   ├── Gameplay/
│   ├── Levels/
│   ├── UI/
│   ├── Audio/
│   └── SaveSystem/
├── ScriptableObjects/
│   ├── Levels/
│   ├── Themes/
│   └── Economy/
└── Settings/
```

## Suggested Gameplay Architecture

| System | Responsibility |
|---|---|
| `GameManager` | Controls level state, pause, win, loss, and restart flow |
| `LevelManager` | Loads level data and creates the board |
| `BoardController` | Tracks cells, occupancy, and valid placement positions |
| `PieceController` | Handles drag, preview, snap, placement, and return animation |
| `PlacementValidator` | Checks bounds, active cells, overlap, and completion state |
| `TimerController` | Runs the countdown and reports star thresholds |
| `HintController` | Finds and previews a valid next placement |
| `ScoreController` | Calculates stars, coins, and completion rewards |
| `SaveManager` | Stores unlocked levels, stars, coins, and settings |
| `AudioManager` | Plays music, UI feedback, placement sounds, and success cues |
| `UIController` | Updates timer, stars, buttons, chapter label, and level number |

## Game Feel and Feedback

To make each placement satisfying, the game should include:

- Soft snap animation when a piece reaches a valid position
- Clear red or muted feedback for invalid placement
- Gentle board-cell highlight during dragging
- Small scale bounce after successful placement
- Cat pop, sparkle, or paw-print particles
- Light haptic feedback on valid placement
- Stronger haptic feedback on level completion
- Short celebratory animation when three stars are earned
- Layered placement, success, button, and reward sounds

## Getting Started

### Requirements

- Unity Hub
- The Unity version listed in `ProjectSettings/ProjectVersion.txt`
- Android Build Support and/or iOS Build Support for mobile builds
- Git LFS if the repository contains large art, audio, or video files

### Installation

```bash
git clone <repository-url>
cd <repository-folder>
```

Then:

1. Open Unity Hub.
2. Select **Add project from disk**.
3. Choose the cloned project folder.
4. Open the project using the Unity version recorded in the project settings.
5. Open the boot, menu, or gameplay scene from `Assets/Scenes/`.
6. Press **Play**.

## Mobile Build

### Android

1. Open **File → Build Profiles** or **Build Settings**.
2. Select **Android**.
3. Switch the active platform.
4. Confirm package name, orientation, icons, and signing settings.
5. Build an APK or App Bundle.

### iOS

1. Open **File → Build Profiles** or **Build Settings**.
2. Select **iOS**.
3. Switch the active platform.
4. Build the Xcode project.
5. Configure signing and deploy from Xcode.

## Recommended Technical Rules

- Use object pooling for reusable VFX and temporary UI feedback.
- Avoid runtime scene-wide object searches during gameplay.
- Keep level configuration separate from presentation prefabs.
- Validate every level in the editor before adding it to production.
- Keep board dimensions and piece formations editor-configurable.
- Save progress after every completed level and economy change.
- Test layouts across common phone aspect ratios and safe areas.
- Keep essential gameplay readable without relying only on color.

## Roadmap

- [ ] Complete drag, snap, and placement validation
- [ ] Add editor-driven level creation tools
- [ ] Add scalable hint generation
- [ ] Add star and reward balancing
- [ ] Add chapter and room progression
- [ ] Add save/load support
- [ ] Add sound, particles, animation, and haptics
- [ ] Add onboarding and tutorial levels
- [ ] Add accessibility and safe-area support
- [ ] Add Android and iOS production builds
- [ ] Add analytics and difficulty-tuning events

## Contributing

1. Create a branch from the main development branch.
2. Keep each change focused on one feature or fix.
3. Test the affected levels and mobile layouts.
4. Use clear commit messages.
5. Open a pull request with screenshots or recordings for visual changes.

Example branch names:

```text
feature/drag-placement
feature/level-editor
fix/piece-snap-offset
art/cozy-room-theme-02
```

## Development Status

CatBlock is currently a **prototype / work in progress**. Gameplay rules, visual assets, level balance, economy, and progression may change during development.

## License

Add the project license here before publishing the repository.

For a private commercial project, replace this section with an appropriate copyright notice, for example:

```text
Copyright © 2026 <Studio Name>. All rights reserved.
```

---

<p align="center">
  Made with care for cozy puzzle-game players.
</p>
