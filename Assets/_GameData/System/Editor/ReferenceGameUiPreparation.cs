using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle.Editor
{
    /// <summary>Builds saved UI once in Edit Mode; no runtime hierarchy searches or UI factories.</summary>
    public static class ReferenceGameUiPreparation
    {
        private const string Art = "Assets/Art/CatBlockPuzzleUI/UI/Slicing/";
        private static Font font;
        private static Sprite Sprite(string folder, string filename) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + folder + "/" + filename + ".png");
        private static UnityEngine.Object Get(UnityEngine.Object target, string field) => new SerializedObject(target).FindProperty(field).objectReferenceValue;
        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            Undo.RecordObject(target, "Wire Reference UI");
            var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedProperties();
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            Undo.RegisterCreatedObjectUndo(go, "Build Reference UI");
            var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
            rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        private static void Stretch(RectTransform rect, float inset = 0)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset,inset); rect.offsetMax = new Vector2(-inset,-inset);
        }
        private static Image Image(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 position)
        {
            var rect = Rect(name,parent,size,position); var image = Undo.AddComponent<Image>(rect.gameObject);
            image.sprite = sprite; image.raycastTarget = false; image.preserveAspect = true; return image;
        }
        private static Text Label(string name, Transform parent, Vector2 size, Vector2 position, int pointSize, string value = "")
        {
            var rect = Rect(name,parent,size,position); var text = Undo.AddComponent<Text>(rect.gameObject);
            text.font = font; text.fontSize = pointSize; text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(.30f,.14f,.08f); text.raycastTarget = false; text.text = value; return text;
        }
        private static Transform Child(Transform root,string name) => root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
        private static void ObjectArray(UnityEngine.Object target,string field,UnityEngine.Object[] items)
        {
            var so=new SerializedObject(target); var p=so.FindProperty(field); p.arraySize=items.Length;
            for(int i=0;i<items.Length;i++) p.GetArrayElementAtIndex(i).objectReferenceValue=items[i]; so.ApplyModifiedProperties();
        }
        public static string Build(int referenceId, int gameplayId, int selectionId)
        {
            if(Application.isPlaying) throw new InvalidOperationException("Build UI in Edit Mode.");
            var reference=((GameObject)EditorUtility.InstanceIDToObject(referenceId)).transform;
            var game=((GameObject)EditorUtility.InstanceIDToObject(gameplayId)).GetComponent<CatBlockPuzzleGame>();
            var selection=((GameObject)EditorUtility.InstanceIDToObject(selectionId)).transform;
            var screen=reference.Find("Gameplay Screen");
            var oldHud=(GameplayHudView)Get(game,"gameplayHud");
            font=(Font)Get(Get(game,"presentationAssets"),"defaultFont");
            var catalog=(CatPuzzleContentCatalog)Get(game,"contentCatalog");
            Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Activate Reference Game UI");
            var hud=screen.GetComponent<GameplayHudView>();
            if(hud==null) { hud=Undo.AddComponent<GameplayHudView>(screen.gameObject); EditorUtility.CopySerialized(oldHud,hud); }
            // Only generated containers are replaced when rerunning this authoring operation.
            foreach(string name in new[]{"Runtime HUD","Gameplay Background","Background Crossfade","Board Area","Tray Area"})
            {
                var found=screen.Find(name); if(found!=null) Undo.DestroyObjectImmediate(found.gameObject);
            }
            var dynamicHud=Rect("Runtime HUD",screen,Vector2.zero,Vector2.zero); Stretch(dynamicHud);
            var bg=Image("Gameplay Background",screen,catalog.roomBackgrounds[0],Vector2.zero,Vector2.zero);
            Stretch(bg.rectTransform); bg.preserveAspect=false; bg.transform.SetAsFirstSibling();
            Undo.AddComponent<CanvasBackgroundFitter>(bg.gameObject);
            var fade=Image("Background Crossfade",screen,catalog.roomBackgrounds[0],Vector2.zero,Vector2.zero);
            Stretch(fade.rectTransform); fade.preserveAspect=false; fade.transform.SetSiblingIndex(1); fade.gameObject.SetActive(false);
            Undo.AddComponent<CanvasBackgroundFitter>(fade.gameObject);
            var levelRoot=(RectTransform)Child(screen,"LevelRoot");
            var rootImage=levelRoot.GetComponent<Image>(); if(rootImage!=null) { Undo.RecordObject(rootImage,"Remove obsolete background"); rootImage.enabled=false; }
            var board=Child(screen,"Board"); var tray=Child(screen,"Pieces Panel");
            var boardArea=Rect("Board Area",screen,new Vector2(790,790),new Vector2(0,164));
            var trayArea=Rect("Tray Area",screen,new Vector2(840,305),new Vector2(0,-472.5f));
            levelRoot.SetSiblingIndex(Mathf.Max(board.GetSiblingIndex(),tray.GetSiblingIndex())+1);
            var levelText=Label("Level Number",dynamicHud,new Vector2(360,80),new Vector2(0,753),40,"Level 1");
            var coinText=Label("Coin Balance",dynamicHud,new Vector2(150,70),new Vector2(-375,882),34,"0");
            var timer=Label("Timer",dynamicHud,new Vector2(110,62),new Vector2(-60,648),30,"02:00");
            var objective=Label("Objective",dynamicHud,new Vector2(780,65),new Vector2(0,582),24);
            var combo=Label("Combo",dynamicHud,new Vector2(300,54),new Vector2(0,-290),26);
            var stars=Rect("Progress Stars",dynamicHud,new Vector2(166,65),new Vector2(95,648));
            var starSprite=Sprite("Gameplay","star_gameplay");
            var emptyStar=Sprite("Gameplay","black_white");
            var starImages=Enumerable.Range(0,3).Select(i=>Image("Star "+(i+1),stars,starSprite,new Vector2(46,44),new Vector2((i-1)*50,0))).ToArray();
            foreach(string name in new[]{"Star","Star Disabled"}) { var old=Child(screen,name); Undo.RecordObject(old.gameObject,"Hide obsolete star"); old.gameObject.SetActive(false); }
            Set(Get(game,"presentationAssets"),"starSprite",starSprite);
            Set(Get(game,"presentationAssets"),"starOutlineSprite",emptyStar);
            Undo.RecordObject(board.GetComponent<Image>(),"Restore authored board color"); board.GetComponent<Image>().color=Color.white;
            var actions=Rect("Gameplay Actions",dynamicHud,new Vector2(680,155),new Vector2(0,-770));
            var hint=Image("Hint",actions,Sprite("Gameplay","hint"),new Vector2(150,150),new Vector2(-210,0));
            hint.raycastTarget=true; var hintButton=Undo.AddComponent<Button>(hint.gameObject);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(hintButton.onClick,game.RequestHint);
            var freeze=Image("Time Freeze",actions,Sprite("Gameplay","time_freeze"),new Vector2(150,150),Vector2.zero);
            freeze.raycastTarget=true;Set(hud,"freezeButton",Undo.AddComponent<Button>(freeze.gameObject));
            Image("Tile Break",actions,Sprite("Gameplay","tile_break"),new Vector2(150,150),new Vector2(210,0)).gameObject.SetActive(false);
            Set(hud,"root",screen); Set(hud,"gameplayScreen",screen); Set(hud,"levelRoot",levelRoot);
            Set(hud,"boardArea",boardArea); Set(hud,"trayArea",trayArea);
            Set(hud,"levelText",levelText); Set(hud,"timerText",timer); Set(hud,"timerPanel",timer.rectTransform);
            Set(hud,"coinText",coinText); Set(hud,"objectiveText",objective); Set(hud,"objectivePanel",objective.rectTransform);
            Set(hud,"comboText",combo); Set(hud,"comboBadge",combo.rectTransform); Set(hud,"starPanel",stars); Set(hud,"actionBar",actions);
            Set(hud,"backgroundImage",bg); Set(hud,"backgroundCrossfade",fade); Set(hud,"defaultBackgroundSprite",catalog.roomBackgrounds[0]);
            Set(hud,"objectiveImage",board.GetComponent<Image>()); Set(hud,"headerBandImage",null);
            Set(hud,"previousTestButton",null); Set(hud,"nextTestButton",null); ObjectArray(hud,"progressStars",starImages);
            var effects=(RectTransform)Get(hud,"fxLayer"); Undo.SetTransformParent(effects,screen,"Move gameplay effects"); effects.SetAsLastSibling();
            Set(game,"gameplayHud",hud);
            var home=game.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<HomeController>(true)).Single();
            var homeWallet=Child(reference.Find("Main Menu Screen"),"Coins BG");
            var existingBalance=homeWallet.Find("Balance");if(existingBalance!=null)Undo.DestroyObjectImmediate(existingBalance.gameObject);
            Set(home,"coinText",Label("Balance",homeWallet,new Vector2(160,65),new Vector2(25,0),30,"0"));
            // Preserve room furnishing and chapter gates behind the authored Rooms destination.
            var meta=Get(game,"metaView"); var metaRoot=(RectTransform)Get(meta,"metaOverlay");
            var rooms=reference.Find("Rooms Screen"); Undo.SetTransformParent(metaRoot,rooms,"Move room progression"); Stretch(metaRoot); metaRoot.gameObject.SetActive(false);
            BuildSelection(selection,game,catalog,starSprite,emptyStar);
            Undo.RecordObject(game,"Enable puzzle controller"); game.enabled=true;
            Undo.RecordObject(reference.gameObject,"Activate Reference UI"); reference.gameObject.SetActive(true);
            var wholePageFitter=reference.GetComponent<CatBlockPuzzle.KawaiiUI.SafeAreaFitter>();
            if(wholePageFitter!=null) Undo.DestroyObjectImmediate(wholePageFitter);
            Undo.RecordObject(reference,"Keep authored full-screen layout"); Stretch((RectTransform)reference);
            reference.parent.Find("Safe Area").gameObject.SetActive(false);
            foreach(Transform page in reference) page.gameObject.SetActive(page.name=="Main Menu Screen");
            if(!game.ValidatePreparedScene(out var error)) throw new InvalidOperationException(error);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene); EditorSceneManager.SaveScene(game.gameObject.scene);
            Undo.CollapseUndoOperations(group);
            return "Reference UI enabled; 10 chapters, 100 levels; authored gameplay HUD and chapter backgrounds wired.";
        }
        private static void BuildSelection(Transform screen,CatBlockPuzzleGame game,CatPuzzleContentCatalog catalog,Sprite earned,Sprite empty)
        {
            var previous=screen.Find("Chapter Board"); if(previous!=null) Undo.DestroyObjectImmediate(previous.gameObject);
            previous=screen.Find("Level Play"); if(previous!=null) Undo.DestroyObjectImmediate(previous.gameObject);
            var root=Rect("Chapter Board",screen,Vector2.zero,Vector2.zero);
            root.anchorMin=Vector2.zero; root.anchorMax=Vector2.one; root.offsetMin=new Vector2(40,255); root.offsetMax=new Vector2(-40,-565);
            var viewport=Rect("Viewport",root,Vector2.zero,Vector2.zero); Stretch(viewport); Undo.AddComponent<RectMask2D>(viewport.gameObject);
            var raycast=Undo.AddComponent<Image>(viewport.gameObject); raycast.color=Color.clear;
            var content=Rect("Chapters",viewport,new Vector2(0,3750),Vector2.zero);
            content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);
            var scroll=Undo.AddComponent<ScrollRect>(root.gameObject);scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            var slots=new LevelSelectionScreen.LevelSlot[100];
            var normal=Sprite("LevelSelection","level");
            var selected=Sprite("LevelSelection","level_selected");
            var locked=Sprite("LevelSelection","hue_saturation");
            var lockIcon=Sprite("LevelSelection","level_lock");
            for(int chapter=0;chapter<10;chapter++)
            {
                var card=Image("Chapter "+(chapter+1),content,Sprite("LevelSelection","panel_level_selection_secondary"),new Vector2(930,350),new Vector2(0,-187.5f-chapter*375));
                card.rectTransform.anchorMin=card.rectTransform.anchorMax=new Vector2(.5f,1); card.preserveAspect=false;
                string[] portraits={"image_alt","image"};
                var portraitRoot=Rect("Room Portrait",card.transform,new Vector2(270,300),new Vector2(-300,0));Undo.AddComponent<RectMask2D>(portraitRoot.gameObject);
                // Match every card to Chapter 1's approved cat-portrait treatment.
                // Alternate the two supplied portrait artworks until unique chapter art is provided.
                var portrait=Image("Art",portraitRoot,Sprite("LevelSelection",portraits[chapter%portraits.Length]),Vector2.zero,Vector2.zero);
                Stretch(portrait.rectTransform);var fit=Undo.AddComponent<AspectRatioFitter>(portrait.gameObject);fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=portrait.sprite.rect.width/portrait.sprite.rect.height;
                Label("Chapter Title",card.transform,new Vector2(610,65),new Vector2(110,122),28,"Chapter "+(chapter+1).ToString("00"));
                var inner=Image("Levels",card.transform,Sprite("LevelSelection","level_selection_panel_internal"),new Vector2(630,230),new Vector2(115,-34)); inner.preserveAspect=false;
                for(int local=0;local<10;local++)
                {
                    int index=chapter*10+local;
                    var tile=Image("Level "+(index+1),inner.transform,normal,new Vector2(100,94),new Vector2((local%5-2)*114,local<5?50:-50));
                    tile.raycastTarget=true;var button=Undo.AddComponent<Button>(tile.gameObject);button.targetGraphic=tile;
                    var number=Label("Number",tile.transform,new Vector2(90,58),new Vector2(0,12),33,(local+1).ToString());
                    var icon=Image("Lock",tile.transform,lockIcon,new Vector2(46,52),new Vector2(0,8));
                    var stars=Enumerable.Range(0,3).Select(s=>Image("Star "+(s+1),tile.transform,empty,new Vector2(23,22),new Vector2((s-1)*25,-28))).ToArray();
                    slots[index]=new LevelSelectionScreen.LevelSlot{button=button,number=number,background=tile,locked=icon.gameObject,stars=stars};
                }
            }
            var play=Image("Level Play",screen,Sprite("LevelSelection","play_btn"),new Vector2(600,170),Vector2.zero);
            play.rectTransform.anchorMin=play.rectTransform.anchorMax=new Vector2(.5f,0);play.rectTransform.anchoredPosition=new Vector2(0,135);play.raycastTarget=true;
            var playButton=Undo.AddComponent<Button>(play.gameObject);
            var controller=screen.GetComponent<LevelSelectionScreen>();if(controller==null)controller=Undo.AddComponent<LevelSelectionScreen>(screen.gameObject);
            var wallet=Child(screen,"CoinsHud (1)");var oldBalance=wallet.Find("Balance");if(oldBalance!=null)Undo.DestroyObjectImmediate(oldBalance.gameObject);
            var coins=Label("Balance",wallet,new Vector2(160,65),new Vector2(25,0),30,"0");
            var selection=Label("Selected Level",play.transform,new Vector2(360,40),new Vector2(0,110),24);
            Set(controller,"gameplay",game);Set(controller,"playButton",playButton);Set(controller,"coinText",coins);Set(controller,"selectionText",selection);
            Set(controller,"normalSprite",normal);Set(controller,"selectedSprite",selected);Set(controller,"lockedSprite",locked);Set(controller,"earnedStar",earned);Set(controller,"emptyStar",empty);
            var so=new SerializedObject(controller);var array=so.FindProperty("levels");array.arraySize=100;
            for(int i=0;i<100;i++)
            {
                var p=array.GetArrayElementAtIndex(i);var slot=slots[i];
                p.FindPropertyRelative("button").objectReferenceValue=slot.button;p.FindPropertyRelative("number").objectReferenceValue=slot.number;
                p.FindPropertyRelative("background").objectReferenceValue=slot.background;p.FindPropertyRelative("locked").objectReferenceValue=slot.locked;
                var stars=p.FindPropertyRelative("stars");stars.arraySize=3;for(int s=0;s<3;s++)stars.GetArrayElementAtIndex(s).objectReferenceValue=slot.stars[s];
            }
            so.ApplyModifiedProperties();
        }
    }
}
