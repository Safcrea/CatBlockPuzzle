using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed class LoadingSceneController : MonoBehaviour
    {
        private const string LoadingSceneName = "Loading";
        private const string TargetSceneKey = "CatBlockPuzzle.Loading.TargetScene";
        private static int requestedBuildIndex = -1;

        [SerializeField] private Slider progressBar;
        // [SerializeField] private Text progressText;
        [SerializeField, Min(0f)] private float minimumDisplaySeconds = 0.35f;

        public static void LoadScene(int targetBuildIndex)
        {
            if (targetBuildIndex < 0 || targetBuildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError("Cannot load build index " + targetBuildIndex + ". Add the scene to Build Settings first.");
                return;
            }

            requestedBuildIndex = targetBuildIndex;
            PlayerPrefs.SetInt(TargetSceneKey, targetBuildIndex);
            PlayerPrefs.Save();

            if (SceneManager.GetActiveScene().name == LoadingSceneName)
            {
                return;
            }

            if (Application.CanStreamedLevelBeLoaded(LoadingSceneName)) SceneManager.LoadScene(LoadingSceneName);
            else SceneManager.LoadScene(targetBuildIndex);
        }

        private IEnumerator Start()
        {
            EnsureRuntimeView();
            int target = requestedBuildIndex >= 0 ? requestedBuildIndex : PlayerPrefs.GetInt(TargetSceneKey, 1);
            if (target == SceneManager.GetActiveScene().buildIndex) target = 1;
            float startedAt = Time.realtimeSinceStartup;
            AsyncOperation operation = SceneManager.LoadSceneAsync(target);
            operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                if (progressBar != null) progressBar.value = progress;
                // if (progressText != null) progressText.text = "Loading " + Mathf.RoundToInt(progress * 100f) + "%";
                bool minimumElapsed = Time.realtimeSinceStartup - startedAt >= minimumDisplaySeconds;
                if (operation.progress >= 0.75f && minimumElapsed) operation.allowSceneActivation = true;
                yield return null;
            }
        }

        private void EnsureRuntimeView()
        {
            if (progressBar != null) return;
            GameObject canvasObject = new GameObject("Loading Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);

            RectTransform background = RuntimeUiFactory.CreateRect(canvas.transform, "Background");
            RuntimeUiFactory.Stretch(background);
            background.gameObject.AddComponent<Image>().color = new Color(1f, 0.92f, 0.85f, 1f);
            Text title = RuntimeUiFactory.CreateText(background, "Title", "Getting the cats ready…", 52, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(title.rectTransform, new Vector2(0f, 120f), new Vector2(820f, 100f));
            // progressText = RuntimeUiFactory.CreateText(background, "Progress", "Loading 0%", 30, TextAnchor.MiddleCenter);
            // RuntimeUiFactory.SetRect(progressText.rectTransform, new Vector2(0f, -80f), new Vector2(500f, 60f));

            RectTransform sliderRect = RuntimeUiFactory.CreateRect(background, "Progress Bar");
            RuntimeUiFactory.SetRect(sliderRect, new Vector2(0f, 10f), new Vector2(650f, 36f));
            progressBar = sliderRect.gameObject.AddComponent<Slider>();
            Image barBackground = sliderRect.gameObject.AddComponent<Image>();
            barBackground.color = Color.white;
            progressBar.targetGraphic = barBackground;
            RectTransform fillArea = RuntimeUiFactory.CreateRect(sliderRect, "Fill Area");
            RuntimeUiFactory.Stretch(fillArea);
            RectTransform fill = RuntimeUiFactory.CreateRect(fillArea, "Fill");
            RuntimeUiFactory.Stretch(fill);
            fill.gameObject.AddComponent<Image>().color = RuntimeUiFactory.Coral;
            progressBar.fillRect = fill;
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
        }
    }
}
