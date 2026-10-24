using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Foggy cells: a soft cloud over a grid slot hides whether it is board, a bowl or empty
    /// space. A cloud fades away once a cat lands on an orthogonally neighbouring board cell
    /// (or on the cell itself). Clouds never block a drop; the player just cannot see there.
    /// </summary>
    internal sealed class FogController : HostedService
    {
        private const string CloudName = "Fog";
        private const float FadeSeconds = 0.35f;

        private readonly PuzzleSession session;
        private readonly PlayerSettings settings;
        private readonly Dictionary<Vector2Int, RectTransform> clouds = new Dictionary<Vector2Int, RectTransform>();

        public FogController(IGameplayContext context, PuzzleSession session, PlayerSettings settings) : base(context)
        {
            this.session = session;
            this.settings = settings;
        }

        public int CloudCount => clouds.Count;
        public bool IsFoggy(Vector2Int cell) => clouds.ContainsKey(cell);

        /// <summary>Covers the active level's foggy cells. Call after the board cells are laid out.</summary>
        public void Layout()
        {
            clouds.Clear();
            foreach (KeyValuePair<Vector2Int, CellView> cell in session.boardCells)
            {
                Transform stale = cell.Value.Rect.Find(CloudName);
                if (stale != null) stale.gameObject.SetActive(false);
            }

            LevelDefinition level = session.activeLevel;
            if (level == null || !level.HasFog) return;
            foreach (Vector2Int coord in level.FogCells)
            {
                if (!session.boardCells.TryGetValue(coord, out CellView view)) continue;
                clouds[coord] = BuildCloud(view.Rect);
            }
        }

        /// <summary>Clears every cloud that now has a cat beside it; call after each placement.</summary>
        public void OnBoardChanged()
        {
            if (clouds.Count == 0) return;
            // Clearing a cloud never places a cat, so one pass over the remaining clouds is enough.
            List<Vector2Int> cleared = null;
            foreach (Vector2Int coord in clouds.Keys)
            {
                if (!HasCatBeside(coord)) continue;
                (cleared ?? (cleared = new List<Vector2Int>())).Add(coord);
            }

            if (cleared == null) return;
            foreach (Vector2Int coord in cleared)
            {
                RectTransform cloud = clouds[coord];
                clouds.Remove(coord);
                if (cloud == null) continue;
                if (settings.ReducedMotion || !Host.gameObject.activeInHierarchy) cloud.gameObject.SetActive(false);
                else StartCoroutine(FadeOut(cloud));
            }
        }

        private bool HasCatBeside(Vector2Int cell)
        {
            if (session.occupancy.ContainsKey(cell)) return true;
            return session.occupancy.ContainsKey(new Vector2Int(cell.x - 1, cell.y)) ||
                session.occupancy.ContainsKey(new Vector2Int(cell.x + 1, cell.y)) ||
                session.occupancy.ContainsKey(new Vector2Int(cell.x, cell.y - 1)) ||
                session.occupancy.ContainsKey(new Vector2Int(cell.x, cell.y + 1));
        }

        private IEnumerator FadeOut(RectTransform cloud)
        {
            CanvasGroup group = cloud.GetComponent<CanvasGroup>();
            float elapsed = 0f;
            while (elapsed < FadeSeconds && cloud != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / FadeSeconds);
                if (group != null) group.alpha = 1f - t;
                cloud.localScale = Vector3.one * (1f + 0.25f * t);
                yield return null;
            }

            if (cloud != null) cloud.gameObject.SetActive(false);
        }

        private RectTransform BuildCloud(RectTransform cell)
        {
            Transform existing = cell.Find(CloudName);
            RectTransform cloud;
            if (existing != null) cloud = (RectTransform)existing;
            else
            {
                cloud = new GameObject(CloudName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup)).GetComponent<RectTransform>();
                cloud.SetParent(cell, false);
                Image image = cloud.GetComponent<Image>();
                image.sprite = BoardMechanicArt.Cloud;
                image.preserveAspect = false;
                image.raycastTarget = false;
            }

            CanvasGroup group = cloud.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = false;
            group.interactable = false;
            // Slightly larger than the cell so neighbouring clouds merge into one fog bank.
            cloud.anchorMin = Vector2.zero;
            cloud.anchorMax = Vector2.one;
            cloud.pivot = new Vector2(0.5f, 0.5f);
            float bleed = Mathf.Min(session.boardCellWidth, session.boardCellHeight) * 0.14f;
            cloud.offsetMin = new Vector2(-bleed, -bleed);
            cloud.offsetMax = new Vector2(bleed, bleed);
            cloud.localScale = Vector3.one;
            cloud.SetAsLastSibling();
            cloud.gameObject.SetActive(true);
            return cloud;
        }
    }
}
