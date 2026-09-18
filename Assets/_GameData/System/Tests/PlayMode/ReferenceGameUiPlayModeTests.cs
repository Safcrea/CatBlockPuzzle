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
        public IEnumerator ProgressStars_DrainBetweenTimerTicksPauseAndResetWithoutDuplicateBackgrounds()
        {
            var system = Screen("GameSystem");
            Invoke(system, "ShowGameplayPage");
            Call("PreviewLevelForTesting", 0);
            yield return new WaitForSecondsRealtime(1.2f);
            var hud = (Component)Field("gameplayHud");
            var stars = (UnityEngine.UI.Image[])hud.GetType().GetField("progressStars", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud);
            Game.GetType().GetField("levelRemainingSeconds", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(Game, 90.5f);
            Game.GetType().GetField("lastTimerSecond", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(Game, 91);
            Call("UpdateTimerDisplay");
            Assert.That(stars[2].type, Is.EqualTo(UnityEngine.UI.Image.Type.Filled));
            Assert.That(stars[0].fillAmount, Is.EqualTo(1f));
            float before = stars[2].fillAmount;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(Field("lastTimerSecond"), Is.EqualTo(91), "This must update before the timer label changes.");
            Assert.That(stars[2].fillAmount, Is.LessThan(before));
            Invoke(system, "OpenPause");
            float paused = stars[2].fillAmount;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(stars[2].fillAmount, Is.EqualTo(paused));
            Invoke(system, "ResumeGame");
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(stars[2].fillAmount, Is.LessThan(paused));
            Call("PreviewLevelForTesting", 0);
            Assert.That(stars.All(s => s.fillAmount == 1f), Is.True);
            var backgrounds = (UnityEngine.UI.Image[])Field("progressStarBackgrounds");
            Assert.That(backgrounds.Length, Is.EqualTo(3));
            Assert.That(stars[0].transform.parent.GetComponentsInChildren<UnityEngine.UI.Image>(true).Length, Is.EqualTo(6));
            for (int i = 0; i < stars.Length; i++)
            {
                Assert.That(backgrounds[i].transform.GetSiblingIndex(), Is.LessThan(stars[i].transform.GetSiblingIndex()));
                Assert.That(backgrounds[i].raycastTarget, Is.False);
                Assert.That(backgrounds[i].rectTransform.anchoredPosition, Is.EqualTo(stars[i].rectTransform.anchoredPosition));
            }
        }

        [UnityTest]
        public IEnumerator DailyRewardBadge_KeepsPoppingAfterDismissalUntilClaimed()
        {
            var system = Screen("GameSystem");
            var navigation = Screen("ReferenceUiNavigation");
            Invoke(system, "GoHome");
            yield return null;
            var home = Screen("HomeController");
            var badge = (RectTransform)home.GetType().GetField("dailyRewardDot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(home);
            Assert.That(badge, Is.Not.Null);
            Assert.That(badge.gameObject.activeInHierarchy, Is.True);
            for (int visit = 0; visit < 2; visit++)
            {
                Invoke(navigation, "ShowDailyReward");
                yield return new WaitForSecondsRealtime(.3f);
                Invoke(system, "GoHome");
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(badge.gameObject.activeInHierarchy, Is.True, "Dismissing a reward without claiming must preserve its badge.");
                var pulse = badge.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "MenuAttentionPulse");
                Assert.That(pulse.isActiveAndEnabled, Is.True);
                Vector3 scale = badge.localScale;
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(Vector3.Distance(scale, badge.localScale), Is.GreaterThan(.001f), "The badge must pop on returning Home.");
            }
            Invoke(navigation, "ShowDailyReward");
            yield return new WaitForSecondsRealtime(.3f);
            Invoke(Screen("DailyRewardScreen"), "RequestClaim", 1);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That((bool)home.GetType().GetProperty("IsOpen").GetValue(home), Is.True);
            Assert.That(badge.gameObject.activeSelf, Is.False, "A successful claim should hide the badge for today.");
            Assert.That((bool)navigation.GetType().GetProperty("HasUnreadDailyReward").GetValue(navigation), Is.False);
        }

        [UnityTest]
        public IEnumerator ShopAndComingSoon_KeepBalanceAndNavigationVisible()
        {
            var system = Screen("GameSystem");
            var navigation = Screen("ReferenceUiNavigation");
            Invoke(system, "GoHome");
            Invoke(navigation, "ShowShop");
            yield return null;
            var shop = Screen("ShopScreen");
            var label = (Component)shop.GetType().GetField("coinLabel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(shop);
            Assert.That(label, Is.Not.Null);
            Assert.That(label.gameObject.activeInHierarchy, Is.True);
            Assert.That(label.GetType().GetProperty("text").GetValue(label), Is.EqualTo(system.GetType().GetProperty("CurrentCoins").GetValue(system).ToString()));
            Invoke(system, "GoHome");
            var home = Screen("HomeController");
            foreach (string field in new[] { "collectionButton", "roomsButton" })
            {
                var button = (UnityEngine.UI.Button)home.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(home);
                Assert.That(button.gameObject.activeInHierarchy, Is.True);
                button.onClick.Invoke();
                yield return new WaitForSecondsRealtime(.3f);
                var popup = Screen("ComingSoonPopup");
                Assert.That(popup.gameObject.activeInHierarchy, Is.True);
                Assert.That(button.gameObject.activeInHierarchy, Is.True, "Opening a placeholder must preserve the home page.");
                Invoke(popup, "Close");
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(popup.gameObject.activeSelf, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator PauseTransitions_RunWhileTimeIsStoppedAndResumeAfterDismissal()
        {
            var system = Screen("GameSystem");
            Invoke(system, "StartLevel", 0);
            yield return new WaitForSecondsRealtime(1.2f);
            Invoke(system, "OpenPause");
            var pause = Screen("PauseMenu");
            var root = (GameObject)pause.GetType().GetField("root", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(pause);
            Assert.That(Time.timeScale, Is.Zero);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(root.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Invoke(system, "OpenSettings", true);
            yield return new WaitForSecondsRealtime(.3f);
            var settings = Screen("SettingsMenu");
            var close = (UnityEngine.UI.Button)settings.GetType().GetField("closeButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(settings);
            close.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(root.activeInHierarchy, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            var resume = (UnityEngine.UI.Button)pause.GetType().GetField("resumeButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(pause);
            resume.onClick.Invoke();
            Assert.That(Time.timeScale, Is.Zero, "Gameplay must stay paused until the dismissal finishes.");
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(root.activeSelf, Is.False);
            Assert.That(Time.timeScale, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator MainMenu_KeepsAuthoredFullCanvasLayoutInPlayMode()
        {
            Invoke(Screen("GameSystem"),"GoHome");yield return null;
            var canvas=(Canvas)Game.GetType().GetProperty("PreparedCanvas").GetValue(Game);
            var reference=(RectTransform)canvas.transform.Find("Reference UI");
            var bg=(RectTransform)reference.Find("Main Menu Screen/BG");
            Assert.That(reference.anchorMin,Is.EqualTo(Vector2.zero));Assert.That(reference.anchorMax,Is.EqualTo(Vector2.one));
            Assert.That(reference.rect.size,Is.EqualTo(((RectTransform)canvas.transform).rect.size));
            Assert.That(bg.rect.size,Is.EqualTo(reference.rect.size));
            CaptureAssignedCamera(canvas,"ReferenceUI_MainMenuFullScreen.png");
        }

        [UnityTest]
        public IEnumerator Freeze_PreservesPieceInteractionPausesExpiresAndResetsOnRestart()
        {
            var system=Screen("GameSystem");Invoke(system,"ShowGameplayPage");Call("PreviewLevelForTesting",4);yield return new WaitForSecondsRealtime(1.2f);
            Game.GetType().GetField("freezeDurationSeconds",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(Game,.35f);
            var hud=(Component)Field("gameplayHud");
            var button=(UnityEngine.UI.Button)hud.GetType().GetField("freezeButton",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
            float timer=(float)Field("levelRemainingSeconds");button.onClick.Invoke();
            var stars=(UnityEngine.UI.Image[])hud.GetType().GetField("progressStars",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
            float frozenStarFill=stars[2].fillAmount;
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
            Assert.That(stars[2].fillAmount,Is.EqualTo(frozenStarFill));
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
        public IEnumerator PowerUpUnlocks_TeachOnceAndKeepTimerStoppedUntilAcknowledged()
        {
            PlayerPrefs.DeleteKey("CatBlockPuzzle.Tutorial.Hint");
            PlayerPrefs.DeleteKey("CatBlockPuzzle.Tutorial.Freeze");
            var hud = (Component)Field("gameplayHud");
            var hint = (UnityEngine.UI.Button)hud.GetType().GetField("hintButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud);
            var freeze = (UnityEngine.UI.Button)hud.GetType().GetField("freezeButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud);
            var system = Screen("GameSystem");
            Invoke(system, "ShowGameplayPage");
            Call("PreviewLevelForTesting", 0);
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(hint.gameObject.activeSelf, Is.False);
            Assert.That(freeze.gameObject.activeSelf, Is.False);
            Assert.That(Invoke(system, "TryUseHintPowerUp"), Is.EqualTo(false));
            Assert.That(Invoke(system, "TryUseFreezePowerUp"), Is.EqualTo(false));
            foreach (int index in new[] { 2, 4 })
            {
                Call("PreviewLevelForTesting", index);
                yield return new WaitForSecondsRealtime(1.2f);
                Assert.That(hint.gameObject.activeSelf, Is.True);
                Assert.That(freeze.gameObject.activeSelf, Is.EqualTo(index == 4));
                Assert.That(Field("timerRunning"), Is.EqualTo(false));
                Assert.That(Field("inputLocked"), Is.EqualTo(true));
                var canvas = (Canvas)Game.GetType().GetProperty("PreparedCanvas").GetValue(Game);
                var popup = canvas.transform.Find("Power Up Tutorial");
                Assert.That(popup, Is.Not.Null);
                popup.GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(Field("timerRunning"), Is.EqualTo(true));
                Invoke(system, "RestartLevel");
                yield return new WaitForSecondsRealtime(1.2f);
                Assert.That(Game.GetType().GetProperty("IsPowerUpTutorialOpen").GetValue(Game), Is.EqualTo(false));
            }
            Invoke(system, "LoadPreviousLevel");
            Assert.That(Game.GetType().GetProperty("CurrentLevelNumber").GetValue(Game), Is.EqualTo(4));
            Invoke(system, "LoadNextLevel");
            Assert.That(Game.GetType().GetProperty("CurrentLevelNumber").GetValue(Game), Is.EqualTo(5));
            Assert.That(Field("levelNavigationTesting"), Is.EqualTo(true));
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
            Invoke(navigation,"ShowLevels");yield return new WaitForSecondsRealtime(1.2f);CaptureAssignedCamera(canvas,"ReferenceUI_LevelSelection.png");
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

        [UnityTest]
        public IEnumerator SadFailPresentation_RunsWhilePausedRestoresOnRetryAndReusesTears()
        {
            var fail = Screen("LevelFailScreen");
            var system = Screen("GameSystem");
            Invoke(system, "StartLevel", 0);
            yield return new WaitForSecondsRealtime(1.2f);
            Call("StopLevelTimer");
            Invoke(fail, "EnsureBindings");
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var cat = (RectTransform)fail.GetType().GetField("sadCatArtwork", flags).GetValue(fail);
            Assert.That(cat, Is.Not.Null);
            Vector3 scale = cat.localScale;
            Vector2 position = cat.anchoredPosition;
            Quaternion rotation = cat.localRotation;
            Time.timeScale = 0f;
            try
            {
                Invoke(fail, "Show", "Try Again", "Time is up");
                yield return new WaitForSecondsRealtime(.18f);
                Assert.That(Vector2.Distance(cat.anchoredPosition, position), Is.GreaterThan(.01f), "The entrance uses unscaled time.");
                var retry = (UnityEngine.UI.Button)fail.GetType().GetField("retryButton", flags).GetValue(fail);
                Assert.That(retry.interactable, Is.True, "Retry stays usable during the reaction.");
                retry.onClick.Invoke();
                Assert.That((bool)fail.GetType().GetProperty("IsOpen").GetValue(fail), Is.False);
                Assert.That(cat.localScale, Is.EqualTo(scale));
                Assert.That(cat.anchoredPosition, Is.EqualTo(position));
                Assert.That(cat.localRotation, Is.EqualTo(rotation));
                Invoke(fail, "Show", "Try Again", "Time is up");
                yield return new WaitForSecondsRealtime(1.9f);
                var tears = (UnityEngine.UI.Image[])fail.GetType().GetField("tears", flags).GetValue(fail);
                Assert.That(tears.Length, Is.EqualTo(2));
                Assert.That(tears.All(t => t.gameObject.activeInHierarchy && !t.raycastTarget), Is.True);
                Assert.That(cat.GetComponentsInChildren<UnityEngine.UI.Image>(true).Count(i => i.name.StartsWith("Sad Tear ")), Is.EqualTo(2));
                var canvas = (Canvas)Game.GetType().GetProperty("PreparedCanvas").GetValue(Game);
                CaptureAssignedCamera(canvas, "ReferenceUI_SadFail.png");
                Invoke(system, "GoHome");
                Assert.That(tears.All(t => !t.gameObject.activeSelf), Is.True);
                Assert.That(cat.localScale, Is.EqualTo(scale));
                Assert.That(cat.anchoredPosition, Is.EqualTo(position));
                Assert.That(fail.GetType().GetField("presentation", flags).GetValue(fail), Is.Null);
            }
            finally { Time.timeScale = 1f; }
        }

        [UnityTest]
        public IEnumerator SadFailPresentation_ReducedMotionKeepsArtworkStillAndControlsUsable()
        {
            PlayerPrefs.SetInt("CatBlockPuzzle.Settings.ReducedMotion", 1);
            var fail = Screen("LevelFailScreen");
            Invoke(fail, "EnsureBindings");
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var cat = (RectTransform)fail.GetType().GetField("sadCatArtwork", flags).GetValue(fail);
            Vector3 scale = cat.localScale;
            Vector2 position = cat.anchoredPosition;
            Invoke(fail, "Show", "Try Again", "Time is up");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(cat.localScale, Is.EqualTo(scale));
            Assert.That(cat.anchoredPosition, Is.EqualTo(position));
            Assert.That(fail.GetType().GetField("presentation", flags).GetValue(fail), Is.Null);
            foreach (string field in new[] { "retryButton", "homeButton", "skipButton" })
            {
                var button = (UnityEngine.UI.Button)fail.GetType().GetField(field, flags).GetValue(fail);
                Assert.That(button.gameObject.activeInHierarchy && button.interactable, Is.True);
            }
            Invoke(fail, "Hide");
        }
    }
}
