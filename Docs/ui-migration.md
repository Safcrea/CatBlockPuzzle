# Production UI integration

The pre-migration workspace is saved in commit `fee0daf`.

## Installed screens

`GameScene` now uses the supplied Main Menu, Level Selection, Shop, Pause, Settings, No Internet, and Daily Rewards layouts. `Loading` uses the supplied loading layout. The matching old screen contents were removed; the existing root objects and game controller references were retained.

The supplied visual language is also extended across gameplay, complete/fail screens, room and collection pages, story and room completion overlays, tutorials, power-up purchases, countdowns and gameplay toasts. Gameplay uses the forest background, cream and honey panels, new board/tray tiles, matching power-up icons and Fredoka labels. Complete/fail screens retain the overlapping reveals and button fades.

`GameUiTheme.asset` in Resources is the shared runtime theme. Generated assets live under `CatBlock_UI_New/Extended`; these are code-authored UI shapes and icons. All 100 level prefabs and shared popup prefabs use the new assets. Playable cat art and the tray skin were moved into `CatBlock_UI_New` with their GUIDs preserved. The retired `Assets/Art/CatBlockPuzzleUI` folder was removed after its dependencies were migrated.

## Game bindings

- Home: Play, Levels, Shop, Settings and coin plus. Daily Rewards and Daily Puzzle shortcuts retain access to existing features.
- Levels: 12 cards per page, arrows and horizontal swipes, all 100 campaign levels, existing level/chapter unlock checks and earned stars. A card opens its level directly.
- Settings: music, sound and vibration use existing preferences. Notification preference is saved; native notification scheduling is not implemented by this UI integration. Language remains the supplied static English artwork.
- Daily Rewards: the existing seven-day configuration and claim persistence are retained. Card states, claimed marks, countdown, quantities and the single Claim button are connected. Freeze/coin rewards replace the delivery's Undo/Shuffle presentation where needed to match actual rewards.
- Shop: the Hint pack uses the existing coin prices, inventory and saved balance. Undo, skins, bundles and cash coin packs show Soon because the prior shop had no purchase handlers or product IDs. No new payment system is included.
- No Internet: Okay closes the popup; Retry closes it when Unity reports network reachability.
- Loading: the existing timed progress slider drives the new gel artwork.
- Gameplay: forest background, gold header and board, cream tray and cards, new Hint/Freeze/Rotate buttons, timer frozen state, progress stars and debug navigation controls. Unlocked power-ups centre themselves in the row.
- Results: new cream panels, cat portraits, native titles, reward board, stars and green/gold action buttons. Existing result and meta progression button references are preserved.
- Rooms, story and collection: shared panel, button and typography styling; furniture and character game art is retained.
- Runtime overlays: purchase, mechanic and unlock tutorial prefabs, fallback UI, toast and onboarding surfaces use the shared theme. Later level loads and level rebakes apply the same theme.

The supplied `CatBlock_UI_New` folder remains intact, including its importer and layout JSON. Saved scene edits contain the game bindings. Reconstructing a layout with the generic importer creates a separate, unbound composition.

Content fits the safe area while backgrounds cover the screen. Existing Fredoka font assets are reused.

## Handover

Scripts were compiled through Unity CLI. Play Mode, interaction and device testing are left to the user as requested. Start with `GameScene` and check the menu navigation, paged levels, settings, shop, rewards and loading scene on the intended aspect ratios.
