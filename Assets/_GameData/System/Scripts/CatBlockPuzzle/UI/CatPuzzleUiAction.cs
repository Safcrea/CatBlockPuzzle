using UnityEngine;

namespace CatBlockPuzzle
{
    public sealed class CatPuzzleUiAction : MonoBehaviour
    {
        public enum ActionKind {
            Rooms, Pause, Settings, Hint, Reset, PreviousTest, NextTest, NextLevel,
            CloseSettings, CloseMeta, ContinueHub, OpenRoom, PlayLevel, Decoration,
            Install, StoryContinue, CompletionContinue, DecorateNow, Home, SkipLevel, Sound, Haptics, Motion
        }
        public CatBlockPuzzleGame controller;
        public ActionKind action;
        public int index;
        public string itemId;
        public void Invoke()
        {
            if (controller != null && controller.isActiveAndEnabled)
                controller.HandlePreparedAction(action, index, itemId);
        }
        public void SetToggle(bool value)
        {
            if (controller != null && controller.isActiveAndEnabled)
                controller.HandlePreparedToggle(action, value);
        }
    }
}
