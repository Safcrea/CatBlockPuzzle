namespace CatBlockPuzzle
{
    /// <summary>Game event names. Which sound each one plays lives in Resources/CatBlockPuzzle/Audio/audio_config.json.</summary>
    internal static class AudioEvents
    {
        public const string ButtonTap = "ButtonTap";
        public const string ButtonBack = "ButtonBack";
        public const string ToggleOn = "ToggleOn";
        public const string ToggleOff = "ToggleOff";
        public const string PopupOpen = "PopupOpen";
        public const string LockedTap = "LockedTap";
        public const string BlockPlace = "BlockPlace";
        public const string BlockInvalid = "BlockInvalid";
        public const string BlockReturn = "BlockReturn";
        public const string RowClear = "RowClear";
        public const string Combo2 = "Combo2";
        public const string Combo3 = "Combo3";
        public const string Combo4 = "Combo4";
        public const string BoardClear = "BoardClear";
        public const string HintUse = "HintUse";
        public const string HintShow = "HintShow";
        public const string HintNone = "HintNone";
        public const string LevelStart = "LevelStart";
        public const string LevelComplete = "LevelComplete";
        public const string StarCollect1 = "StarCollect1";
        public const string StarCollect2 = "StarCollect2";
        public const string StarCollect3 = "StarCollect3";
        public const string LevelUnlock = "LevelUnlock";
        public const string GameOver = "GameOver";
        public const string NewHighscore = "NewHighscore";
        public const string CatHappy = "CatHappy";
        public const string CatTap = "CatTap";
        public const string CatPurr = "CatPurr";
        public const string CatSurprised = "CatSurprised";
        public const string CatSad = "CatSad";
        public const string CatJump = "CatJump";
        public const string Confetti = "Confetti";
        public const string CoinCollect = "CoinCollect";
        public const string ChestOpen = "ChestOpen";
        public const string DailyReward = "DailyReward";
        public const string ShopBuy = "ShopBuy";
        public const string TimerWarning = "TimerWarning";
        public const string HammerArm = "HammerArm";
        public const string HammerHit = "HammerHit";
        public const string HammerLockBreak = "HammerLockBreak";
        public const string HammerBreak = "HammerBreak";

        public static string StarCollect(int starNumber) =>
            starNumber <= 1 ? StarCollect1 : starNumber == 2 ? StarCollect2 : StarCollect3;
    }
}
