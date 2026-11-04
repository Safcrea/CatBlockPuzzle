#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// A clumsy, random "player" for smoke-testing in Play Mode: it drops cats anywhere, knocks them back off, mashes every
    /// power-up, taps the Hammer's board at random and pauses at odd moments. After every move it checks that input is
    /// not stuck, the board bookkeeping still adds up, the Hammer layer is in step and no cat pokes out of its shelf card.
    /// The report lands in Temp/DumbPlayerReport.txt.
    /// </summary>
    internal sealed class DumbPlayerBot : MonoBehaviour
    {
        public static readonly int[] DefaultLevels = { 0, 3, 4, 8, 11, 17, 24, 35, 45, 60, 66, 75, 86, 99 };

        public int[] levels = DefaultLevels;
        public int movesPerLevel = 30;
        public int seed = 7;
        public bool done;

        private readonly StringBuilder report = new StringBuilder();
        private readonly List<string> problems = new List<string>();
        private int errorLogs;
        private string lastError = "";
        private CatBlockPuzzleGame game;
        private string lastAction = "none";

        [MenuItem("Cat Block Puzzle/Testing/Run Dumb Player Bot (Play Mode)")]
        private static void Run()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode first."); return; }
            new GameObject("Dumb Player Bot").AddComponent<DumbPlayerBot>();
        }

        private void OnEnable() => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (condition.Contains("ADB") || condition.Contains("audio listener")) return;
            errorLogs++;
            lastError = condition;
            problems.Add("console " + type + ": " + condition.Split('\n')[0]);
        }

        private IEnumerator Start()
        {
            game = FindFirstObjectByType<CatBlockPuzzleGame>();
            // Borrow the player's save: stocked power-ups, onboarding already seen. Everything is put back afterwards.
            string[] seenKeys = { "CatBlockPuzzle.Tutorial.Hint", "CatBlockPuzzle.Tutorial.Freeze", "CatBlockPuzzle.Tutorial.Rotate", "CatBlockPuzzle.Tutorial.Hammer", "CatBlockPuzzle.Tutorial.Timer", "CatBlockPuzzle.Tutorial.LevelOne.v1" };
            var savedInts = new Dictionary<string, int?>();
            foreach (string k in seenKeys) { savedInts[k] = PlayerPrefs.HasKey(k) ? PlayerPrefs.GetInt(k) : (int?)null; PlayerPrefs.SetInt(k, 1); }
            const string rewardKey = "CatBlockPuzzle.DailyReward.State.v1";
            bool hadReward = PlayerPrefs.HasKey(rewardKey);
            string rewardSave = PlayerPrefs.GetString(rewardKey, "");
            PlayerPrefs.SetString(rewardKey, "{\"version\":1,\"hintCount\":40,\"freezeCount\":40,\"rotateCount\":40,\"hammerCount\":40}");
            var rng = new System.Random(seed);
            report.AppendLine("Dumb player bot, seed " + seed + ", " + movesPerLevel + " moves per level");
            foreach (int level in levels)
            {
                yield return PlayLevel(level, rng);
            }
            foreach (var pair in savedInts) { if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key); }
            if (hadReward) PlayerPrefs.SetString(rewardKey, rewardSave); else PlayerPrefs.DeleteKey(rewardKey);
            PlayerPrefs.Save();
            report.AppendLine();
            report.AppendLine(problems.Count == 0 ? "NO PROBLEMS FOUND" : problems.Count + " PROBLEM(S):");
            foreach (string p in problems) report.AppendLine(" - " + p);
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/DumbPlayerReport.txt")), report.ToString());
            Debug.Log("[DumbPlayerBot] finished: " + (problems.Count == 0 ? "no problems" : problems.Count + " problems") + " (Temp/DumbPlayerReport.txt)");
            done = true;
        }

        private IEnumerator PlayLevel(int levelIndex, System.Random rng)
        {
            GameSystem.Instance.ResumeGame();
            GameSystem.Instance.PreviewLevel(levelIndex);
            yield return WaitUntilPlayable(8f);
            PuzzleSession s = game.Session;
            int placed = 0, hammers = 0, hints = 0, pauses = 0, taps = 0, moves = 0;
            string outcome = "still playing";
            float stuckSince = -1f;
            for (; moves < movesPerLevel; moves++)
            {
                if (game.CompleteScreen != null && game.CompleteScreen.IsOpen) { outcome = "won"; break; }
                if (s.levelFailed) { outcome = "failed"; break; }
                double roll = rng.NextDouble();
                if (roll < 0.50) { lastAction = "drop"; if (DropRandomCat(s, rng)) placed++; }
                else if (roll < 0.62) { lastAction = "knock"; if (KnockRandomCat(s, rng)) taps++; }
                else if (roll < 0.74) { lastAction = "hint"; GameSystem.Instance.TryUseHintPowerUp(); hints++; }
                else if (roll < 0.88)
                {
                    lastAction = "hammer"; GameSystem.Instance.TryUseHammerPowerUp(); hammers++;
                    if (game.Hammer.Active) TapBoardRandomly(s, rng);
                }
                else if (roll < 0.92) { lastAction = "freeze"; GameSystem.Instance.TryUseFreezePowerUp(); }
                else if (roll < 0.96) { lastAction = "rotate"; GameSystem.Instance.TryUseRotatePowerUp(); }
                else
                {
                    pauses++; lastAction = "pause";
                    GameSystem.Instance.OpenPause();
                    yield return new WaitForSecondsRealtime(0.4f);
                    CheckInvariants(levelIndex, "while paused");
                    GameSystem.Instance.ResumeGame();
                }
                yield return new WaitForSecondsRealtime(0.5f);
                CheckInvariants(levelIndex, "move " + moves);
                if (s.inputLocked && !s.levelFailed && !(game.CompleteScreen != null && game.CompleteScreen.IsOpen))
                {
                    if (stuckSince < 0f) stuckSince = Time.realtimeSinceStartup;
                    if (Time.realtimeSinceStartup - stuckSince > 6f) { problems.Add("level " + (levelIndex + 1) + ": input stayed locked for 6s (move " + moves + ", last action " + lastAction + ", hammerActive=" + game.Hammer.Active + ", dragging=" + (s.drag != null) + ", paused=" + GameSystem.Instance.IsPaused + ", revealing=" + game.Flow.IsRevealing + ", tutorial=" + game.IsLevelOneTutorialOpen + ", powerUpTutorial=" + game.IsPowerUpTutorialOpen + ", purchase=" + game.IsPowerUpPurchaseOpen + ", settings=" + GameSystem.Instance.IsSettingsOpen + ", toastOrInfoOpen=" + (FindObjectsByType<InfoPopup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length) + ")"); stuckSince = -1f; s.inputLocked = false; }
                }
                else stuckSince = -1f;
            }
            if (game.Hammer.Active) game.Hammer.Cancel();
            report.AppendLine("level " + (levelIndex + 1) + ": " + outcome + " after " + moves + " moves, drops=" + placed + " hammerPresses=" + hammers +
                " hints=" + hints + " knocks=" + taps + " pauses=" + pauses);
        }

        private IEnumerator WaitUntilPlayable(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end && (game.Session.inputLocked || game.Session.pieces.Count == 0)) yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
        }

        private bool DropRandomCat(PuzzleSession s, System.Random rng)
        {
            if (s.inputLocked || s.drag != null) return false;
            var shelf = new List<PieceState>();
            foreach (PieceState p in s.pieces) if (!p.Placed && p.Slot != null && p.Slot.gameObject.activeSelf) shelf.Add(p);
            if (shelf.Count == 0) return false;
            PieceState piece = shelf[rng.Next(shelf.Count)];
            // Clumsy: usually a random spot, only sometimes one where it really fits.
            for (int attempt = 0; attempt < 12; attempt++)
            {
                int row = rng.Next(s.activeLevel.Rows), col = rng.Next(s.activeLevel.Cols);
                if (!game.Board.CanPlace(piece, row, col)) continue;
                game.Drag.PlacePiece(piece, row, col, true);
                return true;
            }
            return false;
        }

        private bool KnockRandomCat(PuzzleSession s, System.Random rng)
        {
            if (s.inputLocked || s.drag != null) return false;
            var onBoard = new List<PieceState>();
            foreach (PieceState p in s.pieces) if (p.Placed && !p.Definition.IsFixed) onBoard.Add(p);
            if (onBoard.Count == 0) return false;
            return game.Drag.KnockToShelf(onBoard[rng.Next(onBoard.Count)]);
        }

        private void TapBoardRandomly(PuzzleSession s, System.Random rng)
        {
            GameObject catcher = GameObject.Find("Hammer Catcher");
            if (catcher == null || s.boardRoot == null) return;
            var cells = new List<CellView>(s.boardCells.Values);
            if (cells.Count == 0) return;
            RectTransform rect = cells[rng.Next(cells.Count)].Rect;
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rect.TransformPoint(rect.rect.center));
            var data = new PointerEventData(EventSystem.current) { position = screen, button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(catcher, data, ExecuteEvents.pointerClickHandler);
        }

        private void CheckInvariants(int levelIndex, string when)
        {
            PuzzleSession s = game.Session;
            string tag = "level " + (levelIndex + 1) + " (" + when + "): ";
            if (s.activeLevel == null) return;
            // Hammer layer and mode agree.
            bool catcher = GameObject.Find("Hammer Catcher") != null;
            if (catcher != game.Hammer.Active && !s.inputLocked) problems.Add(tag + "Hammer mode is " + game.Hammer.Active + " but the tap layer is " + (catcher ? "present" : "missing"));
            // Board bookkeeping: occupied cells equal the cells of the landed cats.
            int cellsOfPlaced = 0;
            foreach (PieceState p in s.pieces) if (p.Placed) cellsOfPlaced += p.Definition.Cells.Length;
            if (!s.inputLocked && s.drag == null && cellsOfPlaced != s.occupancy.Count)
                problems.Add(tag + "occupancy has " + s.occupancy.Count + " cells but landed cats cover " + cellsOfPlaced);
            // Every cat on the shelf sits on its card and stays inside it.
            if (!s.inputLocked && s.drag == null)
            {
                foreach (PieceState p in s.pieces)
                {
                    if (p.Placed || p.Slot == null || !p.Slot.gameObject.activeInHierarchy) continue;
                    if (p.Rect.parent != p.Slot) { problems.Add(tag + "shelf cat '" + p.Definition.Name + "' is not parented to its card"); continue; }
                    Rect card = ScreenRect(p.Slot);
                    foreach (Image cell in p.CellImages)
                    {
                        if (cell == null || !cell.gameObject.activeInHierarchy) continue;
                        Rect r = ScreenRect(cell.rectTransform);
                        float over = Mathf.Max(card.xMin - r.xMin, r.xMax - card.xMax, card.yMin - r.yMin, r.yMax - card.yMax);
                        if (over > 2f) { problems.Add(tag + "cat '" + p.Definition.Name + "' pokes " + over.ToString("0") + "px out of its card"); break; }
                    }
                }
            }
            foreach (PowerUpKind kind in System.Enum.GetValues(typeof(PowerUpKind)))
                if (PowerUpInventory.GetCount(kind) < 0) problems.Add(tag + kind + " count went negative");
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var c = new Vector3[4];
            rect.GetWorldCorners(c);
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, c[0]), b = RectTransformUtility.WorldToScreenPoint(cam, c[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }
    }
}
#endif
