namespace CatBlockPuzzle
{
    /// <summary>
    /// What the player did during one attempt at a level, summed up for the analytics
    /// complete/fail events instead of sending an event per drag. Reset on every level load.
    /// </summary>
    internal sealed class LevelAttemptStats
    {
        internal int placements;
        internal int boardMoves;
        internal int shelfReturns;
        internal int misdrops;
        internal int cancelledDrops;
        internal int lockBlockedDrops;
        internal int heartsLost;
        internal int hintsUsed;
        internal int freezesUsed;
        internal bool reachedLowTime;
        /// <summary>Seconds the level timer has been running (frozen time included, pauses excluded).</summary>
        internal float playSeconds;
        /// <summary>Play seconds before the first cat landed on the board; -1 until then.</summary>
        internal float firstMoveSeconds = -1f;

        internal void Reset()
        {
            placements = boardMoves = shelfReturns = misdrops = cancelledDrops = lockBlockedDrops = 0;
            heartsLost = hintsUsed = freezesUsed = 0;
            reachedLowTime = false;
            playSeconds = 0f;
            firstMoveSeconds = -1f;
        }

        internal void RecordPlacement(bool movedOnBoard)
        {
            if (movedOnBoard) boardMoves++;
            else placements++;
            if (firstMoveSeconds < 0f) firstMoveSeconds = playSeconds;
        }
    }
}
