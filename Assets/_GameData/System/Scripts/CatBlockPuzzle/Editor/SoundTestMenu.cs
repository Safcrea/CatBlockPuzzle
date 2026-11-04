using UnityEditor;
using UnityEngine;

namespace CatBlockPuzzle
{
    internal static class SoundTestMenu
    {
        [MenuItem("Cat Block Puzzle/Audio/Sound Test")]
        private static void Open()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Sound Test", "Enter Play Mode first; the Sound Test is also on F9 in the Game view.", "OK");
                return;
            }

            AudioManager.EnsureInstance().ToggleSoundTest();
        }
    }
}
