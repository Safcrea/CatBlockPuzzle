# Reference UI review — 15 September 2026

Follow-up implementation is recorded in [ReferenceUI_Setup.md](ReferenceUI_Setup.md). The findings below describe the scene before that implementation.

Inspected the saved `Assets/_GameData/System/Scenes/GameScene.unity`, referenced sprites, Day 1 and Day 7 prefabs, and existing screen scripts against the supplied UI images. Unity MCP timed out, so this is a serialized asset review, not a live visual or Play Mode verification. The supplied Editor screenshot shows an unsaved scene; those changes are outside this review. No scene, prefab, or runtime script was changed.

The implementation target is `UI/Canvas/Reference UI`. All sprite asset GUIDs referenced by its non-prefab scene objects resolve to existing assets. This hierarchy contains artwork and Unity UI components, but no application screen controllers. Its buttons have no persistent click handlers. Existing HomeController, PauseMenu and SettingsMenu references still target the older `UI/Canvas/Safe Area` UI.

| Screen | Saved implementation | Remaining work |
| --- | --- | --- |
| Main Menu | Background, logo, coin artwork, Settings, Daily Reward, Collection, Shop, Rooms, Levels and Play buttons exist. | Connect navigation and coin value. Collection and Rooms destinations are absent from Reference UI; leave their destination references empty. |
| Pause | Panel, title and Home, Restart, Settings, Resume buttons exist under Gameplay Screen. | Assign these authored buttons to PauseMenu. Its current implementation generates a different runtime panel instead of binding this artwork. |
| Settings | Panel, title, Music, Sound, Vibration button images and Okay exist under Main Menu Screen. | Bind authored buttons and enabled/disabled sprites. SettingsMenu currently creates runtime rows with text states. Settings must be accessible while Main Menu is inactive. |
| Shop | Background/header, back button, coin artwork, category artwork, Starter Pack, three coin cards and TMP prices exist. | Connect navigation. Bag and Chest both show `$1.99`; reference prices are `$3.99` and `$4.99`. Starter Pack lacks the large gift illustration and tile-break item shown in the reference. Category artwork has no Button components. No shop purchase controller was found. Leave purchase configuration empty. |
| Daily Reward | Board/title, complete Day 1 prefab instance, five background-only Day 2–6 cards, and Day 7 prefab instance exist. | The BG object is inactive, hiding the board too. Day 2–6 need their day labels, coin icons, amounts and claim controls. No reward controller or close navigation was found. Leave missing configuration empty. |
| Level Selection | Background, title, coin artwork and a Back image exist. | Chapter cards, portraits, level buttons, locks, stars, coin text and Play are absent. Back has no Button component. This remains unfinished. |
| Gameplay | Board, pieces panel, LevelRoot, coin artwork, level plaque, timer artwork, star artwork and Pause button exist. | Background, dynamic level/coin/timer labels, completed star group, booster buttons and integration with the puzzle board/tray are unfinished. Existing GameplayHudView still targets the older Gameplay Screen. |
| No Internet | No screen found under Reference UI. | Leave empty. |

## Recommended next pass

1. Save the current Editor scene and restore a Unity MCP connection so the visible objects can be inspected before edits.
2. Use the existing Reference UI as the authored UI. Move shared Settings and Pause overlays to sibling screen roots under Reference UI so visibility does not depend on another screen being active. Keep the hierarchy within the project's safe-area handling.
3. Bind the existing HomeController, PauseMenu and SettingsMenu to these authored screens and buttons. Stop those controllers from generating substitute panels when an authored view is assigned. Set settings state sprites from preferences.
4. Connect Main Menu → Shop, Daily Reward and Level Selection, plus existing back/close controls. Leave unresolved Collection, Rooms, No Internet and purchase/reward configuration empty.
5. Correct Shop's two price labels and enable Daily Reward's BG; complete its Day 2–6 cards when ready.
6. Complete Level Selection and Gameplay next, then bind their data and puzzle presentation. Do not run the older scene-generation tooling over the new artwork without adapting its authoring paths.
7. Verify one-screen-at-a-time visibility, button clicks, settings persistence, pause/resume and layout at 1080×1920 plus a taller phone aspect ratio in Unity.

Saved state currently has Level Selection active, other top-level Reference UI screens inactive, and the older Safe Area UI active. A live review is required to confirm what is actually visible and whether the two UI branches overlap.
