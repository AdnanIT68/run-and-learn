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

        /// <summary>"bs" when the child is learning Bosnian, "en" when learning English.</summary>
        public string learningLanguage = "bs";

        public List<WordProgress> wordProgress = new List<WordProgress>();

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
