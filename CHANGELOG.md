# CatBlockPuzzle Changelog

## Version 0.1.0 - 2026-07-08 19:22:42 +05:00

### Touch Input Responsiveness

- Changed bottom cat pieces to start dragging immediately on pointer down instead of waiting for Unity's drag threshold.
- Disabled the UI drag threshold for cat piece interactions so short touch movement begins tracking faster.
- Added pointer-up handling so a piece drag can finish correctly even when the finger does not move far enough to trigger a normal drag end event.
- Added active pointer tracking so only the same finger or pointer that started a drag can move or release that piece.
- Removed cat-piece horizontal swipe detection that previously converted piece drags into tray scrolling.
- Disabled tray `ScrollRect` movement while a cat piece is actively being dragged, then re-enabled it after drop, cancel, or fail cleanup.
- Kept tray scrolling available from the tray background or empty tray area.
- Reduced touch visual lift from `70f` to `42f` so dragged pieces stay closer to the finger.
- Reduced mouse visual lift from `28f` to `22f`.
- Replaced delayed drag smoothing with direct target positioning so the dragged piece no longer lags behind the finger.
- Reduced drag tilt intensity from `15f` to `8f` and increased tilt response speed for a tighter feel.
- Ensured fail-state drag cleanup restores tray scrolling.

### Files Changed

- `Assets/Scripts/CatBlockPuzzle/CatBlockPuzzleGame.cs`
- `Assets/Scripts/CatBlockPuzzle/CatBlockPuzzleGame.Drag.cs`
- `Assets/Scripts/CatBlockPuzzle/CatBlockPuzzleGame.Pieces.cs`
- `Assets/Scripts/CatBlockPuzzle/CatBlockPuzzleGame.RuntimeState.cs`
- `Assets/Scripts/CatBlockPuzzle/CatBlockPuzzleGame.Feedback.cs`

### Verification

- `dotnet build Assembly-CSharp.csproj` completed successfully with `0` warnings and `0` errors.
- Unity MCP live Editor validation was not available because the local MCP endpoint timed out.
