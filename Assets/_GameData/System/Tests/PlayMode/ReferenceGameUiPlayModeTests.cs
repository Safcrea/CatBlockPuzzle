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
        public IEnumerator Freeze_PreservesPieceInteractionPausesExpiresAndResetsOnRestart()
        {
            var system=Screen("GameSystem");Invoke(system,"StartLevel",0);yield return new WaitForSecondsRealtime(1.2f);
            Game.GetType().GetField("freezeDurationSeconds",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(Game,.35f);
            var hud=(Component)Field("gameplayHud");
            var button=(UnityEngine.UI.Button)hud.GetType().GetField("freezeButton",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
            float timer=(float)Field("levelRemainingSeconds");button.onClick.Invoke();
            Assert.That(Game.GetType().GetProperty("IsTimerFrozen").GetValue(Game),Is.EqualTo(true));
            Assert.That(button.interactable,Is.False);
            Assert.That(Field("inputLocked"),Is.EqualTo(false));
            var piece=((IList)Field("pieces"))[0];var rect=(RectTransform)piece.GetType().GetField("Rect").GetValue(piece);
            var canvas=(Canvas)Game.GetType().GetProperty("PreparedCanvas").GetValue(Game);
            var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {pointerId=9,position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,rect.position)};
            Assert.That(Call("BeginPieceDrag",piece,pointer),Is.EqualTo(true));Call("CancelActiveDragToRest");
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That((float)Field("levelRemainingSeconds"),Is.EqualTo(timer));
            Invoke(system,"OpenPause");float frozen=(float)Field("freezeRemainingSeconds");yield return new WaitForSecondsRealtime(.2f);
            Assert.That((float)Field("freezeRemainingSeconds"),Is.EqualTo(frozen));
            Invoke(system,"ResumeGame");yield return new WaitForSecondsRealtime(.5f);
            Assert.That(Game.GetType().GetProperty("IsTimerFrozen").GetValue(Game),Is.EqualTo(false));
            Assert.That((float)Field("levelRemainingSeconds"),Is.LessThan(timer));
            Assert.That(Invoke(system,"TryUseFreezePowerUp"),Is.EqualTo(false));
            Invoke(system,"RestartLevel");yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(button.interactable,Is.True);Assert.That((float)Field("freezeRemainingSeconds"),Is.Zero);
            button.onClick.Invoke();Invoke(system,"GoHome");frozen=(float)Field("freezeRemainingSeconds");yield return new WaitForSecondsRealtime(.2f);
            Assert.That((float)Field("freezeRemainingSeconds"),Is.EqualTo(frozen));
        }

        [UnityTest]
        public IEnumerator DailyCardAndClaimButton_ShareRequestsAndConfirmedClaimChangesBothSprites()
        {
            var system=Screen("GameSystem");var navigation=Screen("ReferenceUiNavigation");Invoke(system,"GoHome");Invoke(navigation,"ShowDailyReward");yield return null;
            var daily=Screen("DailyRewardScreen");
            var days=(System.Array)daily.GetType().GetField("days",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(daily);
            var day=days.GetValue(0);var type=day.GetType();
            var card=(UnityEngine.UI.Button)type.GetField("cardButton").GetValue(day);
            var claim=(UnityEngine.UI.Button)type.GetField("claimButton").GetValue(day);
            var cardImage=(UnityEngine.UI.Image)type.GetField("cardImage").GetValue(day);
            var claimImage=(UnityEngine.UI.Image)type.GetField("claimImage").GetValue(day);
            int requests=0;var action=(UnityEngine.Events.UnityEvent)type.GetField("claimRequested").GetValue(day);action.AddListener(()=>requests++);
            card.onClick.Invoke();claim.onClick.Invoke();Assert.That(requests,Is.EqualTo(2));
            Invoke(daily,"SetDayClaimed",1,true);
            Assert.That(cardImage.sprite,Is.EqualTo(type.GetField("claimedCardSprite").GetValue(day)));
            Assert.That(claimImage.sprite,Is.EqualTo(type.GetField("claimedClaimSprite").GetValue(day)));
            Assert.That(card.interactable,Is.False);Assert.That(claim.interactable,Is.False);
            card.onClick.Invoke();claim.onClick.Invoke();Assert.That(requests,Is.EqualTo(2),"Claimed card requested a duplicate reward");
            for(int i=0;i<3;i++){Invoke(system,"GoHome");Invoke(navigation,"ShowDailyReward");yield return null;}
            Assert.That(cardImage.sprite,Is.EqualTo(type.GetField("claimedCardSprite").GetValue(day)));
            var canvas=(Canvas)Game.GetType().GetProperty("PreparedCanvas").GetValue(Game);CaptureAssignedCamera(canvas,"ReferenceUI_DailyClaimed.png");
            Invoke(daily,"SetDayClaimed",1,false);card.onClick.Invoke();Assert.That(requests,Is.EqualTo(3),"Reopening the screen accumulated click listeners");
            Assert.That(cardImage.sprite,Is.EqualTo(type.GetField("availableCardSprite").GetValue(day)));
            Assert.That(claimImage.sprite,Is.EqualTo(type.GetField("availableClaimSprite").GetValue(day)));
        }

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
            Invoke(system,"RestartLevel");Invoke(system,"OpenPause");yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(Field("timerRunning"),Is.EqualTo(false),"Opening animation restarted a paused timer");
            Invoke(system,"ResumeGame");Assert.That(Field("timerRunning"),Is.EqualTo(true));
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
