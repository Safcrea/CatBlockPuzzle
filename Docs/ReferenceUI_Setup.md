# Reference UI game setup

GameScene now runs through Canvas/Reference UI. Game System coordinates navigation and pause state; HomeController, PauseMenu, SettingsMenu, LevelSelectionScreen and the separate result controllers own their screen references. CatBlockPuzzleGame owns puzzle state and exposes gameplay/progression actions; GameplayHudView owns its scene presentation references.

## Implemented

- Main Menu starts without loading a hidden puzzle. Play loads the recommended available level; Levels opens the new selector. Selecting an unlocked tile and pressing Play starts that level.
- The scrolling chapter board contains all 10 chapters and 100 levels, with ten levels in each chapter. It reads saved stars and preserves sequential level locks and the existing room-furnishing chapter unlock rules.
- The new gameplay board and tray receive the existing level prefab. Only one level prefab is instantiated; Restart reuses it. Old prefab board/tray frames are hidden in favor of the authored artwork.
- Gameplay has dynamic level, objective, coin, timer, combo and three-star displays. Chapter backgrounds fill the canvas, while controls remain in the safe area.
- Pause, Settings, Restart, Home and separate completion/failure screens use Game System. Home suspends the level timer and cancels active interaction. Pausing during the opening animation also keeps the timer stopped.
- Rooms opens the existing room-furnishing progression inside the new Rooms page, preserving saved decorations and chapter gates. The older Canvas/Safe Area UI remains inactive.
- Settings keeps independent music/SFX preferences and haptics. Gameplay sound reads SoundManager's current state rather than an outdated cached toggle.
- No Internet is available through GameSystem.ShowNoInternet(); its confirmation button dismisses the overlay. Offline puzzle play does not require a connection.

## Deliberately unconfigured

Existing Shop product IDs/purchase callbacks and Daily Reward claim callbacks remain empty. Collection content is still empty. Hint is connected to the existing hint mechanic. Time Freeze and Tile Break artwork is present, with no guessed inventory, purchase or effect rules attached.

## Authoring and validation

ReferenceGameUiPreparation builds the saved UI in Edit Mode, with Undo support. It does not build UI at runtime. It accepts inspected scene instance IDs and validates dependencies before saving GameScene.

ReferenceGameUiTests checks the saved scene, active UI roots, authored HUD and all 100 level slots. ReferenceGameUiPlayModeTests covers selection/locks, pause/settings/resume, restart reuse, hidden timer suspension and separate completion/failure screens. Its fixture backs up and restores the player's progress, stars and audio/settings preferences.
