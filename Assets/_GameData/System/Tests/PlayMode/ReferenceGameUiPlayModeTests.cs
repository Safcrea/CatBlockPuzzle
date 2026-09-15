using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CatBlockPuzzle.Tests
{
    public sealed class ReferenceGameUiPlayModeTests : PreparedScenePlayModeFixture
    {
        protected override string SceneName => "GameScene";
        private static MonoBehaviour Screen(string type) => Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(g=>g.GetType().Name==type);
        private static object Invoke(object target,string method,params object[] args) => target.GetType().GetMethod(method,BindingFlags.Public|BindingFlags.Instance).Invoke(target,args);

        [UnityTest]
        public IEnumerator SelectionPauseRestartAndHome_UseNewUiAndStopHiddenGameplay()
        {
            var system=Screen("GameSystem");var navigation=Screen("ReferenceUiNavigation");var selector=Screen("LevelSelectionScreen");
            Invoke(system,"GoHome");Invoke(navigation,"ShowLevels");yield return null;
            Assert.That((int)selector.GetType().GetProperty("SlotCount").GetValue(selector),Is.EqualTo(100));
            Invoke(selector,"Select",1);
            Assert.That((int)selector.GetType().GetProperty("SelectedLevel").GetValue(selector),Is.EqualTo(0),"Locked level was selected");
            Invoke(selector,"Play");yield return new WaitForSecondsRealtime(1.2f);
            Assert.That((bool)system.GetType().GetProperty("IsGameplayOpen").GetValue(system),Is.True);
            Assert.That(Field("timerRunning"),Is.EqualTo(true));
            var canvas=(Canvas)Game.GetType().GetProperty("PreparedCanvas").GetValue(Game);
            CaptureAssignedCamera(canvas,"ReferenceUI_Gameplay.png");
            var loaded=Field("loadedLevel");
            Invoke(system,"OpenPause");Assert.That(Time.timeScale,Is.Zero);Assert.That(Field("timerRunning"),Is.EqualTo(false));
            Invoke(system,"OpenSettings",true);Invoke(system,"ReturnToPause");Invoke(system,"ResumeGame");
            Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(Field("timerRunning"),Is.EqualTo(true));
            Invoke(system,"RestartLevel");yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(Field("loadedLevel"),Is.SameAs(loaded),"Restart rebuilt the current level prefab");
            Invoke(system,"GoHome");float remaining=(float)Field("levelRemainingSeconds");yield return new WaitForSecondsRealtime(.2f);
            Assert.That(Field("timerRunning"),Is.EqualTo(false));Assert.That((float)Field("levelRemainingSeconds"),Is.EqualTo(remaining));
            Invoke(navigation,"ShowLevels");yield return null;CaptureAssignedCamera(canvas,"ReferenceUI_LevelSelection.png");
        }

        [UnityTest]
        public IEnumerator CompletingAndFailing_UseSeparateResultScreens()
        {
            var system=Screen("GameSystem");Invoke(system,"StartLevel",0);yield return new WaitForSecondsRealtime(1.2f);
            var pieces=(IList)Field("pieces");
            foreach(var piece in pieces)
            {
                var definition=piece.GetType().GetField("Definition").GetValue(piece);
                int row=(int)definition.GetType().GetField("SolutionRow").GetValue(definition);
                int col=(int)definition.GetType().GetField("SolutionCol").GetValue(definition);
                Call("PlacePiece",piece,row,col,false);
            }
            yield return new WaitForSecondsRealtime(1f);
            Assert.That((bool)Screen("LevelCompleteScreen").GetType().GetProperty("IsOpen").GetValue(Screen("LevelCompleteScreen")),Is.True);
            Invoke(system,"RestartLevel");yield return new WaitForSecondsRealtime(1.2f);Call("FailLevel");yield return null;
            Assert.That((bool)Screen("LevelFailScreen").GetType().GetProperty("IsOpen").GetValue(Screen("LevelFailScreen")),Is.True);
        }
    }
}
