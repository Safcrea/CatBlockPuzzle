#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        public const string PreparedAssetFolder = "Assets/Generated/CatPuzzleScene";
        private readonly Dictionary<Sprite, Sprite> persistedSprites = new Dictionary<Sprite, Sprite>();

        public static string PreparationFingerprint()
        {
            string[] sources = {
                "Assets/Resources/CatBlockPuzzle/levels_100.json",
                "Assets/Resources/CatBlockPuzzle/meta_chapters.json",
                "Assets/Scripts/CatBlockPuzzle/ShapeLibrary.cs",
                "Assets/Scripts/CatBlockPuzzle/CatBlockPuzzleGame.Authoring.cs",
                "Assets/Scripts/CatBlockPuzzle/CatBlockPuzzleGame.EditorPreparation.cs",
                "Assets/Scripts/CatBlockPuzzle/CatBlockPuzzleGame.EditorLevelPrefabs.cs",
                "Assets/Scripts/CatBlockPuzzle/CatBlockPuzzleGame.Sprites.cs"
            };
            return Hash128.Compute(string.Join("|", sources.Select(p => AssetDatabase.GetAssetDependencyHash(p).ToString()))).ToString();
        }

        public void PrepareInEditor(Camera camera, Transform uiGroup, Transform controllers)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Prepare the scene in Edit Mode.");
            sceneCamera = camera;
            reducedMotion = true;
            PrepareContentAssets();
            levelManager = contentCatalog.CreateLevelManager();
            metaCatalog = contentCatalog.CreateMetaCatalog();
            // Authoring never touches the player's preferences or progression.
            metaProgress = CatMetaProgressStore.Load(metaCatalog, new AuthoringPreferences(), CatMetaStorageKeys.Production);
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            layoutProfile = AssetDatabase.LoadAssetAtPath<PortraitLayoutProfile>("Assets/Resources/CatBlockPuzzle/portrait_layout_profile.asset");
            if (layoutProfile == null) throw new InvalidOperationException("Portrait layout profile is missing.");
            PrepareVisualAssets();
            BuildAudio();
            audioSource.gameObject.name = "SoundManager";
            audioSource.transform.SetParent(transform.parent, false);
            buttonClip = PersistAudio(buttonClip, "Button"); snapClip = PersistAudio(snapClip, "Snap");
            wrongClip = PersistAudio(wrongClip, "Wrong"); winClip = PersistAudio(winClip, "Win");
            var eventObject = new GameObject("EventSystem", typeof(EventSystem));
            eventObject.transform.SetParent(controllers, false);
            sceneEventSystem = eventObject.GetComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var input = eventObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            input.AssignDefaultActions();
#else
            eventObject.AddComponent<StandaloneInputModule>();
