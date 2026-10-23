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
        public string selectedCharacterId = "rukajja";
        public int greenHillsUnlockedLevel = 1;
        public int greenHillsLevel1Stars;

        /// <summary>"bs" when the child is learning Bosnian, "en" when learning English.</summary>
        public string learningLanguage = "bs";

        public List<WordProgress> wordProgress = new List<WordProgress>();

        /// <summary>Reserved for future skins and cosmetics.</summary>
        public List<string> unlockedItems = new List<string>();
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
}
