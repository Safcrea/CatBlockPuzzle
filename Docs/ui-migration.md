# Production UI integration

The pre-migration workspace is saved in commit `fee0daf`.

## Installed screens

`GameScene` now uses the supplied Main Menu, Level Selection, Shop, Pause, Settings, No Internet, and Daily Rewards layouts. `Loading` uses the supplied loading layout. The matching old screen contents were removed; the existing root objects and game controller references were retained.

The delivery excludes gameplay and result artwork. Those screens and their shared assets remain in use, including the complete/fail fades and tray return fix.

## Game bindings

- Home: Play, Levels, Shop, Settings and coin plus. Daily Rewards and Daily Puzzle shortcuts retain access to existing features.
- Levels: 12 cards per page, arrows and horizontal swipes, all 100 campaign levels, existing level/chapter unlock checks and earned stars. A card opens its level directly.
- Settings: music, sound and vibration use existing preferences. Notification preference is saved; native notification scheduling is not implemented by this UI integration. Language remains the supplied static English artwork.
- Daily Rewards: the existing seven-day configuration and claim persistence are retained. Card states, claimed marks, countdown, quantities and the single Claim button are connected. Freeze/coin rewards replace the delivery's Undo/Shuffle presentation where needed to match actual rewards.
- Shop: the Hint pack uses the existing coin prices, inventory and saved balance. Undo, skins, bundles and cash coin packs show Soon because the prior shop had no purchase handlers or product IDs. No new payment system is included.
- No Internet: Okay closes the popup; Retry closes it when Unity reports network reachability.
- Loading: the existing timed progress slider drives the new gel artwork.

The supplied `CatBlock_UI_New` folder remains intact, including its importer and layout JSON. Saved scene edits contain the game bindings. Reconstructing a layout with the generic importer creates a separate, unbound composition.

Content fits the safe area while backgrounds cover the screen. Existing Fredoka font assets are reused.

## Handover

Scripts were compiled through Unity CLI. Play Mode, interaction and device testing are left to the user as requested. Start with `GameScene` and check the menu navigation, paged levels, settings, shop, rewards and loading scene on the intended aspect ratios.