#endif
            BuildCanvas();
            canvas.name = "Canvas";
            canvas.transform.SetParent(uiGroup, false);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = sceneCamera;
            canvas.planeDistance = 3f;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 1;
            objectiveImage = objectivePanel.GetComponent<Image>();
            backgroundCrossfade = CreateImage((RectTransform)canvas.transform, "FadeOverlay", Color.white);
            Stretch(backgroundCrossfade.rectTransform);
            backgroundCrossfade.raycastTarget = false;
            backgroundCrossfade.transform.SetAsFirstSibling();
            backgroundCrossfade.gameObject.SetActive(false);

            gameplayScreen = CreatePanel(root, "Gameplay Screen", Color.clear);
            Stretch(gameplayScreen);
            gameplayScreen.GetComponent<Image>().raycastTarget = false;
            gameplayScreen.SetAsFirstSibling();
            var staticChildren = root.Cast<Transform>().ToArray();
            foreach (var child in staticChildren)
                if (child != gameplayScreen && child != fxLayer && child != winOverlay && child != failOverlay && child != settingsOverlay && child != metaOverlay)
                    child.SetParent(gameplayScreen, false);
            levelRoot = CreatePanel(gameplayScreen, "LevelRoot", Color.clear);
            Stretch(levelRoot); levelRoot.GetComponent<Image>().raycastTarget = false;
            levelRoot.SetAsFirstSibling();
            var templateRoot = CreatePanel(levelRoot, "Level Template", Color.clear);
            Stretch(templateRoot); templateRoot.GetComponent<Image>().raycastTarget = false;
            foreach (var child in new[] { boardBackdrop, boardRoot, trayRoot, pieceLayer }) child.SetParent(templateRoot, false);
            var template = templateRoot.gameObject.AddComponent<CatPuzzleLevelView>();
            template.board = boardRoot; template.boardFrame = boardBackdrop;
            template.boardFrameImage = boardBackdrop.GetComponent<Image>();
            template.boardOutline = boardBackdrop.GetComponent<Outline>();
            template.boardEars = new[] { boardBackdrop.Find("Board Ear Left").GetComponent<Image>(), boardBackdrop.Find("Board Ear Right").GetComponent<Image>() };
            template.tray = trayRoot; template.trayImage = trayImage; template.viewport = trayViewport;
            template.content = trayContent; template.layout = trayLayout; template.scroll = trayScrollRect; template.pieceLayer = pieceLayer;
            template.gameObject.SetActive(false);
            preparedLevels = new CatPuzzleLevelView[levelManager.LevelCount];
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < preparedLevels.Length; i++)
            {
                var view = Instantiate(template, levelRoot);
                view.name = "Level_" + (i + 1).ToString("000"); view.levelIndex = i;
                view.gameObject.SetActive(true);
                preparedLevels[i] = view;
                ApplyLevelBindings(view);
                activeLevel = levelManager.GetLevel(i); levelIndex = i;
                pieces.Clear();
                BuildBoardInEditor(); BuildPiecesInEditor();
                view.cells = new CatPuzzleLevelView.BoardCell[activeLevel.Rows * activeLevel.Cols];
                int cellIndex = 0;
                for (int row = 0; row < activeLevel.Rows; row++)
                    for (int col = 0; col < activeLevel.Cols; col++)
                    {
                        var cell = boardCells[new Vector2Int(row, col)];
                        bool active = activeLevel.ActiveCells.Contains(new Vector2Int(row, col));
                        view.cells[cellIndex++] = new CatPuzzleLevelView.BoardCell {
                            row = row, col = col, active = active, image = cell.Image, preview = cell.Preview,
                            shine = active ? (RectTransform)cell.Rect.Find("Cell Shine") : null,
                            paw = active ? (RectTransform)cell.Rect.Find("Paw Print") : null
                        };
                        cell.Rect.localScale = Vector3.one;
                    }
                view.pieces = new CatPuzzlePieceView[pieces.Count];
                for (int p = 0; p < pieces.Count; p++)
                {
                    var state = pieces[p];
                    var piece = state.Slot.gameObject.AddComponent<CatPuzzlePieceView>();
                    piece.pieceIndex = p; piece.slot = state.Slot; piece.slotImage = state.SlotImage;
                    piece.slotLayout = state.SlotLayout; piece.visual = state.Rect; piece.cats = state.CellImages.ToArray();
                    piece.slotInput = AddPieceInput(state.Slot, i, p, true);
                    piece.pieceInput = AddPieceInput(state.Rect, i, p, false);
                    view.pieces[p] = piece;
                }
                view.gameObject.SetActive(false);
            }
            DestroyImmediate(templateRoot.gameObject);
            CaptureMetaScreens();
            PreparePersistentControls();
            preparedEffects = new Image[256];
            for (int i = 0; i < preparedEffects.Length; i++)
            {
                var effect = CreateImage(fxLayer, "FX_" + i.ToString("000"), Color.white);
                effect.sprite = circleSprite; effect.raycastTarget = false; effect.gameObject.SetActive(false);
                preparedEffects[i] = effect;
            }
            winOverlay.name = "Level Complete Screen"; failOverlay.name = "Level Failed Screen";
            settingsOverlay.name = "Settings Screen"; metaOverlay.name = "Home and Rooms";
            metaHubPage.name = "Home Screen"; metaRoomPage.name = "Room Screen";
            metaStoryOverlay.name = "Story Overlay"; metaCompletionOverlay.name = "Room Complete Overlay";
            foreach (var t in canvas.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = LayerMask.NameToLayer("UI");
            winOverlay.gameObject.SetActive(false); failOverlay.gameObject.SetActive(false); settingsOverlay.gameObject.SetActive(false);
            metaOverlay.gameObject.SetActive(false); metaHubPage.gameObject.SetActive(false); metaRoomPage.gameObject.SetActive(false);
            metaStoryOverlay.gameObject.SetActive(false); metaCompletionOverlay.gameObject.SetActive(false);
            reducedMotion = false;
            ExportPreparedLevelsInEditor();
            PreviewPreparedLevelInEditor(0);
            contentCatalog.sourceFingerprint = PreparationFingerprint();
            EditorUtility.SetDirty(contentCatalog); EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            if (!ValidatePreparedScene(out string error)) throw new InvalidOperationException(error);
        }

        private CatPuzzlePieceDragView AddPieceInput(RectTransform target, int level, int piece, bool proxy)
        {
            var input = target.gameObject.AddComponent<CatPuzzlePieceDragView>();
            input.controller = this; input.levelIndex = level; input.pieceIndex = piece; input.slotProxy = proxy;
            return input;
        }

        public void PreviewPreparedLevelInEditor(int index)
        {
            if (Application.isPlaying) { PreviewLevelForTesting(index); return; }
            levelManager = contentCatalog.CreateLevelManager();
            ClearLevelPreviewInEditor();
            pieces.Clear(); occupancy.Clear(); previewCells.Clear();
            levelIndex = Mathf.Clamp(index, 0, contentCatalog.LevelCount - 1);
            activeLevel = levelManager.GetLevel(levelIndex);
            var prefab = AssetDatabase.LoadAssetAtPath<CatPuzzleLevelView>("Assets/Resources/" + contentCatalog.levelPrefabResources[levelIndex] + ".prefab");
            if (!ValidateLevelPrefab(prefab, levelIndex, out string error)) throw new InvalidOperationException(error);
            ActivatePreparedLevel(levelIndex, prefab);
            foreach (var t in loadedLevel.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags = HideFlags.DontSaveInEditor;
            Canvas.ForceUpdateCanvases();
            LayoutPreparedLevel(); ApplyLevelTheme(levelIndex);
            backgroundImage.sprite = contentCatalog.roomBackgrounds[levelIndex / 10];
            levelText.text = "Level " + (levelIndex + 1);
            objectiveText.text = BuildThemedObjectiveTitle(activeLevel.Title);
            timerText.text = "2:00"; coinText.text = "0";
            foreach (var cell in loadedLevel.cells) cell.image.rectTransform.localScale = Vector3.one;
            foreach (var state in pieces) AttachPieceToTray(state);
            Canvas.ForceUpdateCanvases();
            UpdateTestLevelButtons();
        }

        private void PrepareContentAssets()
        {
            Directory.CreateDirectory(PreparedAssetFolder);
            AssetDatabase.Refresh();
            var pack = JsonUtility.FromJson<LevelPackData>(File.ReadAllText("Assets/Resources/CatBlockPuzzle/levels_100.json"));
            if (!LevelValidator.TryValidatePack(pack, out string error)) throw new InvalidOperationException(error);
            var meta = JsonUtility.FromJson<CatMetaCatalogData>(File.ReadAllText("Assets/Resources/CatBlockPuzzle/meta_chapters.json"));
            contentCatalog = AssetDatabase.LoadAssetAtPath<CatPuzzleContentCatalog>(PreparedAssetFolder + "/Content.asset");
            if (contentCatalog == null)
            {
                contentCatalog = ScriptableObject.CreateInstance<CatPuzzleContentCatalog>();
                AssetDatabase.CreateAsset(contentCatalog, PreparedAssetFolder + "/Content.asset");
            }
            contentCatalog.levelPack = pack; contentCatalog.metaData = meta;
            metaCatalog = contentCatalog.CreateMetaCatalog();
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

        private void PrepareVisualAssets()
        {
            visualCatalog = AssetDatabase.LoadAssetAtPath<CatVisualCatalog>(PreparedAssetFolder + "/Visuals.asset");
            if (visualCatalog == null) { visualCatalog = ScriptableObject.CreateInstance<CatVisualCatalog>(); AssetDatabase.CreateAsset(visualCatalog, PreparedAssetFolder + "/Visuals.asset"); }
            const string art = "Assets/Resources/CatBlockPuzzle/Art/";
            visualCatalog.CozyRoomBackground = AssetDatabase.LoadAssetAtPath<Texture2D>(art + "cozy_room_background.png");
            visualCatalog.ThemeAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(art + "Themes/theme_atlas.png");
            visualCatalog.NeutralCatAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(art + "cat_portraits.png");
            visualCatalog.HappyCatAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(art + "cat_portraits_happy.png");
            visualCatalog.WorriedCatAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(art + "cat_portraits_worried.png");
            EditorUtility.SetDirty(visualCatalog);
            whiteSprite = PersistSprite(CreateSolidSprite(Color.white), "White");
            roundedBoxSprite = PersistSprite(CreateRoundedBoxSprite(), "RoundedBox");
            circleSprite = PersistSprite(CreateCircleSprite(), "Circle"); coinSprite = PersistSprite(CreateCoinSprite(), "Coin");
            catHeadSprite = PersistSprite(CreateCatHeadSprite(), "CatHead"); mouthSprite = PersistSprite(CreateMouthSprite(), "Mouth");
            tailSprite = PersistSprite(CreateTailSprite(), "Tail"); pawSprite = PersistSprite(CreatePawSprite(), "Paw");
            starSprite = PersistSprite(CreateStarSprite(false), "Star"); starOutlineSprite = PersistSprite(CreateStarSprite(true), "StarOutline");
            backIconSprite = PersistSprite(CreateUiIconSprite(UiIcon.Back), "Back"); pauseIconSprite = PersistSprite(CreateUiIconSprite(UiIcon.Pause), "Pause");
            settingsIconSprite = PersistSprite(CreateUiIconSprite(UiIcon.Settings), "Settings"); hintIconSprite = PersistSprite(CreateUiIconSprite(UiIcon.Hint), "Hint");
            resetIconSprite = PersistSprite(CreateUiIconSprite(UiIcon.Reset), "Reset"); closeIconSprite = PersistSprite(CreateUiIconSprite(UiIcon.Close), "Close");
            defaultBackgroundSprite = PersistSprite(CreateBackgroundSprite(), "DefaultBackground");
            LoadAuthoredCatPortraits();
            for (int i = 0; i < catPortraitSprites.Length; i++) catPortraitSprites[i] = PersistSprite(catPortraitSprites[i], "Cat_" + i);
            for (int i = 0; i < themeBackgroundSprites.Length; i++)
            {
                var theme = CatPuzzleThemeCatalog.GetTheme(i);
                var atlas = visualCatalog.ThemeAtlas;
                float w = atlas.width / 3f, h = atlas.height / 3f;
                int col = theme.AtlasIndex % 3, row = theme.AtlasIndex / 3;
                var sprite = Sprite.Create(atlas, new Rect(col*w + .75f, atlas.height-(row+1)*h+.75f, w-1.5f,h-1.5f), new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
                themeBackgroundSprites[i] = PersistSprite(sprite, "Theme_" + i);
            }
        }

        private Sprite PersistFullTexture(string resource, string name)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/" + resource + ".png");
            if (texture == null) throw new InvalidOperationException("Required art missing: " + resource);
            return PersistSprite(Sprite.Create(texture, new Rect(0,0,texture.width,texture.height), new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect), name);
        }

        private Sprite PersistSprite(Sprite source, string name)
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
            var saved = Sprite.Create(texture, source.rect, new Vector2(source.pivot.x/source.rect.width, source.pivot.y/source.rect.height), source.pixelsPerUnit,0,SpriteMeshType.FullRect,source.border);
            saved.name = name;
            if (previous != null) { EditorUtility.CopySerialized(saved, previous); DestroyImmediate(saved); saved = previous; EditorUtility.SetDirty(previous); }
            else AssetDatabase.CreateAsset(saved, path);
            persistedSprites[source] = saved;
            return saved;
        }

        private AudioClip PersistAudio(AudioClip clip, string name)
        {
            string path = PreparedAssetFolder + "/" + name + ".wav";
            float[] samples = new float[clip.samples * clip.channels]; clip.GetData(samples, 0);
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length*2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1);
                writer.Write((short)clip.channels); writer.Write(clip.frequency); writer.Write(clip.frequency*clip.channels*2);
                writer.Write((short)(clip.channels*2)); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length*2);
                foreach (float sample in samples) writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample,-1,1)*32767));
            }
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private sealed class AuthoringPreferences : ICatMetaPreferences
        {
            private readonly Dictionary<string,string> strings = new Dictionary<string,string>();
            private readonly Dictionary<string,int> ints = new Dictionary<string,int>();
            public bool HasKey(string key) => strings.ContainsKey(key) || ints.ContainsKey(key);
            public string GetString(string key, string fallback) => strings.TryGetValue(key,out var value) ? value : fallback;
            public int GetInt(string key,int fallback) => ints.TryGetValue(key,out var value) ? value : fallback;
            public void SetString(string key,string value) => strings[key] = value;
            public void SetInt(string key,int value) => ints[key] = value;
            public void Save() { }
        }
    }
}
#endif
