# UI scene authoring audit

Date: 2026-09-06

## Status and requirement

Source and saved-scene inspection completed. No gameplay code, scene, prefab, project settings, or existing user changes were modified during this audit. Unity compilation, live hierarchy inspection, screenshots, and Play Mode checks have not run.

The requested target is UI present in the scene before Play Mode. Runtime code may bind data, change text and sprites, toggle visibility, reposition existing pieces, and animate existing views. It must not create UI GameObjects, add missing components, instantiate UI prefabs, generate textures/sprites, or expand a pool during play. Prefab assets alone do not meet this requirement: their instances must already be saved in the scene.

## Findings

| Priority | Evidence | Implication |
| --- | --- | --- |
| High | `Assets/Scenes/CatBlockPuzzle.unity` contains Main Camera, Directional Light, and Global Volume; no saved UI or game controller. `CatBlockPuzzleGame.cs:209` creates the controller after scene load. | The UI cannot be edited or inspected in the saved scene. Bootstrap also applies to other scenes without a controller. |
| High | `CatBlockPuzzleGame.cs:220` creates visual resources and calls `BuildAudio`, `EnsureEventSystem`, and `BuildCanvas`. `CatBlockPuzzleGame.UI.cs:16` constructs the UI hierarchy and wires callbacks. | Converting only the canvas is insufficient; startup, references, event wiring, assets, and components must migrate together. |
| High | `CatBlockPuzzleGame.cs:296` destroys board/tray children on each level load. `CatBlockPuzzleGame.Board.cs:16` and `CatBlockPuzzleGame.Pieces.cs:16` rebuild them. | Reset and level navigation still generate UI even if the HUD becomes authored. Reuse serialized views instead. |
| High | `CatBlockPuzzleGame.Feedback.cs:636` creates an Image when the FX pool is empty. | The existing pool is not a fixed, scene-authored pool. Pool exhaustion needs a deliberate skip/recycle policy. |
| High | `CatBlockPuzzleGame.Sprites.cs` creates textures, sprites, atlas slices, and audio clips. `CatBlockPuzzleGame.Themes.cs` also creates sprites. | Bake assets in the editor and serialize references, including themed background and cat atlas slices. |
| Medium | `CatBlockPuzzleGame.UI.cs` uses one overlay canvas and sibling order for gameplay, effects, and menus. | A single canvas is not inherently incorrect, but there is no authored sorting contract. Define and verify ordering and input blocking for all UI states. |
| Medium | `SafeAreaFitter` updates its rect when screen dimensions change; board sizing is calculated in `BuildBoard`, and tray sizing in `ConfigureTrayForPieceCount`. | Safe-area changes can leave dependent fixed-size layout stale until another layout-triggering action. Reflow existing views when available space changes. |
| Medium | Board layout enforces minimum available dimensions; tray layout enforces minimum width; dialog sizes are fixed. | Small usable areas need measured fit checks; source inspection alone cannot establish clipping or overlap on devices. |
| Medium | Modal overlays are children of Safe Area. | Their dimming/input surface is limited to that rect. Prefer full-canvas backdrops with safe-area-aware dialog content. |
| Medium | KawaiiUI helpers include `FindOrCreate`, `Ensure` component addition, and sprite generation paths. | These are additional conversion targets if retained. Merely assigning some references will not eliminate fallback creation. |
| Medium | Portrait test waits for a runtime canvas at one 540×960 size and changes its render mode for capture without restoring it. | Tests need explicit scene loading, cleanup, broader size coverage, and checks that object identities remain stable. |

These are project-specific findings against the requested workflow, not a claim that runtime UI construction is universally wrong.

## Foundations to retain

- LevelManager validates the JSON level pack before converting it into gameplay definitions.
- Placement resolution, result calculation, and theme selection have separate logic and editor tests.
- SafeAreaFitter responds to screen and safe-area changes.
- CanvasScaler already uses Scale With Screen Size and a 1080×1920 reference resolution.
- Tray layout uses a HorizontalLayoutGroup and ScrollRect.
- Many decorative images explicitly disable raycast targeting.
- Existing interaction tests cover tray behavior, drag motion, and theme changes.

Tests were read, not executed. Partial class files organize the large controller but do not independently separate its responsibilities.

## Sequential conversion

1. **Audit — completed.** Trace startup, UI builders, resource generation, level resets, effects, and tests. Preserve existing uncommitted work.
2. **Bake and author the base UI — blocked on Unity access.** Import/bake current visual and audio assets through editor code. Save the controller, EventSystem, AudioSource, HUD, board frame, tray, actions, and all dialogs into the scene. Add explicit serialized bindings and validation. Replace bootstrap and runtime base-UI builders only when their replacements are saved and verified. Bind callbacks once; initialize toggles without firing preference callbacks.
3. **Author reusable gameplay views.** Current JSON contains 100 levels, with maximum 8 rows, 8 columns, and 8 pieces. The shape library has at most 5 cells per piece. Save 64 board-cell views and 8 piece-slot/group views with up to 5 cat-cell views each. Include preview/decorative children and drag components in those views. Configure active subsets for each level; reset state and parentage instead of destroying children. Validate capacity against every level during authoring/builds.
4. **Author a bounded FX pool.** Determine simultaneous usage from trail, hint, snap, and win effects. Save that many complete FX objects, recycle them, and skip optional particles if capacity is exhausted. No runtime expansion or component addition.
5. **Finalize anchoring, sorting, and input.** Keep full-screen background and modal backdrops outside safe-area content. Anchor header to top, actions/tray to bottom, and board to the remaining region. Recalculate layout on usable-size changes without creating views. Start with an explicit order such as background 0, gameplay/HUD 10, drag 20, non-interactive FX 30, modal 100. Add separate canvases only where useful for ordering/rebuild isolation, and verify drag coordinates if canvas structure changes. Decorations should not intercept input; modal surfaces should block underlying controls.
6. **Validate and remove obsolete runtime builders.** Load the authored scene explicitly in tests. Check serialized references, missing scripts, anchors, sprite assignments, sorting, and callbacks in Edit Mode. In Play Mode exercise reset, navigation through all 100 levels, drag/return, scrolling, hints, settings, fail, and win. Compare UI object/component identities before and after transitions, including inactive views. Test tall/narrow portrait, short portrait, tablet, and safe-area changes. Verify screenshots and Console output before declaring conversion complete.

Runtime gameplay data structures are distinct from generated Unity UI objects. Reading level data and updating existing views remains necessary for gameplay.

## Connection blocker

No native Unity MCP tools are exposed in this session. The documented Funplay endpoint at `http://127.0.0.1:8765/mcp` refused the tools/list request. The current package manifest lists `com.coplaydev.unity-mcp` version 10.0.0, whereas the local workflow guidance describes Funplay. Unity processes exist, but their active project, scene, and unsaved state could not be confirmed.

Connect the running editor for `E:/Unity/CatBlockPuzzle` through its installed MCP integration, or provide the active Funplay endpoint if that integration is running elsewhere. Resume at step 2 once editor inspection is available. The bootstrap has deliberately not been removed before a replacement scene exists, since doing so would leave the current game without its UI.
