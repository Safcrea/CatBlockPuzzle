# Prepared CatBlockPuzzle scene

Open `Assets/_GameData/System/Scenes/CatBlockPuzzle.unity` and press Play. The saved `GameScene` hierarchy contains the UI screens, 10 rooms, persistent button events, and 256 reusable effect images. Its `LevelRoot` is intentionally empty in the saved scene.

All 100 prebuilt levels (529 pieces total) live in `Assets/_GameData/System/Resources/CatBlockPuzzle/Levels/Level_001.prefab` through `Level_100.prefab`. The content catalog stores their Resource addresses, not direct prefab references. Runtime loads and instantiates only the current level. Switching levels deactivates/removes the previous instance, destroys it at the end of the frame, and schedules unused-asset cleanup. Retrying reuses the current instance without loading or instantiating again. Rapid switches are supported.

Each prefab retains its internal board, tray, piece, and input references. The controller is injected into the prefab's saved input references once when it is instantiated, without finding objects by name. Gameplay still updates state, layout, labels, artwork selection, and animations; it does not construct UI or individual board/piece objects at runtime.

The Canvas uses Screen Space - Camera, the assigned Main Camera, plane distance 3, 1080 x 1920 reference resolution, match 0.5, and UI sorting order 1.

- `Cat Block Puzzle > Validate Game Scene`: checks bindings, missing scripts, and source fingerprint. Builds also validate the saved scene.
- The UI tuning window's `Preview Level Prefab` loads a temporary, unsaved prefab preview in Edit Mode. This preview is excluded from scene serialization.
- Edit level visuals inside the individual prefab. Save the prefab to keep your changes; the scene does not need all levels placed into it.
- `Cat Block Puzzle > Convert Scene Levels to Prefabs` is a one-time migration for the previous all-levels-in-scene version. It backs up the scene, exports and validates every level, then removes only the exported scene instances.
- `Cat Block Puzzle > Prepare Game Scene`: explicitly regenerates the UI hierarchy, level prefabs, and assets after changing level/source content. It asks for confirmation when a prepared hierarchy exists. It overwrites manual edits **inside GameScene and the generated level prefabs**; keep custom changes in authoring code or retain backups. Other scene roots are preserved. Scene backups are stored in `SceneBackups/` outside Assets.

Generated sprites, audio, and serialized content live in `Assets/_GameData/Art/UI/CatPuzzleScene/`. Keep their `.meta` files and the new script `.meta` files in version control. The portrait layout asset retains the existing tuning values.

Tests are in `Assets/Tests/Editor` and `Assets/Tests/PlayMode`. Play Mode fixtures temporarily isolate and then restore the game's existing PlayerPrefs. Screenshot artifacts are written to `TestArtifacts/`.

The saved scene decreased from about 50 MB to 3.7 MB of text YAML. Level data and shared UI/art/room assets remain resident by design; this is not an all-assets streaming system. Scene YAML size is not a measurement of runtime RAM. Profile loading, frame time, and memory on target phones before claiming a device-performance improvement.
