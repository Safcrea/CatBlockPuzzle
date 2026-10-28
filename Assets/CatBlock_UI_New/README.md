# Cat Blocks UI — Unity developer delivery

This package contains 77 production PNGs, eight screen layouts, 80 stable asset IDs,
45 blank editable text fields, eight reference screens, nine assembled previews,
and the complete Unity importer/runtime scripts. The current Daily Rewards button
and Shop Coin Pack corrections are included. Gameplay assets are outside this delivery.

## Install and reconstruct

1. Extract the ZIP. Copy the complete `CatBlock_UI_New` folder into your Unity
   project's `Assets` folder. Keep its internal folder structure together.
2. Use Unity 6 with uGUI and TextMeshPro. Compilation was checked against
   Unity 6000.3.17f1. Import TMP Essential Resources if needed and assign a suitable
   TMP font; Fredoka Bold is the intended visual style, but no font is bundled.
3. Install only one copy of the `CatBlocksUI` C# scripts. If an older version is
   already installed, replace its scripts with this delivery instead of duplicating them.
4. Open the destination scene in Edit Mode, outside Prefab Mode. Choose
   **Cat Blocks > UI > Reconstruct From JSON** and select one of
   `layouts/<screen>/layout.json` inside the imported folder.
5. The importer resolves the PNGs and creates the Canvas, scaler, hierarchy,
   Images, Buttons, Toggles and blank TMP fields. Reconstructing the same source
   replaces its previous instance in the active scene. Save your scene afterward.
6. Popup roots start inactive. Activate the desired popup over the current screen;
   its Canvas uses sorting order 100 and includes a separate runtime dimmer.

For font selection, native anchoring controls or reusable prefab saving, open
**Tools > UI Layout Importer**, choose the complete `CatBlock_UI_New` folder as
the UI folder, scan it and choose the required layout. Assembling from this generic
window creates a new instance; use the production menu above for replacement behavior.

## Contents

- `UI/`: final PNGs in btns, bars, icons, titles, popups, BG, cards and branding.
- `layouts/`: all eight JSON layouts. Paths are relative to this package root.
- `unity/Editor/`: asset resolution and reconstruction tools.
- `unity/Runtime/`: layout tags, text bindings and toggle state components.
- `references/`: original supplied screen images for visual comparison.
- `previews/`: assembled screen previews and `All-Screens.png`. Popup preview
  backgrounds are inspection backdrops, not additional runtime background assets.
- `manifests/global_asset_registry.json`: asset IDs, PNG paths, sizes and hashes.
- `manifests/component_sizes.json`: measured component/button geometry.
- `manifests/layout.schema.json`: layout contract.
- `manifests/validation-status.json`: concise validation status and remaining app checks.
- `screen-index.json`, `asset-library.json`, `dynamic-bindings.json`: layout entry
  points, compatibility registry and editable text binding keys.

## Runtime integration

Set dynamic values using the generated `CatBlocksUI.ScreenLayout` component:

```csharp
screenLayout.SetText("economy.coins", "320");
screenLayout.SetToggle("settings.music", true);
```

Use `dynamic-bindings.json` for all supplied text keys. Prices, quantities and day/
level labels intentionally start blank. Connect Button events, navigation,
purchases, persistence, reward state selection and safe-area behavior to your game.
The importer assembles the designed default composition; it does not implement game rules.

Daily card `state_variants` identifies the backing assets for Default, Claimed,
Current and Locked. Select the backing and enable the separate claimed tick and
reward/text layers from your rewards controller. Settings bars and knobs remain
separate; the supplied toggle controller swaps their state and knob position.

Daily Rewards uses a taller, vertically sliced panel and aspect-preserved Claim/
Later buttons. Keep its declared Sprite borders and pixels-per-unit behavior;
the importer configures them. Coin Pack rows use their own compact green button
and independent round coin icons. Avoid nonuniform scaling of these sprites.

The optional Shop Back/plus controls have inactive off-canvas placements because
their positions were absent in the reference. Decide their positions before enabling.

## Layout editing and validation

The same layout file contains the V3.2 `objects` projection and the compatibility
`assets`/`elements` projection used by the included importer. Keep their geometry
in sync. Generic importer JSON overrides use the compatibility contract and do
not regenerate the V3.2 projection. Saved prefabs retain Unity component/event edits.

The reference canvas is 1080 x 1920 with CanvasScaler match 0.5. Test aspect ratios
and device safe areas during integration. Backgrounds/dimmers cover the full screen;
interactive content needs your application's safe-area handling.

Unity C# compilation and 1,718 source-package offline checks passed. The delivery
ZIP is checked after extraction for asset hashes, required files and layout links.
Native Unity rendering, interaction, prefab behavior and device behavior still need
verification in the developer's project. Previews are offline composites.
