#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static CatBlockPuzzle.RectLayout;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Edit-mode scene preparation: bakes the content and visual assets, builds the full
    /// gameplay/meta hierarchy through the authoring classes, exports the 100 level prefabs,
    /// and previews a level prefab in the saved scene.
    /// </summary>
    internal static class CatPuzzleScenePreparer
    {
        public const string PreparedAssetFolder = "Assets/_GameData/Art/UI/CatPuzzleScene";
        private const string LevelsSourcePath = "Assets/_GameData/System/Resources/CatBlockPuzzle/levels_100.json";
        private const string RoomsSourcePath = "Assets/_GameData/System/Resources/CatBlockPuzzle/meta_chapters.json";

        public static bool ValidatePreparedContentSources(CatPuzzleContentCatalog contentCatalog, out string error)
        {
            error = null;
            if (contentCatalog == null) { error = "The scene's content catalog is missing."; return false; }
            try
            {
                var sourceLevels = JsonUtility.FromJson<LevelPackData>(File.ReadAllText(LevelsSourcePath));
                if (!LevelValidator.TryValidatePack(sourceLevels, out string levelError))
                {
                    error = "Invalid level source data: " + levelError;
                    return false;
                }
                if (JsonUtility.ToJson(sourceLevels) != JsonUtility.ToJson(contentCatalog.levelPack))
                {
                    error = "Prepared level data differs from " + LevelsSourcePath + ". Regenerate the prepared content after your level-data changes.";
                    return false;
                }
                var sourceRooms = JsonUtility.FromJson<CatMetaCatalogData>(File.ReadAllText(RoomsSourcePath));
                if (sourceRooms == null || sourceRooms.chapters == null || sourceRooms.chapters.Length == 0)
                {
                    error = "Room source data is empty or invalid: " + RoomsSourcePath;
                    return false;
                }
                if (JsonUtility.ToJson(sourceRooms) != JsonUtility.ToJson(contentCatalog.metaData))
                {
                    error = "Prepared room data differs from " + RoomsSourcePath + ". Regenerate the prepared content after your room-data changes.";
                    return false;
                }
            }
            catch (Exception exception)
            {
                error = "Could not validate prepared source content: " + exception.Message;
                return false;
            }
            return true;
        }

        public static string PreparationFingerprint()
        {
            string[] sources = {
                LevelsSourcePath,
                RoomsSourcePath,
                "Assets/_GameData/System/Scripts/CatBlockPuzzle/Gameplay/ShapeLibrary.cs",
                "Assets/_GameData/System/Scripts/CatBlockPuzzle/EditorTools/CatPuzzleUiAuthoring.cs",
                "Assets/_GameData/System/Scripts/CatBlockPuzzle/EditorTools/CatPuzzleMetaAuthoring.cs",
                "Assets/_GameData/System/Scripts/CatBlockPuzzle/EditorTools/CatPuzzleScenePreparer.cs",
                "Assets/_GameData/System/Scripts/CatBlockPuzzle/EditorTools/CatPuzzleLevelPrefabExporter.cs",
                "Assets/_GameData/System/Scripts/CatBlockPuzzle/EditorTools/CatPuzzleSpriteFactory.cs"
            };
            return Hash128.Compute(string.Join("|", sources.Select(p => AssetDatabase.GetAssetDependencyHash(p).ToString()))).ToString();
        }

        public static void PrepareInEditor(CatBlockPuzzleGame game, Camera camera, Transform uiGroup, Transform controllers)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Prepare the scene in Edit Mode.");
            if (game.Hud == null) game.Hud = game.gameObject.AddComponent<GameplayHudView>();
            game.Hud.sceneCamera = camera;
            Transform existingPresentation = game.transform.parent != null ? game.transform.parent.Find("Gameplay Presentation") : null;
            var presentationObject = existingPresentation != null ? existingPresentation.gameObject : new GameObject("Gameplay Presentation");
            if (existingPresentation == null) presentationObject.transform.SetParent(game.transform.parent, false);
            game.Assets = MoveBinding(game.Assets, presentationObject);
            game.Recompose();
            game.Settings.reducedMotion = true;
            PrepareContentAssets(game);
            game.Levels = game.Catalog.CreateLevelManager();
            // Authoring never touches the player's preferences or progression.
            CatMetaCatalog metaCatalog = game.Catalog.CreateMetaCatalog();
            game.Meta.UseAuthoringData(metaCatalog, CatMetaProgressStore.Load(metaCatalog, new AuthoringPreferences(), CatMetaStorageKeys.Production));
            game.Assets.defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            game.Assets.layoutProfile = AssetDatabase.LoadAssetAtPath<PortraitLayoutProfile>("Assets/_GameData/System/Resources/CatBlockPuzzle/portrait_layout_profile.asset");
            if (game.Assets.layoutProfile == null) throw new InvalidOperationException("Portrait layout profile is missing.");
            PrepareVisualAssets(game);
            var eventObject = new GameObject("EventSystem", typeof(EventSystem));
            eventObject.transform.SetParent(controllers, false);
            game.Hud.sceneEventSystem = eventObject.GetComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var input = eventObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            input.AssignDefaultActions();
#else
            eventObject.AddComponent<StandaloneInputModule>();
#endif
            var ui = new CatPuzzleUiAuthoring(game);
            var metaUi = new CatPuzzleMetaAuthoring(game, ui);
            ui.BuildCanvas(metaUi);
            GameplayHudView hud = game.Hud;
            Canvas canvas = hud.canvas;
            canvas.name = "Canvas";
            canvas.transform.SetParent(uiGroup, false);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 3f;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 1;
            hud.objectiveImage = hud.objectivePanel.GetComponent<Image>();
            hud.backgroundCrossfade = ui.CreateImage((RectTransform)canvas.transform, "FadeOverlay", Color.white);
            Stretch(hud.backgroundCrossfade.rectTransform);
            hud.backgroundCrossfade.raycastTarget = false;
            hud.backgroundCrossfade.transform.SetAsFirstSibling();
            hud.backgroundCrossfade.gameObject.SetActive(false);

            RectTransform root = hud.root;
            RectTransform gameplayScreen = ui.CreatePanel(root, "Gameplay Screen", Color.clear);
            hud.gameplayScreen = gameplayScreen;
            game.Hud = hud = MoveBinding(hud, gameplayScreen.gameObject);
            Stretch(gameplayScreen);
            gameplayScreen.GetComponent<Image>().raycastTarget = false;
            gameplayScreen.SetAsFirstSibling();
            RectTransform winOverlay = game.CompleteScreen.RootRect;
            RectTransform failOverlay = game.FailScreen.RootRect;
            RectTransform metaOverlay = game.MetaView.metaOverlay;
            var staticChildren = root.Cast<Transform>().ToArray();
            foreach (var child in staticChildren)
                if (child != gameplayScreen && child != hud.fxLayer && child != winOverlay && child != failOverlay && child != metaOverlay)
                    child.SetParent(gameplayScreen, false);
            RectTransform levelRoot = ui.CreatePanel(gameplayScreen, "LevelRoot", Color.clear);
            hud.levelRoot = levelRoot;
            Stretch(levelRoot); levelRoot.GetComponent<Image>().raycastTarget = false;
            levelRoot.SetAsFirstSibling();
            var templateRoot = ui.CreatePanel(levelRoot, "Level Template", Color.clear);
            Stretch(templateRoot); templateRoot.GetComponent<Image>().raycastTarget = false;
            PuzzleSession session = game.Session;
            foreach (var child in new[] { session.boardBackdrop, session.boardRoot, session.trayRoot, session.pieceLayer }) child.SetParent(templateRoot, false);
            var template = templateRoot.gameObject.AddComponent<CatPuzzleLevelView>();
            template.board = session.boardRoot; template.boardFrame = session.boardBackdrop;
            template.boardFrameImage = session.boardBackdrop.GetComponent<Image>();
            template.boardOutline = session.boardBackdrop.GetComponent<Outline>();
            template.boardEars = new[] { session.boardBackdrop.Find("Board Ear Left").GetComponent<Image>(), session.boardBackdrop.Find("Board Ear Right").GetComponent<Image>() };
            template.tray = session.trayRoot; template.trayImage = session.trayImage; template.viewport = session.trayViewport;
            template.content = session.trayContent; template.layout = session.trayLayout; template.scroll = session.trayScrollRect; template.pieceLayer = session.pieceLayer;
            template.gameObject.SetActive(false);
            var preparedLevels = new CatPuzzleLevelView[game.Levels.LevelCount];
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < preparedLevels.Length; i++)
            {
                var view = UnityEngine.Object.Instantiate(template, levelRoot);
                view.name = "Level_" + (i + 1).ToString("000"); view.levelIndex = i;
                view.gameObject.SetActive(true);
                preparedLevels[i] = view;
                session.BindLevelView(view);
                session.activeLevel = game.Levels.GetLevel(i); session.levelIndex = i;
                session.pieces.Clear();
                ui.BuildBoardInEditor(); ui.BuildPiecesInEditor();
                LevelDefinition activeLevel = session.activeLevel;
                view.cells = new CatPuzzleLevelView.BoardCell[activeLevel.Rows * activeLevel.Cols];
                int cellIndex = 0;
                for (int row = 0; row < activeLevel.Rows; row++)
                    for (int col = 0; col < activeLevel.Cols; col++)
                    {
                        var cell = session.boardCells[new Vector2Int(row, col)];
                        bool active = activeLevel.ActiveCells.Contains(new Vector2Int(row, col));
                        view.cells[cellIndex++] = new CatPuzzleLevelView.BoardCell {
                            row = row, col = col, active = active, image = cell.Image, preview = cell.Preview,
                            shine = active ? (RectTransform)cell.Rect.Find("Cell Shine") : null,
                            paw = active ? (RectTransform)cell.Rect.Find("Paw Print") : null
                        };
                        cell.Rect.localScale = Vector3.one;
                    }
                view.pieces = new CatPuzzlePieceView[session.pieces.Count];
                for (int p = 0; p < session.pieces.Count; p++)
                {
                    var state = session.pieces[p];
                    var piece = state.Slot.gameObject.AddComponent<CatPuzzlePieceView>();
                    piece.pieceIndex = p; piece.slot = state.Slot; piece.slotImage = state.SlotImage;
                    piece.slotLayout = state.SlotLayout; piece.visual = state.Rect; piece.cats = state.CellImages.ToArray();
                    piece.slotInput = AddPieceInput(game, state.Slot, i, p, true);
                    piece.pieceInput = AddPieceInput(game, state.Rect, i, p, false);
                    view.pieces[p] = piece;
                }
                view.gameObject.SetActive(false);
            }
            game.PreparedLevels = preparedLevels;
            UnityEngine.Object.DestroyImmediate(templateRoot.gameObject);
            CatPuzzleMetaCapture.CaptureMetaScreens(game, ui, metaUi);
            CatPuzzleMetaCapture.PreparePersistentControls(game);
            hud.preparedEffects = new Image[256];
            for (int i = 0; i < hud.preparedEffects.Length; i++)
            {
                var effect = ui.CreateImage(hud.fxLayer, "FX_" + i.ToString("000"), Color.white);
                effect.sprite = game.Assets.circleSprite; effect.raycastTarget = false; effect.gameObject.SetActive(false);
                hud.preparedEffects[i] = effect;
            }
            winOverlay.name = "Level Complete Screen"; failOverlay.name = "Level Failed Screen";
            metaOverlay.name = "Home and Rooms";
            game.MetaView = MoveBinding(game.MetaView, metaOverlay.gameObject);
            MetaProgressionView metaView = game.MetaView;
            metaView.metaHubPage.name = "Home Screen"; metaView.metaRoomPage.name = "Room Screen";
            metaView.metaStoryOverlay.name = "Story Overlay"; metaView.metaCompletionOverlay.name = "Room Complete Overlay";
            foreach (var t in canvas.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = LayerMask.NameToLayer("UI");
            winOverlay.gameObject.SetActive(false); failOverlay.gameObject.SetActive(false);
            metaOverlay.gameObject.SetActive(false); metaView.metaHubPage.gameObject.SetActive(false); metaView.metaRoomPage.gameObject.SetActive(false);
            metaView.metaStoryOverlay.gameObject.SetActive(false); metaView.metaCompletionOverlay.gameObject.SetActive(false);
            game.Settings.reducedMotion = false;
            CatPuzzleLevelPrefabExporter.ExportPreparedLevelsInEditor(game);
            PreviewPreparedLevelInEditor(game, 0);
            game.Catalog.sourceFingerprint = PreparationFingerprint();
            EditorUtility.SetDirty(game.Catalog); EditorUtility.SetDirty(game);
            EditorUtility.SetDirty(game.Hud); EditorUtility.SetDirty(game.Assets); EditorUtility.SetDirty(game.MetaView);
            EditorUtility.SetDirty(game.CompleteScreen); EditorUtility.SetDirty(game.FailScreen);
            AssetDatabase.SaveAssets();
            if (!game.ValidatePreparedScene(out string error)) throw new InvalidOperationException(error);
        }

        private static T MoveBinding<T>(T source, GameObject target) where T : Component
        {
            T destination = target.GetComponent<T>();
            if (destination == null) destination = target.AddComponent<T>();
            if (source != null && source != destination)
            {
                EditorUtility.CopySerialized(source, destination);
                UnityEngine.Object.DestroyImmediate(source);
            }
            return destination;
        }

        private static CatPuzzlePieceDragView AddPieceInput(CatBlockPuzzleGame game, RectTransform target, int level, int piece, bool proxy)
        {
            var input = target.gameObject.AddComponent<CatPuzzlePieceDragView>();
            input.controller = game; input.levelIndex = level; input.pieceIndex = piece; input.slotProxy = proxy;
            return input;
        }

        /// <summary>Loads one level prefab into the saved scene as a temporary, unsaved preview.</summary>
        public static void PreviewPreparedLevelInEditor(CatBlockPuzzleGame game, int index)
        {
            game.Levels = game.Catalog.CreateLevelManager();
            game.EnsureComposed();
            CatPuzzleLevelPrefabExporter.ClearLevelPreviewInEditor(game);
            PuzzleSession session = game.Session;
            session.pieces.Clear(); session.occupancy.Clear(); session.previewCells.Clear();
            session.levelIndex = Mathf.Clamp(index, 0, game.Catalog.LevelCount - 1);
            int levelIndex = session.levelIndex;
            session.activeLevel = game.Levels.GetLevel(levelIndex);
            var prefab = AssetDatabase.LoadAssetAtPath<CatPuzzleLevelView>("Assets/_GameData/System/Resources/" + game.Catalog.levelPrefabResources[levelIndex] + ".prefab");
            if (!PreparedLevelLoader.ValidateLevelPrefab(game.Catalog, prefab, levelIndex, out string error)) throw new InvalidOperationException(error);
            game.Loader.Activate(levelIndex, prefab);
            CatPuzzleLevelView loadedLevel = game.Loader.LoadedLevel;
            foreach (var t in loadedLevel.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags = HideFlags.DontSaveInEditor;
            Canvas.ForceUpdateCanvases();
            game.Board.LayoutCells(loadedLevel);
            game.Tray.LayoutPieces();
            foreach (var state in session.pieces) game.Mood.SetMood(state, CatMood.Neutral);
            game.Theme.Apply(levelIndex, loadedLevel);
            GameplayHudView hud = game.Hud;
            hud.backgroundImage.sprite = game.Catalog.roomBackgrounds[levelIndex / 10];
            hud.levelText.text = "Level " + (levelIndex + 1);
            hud.objectiveText.text = game.Theme.ObjectiveTitle(session.activeLevel.Title);
            hud.timerText.text = "2:00"; hud.coinText.text = "0";
            foreach (var cell in loadedLevel.cells) cell.image.rectTransform.localScale = Vector3.one;
            foreach (var state in session.pieces) game.Tray.AttachPieceToTray(state);
            Canvas.ForceUpdateCanvases();
            game.Flow.UpdateTestLevelButtons();
        }

        /// <summary>
        /// Reloads the level and room JSON into the prepared catalog without rebuilding the scene.
        /// Only valid while the grids and piece counts are unchanged, because the level prefabs
        /// bake one cell per grid slot; anything else needs a full Prepare Game Scene.
        /// </summary>
        public static void RefreshContentData()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            CatPuzzleContentCatalog contentCatalog = AssetDatabase.LoadAssetAtPath<CatPuzzleContentCatalog>(PreparedAssetFolder + "/Content.asset");
            if (contentCatalog == null) throw new InvalidOperationException("No prepared content catalog; run Prepare Game Scene first.");
            var pack = JsonUtility.FromJson<LevelPackData>(File.ReadAllText(LevelsSourcePath));
            if (!LevelValidator.TryValidatePack(pack, out string error)) throw new InvalidOperationException(error);
            LevelData[] previous = contentCatalog.levelPack?.levels;
            if (previous != null && previous.Length == pack.levels.Length)
            {
                for (int i = 0; i < pack.levels.Length; i++)
                {
                    if (pack.levels[i].rows != previous[i].rows || pack.levels[i].cols != previous[i].cols ||
                        pack.levels[i].pieces.Length != previous[i].pieces.Length)
                        throw new InvalidOperationException("Level " + (i + 1) + " changed its grid or piece count; the level prefabs must be regenerated with Prepare Game Scene.");
                }
            }
            contentCatalog.levelPack = pack;
            contentCatalog.metaData = JsonUtility.FromJson<CatMetaCatalogData>(File.ReadAllText(RoomsSourcePath));
            contentCatalog.sourceFingerprint = PreparationFingerprint();
            EditorUtility.SetDirty(contentCatalog);
            AssetDatabase.SaveAssets();
            Debug.Log("CAT_CONTENT_REFRESHED levels=" + pack.levels.Length);
        }

        private static void PrepareContentAssets(CatBlockPuzzleGame game)
        {
            Directory.CreateDirectory(PreparedAssetFolder);
            AssetDatabase.Refresh();
            var pack = JsonUtility.FromJson<LevelPackData>(File.ReadAllText(LevelsSourcePath));
            if (!LevelValidator.TryValidatePack(pack, out string error)) throw new InvalidOperationException(error);
            var meta = JsonUtility.FromJson<CatMetaCatalogData>(File.ReadAllText(RoomsSourcePath));
            CatPuzzleContentCatalog contentCatalog = AssetDatabase.LoadAssetAtPath<CatPuzzleContentCatalog>(PreparedAssetFolder + "/Content.asset");
            if (contentCatalog == null)
            {
                contentCatalog = ScriptableObject.CreateInstance<CatPuzzleContentCatalog>();
                AssetDatabase.CreateAsset(contentCatalog, PreparedAssetFolder + "/Content.asset");
            }
            contentCatalog.levelPack = pack; contentCatalog.metaData = meta;
            game.Catalog = contentCatalog;
            CatMetaCatalog metaCatalog = contentCatalog.CreateMetaCatalog();
            contentCatalog.roomBackgrounds = new Sprite[10]; contentCatalog.roomThumbnails = new Sprite[10];
            var decorationArt = new List<CatPuzzleContentCatalog.DecorationArt>();
            foreach (var chapter in metaCatalog.Chapters)
            {
                contentCatalog.roomBackgrounds[chapter.Index] = PersistFullTexture(chapter.BackgroundResourcePath, "Room_" + chapter.Index);
                contentCatalog.roomThumbnails[chapter.Index] = PersistFullTexture(chapter.ThumbnailResourcePath, "RoomThumb_" + chapter.Index);
                foreach (var decoration in chapter.Decorations)
                    decorationArt.Add(new CatPuzzleContentCatalog.DecorationArt { id = decoration.Id, sprite = PersistFullTexture(decoration.SpriteResourcePath, decoration.Id) });
            }
            contentCatalog.decorations = decorationArt.ToArray();
        }

        private static void PrepareVisualAssets(CatBlockPuzzleGame game)
        {
            GameplayPresentationAssets assets = game.Assets;
            CatVisualCatalog visualCatalog = AssetDatabase.LoadAssetAtPath<CatVisualCatalog>(PreparedAssetFolder + "/Visuals.asset");
            if (visualCatalog == null) { visualCatalog = ScriptableObject.CreateInstance<CatVisualCatalog>(); AssetDatabase.CreateAsset(visualCatalog, PreparedAssetFolder + "/Visuals.asset"); }
            assets.visualCatalog = visualCatalog;
            const string art = "Assets/_GameData/System/Resources/CatBlockPuzzle/Art/";
            visualCatalog.CozyRoomBackground = AssetDatabase.LoadAssetAtPath<Texture2D>(art + "cozy_room_background.png");
            visualCatalog.ThemeAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(art + "Themes/theme_atlas.png");
            EditorUtility.SetDirty(visualCatalog);
            assets.whiteSprite = PersistSprite(CatPuzzleSpriteFactory.CreateSolidSprite(Color.white), "White");
            assets.roundedBoxSprite = PersistSprite(CatPuzzleSpriteFactory.CreateRoundedBoxSprite(), "RoundedBox");
            assets.circleSprite = PersistSprite(CatPuzzleSpriteFactory.CreateCircleSprite(), "Circle");
            assets.coinSprite = PersistSprite(CatPuzzleSpriteFactory.CreateCoinSprite(), "Coin");
            assets.catHeadSprite = PersistSprite(CatPuzzleSpriteFactory.CreateCatHeadSprite(), "CatHead");
            assets.mouthSprite = PersistSprite(CatPuzzleSpriteFactory.CreateMouthSprite(), "Mouth");
            assets.tailSprite = PersistSprite(CatPuzzleSpriteFactory.CreateTailSprite(), "Tail");
            assets.pawSprite = PersistSprite(CatPuzzleSpriteFactory.CreatePawSprite(), "Paw");
            assets.starSprite = PersistSprite(CatPuzzleSpriteFactory.CreateStarSprite(false), "Star");
            assets.starOutlineSprite = PersistSprite(CatPuzzleSpriteFactory.CreateStarSprite(true), "StarOutline");
            assets.backIconSprite = PersistSprite(CatPuzzleSpriteFactory.CreateUiIconSprite(CatPuzzleSpriteFactory.UiIcon.Back), "Back");
            assets.pauseIconSprite = PersistSprite(CatPuzzleSpriteFactory.CreateUiIconSprite(CatPuzzleSpriteFactory.UiIcon.Pause), "Pause");
            assets.settingsIconSprite = PersistSprite(CatPuzzleSpriteFactory.CreateUiIconSprite(CatPuzzleSpriteFactory.UiIcon.Settings), "Settings");
            assets.hintIconSprite = PersistSprite(CatPuzzleSpriteFactory.CreateUiIconSprite(CatPuzzleSpriteFactory.UiIcon.Hint), "Hint");
            assets.resetIconSprite = PersistSprite(CatPuzzleSpriteFactory.CreateUiIconSprite(CatPuzzleSpriteFactory.UiIcon.Reset), "Reset");
            assets.closeIconSprite = PersistSprite(CatPuzzleSpriteFactory.CreateUiIconSprite(CatPuzzleSpriteFactory.UiIcon.Close), "Close");
            game.Hud.defaultBackgroundSprite = PersistSprite(CatPuzzleSpriteFactory.CreateBackgroundSprite(visualCatalog), "DefaultBackground");
            CatPuzzleSpriteFactory.LoadAuthoredCatPortraits(assets);
            for (int i = 0; i < assets.catPortraitSprites.Length; i++) assets.catPortraitSprites[i] = PersistSprite(assets.catPortraitSprites[i], "Cat_" + i);
            for (int i = 0; i < assets.themeBackgroundSprites.Length; i++)
            {
                var theme = CatPuzzleThemeCatalog.GetTheme(i);
                var atlas = visualCatalog.ThemeAtlas;
                float w = atlas.width / 3f, h = atlas.height / 3f;
                int col = theme.AtlasIndex % 3, row = theme.AtlasIndex / 3;
                var sprite = Sprite.Create(atlas, new Rect(col * w + .75f, atlas.height - (row + 1) * h + .75f, w - 1.5f, h - 1.5f), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                assets.themeBackgroundSprites[i] = PersistSprite(sprite, "Theme_" + i);
            }
        }

        private static Sprite PersistFullTexture(string resource, string name)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_GameData/System/Resources/" + resource + ".png");
            if (texture == null) throw new InvalidOperationException("Required art missing: " + resource);
            return PersistSprite(Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect), name);
        }

        private static Sprite PersistSprite(Sprite source, string name)
        {
            if (source == null) throw new InvalidOperationException("Required sprite missing: " + name);
            string path = PreparedAssetFolder + "/" + name.Replace('.', '_') + ".asset";
            var previous = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var texture = source.texture;
            if (!AssetDatabase.Contains(texture))
            {
                string texturePath = PreparedAssetFolder + "/" + name + ".png";
                File.WriteAllBytes(texturePath, texture.EncodeToPNG());
                AssetDatabase.ImportAsset(texturePath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.textureType = TextureImporterType.Default; importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true; importer.SaveAndReimport();
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            }
            var saved = Sprite.Create(texture, source.rect, new Vector2(source.pivot.x / source.rect.width, source.pivot.y / source.rect.height), source.pixelsPerUnit, 0, SpriteMeshType.FullRect, source.border);
            saved.name = name;
            if (previous != null) { EditorUtility.CopySerialized(saved, previous); UnityEngine.Object.DestroyImmediate(saved); saved = previous; EditorUtility.SetDirty(previous); }
            else AssetDatabase.CreateAsset(saved, path);
            return saved;
        }

        private sealed class AuthoringPreferences : ICatMetaPreferences
        {
            private readonly Dictionary<string, string> strings = new Dictionary<string, string>();
            private readonly Dictionary<string, int> ints = new Dictionary<string, int>();
            public bool HasKey(string key) => strings.ContainsKey(key) || ints.ContainsKey(key);
            public string GetString(string key, string fallback) => strings.TryGetValue(key, out var value) ? value : fallback;
            public int GetInt(string key, int fallback) => ints.TryGetValue(key, out var value) ? value : fallback;
            public void SetString(string key, string value) => strings[key] = value;
            public void SetInt(string key, int value) => ints[key] = value;
            public void Save() { }
        }
    }
}
#endif
