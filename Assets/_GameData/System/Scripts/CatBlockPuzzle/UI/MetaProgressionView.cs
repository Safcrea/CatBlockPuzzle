using System;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    [Serializable]
    internal sealed class HubCardView
    {
        public Image background;
        public Image thumbnail;
        public Text number;
        public Text title;
        public Text progress;
        public Text status;
        public Image badge;
        public Image[] steps;
    }

    [DisallowMultipleComponent]
    public sealed class MetaProgressionView : MonoBehaviour
    {
        [Header("Pages")]
        [SerializeField] internal RectTransform metaOverlay;
        [SerializeField] internal RectTransform metaHubPage;
        [SerializeField] internal RectTransform metaRoomPage;
        [SerializeField] internal RectTransform metaStoryOverlay;
        [SerializeField] internal RectTransform metaCompletionOverlay;
        [SerializeField] internal Text metaStatusText;

        [Header("Level Complete Actions")]
        [SerializeField] internal Button metaNextLevelButton;
        [SerializeField] internal Text metaNextLevelButtonText;
        [SerializeField] internal Button metaDecorateNowButton;

        [Header("Home and Rooms")]
        [SerializeField] internal HubCardView[] hubCards;
        [SerializeField] internal CatPuzzleRoomView[] roomViews;
        [SerializeField] internal Text hubContinueLabel;
        [SerializeField] internal Text hubStatus;
        [SerializeField] internal Text[] metaCoinLabels;

        [Header("Story")]
        [SerializeField] internal RectTransform storyPanel;
        [SerializeField] internal Image storyPortrait;
        [SerializeField] internal Text storyTitle;
        [SerializeField] internal Text storyBody;
        [SerializeField] internal Text storyActionLabel;

        [Header("Chapter Complete")]
        [SerializeField] internal RectTransform completionPanel;
        [SerializeField] internal Text completionKicker;
        [SerializeField] internal Text completionTitle;
        [SerializeField] internal Image completionPortrait;
        [SerializeField] internal Text completionBody;
        [SerializeField] internal RectTransform completionNextPreview;
        [SerializeField] internal Image completionNextPortrait;
        [SerializeField] internal Text completionNextText;
        [SerializeField] internal Text completionActionLabel;

        public bool IsOpen => metaOverlay != null && metaOverlay.gameObject.activeInHierarchy;

        public bool Validate(out string error)
        {
            error = null;
            if (metaOverlay == null || metaHubPage == null || metaRoomPage == null ||
                metaStoryOverlay == null || metaCompletionOverlay == null)
                error = "home, room, story, or completion pages are missing";
            else if (metaNextLevelButton == null || metaDecorateNowButton == null)
                error = "meta progression actions are missing";
            return error == null;
        }
    }
}
