using System;
using System.Collections.Generic;

namespace FootballRunner.SaveSystem
{
    /// <summary>
    /// Everything that must survive closing the app. Add new fields freely:
    /// missing fields in an older save file simply keep their default value.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int highScore;
        public int totalCoins;
        public bool musicEnabled = true;
        public bool soundEnabled = true;
        public bool vibrationEnabled = true;
        public string selectedCharacterId = "rukajja";
        public int greenHillsUnlockedLevel = 1;
        public int greenHillsLevel1Stars;

        /// <summary>"bs", "en", "de" or "ar": the language the child is learning.</summary>
        public string learningLanguage = "bs";

        /// <summary>Show the Latin transliteration under Arabic words (parents corner switch).</summary>
        public bool showTransliteration = true;

        /// <summary>Easy mode for 3 and 4 year olds (parents corner switch).</summary>
        public bool easyMode;

        /// <summary>Full sticker pages already rewarded, as "World1:bs=harun" (or "=coins").</summary>
        public List<string> stickerPages = new List<string>();

        /// <summary>For children learning Bosnian: "en" or "de" for menus and hints.</summary>
        public string homeLanguage = "en";

        public List<WordProgress> wordProgress = new List<WordProgress>();

        /// <summary>Counts every word asked, in any language; the clock for spaced repetition.</summary>
        public int askTick;

        /// <summary>Set once the child has caught a first word with the guided intro.</summary>
        public bool wordTutorialDone;

        /// <summary>The word world the next run uses, e.g. "World1".</summary>
        public string selectedPack = "World1";

        /// <summary>The level in that world the next run uses, 0 for level 1.</summary>
        public int selectedLevel;

        /// <summary>Best stars per world and language, from finished levels.</summary>
        public List<WorldResult> worldResults = new List<WorldResult>();

        /// <summary>Bought players ("character_harun") and future cosmetics.</summary>
        public List<string> unlockedItems = new List<string>();

        /// <summary>Day ("yyyy-MM-dd") the daily gift was last taken.</summary>
        public string dailyGiftDate = "";

        /// <summary>Next day on the seven-day gift track, 0 to 6.</summary>
        public int dailyGiftStep;

        /// <summary>Day today's three tasks belong to.</summary>
        public string missionDate = "";

        public List<DailyMission> missions = new List<DailyMission>();
    }

    /// <summary>One of the day's small tasks and how far the child got.</summary>
    [Serializable]
    public class DailyMission
    {
        public string kind;
        public int target;
        public int progress;
        public int reward;
        public bool done;
    }

    /// <summary>How often one word was caught or missed, per learned language.</summary>
    [Serializable]
    public class WordProgress
    {
        public string id;
        public string language;
        public int correct;
        public int missed;

        /// <summary>
        /// Spaced repetition box, 0 to 4: a right answer moves the word up one box, a
        /// miss sends it back to 0. Higher boxes are asked less often and after longer gaps.
        /// </summary>
        public int box;

        /// <summary>SaveData.askTick when the word was last asked; spacing is counted in asks.</summary>
        public int lastAsked;

        /// <summary>False for records saved before boxes existed; their box is estimated once.</summary>
        public bool spaced;
    }

    /// <summary>One level's best result in a world, for one learned language.</summary>
    [Serializable]
    public class WorldResult
    {
        public string pack;

        /// <summary>0 for level 1. Saves from before levels existed hold level 1 here.</summary>
        public int level;
        public string language;
        public int stars;
        public int completions;
    }
}
