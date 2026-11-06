using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Notices a player who is stuck (idle for a while, or a few wrong drops in a row) and points at the power-up that
    /// would help: a pulsing button, a hand and a short line. The nudge goes away as soon as the player does anything.
    /// </summary>
    internal sealed class StuckAdvisor : HostedService
    {
        /// <summary>What the advisor knows when it picks a power-up.</summary>
        internal struct Facts
        {
            public bool HintReady, HammerReady, FreezeReady, HammerHasObstacle;
            /// <summary>The player keeps missing because something is in the way: a padlock, or cats sitting on the wrong spots.</summary>
            public bool LockBlocked;
            public bool TimeIsLow;
        }

        public const float IdleSeconds = 12f;
        public const int MissedDrops = 3;
        public const float HoldSeconds = 8f;
        public const float CooldownSeconds = 20f;
        public const int MaxPerLevel = 3;

        private readonly PuzzleSession session;
        private readonly LevelTimer timer;
        private readonly HammerController hammer;
        private readonly RotateController rotate;
        private readonly CellLockController locks;
        private readonly InteractionGates gates;
        private readonly LevelOneTutorial tutorial;
        private readonly IPuzzleConfig config;
        private readonly HintPresenter hint;

        private LevelDefinition level;
        private float lastPlaySeconds;
        private int lastActivity;
        private int lastPlacements;
        private int lastLockBlocked;
        private int lastMisses;
        private int missedSincePlacement;
        private bool lockBlockedSincePlacement;
        private float idle;
        private float shownFor;
        private float cooldown;
        private int shownThisLevel;
        private PowerUpKind? showing;

        public StuckAdvisor(IGameplayContext context, PuzzleSession session, LevelTimer timer, HammerController hammer,
            RotateController rotate, CellLockController locks, InteractionGates gates, LevelOneTutorial tutorial,
            IPuzzleConfig config, HintPresenter hint) : base(context)
        {
            this.hint = hint;
            this.session = session;
            this.timer = timer;
            this.hammer = hammer;
            this.rotate = rotate;
            this.locks = locks;
            this.gates = gates;
            this.tutorial = tutorial;
            this.config = config;
        }

        /// <summary>The power-up currently being pointed at, if any.</summary>
        public PowerUpKind? Showing => showing;

        /// <summary>The power-up that fits the situation best, or null when none would help.</summary>
        internal static PowerUpKind? Choose(Facts f)
        {
            if (f.LockBlocked && f.HammerReady && f.HammerHasObstacle) return PowerUpKind.Hammer;
            if (f.TimeIsLow && f.FreezeReady) return PowerUpKind.Freeze;
            if (f.HintReady) return PowerUpKind.Hint;
            if (f.HammerReady) return PowerUpKind.Hammer;
            return null;
        }

        public void Tick()
        {
            if (Hud == null || timer == null) return;
            if (level != session.activeLevel || session.stats.playSeconds + 0.5f < lastPlaySeconds) Restart();
            lastPlaySeconds = session.stats.playSeconds;

            int activity = Activity();
            bool busy = session.inputLocked || session.drag != null || !timer.timerRunning || session.levelFailed ||
                gates.ResultScreenOpen || gates.MetaUiOpen || gates.PowerUpTutorialOpen || gates.PowerUpPurchaseOpen ||
                gates.SystemPaused || gates.GameplayHidden || tutorial.IsOpen || hammer.Active || rotate.Active;
            if (activity != lastActivity || session.drag != null)
            {
                SeeActivity(activity);
                Hide();
            }
            if (busy)
            {
                if (showing.HasValue && (hammer.Active || session.drag != null || gates.SystemPaused)) Hide();
                return;
            }

            float dt = Time.unscaledDeltaTime;
            idle += dt;
            if (cooldown > 0f) cooldown -= dt;
            if (showing.HasValue)
            {
                shownFor += dt;
                if (shownFor >= HoldSeconds) { Hide(); cooldown = CooldownSeconds; }
                return;
            }

            // The Hammer's first lesson comes as soon as something in the way has cost the player a couple of drops.
            bool stuck = idle >= IdleSeconds || missedSincePlacement >= MissedDrops ||
                ((missedSincePlacement >= 2 || lockBlockedSincePlacement) && HammerIntroDue());
            if (!stuck || cooldown > 0f || shownThisLevel >= MaxPerLevel) return;
            Show();
        }

        /// <summary>Takes the nudge down, e.g. when the level ends or the HUD is left.</summary>
        public void Hide()
        {
            if (!showing.HasValue) return;
            showing = null;
            Hud?.HidePowerUpNudge();
        }

        // True the first time the Hammer would really help (cats, padlocks or fog in the way) and the player has never been taught it.
        private bool HammerIntroDue() =>
            Hud.IsPowerUpUnlocked(PowerUpKind.Hammer) && PlayerPrefs.GetInt(PowerUpTutorialManager.HammerSeenKey, 0) == 0 && hammer.HasTarget;

        private void Show()
        {
            if (HammerIntroDue())
            {
                // Taught when it is needed, with a gift to try it: spotlight on the button, and the press swings the Hammer for real.
                if (Hud.ShowPowerUpNeedIntro(PowerUpKind.Hammer, "SOMETHING IN YOUR WAY?\nTAP HAMMER!", PowerUpTutorialManager.HammerSeenKey,
                        Hud.FreeOnUnlock + 1, () => GameSystem.Instance?.TryUseHammerPowerUp()))
                    shownThisLevel++;
                cooldown = CooldownSeconds;
                return;
            }
            Facts facts = new Facts
            {
                HintReady = Hud.IsPowerUpUnlocked(PowerUpKind.Hint),
                HammerReady = Hud.IsPowerUpUnlocked(PowerUpKind.Hammer) && PowerUpInventory.GetCount(PowerUpKind.Hammer) > 0 && hammer.HasTarget,
                HammerHasObstacle = hammer.HasObstacle,
                FreezeReady = Hud.IsPowerUpUnlocked(PowerUpKind.Freeze) && PowerUpInventory.GetCount(PowerUpKind.Freeze) > 0 && !timer.FreezeUsedThisAttempt,
                LockBlocked = (lockBlockedSincePlacement && locks.HasClosedLocks) || (hammer.BlockerCount > 0 && missedSincePlacement >= 2),
                TimeIsLow = !timer.Untimed && timer.Profile.DurationSeconds > 0f && timer.RemainingSeconds < timer.Profile.DurationSeconds * 0.35f
            };
            PowerUpKind? pick = Choose(facts);
            if (!pick.HasValue)
            {
                // Before the Hint unlocks there is no power-up to point at, so a stuck beginner gets the free glow instead.
                if (!facts.HintReady && hint.ShowHint(HintReveal.FullShape)) shownThisLevel++;
                cooldown = CooldownSeconds;
                return;
            }
            showing = pick;
            shownFor = 0f;
            shownThisLevel++;
            Hud.ShowPowerUpNudge(pick.Value, config != null ? config.TutorialHandSprite : null);
            PuzzleAudio.Play(AudioEvents.HintShow);
        }

        private void Restart()
        {
            level = session.activeLevel;
            lastPlaySeconds = 0f;
            shownThisLevel = 0;
            cooldown = 0f;
            idle = 0f;
            missedSincePlacement = 0;
            lockBlockedSincePlacement = false;
            lastActivity = Activity();
            lastPlacements = session.stats.placements;
            lastLockBlocked = session.stats.lockBlockedDrops;
            lastMisses = session.stats.misdrops + session.stats.cancelledDrops;
            Hide();
        }

        private int Activity()
        {
            LevelAttemptStats s = session.stats;
            return s.placements + s.boardMoves + s.shelfReturns + s.misdrops + s.cancelledDrops + s.hintsUsed + s.freezesUsed + s.rotatesUsed + s.hammersUsed;
        }

        private void SeeActivity(int activity)
        {
            LevelAttemptStats s = session.stats;
            idle = 0f;
            int misses = s.misdrops + s.cancelledDrops;
            if (s.placements != lastPlacements)
            {
                missedSincePlacement = 0;
                lockBlockedSincePlacement = false;
            }
            else if (misses > lastMisses)
            {
                missedSincePlacement += misses - lastMisses;
                if (s.lockBlockedDrops != lastLockBlocked) lockBlockedSincePlacement = true;
            }
            lastPlacements = s.placements;
            lastLockBlocked = s.lockBlockedDrops;
            lastMisses = misses;
            lastActivity = activity;
        }
    }
}
