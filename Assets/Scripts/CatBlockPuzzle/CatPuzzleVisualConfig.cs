using UnityEngine;

namespace CatBlockPuzzle
{
    [CreateAssetMenu(fileName = "CatVisualCatalog", menuName = "Cat Block Puzzle/Visual Catalog")]
    public sealed class CatVisualCatalog : ScriptableObject
    {
        public Texture2D CozyRoomBackground;
        public Texture2D ThemeAtlas;
        public Texture2D NeutralCatAtlas;
        public Texture2D HappyCatAtlas;
        public Texture2D WorriedCatAtlas;

        internal static CatVisualCatalog LoadOrCreate()
        {
            CatVisualCatalog catalog = Resources.Load<CatVisualCatalog>("CatBlockPuzzle/cat_visual_catalog");
            if (catalog == null)
            {
                catalog = CreateInstance<CatVisualCatalog>();
            }

            if (catalog.CozyRoomBackground == null)
            {
                catalog.CozyRoomBackground = Resources.Load<Texture2D>("CatBlockPuzzle/Art/cozy_room_background");
            }

            if (catalog.ThemeAtlas == null)
            {
                catalog.ThemeAtlas = Resources.Load<Texture2D>("CatBlockPuzzle/Art/Themes/theme_atlas");
            }

            if (catalog.NeutralCatAtlas == null)
            {
                catalog.NeutralCatAtlas = Resources.Load<Texture2D>("CatBlockPuzzle/Art/cat_portraits");
            }

            if (catalog.HappyCatAtlas == null)
            {
                catalog.HappyCatAtlas = Resources.Load<Texture2D>("CatBlockPuzzle/Art/cat_portraits_happy");
            }

            if (catalog.WorriedCatAtlas == null)
            {
                catalog.WorriedCatAtlas = Resources.Load<Texture2D>("CatBlockPuzzle/Art/cat_portraits_worried");
            }

            return catalog;
        }
    }

    [CreateAssetMenu(fileName = "PortraitLayoutProfile", menuName = "Cat Block Puzzle/Portrait Layout Profile")]
    public sealed class PortraitLayoutProfile : ScriptableObject
    {
        [Header("Gameplay Layout")]
        [Min(80f)] public float SideMargin = 108f;
        [Min(100f)] public float HeaderHeight = 184f;
        [Min(180f)] public float TrayHeight = 292f;
        [Min(64f)] public float ActionBarHeight = 96f;
        [Min(16f)] public float SectionGap = 34f;
        [Range(0.15f, 0.4f)] public float TrayViewportFraction = 0.25f;

        [Header("Tray Cards")]
        [Min(120f)] public float TrayCardWidth = 212f;
        [Min(150f)] public float TrayCardHeight = 246f;
        [Min(80f)] public float TrayCardMinWidth = 104f;
        [Min(120f)] public float TrayCardMinHeight = 154f;
        [Range(20f, 64f)] public float TrayCatCellSize = 46f;
        [Range(0f, 0.2f)] public float RemainingCardGrowth = 0.08f;
        [Range(0f, 0.35f)] public float MaximumCardGrowth = 0.16f;

        [Header("Touch And Drag")]
        [Range(4f, 48f)] public float PiecePickDragThreshold = 14f;
        [Range(0.35f, 1.2f)] public float PiecePickVerticalBias = 0.65f;
        [Range(0.8f, 2f)] public float TrayScrollHorizontalBias = 1.1f;
        [Range(0f, 120f)] public float TouchVisualLift = 42f;
        [Range(0f, 80f)] public float MouseVisualLift = 22f;
        [Range(1f, 1.75f)] public float MaximumVerticalGain = 1.35f;
        [Range(0f, 320f)] public float MaximumExtraReach = 180f;
        [Range(0.1f, 0.75f)] public float CatSnapThreshold = 0.45f;
        [Range(0f, 180f)] public float BoardSnapPadding = 84f;

        [Header("Motion")]
        [Range(0.05f, 0.5f)] public float PieceSizeTransitionSeconds = 0.2f;
        [Range(0f, 0.12f)] public float DragScaleOvershoot = 0.05f;
        [Range(0.02f, 0.18f)] public float DragFollowDelay = 0.075f;
        [Range(0f, 12f)] public float DragTiltAmount = 8f;
        [Range(0f, 0.14f)] public float DragJellyAmount = 0.065f;

