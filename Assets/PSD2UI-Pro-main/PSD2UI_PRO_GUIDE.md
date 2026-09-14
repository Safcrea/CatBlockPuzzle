# PSD2UI Pro — Quick Guide

## 1. Export from Photoshop

1. Open your PSD in Photoshop.
2. `File > Scripts > Browse…` → select `Photoshop/psd2ui_export.jsx`.
3. Pick an output folder.
4. Choose text mode when prompted:
   - **Export as Images** — rasterizes text to PNG (pixel-perfect, not editable).
   - **Export as Text** — stores text as metadata for editable TextMeshPro in Unity.
5. Wait for the progress bar to finish. The folder now contains individual PNGs and a `layout.json`.

## 2. Import in Unity

1. `Window > PSD2UI Pro`.
2. Point **Layout JSON** at the exported `layout.json` (or drag-drop it onto the window).
3. Adjust settings if needed, then click **Import**.

## Settings

| Setting | Default | Notes |
|---------|---------|-------|
| Layout JSON | *(required)* | Path to `layout.json` from the Photoshop export. |
| Import Sprites To | `Assets/PSD2UI Imports` | A subfolder is created per layout. |
| Target Canvas | *(none)* | Leave empty to auto-create a canvas sized to the PSD. |
| Create Text Objects | On | Imports text layers as TextMeshProUGUI components. |
| Respect Visibility | On | Hidden Photoshop layers become inactive GameObjects. |
| Copy Images Into Project | On | Copies PNGs into your Assets folder. Keep enabled. |
| Layer Pivot | Center | 9 presets available (TopLeft, BottomRight, etc.). |

## What gets created

- **Canvas** — auto-created with `ScaleWithScreenSize` matching the PSD resolution, or parented to your target canvas.
- **Image layers** → `GameObject` + `Image` + imported Sprite. Opacity preserved, Raycast Target off.
- **Text layers** → `GameObject` + `TextMeshProUGUI` with the original text content (when exported as metadata).
- **Groups** → empty parent `GameObject`. Gets a `CanvasGroup` when opacity < 100%.
- **Hidden layers** → imported as inactive GameObjects (when Respect Visibility is on).
- **Undo** — the entire import is a single Ctrl+Z operation.

## Troubleshooting

| Problem | Fix |
|---------|-----|
| "Layout JSON path is invalid" | Check the file exists at that path. |
| Missing source image warnings | Make sure all PNGs are next to the JSON. |
| Text not appearing | Enable "Create Text Objects" and re-export with "Export as Text". |
| Images not showing | Check Console for errors; verify sprite type is *Sprite (2D and UI)*. |

## Limitations

- Layer effects, masks, and smart objects are baked into PNGs during export.
- Animations are not imported.
- TMP text uses default font/size/color — customize after import.
- Requires the **TextMeshPro** package (bundled with Unity 2018.3+).

---

*PSD2UI Pro v1.0 — Unity 2021.3+*
