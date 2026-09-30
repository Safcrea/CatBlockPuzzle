using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>Layout and motion constants shared by more than one gameplay service.</summary>
    internal static class PuzzleTuning
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;
        public const float MaxBoardWidth = 720f;
        public const float MaxBoardHeight = 820f;
        public const float BoardCenterY = 54f;
        public const float TimerBoardGap = 24f;
        public const float TimerHeight = 70f;
        public const float BoardGap = 8f;
        public const float TrayGap = 5f;
        public const float PopSeconds = 0.22f;
        public const float SparkSeconds = 0.55f;
        public const float TouchVisualLift = 42f;
        public const float MouseVisualLift = 22f;
        public const float BoardSnapPadding = 84f;
        public const float TrayReturnPadding = 18f;
        public const float PiecePickDragThreshold = 14f;
        public const float PiecePickVerticalBias = 0.65f;
        public const float TrayScrollHorizontalBias = 1.1f;
        public const float DragVerticalGain = 1.1f;
        public const float DragExtraReach = 80f;
        public const float DragFollowDelay = 0.11f;
        public const float DragTiltAmount = 4f;
        public const float DragTiltSpeed = 16f;
        public const float DragTiltVelocityScale = 900f;
        public const float DragJellyAmount = 0.035f;
        public const float DragJellySpeed = 18f;
        public const float DragJellyVelocityScale = 900f;
        public const float BoardRevealCellSeconds = 0.24f;
        public const float BoardRevealStaggerSeconds = 0.035f;
        public const float BoardRevealOvershoot = 1.14f;
        public const float TrayMaxWidth = 1040f;
        public const float TrayFloatAmplitude = 9f;
        public const float TrayFloatRotation = 2.5f;
        public const float TrayFloatSpeed = 1.25f;
        public const float LevelDurationSeconds = 120f;
        public const float TimerWarningSeconds = 20f;
        public const float TimerPulseScale = 1.1f;

        public static float EaseOutCubic(float value)
        {
            value = Mathf.Clamp01(value);
            float inverse = 1f - value;
            return 1f - (inverse * inverse * inverse);
        }
    }
}
