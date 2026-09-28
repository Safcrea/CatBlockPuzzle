# Gameplay validation — 28 September 2026

Status: tutorial change validated; broader project validation has unresolved failures.

Unity 6000.3.14f1, active target Android. Checks used the running Editor through Unity CLI. No Android player build or physical-device test was performed.

## Results

| Check | Passed | Failed |
| --- | ---: | ---: |
| CatBlockPuzzle Play Mode tests | 21 | 12 |
| CatBlockPuzzle Edit Mode tests | 50 | 3 |
| Total automated tests | 71 | 15 |
| Level prefab validation | 100 | 0 |

Compilation completed with no errors. All eight tutorial tests passed. Screenshots verified Handpointer artwork, green targets behind dragged cats, settled cats, and reduced-motion presentation. Tutorial completion, skip, pause, replay, target alignment, and the real drag destination passed.

Other passing runtime checks cover level navigation and boundaries, board layout on later levels, power-up purchase costs and inventory, insufficient coins, freeze expiry and restart, power-up unlock tutorials, timer stars, ordinary pause transitions, and completion/failure screens.

## Remaining failures

- Eight older Play Mode tests fail during setup while loading the legacy `CatBlockPuzzle` scene. The current enabled scenes are Loading and GameScene. These tests never reached their gameplay assertions.
- The main-menu layout test searches for `Reference UI`, while the current hierarchy uses `MainUi`; its lookup throws.
- The daily-reward test expects two successful claim notifications from consecutive clicks. The current implementation claims once and rejects the second claim. This expectation is stale; it does not demonstrate a duplicate-reward bug.
- The Coming Soon popup test fails its assertion that the popup is inactive 0.25 seconds after Close. Runtime closure timing still needs investigation.
- The combined selection/restart/pause test fails the timer-running assertion after resuming an immediately paused restart. A direct smoke check was interrupted when Play Mode exited; the root cause remains unresolved.
- Edit Mode preview throws `Grid gaps exceed the available space` while previewing a level. Preview layout remains unresolved.
- Serialized-event validation expects one persistent Settings listener but finds zero. Runtime code binds listeners, so this assertion alone does not establish a broken Settings button.
- Saved GameScene UI validation encounters a missing serialized daily-reward property. The test references an older UI schema.

These broader failures are outside the new tutorial-hand and target-layer code. Their pre-change runtime status was not measured, so unresolved failures are not labeled proven regressions or proven pre-existing runtime bugs.

## Test maintenance performed

Tutorial tests now explicitly open gameplay before checking visibility, wait for the intentional success celebration before picking up the next cat, and derive an invalid destination from each piece's actual solution. Added reduced-motion, pause, and replay coverage. No unrelated production behavior was changed to make tests pass.

## Evidence

- [Play Mode results](playmode-results.json)
- [Edit Mode results](editmode-results.json)
- [Level prefab and sprite readback](asset-readback.json)
- [Hand and tutorial](../../TestArtifacts/level-one-tutorial-placement.png)
- [Dragged cat above green targets](../../TestArtifacts/tutorial-cat-over-target.png)
- [Settled cats](../../TestArtifacts/tutorial-cat-placed.png)
- [Reduced motion](../../TestArtifacts/tutorial-reduced-motion.png)

Generated font-cache changes and the overwritten tracked gameplay screenshot were restored. Unity was left in Edit Mode on GameScene. The next useful investigation is the restart/pause timer case, followed by editor-preview sizing and the obsolete test assumptions.
