using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CatBlockPuzzle.Editor
{
    /// <summary>Authors the common production skin without changing puzzle definitions or input bindings.</summary>
    public static class FullGameUiAuthoring
    {
        private const string Art = "Assets/CatBlock_UI_New/Extended";
        private const string Delivery = "Assets/CatBlock_UI_New/UI/";
        private static readonly Color Ink = new Color(.38f, .153f, .035f);
        private static GameUiTheme theme;
        private static GameplayHudView pendingHud;
        private static GameplayPresentationAssets pendingAssets;
        private static LevelCompleteScreen pendingComplete;
        private static LevelFailScreen pendingFail;
        private static MetaProgressionView pendingMeta;
        public static string Schedule(GameplayHudView hud, GameplayPresentationAssets assets, LevelCompleteScreen complete, LevelFailScreen fail, MetaProgressionView meta)
        {
            pendingHud=hud;pendingAssets=assets;pendingComplete=complete;pendingFail=fail;pendingMeta=meta;
            SessionState.SetString("FullGameUiAuthoring","Queued native authoring command");
            EditorApplication.delayCall -= RunScheduled;
            EditorApplication.delayCall += RunScheduled;
            return "Queued compiled scene and prefab authoring command";
        }
        private static void RunScheduled()
        {
            try {
                SessionState.SetString("FullGameUiAuthoring","Updating scene");
                string scene=Apply(pendingHud,pendingAssets,pendingComplete,pendingFail,pendingMeta);
                SessionState.SetString("FullGameUiAuthoring",scene+" Updating shared prefabs...");
                string prefabs=SkinPrefabsAndConfigs();
                SessionState.SetString("FullGameUiAuthoring",scene+" "+prefabs);
            } catch(Exception e) {SessionState.SetString("FullGameUiAuthoring",e.ToString());Debug.LogException(e);}
        }
        private static T Get<T>(Object obj, string field) where T : Object => new SerializedObject(obj).FindProperty(field).objectReferenceValue as T;
        private static void Set(Object obj, string field, Object value)
        {
            Undo.RecordObject(obj, "Connect complete game UI"); var so = new SerializedObject(obj);
            var p = so.FindProperty(field); if (p == null) throw new InvalidOperationException("Missing field " + field);
            p.objectReferenceValue = value; so.ApplyModifiedProperties();
        }
        private static Sprite Supplied(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(Delivery + path);
        private static float Box(Vector2 p, float inset, float radius)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - 64), Mathf.Abs(p.y - 64)) - Vector2.one * (64 - inset - radius);
            return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
        }
        private static float Line(Vector2 p, Vector2 a, Vector2 b, float radius)
        { Vector2 ab = b-a; float t = Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude); return Vector2.Distance(p,a+ab*t)-radius; }
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#"+value,out var c); return c; }
        private static Sprite Draw(string name, Func<Vector2, Color> draw, bool sliced)
        {
            string path = Art + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path); if (existing != null) return existing;
            var texture = new Texture2D(128,128,TextureFormat.RGBA32,false) {name=name+" Texture",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels = new Color[128*128];
            for(int y=0;y<128;y++) for(int x=0;x<128;x++) pixels[y*128+x]=draw(new Vector2(x+.5f,y+.5f));
            texture.SetPixels(pixels);texture.Apply();
            var sprite = Sprite.Create(texture,new Rect(0,0,128,128),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,sliced?Vector4.one*36:Vector4.zero);
            sprite.name=name;AssetDatabase.CreateAsset(sprite,path);AssetDatabase.AddObjectToAsset(texture,sprite);return sprite;
        }
        private static Sprite Panel(string name, string top, string bottom, string border)
        {
            return Draw(name,p=> {
                float outer=Box(p,3,28); if(outer>1)return Color.clear;
                Color c=Hex(border);
                if(Box(p,7,25)<0)c=Hex("FFE3A1");
                if(Box(p,10,22)<0)c=Hex("FFFDF0");
                if(Box(p,13,19)<0)c=Color.Lerp(Hex(bottom),Hex(top),Mathf.Clamp01((p.y-13)/102));
                c.a=Mathf.Clamp01(1-outer);return c;
            },true);
        }
        private static Sprite Icon(string kind)
        {
            return Draw(kind,p=> {
                float d=1000; Color fill=Ink;
                if(kind=="Pause") {d=Mathf.Min(Line(p,new Vector2(45,37),new Vector2(45,91),7),Line(p,new Vector2(83,37),new Vector2(83,91),7));}
                else if(kind=="Clock") {
                    d=Mathf.Abs(Vector2.Distance(p,Vector2.one*64)-44)-4;
                    d=Mathf.Min(d,Line(p,Vector2.one*64,new Vector2(64,91),4));d=Mathf.Min(d,Line(p,Vector2.one*64,new Vector2(86,53),4));
                }
                else if(kind=="Freeze") {
                    fill=Hex("47C9EF");
                    for(int k=0;k<6;k++) {
                        float a=k*Mathf.PI/3;Vector2 v=new Vector2(Mathf.Cos(a),Mathf.Sin(a)),n=new Vector2(-v.y,v.x);
                        d=Mathf.Min(d,Line(p,Vector2.one*64,Vector2.one*64+v*43,5));
                        d=Mathf.Min(d,Line(p,Vector2.one*64+v*25,Vector2.one*64+v*35+n*12,4));
                        d=Mathf.Min(d,Line(p,Vector2.one*64+v*25,Vector2.one*64+v*35-n*12,4));
                    }
                }
                else if(kind=="Rotate") {
                    fill=Hex("37BDEB");float angle=Mathf.Atan2(p.y-64,p.x-64)*Mathf.Rad2Deg;
                    if(angle<130&&angle>-150)d=Mathf.Abs(Vector2.Distance(p,Vector2.one*64)-34)-7;
                    d=Mathf.Min(d,Line(p,new Vector2(40,96),new Vector2(36,76),7));d=Mathf.Min(d,Line(p,new Vector2(36,76),new Vector2(56,79),7));
                }
                else if(kind=="Pointer") {
                    d=Line(p,new Vector2(62,37),new Vector2(62,102),10);
                    d=Mathf.Min(d,Line(p,new Vector2(76,32),new Vector2(76,65),10));
                    d=Mathf.Min(d,Line(p,new Vector2(91,30),new Vector2(91,59),10));
                    d=Mathf.Min(d,Line(p,new Vector2(44,43),new Vector2(31,58),10));
                    d=Mathf.Min(d,Vector2.Distance(p,new Vector2(65,33))-31);fill=Hex("FFF3D9");
                }
                else if(kind=="Lock") {
                    float arch=Mathf.Abs(Vector2.Distance(p,new Vector2(64,76))-25)-6;
                    if(p.y>=76)d=arch;
                    d=Mathf.Min(d,Line(p,new Vector2(39,55),new Vector2(39,77),6));d=Mathf.Min(d,Line(p,new Vector2(89,55),new Vector2(89,77),6));
                    if(p.x>28&&p.x<100&&p.y>25&&p.y<65){if(Vector2.Distance(p,new Vector2(64,48))<7)return Ink;return Hex("FFD460");}
                    fill=Hex("D9A548");
                }
                else if(kind=="Empty Star") {
                    var points=new Vector2[10];for(int i=0;i<10;i++){float a=(90+i*36)*Mathf.Deg2Rad;points[i]=Vector2.one*64+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(i%2==0?49:24);}
                    bool inside=false;float edge=1000;for(int i=0,j=9;i<10;j=i++) {var a=points[i];var b=points[j];if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;edge=Mathf.Min(edge,Line(p,a,b,0));}
                    if(inside)return edge<4?Hex("DAB16D"):Hex("E7D2AB");return Color.clear;
                }
                if(d>3)return Color.clear;
                Color color=d>0?Ink:fill;color.a=Mathf.Clamp01(3-d);return color;
            },false);
        }
        public static GameUiTheme CreateTheme(GameplayPresentationAssets presentation)
        {
            Directory.CreateDirectory(Art);AssetDatabase.Refresh();
            theme=AssetDatabase.LoadAssetAtPath<GameUiTheme>("Assets/_GameData/System/Resources/CatBlockPuzzle/GameUiTheme.asset");
            if(theme==null){theme=ScriptableObject.CreateInstance<GameUiTheme>();AssetDatabase.CreateAsset(theme,"Assets/_GameData/System/Resources/CatBlockPuzzle/GameUiTheme.asset");}
            theme.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/_GameData/Art/Font/fredoka-one.one-regular.ttf");
            theme.panel=Panel("Cream Panel","FFFDF2","FFE8BE","D49A4E");theme.goldPanel=Panel("Honey Panel","FFF3C9","FFD77B","CB8737");
            theme.board=Panel("Puzzle Board","EBD2A5","D9B879","AF773D");theme.tile=Panel("Cat Tray Tile","FFF9DF","F5DBAB","D7AC6E");
            theme.icePanel=Panel("Frozen Timer","F0FDFF","C9F1FA","71CADD");
            theme.primaryButton=Panel("Green Button","A6F34D","59C814","369008");theme.secondaryButton=Panel("Amber Button","FFF39A","FFC532","D98D14");
            theme.dangerButton=Panel("Coral Button","FF9293","F65E6D","C53C55");theme.blueButton=Panel("Blue Button","9DE5FF","42BEEB","2188B7");
            theme.background=Supplied("BG/LV_BG.png");theme.coin=Supplied("icons/SH_CoinCat_icon.png");theme.hint=Supplied("icons/DRP_Hint_icon.png");
            theme.freeze=Icon("Freeze");theme.rotate=Icon("Rotate");theme.pause=Icon("Pause");theme.clock=Icon("Clock");theme.pointer=Icon("Pointer");
            theme.emptyStar=Icon("Empty Star");theme.lockIcon=Icon("Lock");theme.star=Supplied("icons/LV_FilledStar_icon.png");theme.close=Supplied("btns/SH_CloseRed_btn.png");theme.back=Supplied("btns/LV_Back_btn.png");
            theme.happyCat=Get<Sprite>(presentation,"catPortraitSprites.Array.data[0]");theme.sadCat=Get<Sprite>(presentation,"catPortraitSprites.Array.data[8]");
            EditorUtility.SetDirty(theme);AssetDatabase.SaveAssetIfDirty(theme);return theme;
        }

        private static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"Create production UI");var rt=(RectTransform)go.transform;rt.SetParent(parent,false);go.layer=parent.gameObject.layer;Place(rt,position,size);return rt;
        }
        private static void Place(RectTransform rt,Vector2 position,Vector2 size)
        {
            Undo.RecordObject(rt,"Lay out production UI");rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.one*.5f;rt.anchoredPosition3D=new Vector3(position.x,position.y,0);rt.sizeDelta=size;rt.localScale=Vector3.one;rt.localRotation=Quaternion.identity;
        }
        private static Image Image(Transform parent,string name,Sprite sprite,Vector2 position,Vector2 size,bool sliced=true)
        {
            var rt=Rect(parent,name,position,size);var im=Undo.AddComponent<Image>(rt.gameObject);GameUiTheme.Skin(im,sprite,sliced);im.raycastTarget=false;return im;
        }
        private static Text Text(Transform parent,string name,string value,Vector2 position,Vector2 size,int fontSize=40,Color? color=null)
        {
            var rt=Rect(parent,name,position,size);var text=Undo.AddComponent<Text>(rt.gameObject);text.font=theme.font;text.text=value;text.fontSize=fontSize;text.alignment=TextAnchor.MiddleCenter;text.color=color??theme.ink;text.raycastTarget=false;text.resizeTextForBestFit=true;text.resizeTextMinSize=18;text.resizeTextMaxSize=fontSize;return text;
        }
        private static void Reparent(RectTransform rt,Transform parent,Vector2 position,Vector2 size)
        {Undo.SetTransformParent(rt,parent,"Preserve game control");Place(rt,position,size);}
        private static void ButtonSkin(Button button,Sprite skin,string label)
        {
            Undo.RecordObject(button,"Restyle button");var image=button.GetComponent<Image>();if(image==null)image=Undo.AddComponent<Image>(button.gameObject);Undo.RecordObject(image,"Restyle button artwork");
            GameUiTheme.Skin(image,skin);image.raycastTarget=true;button.targetGraphic=image;button.transition=Selectable.Transition.ColorTint;
            var colors=button.colors;colors.normalColor=colors.highlightedColor=Color.white;colors.pressedColor=new Color(.9f,.92f,.83f);colors.disabledColor=new Color(.82f,.8f,.74f,.7f);button.colors=colors;
            var texts=button.GetComponentsInChildren<Text>(true);
            if(texts.Length==0&&button.GetComponentsInChildren<TMP_Text>(true).Length==0&&label!=null) {
                var text=Text(button.transform,"Label",label,Vector2.zero,((RectTransform)button.transform).sizeDelta-new Vector2(30,12),38,skin==theme.primaryButton||skin==theme.dangerButton?Color.white:theme.ink);text.raycastTarget=false;
            } else foreach(var text in texts){text.font=theme.font;text.color=skin==theme.primaryButton||skin==theme.dangerButton?Color.white:theme.ink;if(label!=null)text.text=label;}
        }

        private static Sprite MapLegacy(Sprite sprite,string field="")
        {
            if(sprite==null)return null;string path=AssetDatabase.GetAssetPath(sprite).ToLowerInvariant();
            if(!path.StartsWith("assets/art/catblockpuzzleui/"))return sprite;
            if(path.Contains("newcats"))return sprite;
            string name=System.IO.Path.GetFileNameWithoutExtension(path);
            if(name.Contains("lock"))return theme.lockIcon;
            if(name.Contains("black")&&name.Contains("white"))return theme.emptyStar;
            if(path.Contains("gameplayrooms")||name=="image"||name=="image_alt")return theme.background;
            if(name.Contains("cat")&&(path.Contains("levellose")||path.Contains("levelwin")))return path.Contains("levellose")?theme.sadCat:theme.happyCat;
            if(name.Contains("hand"))return theme.pointer;
            if(name.Contains("board")||name.Contains("panel")||name=="bg"||name.StartsWith("bg_"))return name.Contains("froze")?theme.icePanel:theme.panel;
            if(name.Contains("star"))return name.Contains("white")||name.Contains("disable")?theme.emptyStar:theme.star;
            if(name.Contains("coin"))return theme.coin;
            if(name.Contains("freeze")||name.Contains("froze"))return theme.freeze;
            if(name.Contains("time"))return theme.clock;
            if(name.Contains("hint"))return theme.hint;
            if(name.Contains("rotate")||name.Contains("reset"))return theme.rotate;
            if(name.Contains("pause"))return theme.pause;
            if(name.Contains("close"))return theme.close;
            if(name.Contains("back"))return theme.back;
            return field.ToLowerInvariant().Contains("icon")?theme.hint:theme.panel;
        }
        private static void RemapSerialized(Object obj)
        {
            var so=new SerializedObject(obj);var it=so.GetIterator();bool changed=false;
            while(it.NextVisible(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue is Sprite sprite){var next=MapLegacy(sprite,it.propertyPath);if(next!=sprite){it.objectReferenceValue=next;changed=true;}}
            if(changed){Undo.RecordObject(obj,"Replace legacy sprite references");so.ApplyModifiedProperties();EditorUtility.SetDirty(obj);}
        }
        private static void StyleTree(GameObject root)
        {
            foreach(var image in root.GetComponentsInChildren<Image>(true)) {
                string path=image.sprite!=null?AssetDatabase.GetAssetPath(image.sprite):"";
                if(path.Contains("/NewCats/")||path.Contains("/Characters/"))continue;
                bool legacy=path.StartsWith("Assets/Art/CatBlockPuzzleUI/");bool basic=path.EndsWith("RoundedBox.asset");
                if(legacy||basic){Undo.RecordObject(image,"Replace old UI artwork");float alpha=image.color.a;var sprite=legacy?MapLegacy(image.sprite):theme.panel;GameUiTheme.Skin(image,sprite,sprite==theme.panel||sprite==theme.goldPanel||sprite==theme.tile||sprite==theme.icePanel||sprite==theme.board);if(alpha==0)image.color=Color.clear;}
                foreach(var shadow in image.GetComponents<Shadow>()){Undo.RecordObject(shadow,"Restyle UI shadow");shadow.effectColor=shadow is Outline?Color.clear:new Color(.35f,.16f,.04f,.15f);shadow.effectDistance=new Vector2(0,-4);}
            }
            foreach(var text in root.GetComponentsInChildren<Text>(true)){Undo.RecordObject(text,"Apply production typography");text.font=theme.font;text.color=theme.ink;text.raycastTarget=false;}
            foreach(var button in root.GetComponentsInChildren<Button>(true)) {
                if(button.GetComponentInParent<CatBlocksUI.ScreenLayout>()!=null)continue;
                string name=button.name.ToLowerInvariant();Sprite sprite=name.Contains("home")||name.Contains("back")||name.Contains("skip")||name.Contains("close")?theme.secondaryButton:theme.primaryButton;
                ButtonSkin(button,sprite,null);
            }
            foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))if(component!=null)RemapSerialized(component);
        }

        public static string Apply(GameplayHudView hud,GameplayPresentationAssets presentation,LevelCompleteScreen complete,LevelFailScreen fail,MetaProgressionView meta)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Leave Play Mode before replacing UI");
            if(hud==null||presentation==null||complete==null||fail==null||meta==null)throw new InvalidOperationException("An inspected game UI object is missing");
            if(hud.transform.Find("Gameplay Safe Area")!=null)return "Production gameplay UI is already installed.";
            Directory.CreateDirectory("SceneBackups");EditorSceneManager.SaveScene(hud.gameObject.scene,"SceneBackups/BeforeCompleteUi_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Complete production game UI");
            try {
                CreateTheme(presentation);
                StyleGameplay(hud,presentation);
                StyleResult(complete,meta,true);StyleResult(fail,meta,false);
                var nav=Object.FindFirstObjectByType<ReferenceUiNavigation>(FindObjectsInactive.Include);
                foreach(var page in new[]{Get<GameObject>(nav,"roomsScreen"),Get<GameObject>(nav,"collectionScreen")})if(page!=null)StyleTree(page);
                StyleTree(hud.gameObject); // Includes test controls and every serialized frozen state.
                Set(presentation,"coinSprite",theme.coin);Set(presentation,"starSprite",theme.star);Set(presentation,"starOutlineSprite",theme.emptyStar);
                Set(presentation,"roundedBoxSprite",theme.tile);Set(presentation,"backIconSprite",theme.back);Set(presentation,"pauseIconSprite",theme.pause);Set(presentation,"hintIconSprite",theme.hint);Set(presentation,"resetIconSprite",theme.rotate);Set(presentation,"closeIconSprite",theme.close);
                var so=new SerializedObject(presentation);var backgrounds=so.FindProperty("themeBackgroundSprites");for(int i=0;i<backgrounds.arraySize;i++)backgrounds.GetArrayElementAtIndex(i).objectReferenceValue=theme.background;so.ApplyModifiedProperties();
                // Ensure the general button pass leaves the utility controls as icon buttons.
                StylePowerButtons(hud);StylePause(nav);
                foreach(var layout in Object.FindObjectsByType<CatBlocksUI.ScreenLayout>(FindObjectsInactive.Include,FindObjectsSortMode.None))foreach(var image in layout.GetComponentsInChildren<Image>(true))if(image.sprite!=null&&AssetDatabase.GetAssetPath(image.sprite).StartsWith("Assets/Art/CatBlockPuzzleUI/")){Undo.RecordObject(image,"Replace shared legacy UI icon");GameUiTheme.Skin(image,MapLegacy(image.sprite),false);}
                EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);EditorSceneManager.SaveScene(hud.gameObject.scene);
                Undo.CollapseUndoOperations(group);return "Saved gameplay, both result screens, room UI and shared production theme.";
            }catch{Undo.RevertAllDownToGroup(group);throw;}
        }

        private static void StyleGameplay(GameplayHudView hud,GameplayPresentationAssets presentation)
        {
            var root=(RectTransform)hud.transform;var safe=Rect(root,"Gameplay Safe Area",Vector2.zero,new Vector2(1080,1920));
            var background=Get<Image>(hud,"backgroundImage");var crossfade=Get<Image>(hud,"backgroundCrossfade");
            foreach(Transform child in root.Cast<Transform>().ToArray()) {
                if(child==safe||child==background.transform||child==crossfade.transform)continue;
                var rt=child as RectTransform;if(rt==null)continue;var pos=rt.anchoredPosition3D;var size=rt.sizeDelta;var scale=rt.localScale;Undo.SetTransformParent(rt,safe,"Group safe gameplay content");rt.anchoredPosition3D=pos;rt.sizeDelta=size;rt.localScale=scale;
            }
            var fitter=Undo.AddComponent<ProductionUiSafeArea>(hud.gameObject);Set(fitter,"content",safe);Set(hud,"root",safe);
            foreach(var image in new[]{background,crossfade}){Undo.RecordObject(image,"Use production gameplay background");GameUiTheme.Skin(image,theme.background,false);image.preserveAspect=false;}
            Set(hud,"defaultBackgroundSprite",theme.background);
            var board=root.GetComponentsInChildren<Image>(true).First(i=>i.name=="Board");GameUiTheme.Skin(board,theme.board);Place(board.rectTransform,new Vector2(0,110),new Vector2(930,920));
            var tray=root.GetComponentsInChildren<Image>(true).First(i=>i.name=="Pieces Panel");GameUiTheme.Skin(tray,theme.panel);Place(tray.rectTransform,new Vector2(0,-540),new Vector2(950,340));
            Place(Get<RectTransform>(hud,"boardArea"),new Vector2(0,110),new Vector2(850,840));Place(Get<RectTransform>(hud,"trayArea"),new Vector2(0,-540),new Vector2(870,270));
            var level=Get<Text>(hud,"levelText");Place(level.rectTransform.parent as RectTransform,new Vector2(0,785),new Vector2(450,112));GameUiTheme.Skin(level.transform.parent.GetComponent<Image>(),theme.goldPanel);level.fontSize=52;level.resizeTextMaxSize=52;
            var coin=Get<Text>(hud,"coinText");Place(coin.rectTransform.parent as RectTransform,new Vector2(-355,790),new Vector2(240,94));GameUiTheme.Skin(coin.transform.parent.GetComponent<Image>(),theme.panel);
            Place(coin.rectTransform,new Vector2(30,0),new Vector2(140,76));var coinIcon=coin.transform.parent.GetComponentsInChildren<Image>(true).First(i=>i.name=="Coin");GameUiTheme.Skin(coinIcon,theme.coin,false);Place(coinIcon.rectTransform,new Vector2(-76,0),new Vector2(70,70));
            var timer=Get<Text>(hud,"timerText");var timerRoot=timer.transform.parent as RectTransform;Place(timerRoot,new Vector2(0,650),new Vector2(510,100));GameUiTheme.Skin(timerRoot.GetComponent<Image>(),theme.panel);
            Place(timer.rectTransform,new Vector2(-80,0),new Vector2(150,78));timer.fontSize=42;
            var clock=timerRoot.GetComponentsInChildren<Image>(true).First(i=>i.name=="Time");GameUiTheme.Skin(clock,theme.clock,false);Place(clock.rectTransform,new Vector2(-198,0),new Vector2(58,58));
            var stars=Get<RectTransform>(hud,"starPanel");Place(stars,new Vector2(130,0),new Vector2(195,80));var starImages=stars.GetComponentsInChildren<Image>(true);for(int i=0;i<starImages.Length;i++){GameUiTheme.Skin(starImages[i],theme.star,false);Place(starImages[i].rectTransform,new Vector2((i-1)*60,0),new Vector2(54,54));}
            var freezeSkin=Get<TimerFreezeSkin>(hud,"timerFreezeSkin");Set(freezeSkin,"frozenBackground",theme.icePanel);Set(freezeSkin,"frozenIcon",theme.freeze);
            var fx=Rect(safe,"Gameplay Effects",Vector2.zero,new Vector2(1080,1920));Set(hud,"fxLayer",fx);
            var actions=Get<RectTransform>(hud,"actionBar");Place(actions,new Vector2(0,-790),new Vector2(820,160));
            var objectives=Get<Text>(hud,"objectiveText");if(objectives!=null)Place(objectives.rectTransform,new Vector2(0,569),new Vector2(800,48));
            var combo=Get<RectTransform>(hud,"comboBadge");if(combo!=null)Place(combo,new Vector2(0,-357),new Vector2(300,50));
            foreach(string obsolete in new[]{"Star Disabled","Star","Tile Break"})foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name==obsolete).ToArray())Undo.DestroyObjectImmediate(t.gameObject);
            Set(hud,"purchasePopupPrefab",AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/CatBlockPuzzle/PowerUpPurchasePopup.prefab"));
        }
        private static void StylePause(ReferenceUiNavigation nav)
        {
            var button=Get<Button>(nav,"pauseButton");Place(button.transform as RectTransform,new Vector2(420,790),new Vector2(100,100));ButtonSkin(button,theme.panel,null);
            foreach(var child in button.GetComponentsInChildren<Text>(true))Undo.DestroyObjectImmediate(child.gameObject);
            Image(button.transform,"Pause Icon",theme.pause,Vector2.zero,new Vector2(58,58),false);
        }
        private static void StylePowerButtons(GameplayHudView hud)
        {
            string[] fields={"hintButton","freezeButton","rotateButton"};Sprite[] icons={theme.hint,theme.freeze,theme.rotate};string[] labels={"Hint","Freeze","Rotate"};
            for(int i=0;i<fields.Length;i++){
                var button=Get<Button>(hud,fields[i]);Place(button.transform as RectTransform,new Vector2(i==0?-190:190,0),new Vector2(168,154));ButtonSkin(button,theme.goldPanel,null);
                foreach(var text in button.GetComponentsInChildren<Text>(true).Where(t=>t.name=="Label").ToArray())Undo.DestroyObjectImmediate(text.gameObject);
                foreach(var image in button.GetComponentsInChildren<Image>(true).Where(im=>im.name=="Rotate Icon").ToArray())Undo.DestroyObjectImmediate(image.gameObject);
                Image(button.transform,"Production Power Icon",icons[i],new Vector2(0,12),new Vector2(100,100),false);
                Text(button.transform,"Power Name",labels[i],new Vector2(0,-48),new Vector2(142,35),25);
                var count=Get<Text>(hud,i==0?"hintCountText":i==1?"freezeCountText":"rotateCountText");var badge=count.transform.parent as RectTransform;Place(badge,new Vector2(0,-98),new Vector2(108,40));GameUiTheme.Skin(badge.GetComponent<Image>(),theme.panel);Place(count.rectTransform,Vector2.zero,new Vector2(96,34));count.fontSize=26;count.resizeTextMaxSize=26;
            }
        }
        private static void StyleResult(MonoBehaviour controller,MetaProgressionView meta,bool won)
        {
            var root=(RectTransform)controller.transform;var buttons=new List<Button>();foreach(string field in won?new[]{"continueButton","doubleCoinsButton"}:new[]{"retryButton","homeButton","skipButton"}){var b=Get<Button>(controller,field);if(b!=null)buttons.Add(b);}
            var decorate=won?Get<Button>(meta,"metaDecorateNowButton"):null;if(decorate!=null)buttons.Add(decorate);
            var staging=Rect(root,"Preserved Controls",Vector2.zero,new Vector2(1080,1920));foreach(var b in buttons)Undo.SetTransformParent(b.transform,staging,"Preserve result button bindings");
            foreach(Transform child in root.Cast<Transform>().Where(t=>t!=staging).ToArray())Undo.DestroyObjectImmediate(child.gameObject);
            var dim=Image(root,"Dimmer",null,Vector2.zero,Vector2.zero);dim.color=new Color(.19f,.1f,.025f,.58f);dim.raycastTarget=true;dim.rectTransform.anchorMin=Vector2.zero;dim.rectTransform.anchorMax=Vector2.one;dim.rectTransform.offsetMin=dim.rectTransform.offsetMax=Vector2.zero;
            var safe=Rect(root,"Safe Area",Vector2.zero,new Vector2(1080,1920));var fitter=Undo.AddComponent<ProductionUiSafeArea>(root.gameObject);Set(fitter,"content",safe);
            var panel=Image(safe,won?"Win Panel":"Fail Panel",theme.panel,Vector2.zero,new Vector2(870,1350));Set(controller,"root",root.gameObject);Set(controller,"panel",panel.rectTransform);
            var title=Text(panel.transform,"Title",won?"Level complete!":"Try again!",new Vector2(0,515),new Vector2(760,110),66);Set(controller,"titleArtwork",title.rectTransform);
            var cat=Image(panel.transform,"Cat",won?theme.happyCat:theme.sadCat,new Vector2(0,285),new Vector2(270,270),false);
            Text(panel.transform,"Encouragement",won?"A purr-fect fit!":"Those cats still need you.",new Vector2(0,105),new Vector2(720,65),36);
            if(won){
                var stars=new Image[3];for(int i=0;i<3;i++)stars[i]=Image(panel.transform,"Result Star "+(i+1),theme.emptyStar,new Vector2((i-1)*158,-12),new Vector2(130,130),false);
                var so=new SerializedObject(controller);var array=so.FindProperty("stars");array.arraySize=3;for(int i=0;i<3;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=stars[i];so.FindProperty("sprites.blackAndWhite").objectReferenceValue=theme.emptyStar;so.FindProperty("sprites.disabled").objectReferenceValue=theme.emptyStar;so.FindProperty("sprites.winStars").objectReferenceValue=theme.star;so.ApplyModifiedProperties();
                var reward=Image(panel.transform,"Board",theme.goldPanel,new Vector2(0,-180),new Vector2(580,130));Image(reward.transform,"Coin",theme.coin,new Vector2(-175,0),new Vector2(86,86),false);var label=Text(reward.transform,"Reward","+0 coins",new Vector2(55,0),new Vector2(350,90),44);
                Set(controller,"coinBoard",reward.rectTransform);Set(controller,"coinReward",reward.rectTransform);Set(controller,"rewardText",label);
                var next=Get<Button>(controller,"continueButton");var twice=Get<Button>(controller,"doubleCoinsButton");Reparent(next.transform as RectTransform,panel.transform,new Vector2(0,-365),new Vector2(660,120));Reparent(twice.transform as RectTransform,panel.transform,new Vector2(0,-515),new Vector2(660,112));
                foreach(var b in new[]{next,twice})foreach(Transform child in b.transform.Cast<Transform>().ToArray())Undo.DestroyObjectImmediate(child.gameObject);
                ButtonSkin(next,theme.primaryButton,"Next level");ButtonSkin(twice,theme.secondaryButton,"2x coins  ·  Watch ad");
                Set(meta,"metaNextLevelButtonText",next.GetComponentInChildren<Text>(true));
                if(decorate!=null){Reparent(decorate.transform as RectTransform,panel.transform,new Vector2(0,-640),new Vector2(480,85));ButtonSkin(decorate,theme.secondaryButton,"Decorate room");decorate.gameObject.SetActive(false);}
            }else{
                Set(controller,"sadCatArtwork",cat.rectTransform);string[] names={"Retry","Home","Skip level"};for(int i=0;i<buttons.Count;i++){var button=buttons[i];Reparent(button.transform as RectTransform,panel.transform,new Vector2(0,-155-i*155),new Vector2(640,120));foreach(Transform child in button.transform.Cast<Transform>().ToArray())Undo.DestroyObjectImmediate(child.gameObject);ButtonSkin(button,i==0?theme.primaryButton:theme.secondaryButton,names[i]);}
            }
            Undo.DestroyObjectImmediate(staging.gameObject);root.gameObject.SetActive(false);
        }

        public static string SkinPrefabsAndConfigs()
        {
            theme=GameUiTheme.Active;if(theme==null)throw new InvalidOperationException("Create the theme first");int levels=0,popups=0;
            var paths=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&p.EndsWith(".prefab")&&(p.Contains("CatBlockPuzzle")||p.Contains("Sunny Slot"))).ToArray();
            foreach(var path in paths){
                var root=PrefabUtility.LoadPrefabContents(path);
                try{var view=root.GetComponent<CatPuzzleLevelView>();if(view!=null){theme.ApplyLevel(view);levels++;}else{StyleTree(root);popups++;}
                    if(path.Contains("Sunny Slot"))GameUiTheme.Skin(root.GetComponent<Image>(),theme.tile);
                    foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))if(component!=null)RemapSerialized(component);
                    if(path.Contains("PowerUpUnlockTutorial")){var tutorial=root.GetComponent<PowerUpUnlockTutorial>();if(tutorial!=null){var so=new SerializedObject(tutorial);so.FindProperty("handRotation").floatValue=0;so.ApplyModifiedProperties();}}
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            foreach(var path in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/_GameData/")&&p.EndsWith(".asset")).ToArray())foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))if(asset is ScriptableObject){RemapSerialized(asset);AssetDatabase.SaveAssetIfDirty(asset);}
            return "Restyled "+levels+" level prefabs and "+popups+" shared UI prefabs, including tutorials, purchases and the tray skin.";
        }

        public static string FinishScene()
        {
            theme=GameUiTheme.Active;if(theme==null)throw new InvalidOperationException("Production theme missing");
            theme.lockIcon=Icon("Lock");EditorUtility.SetDirty(theme);AssetDatabase.SaveAssetIfDirty(theme);
            foreach(var obj in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(obj!=null)RemapSerialized(obj);
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return "Saved remaining home, level picker and shared scene artwork references.";
        }

        public static string RetireLegacyArt()
        {
            theme=GameUiTheme.Active;
            if(theme==null)throw new InvalidOperationException("Production theme missing");
            foreach(string path in new[]{"Assets/_GameData/Art/UI/Prefabs/ComingSoonPopup.prefab","Assets/_GameData/Art/UI/Prefabs/Day 1.prefab","Assets/_GameData/Art/UI/Prefabs/Day 7.prefab"}) {
                var root=PrefabUtility.LoadPrefabContents(path);
                try{StyleTree(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            if(!AssetDatabase.IsValidFolder("Assets/CatBlock_UI_New/Characters"))AssetDatabase.CreateFolder("Assets/CatBlock_UI_New","Characters");
            for(int i=1;i<=9;i++) {
                string from="Assets/Art/CatBlockPuzzleUI/UI/Slicing/Gameplay/NewCats/Cat_ ("+i+").png";
                string to="Assets/CatBlock_UI_New/Characters/Cat_ ("+i+").png";
                if(AssetDatabase.LoadAssetAtPath<Sprite>(from)==null)continue;
                string error=AssetDatabase.MoveAsset(from,to);if(!string.IsNullOrEmpty(error))throw new IOException(error);
            }
            const string tray="Assets/Art/CatBlockPuzzleUI/UI/Sunny Slot.prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(tray)!=null) {
                string error=AssetDatabase.MoveAsset(tray,Art+"/Tray Slot.prefab");if(!string.IsNullOrEmpty(error))throw new IOException(error);
            }
            var refs=new List<string>();
            foreach(string path in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!p.StartsWith("Assets/Art/CatBlockPuzzleUI/")&&new[]{".unity",".prefab",".asset",".mat",".anim",".controller"}.Contains(System.IO.Path.GetExtension(p)))) {
                foreach(string dependency in AssetDatabase.GetDependencies(path,false))if(dependency.StartsWith("Assets/Art/CatBlockPuzzleUI/"))refs.Add(path+" -> "+dependency);
            }
            if(refs.Count>0)return "Legacy art retained because references still need migration:\n"+string.Join("\n",refs.Distinct());
            if(!AssetDatabase.DeleteAsset("Assets/Art/CatBlockPuzzleUI"))throw new IOException("Could not remove the retired UI artwork folder.");
            return "Moved the nine playable cats and tray skin to the production UI folder; removed the retired UI artwork folder.";
        }
    }
}
