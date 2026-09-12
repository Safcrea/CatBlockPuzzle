using System;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed class CatPuzzleRoomView : MonoBehaviour
    {
        public int chapterIndex;
        public Image background;
        public Text progress;
        public Text status;
        public Image[] levelMarkers;
        public DecorationCard[] cards;
        public DecorationPlacement[] placements;

        [Serializable]
        public sealed class DecorationCard
        {
            public string id;
            public Image background;
            public Image art;
            public Text status;
        }
        [Serializable]
        public sealed class DecorationPlacement
        {
            public string id;
            public RectTransform root;
            public Image background;
            public Image art;
            public Button button;
            public Outline outline;
            public Text prompt;
        }
    }
}
