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

        /// <summary>Reserved for future skins and cosmetics.</summary>
        public List<string> unlockedItems = new List<string>();
    }
}
