using System.Collections;
using UnityEngine;

namespace CatBlockPuzzle
{
    internal enum CatMood
    {
        Neutral = 0,
        Happy = 1,
        Worried = 2
    }

    /// <summary>Swaps cat portraits between neutral, happy and worried, and restores them after a beat.</summary>
    internal sealed class CatMoodPresenter : HostedService
    {
        private readonly PuzzleSession session;
        private readonly PlayerSettings settings;
        private readonly InteractionGates gates;

        public CatMoodPresenter(IGameplayContext context, PuzzleSession session, PlayerSettings settings, InteractionGates gates) : base(context)
        {
            this.session = session;
            this.settings = settings;
            this.gates = gates;
        }

        public void ShowHappy(PieceState state)
        {
            SetMood(state, CatMood.Happy);
            StartCoroutine(RestoreMood(state, 0.42f));
        }

        public void ShowWorried(PieceState state)
        {
            SetMood(state, CatMood.Worried);
            StartCoroutine(RestoreMood(state, 0.58f));
        }

        private IEnumerator RestoreMood(PieceState state, float delay)
        {
            yield return new WaitForSecondsRealtime(settings.ReducedMotion ? Mathf.Max(0.28f, delay * 0.5f) : delay);
            if (state != null && state.Rect != null && !session.inputLocked && !gates.LevelCompleteOpen)
            {
                SetMood(state, CatMood.Neutral);
            }
        }

        public void SetMood(PieceState state, CatMood mood)
        {
            if (state == null)
            {
                return;
            }

            Sprite sprite = Portrait(mood, state.AtlasIndex);
            if (sprite == null)
            {
                return;
            }

            for (int i = 0; i < state.CatViews.Count; i++)
            {
                CatCellView view = state.CatViews[i];
                if (view.Portrait == null)
                {
                    continue;
                }

                // Only the head cell wears the face; the generated rig has no head/body split, so it
                // still changes mood on every cell.
                if (view.Authored && i != PieceState.HeadCellIndex)
                {
                    continue;
                }

                view.Portrait.sprite = sprite;
            }
        }

        public Sprite Portrait(CatMood mood, int atlasIndex)
        {
            Sprite[] catPortraitSprites = Assets.catPortraitSprites;
            int index = Mathf.Abs(atlasIndex) % GameplayPresentationAssets.CatCount;
            Sprite requested = catPortraitSprites[(int)mood * GameplayPresentationAssets.CatCount + index];
            return requested != null ? requested : catPortraitSprites[index];
        }

        /// <summary>The plain block the cells of a piece other than its head draw.</summary>
        public Sprite Body(int atlasIndex)
        {
            Sprite[] bodies = Assets.catBodySprites;
            if (bodies == null || bodies.Length == 0)
            {
                return null;
            }

            return bodies[Mathf.Abs(atlasIndex) % bodies.Length];
        }

        /// <summary>
        /// Gives a piece its authored tiles: the head on its first cell, the plain body on the rest.
        /// Level prefabs bake one sprite per cell, so this re-skins them as a level loads.
        /// </summary>
        public void ApplyTiles(PieceState state)
        {
            if (state == null)
            {
                return;
            }

            Sprite head = Portrait(CatMood.Neutral, state.AtlasIndex);
            Sprite body = Body(state.AtlasIndex);
            if (head == null || body == null)
            {
                return;
            }

            for (int i = 0; i < state.CatViews.Count; i++)
            {
                CatCellView view = state.CatViews[i];
                if (!view.Authored || view.Portrait == null)
                {
                    continue;
                }

                view.Portrait.sprite = i == PieceState.HeadCellIndex ? head : body;
                view.Portrait.color = Color.white;
            }
        }
    }
}