        public void ResetToDefaults()
        {
            SideMargin = 108f;
            HeaderHeight = 184f;
            TrayHeight = 292f;
            ActionBarHeight = 96f;
            SectionGap = 34f;
            TrayViewportFraction = 0.25f;
            TrayCardWidth = 212f;
            TrayCardHeight = 246f;
            TrayCardMinWidth = 104f;
            TrayCardMinHeight = 154f;
            TrayCatCellSize = 46f;
            RemainingCardGrowth = 0.08f;
            MaximumCardGrowth = 0.16f;
            PiecePickDragThreshold = 14f;
            PiecePickVerticalBias = 0.65f;
            TrayScrollHorizontalBias = 1.1f;
            TouchVisualLift = 42f;
            MouseVisualLift = 22f;
            MaximumVerticalGain = 1.35f;
            MaximumExtraReach = 180f;
            CatSnapThreshold = 0.45f;
            BoardSnapPadding = 84f;
            PieceSizeTransitionSeconds = 0.2f;
            DragScaleOvershoot = 0.05f;
            DragFollowDelay = 0.075f;
            DragTiltAmount = 8f;
            DragJellyAmount = 0.065f;
        }

        public void ValidateValues()
        {
            SideMargin = Mathf.Max(80f, SideMargin);
            HeaderHeight = Mathf.Max(100f, HeaderHeight);
            TrayHeight = Mathf.Max(180f, TrayHeight);
            ActionBarHeight = Mathf.Max(64f, ActionBarHeight);
            SectionGap = Mathf.Max(16f, SectionGap);
            TrayViewportFraction = Mathf.Clamp(TrayViewportFraction, 0.15f, 0.4f);
            TrayCardWidth = Mathf.Max(120f, TrayCardWidth);
            TrayCardHeight = Mathf.Max(150f, TrayCardHeight);
            TrayCardMinWidth = Mathf.Clamp(TrayCardMinWidth, 80f, TrayCardWidth);
            TrayCardMinHeight = Mathf.Clamp(TrayCardMinHeight, 120f, TrayCardHeight);
            TrayCatCellSize = Mathf.Clamp(TrayCatCellSize, 20f, 64f);
            RemainingCardGrowth = Mathf.Clamp(RemainingCardGrowth, 0f, 0.2f);
            MaximumCardGrowth = Mathf.Clamp(MaximumCardGrowth, 0f, 0.35f);
            PiecePickDragThreshold = Mathf.Clamp(PiecePickDragThreshold, 4f, 48f);
            PiecePickVerticalBias = Mathf.Clamp(PiecePickVerticalBias, 0.35f, 1.2f);
            TrayScrollHorizontalBias = Mathf.Clamp(TrayScrollHorizontalBias, 0.8f, 2f);
            TouchVisualLift = Mathf.Clamp(TouchVisualLift, 0f, 120f);
            MouseVisualLift = Mathf.Clamp(MouseVisualLift, 0f, 80f);
            MaximumVerticalGain = Mathf.Clamp(MaximumVerticalGain, 1f, 1.75f);
            MaximumExtraReach = Mathf.Clamp(MaximumExtraReach, 0f, 320f);
            CatSnapThreshold = Mathf.Clamp(CatSnapThreshold, 0.1f, 0.75f);
            BoardSnapPadding = Mathf.Clamp(BoardSnapPadding, 0f, 180f);
            PieceSizeTransitionSeconds = Mathf.Clamp(PieceSizeTransitionSeconds, 0.05f, 0.5f);
            DragScaleOvershoot = Mathf.Clamp(DragScaleOvershoot, 0f, 0.12f);
            DragFollowDelay = Mathf.Clamp(DragFollowDelay, 0.02f, 0.18f);
            DragTiltAmount = Mathf.Clamp(DragTiltAmount, 0f, 12f);
            DragJellyAmount = Mathf.Clamp(DragJellyAmount, 0f, 0.14f);
        }

        private void OnValidate()
        {
            ValidateValues();
        }

        internal static PortraitLayoutProfile LoadOrCreate()
        {
            PortraitLayoutProfile profile = Resources.Load<PortraitLayoutProfile>("CatBlockPuzzle/portrait_layout_profile");
            if (profile == null)
            {
                profile = CreateInstance<PortraitLayoutProfile>();
            }

            profile.ValidateValues();
            return profile;
        }
    }
}
