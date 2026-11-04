using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>Development screen listing every sound with a play button, its file, length and review status.</summary>
    internal sealed class SoundTestPanel : MonoBehaviour
    {
        private Vector2 scroll;
        private GUIStyle rowStyle;
        private GUIStyle statusStyle;

        public bool Visible { get; set; }

        private void OnGUI()
        {
            if (!Visible) return;
            AudioManager audio = AudioManager.Instance;
            if (audio == null) return;
            float scale = Mathf.Max(1f, Screen.dpi > 0f ? Screen.dpi / 130f : 1f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale, height = Screen.height / scale;
            if (rowStyle == null)
            {
                rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
                statusStyle = new GUIStyle(rowStyle) { fontStyle = FontStyle.Bold };
            }

            GUI.Box(new Rect(0, 0, width, height), GUIContent.none);
            GUI.Label(new Rect(8, 6, width - 120, 26), "Sound Test  (F9 / four fingers closes)  -  sound " + (audio.SfxEnabled ? "ON" : "OFF"));
            if (GUI.Button(new Rect(width - 100, 6, 92, 26), "Close")) Visible = false;
            scroll = GUI.BeginScrollView(new Rect(0, 38, width, height - 38), scroll, new Rect(0, 0, width - 20, audioCount(audio) * 32f));
            float y = 0f;
            foreach (AudioConfig.SoundEntry entry in audio.AllSounds)
            {
                string status = audio.EffectiveStatus(entry);
                GUI.enabled = audio.HasClip(entry.name);
                if (GUI.Button(new Rect(6, y + 2, 64, 28), "Play")) audio.Audition(entry.name);
                GUI.enabled = true;
                GUI.Label(new Rect(78, y, width * 0.30f, 32), entry.name, rowStyle);
                GUI.Label(new Rect(78 + width * 0.30f, y, width * 0.30f, 32), entry.file, rowStyle);
                GUI.Label(new Rect(78 + width * 0.60f, y, 52, 32), audio.ClipLength(entry.name).ToString("0.00") + "s", rowStyle);
                statusStyle.normal.textColor = status == "ok" ? Color.green : status == "review" ? Color.yellow : Color.red;
                GUI.Label(new Rect(134 + width * 0.60f, y, width * 0.30f, 32), status, statusStyle);
                y += 32f;
            }

            GUI.EndScrollView();
        }

        private static int audioCount(AudioManager audio)
        {
            int count = 0;
            foreach (var _ in audio.AllSounds) count++;
            return count;
        }
    }
}
