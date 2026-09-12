using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CatBlockPuzzle.Tests
{
    public sealed class CatPuzzlePreparedSceneTests : PreparedScenePlayModeFixture
    {
        private int[] SceneIdentities() => Game.transform.root.GetComponentsInChildren<Transform>(true)
            .Select(t => t.gameObject.GetInstanceID()).OrderBy(i => i).ToArray();
        private int[] StaticUiIdentities()
        {
            var levelRoot = (Transform)Field("levelRoot");
            return Game.transform.root.GetComponentsInChildren<Transform>(true)
                .Where(t=>t==levelRoot || !t.IsChildOf(levelRoot))
                .Select(t=>t.gameObject.GetInstanceID()).OrderBy(i=>i).ToArray();
        }

        [UnityTest]
        public IEnumerator AllHundredLevels_KeepOnePrefab_AndRetriesReuseCurrentObjects()
        {
            int[] identities = StaticUiIdentities();
            var levelRoot = (Transform)Field("levelRoot");
            var canvas = (Canvas)Field("canvas");
            Assert.That(canvas.renderMode,Is.EqualTo(RenderMode.ScreenSpaceCamera));
            Assert.That(canvas.worldCamera,Is.EqualTo(Field("sceneCamera")));
            Assert.That(canvas.planeDistance,Is.EqualTo(3f));
            for (int i=0; i<100; i++)
            {
                Call("PreviewLevelForTesting",i);
                yield return null;
                var view = (Component)Field("loadedLevel");
                var type = view.GetType();
                Assert.That(view.gameObject.activeSelf,Is.True);
                Assert.That((int)type.GetField("levelIndex").GetValue(view),Is.EqualTo(i));
                Assert.That(levelRoot.childCount,Is.EqualTo(1));
                Assert.That(Field("boardRoot"),Is.EqualTo(type.GetField("board").GetValue(view)));
                var pieces = (IList)Field("pieces");
                Assert.That(pieces.Count,Is.EqualTo(((System.Array)type.GetField("pieces").GetValue(view)).Length));
                var retryIdentities = SceneIdentities();
                Call("ResetLevel");
                yield return null;
                Assert.That(Field("loadedLevel"),Is.SameAs(view),"Retry recreated the prefab");
                Assert.That(SceneIdentities(),Is.EqualTo(retryIdentities),"Retry rebuilt level " + (i+1));
                Assert.That(StaticUiIdentities(),Is.EqualTo(identities),"Prebuilt UI changed");
                var instances = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                    .Where(b=>b.GetType()==type).ToArray();
                Assert.That(instances.Length,Is.EqualTo(1),"Previous level instance was retained");
            }
            Call("PreviewLevelForTesting",0);
            yield return new WaitForSecondsRealtime(.85f);
        }

        [UnityTest]
        public IEnumerator RapidLevelChanges_ReleaseOldInstancesAndKeepAssignedInputs()
        {
            Call("PreviewLevelForTesting",1);
            Call("PreviewLevelForTesting",99);
            Call("PreviewLevelForTesting",10);
            yield return null;
            var view = (Component)Field("loadedLevel");
            Assert.That(((Transform)Field("levelRoot")).childCount,Is.EqualTo(1));
            Assert.That(UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Count(b=>b.GetType()==view.GetType()),Is.EqualTo(1));
            foreach (var piece in (System.Array)view.GetType().GetField("pieces").GetValue(view))
                foreach (string inputName in new[]{"slotInput","pieceInput"})
                {
                    var input = piece.GetType().GetField(inputName).GetValue(piece);
                    Assert.That(input.GetType().GetField("controller").GetValue(input),Is.SameAs(Game));
                }
            Call("PreviewLevelForTesting",0);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NavigationSettingsAndPoolExhaustion_DoNotCreateObjects()
        {
            int[] identities = SceneIdentities();
            var canvas = (Canvas)Field("canvas");
            for (int i=0;i<3;i++)
            {
                Call("OpenRoomHub"); yield return null;
                Call("OpenRoomDetail",0); yield return null;
                Call("CloseMetaToGameplay");
                Call("OpenPause"); yield return null;
                Assert.That(((RectTransform)Field("settingsOverlay")).gameObject.activeSelf,Is.True);
                Call("CloseSettings");
                Call("SpawnFixedBurst",new Vector2(270,480),300,Color.white);
                yield return null;
                Assert.That(SceneIdentities(),Is.EqualTo(identities));
                Call("PreviewLevelForTesting",0);
                yield return null;
                Assert.That(((Image[])Field("preparedEffects")).All(e=>!e.gameObject.activeSelf),Is.True);
            }
            foreach (var button in canvas.GetComponentsInChildren<Button>(true))
            {
                Assert.That(button.onClick.GetPersistentEventCount(),Is.EqualTo(1),button.name);
                Assert.That(button.onClick.GetPersistentTarget(0),Is.Not.Null,button.name);
            }
        }
    }
}
