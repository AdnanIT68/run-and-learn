using FootballRunner.SaveSystem;
using UnityEngine;

namespace FootballRunner.Learning
{
    /// <summary>
    /// What Pepi says out loud, in the language the child already speaks: praise for a
    /// run of right answers, a warning before a word the child keeps missing, a welcome
    /// to each world and the news that a new world is open. Voice only, no music.
    /// Lines come from Tools/pepi_lines.json via Tools/generate_pepi.py.
    /// </summary>
    public static class PepiVoice
    {
        public const string Remind = "pepi_remind";
        public const string Unlocked = "pepi_unlocked";
        public const string StreakPraise = "pepi_praise_3";

        /// <summary>Right answers in a row that earn a word from Pepi (3, 6, 9...).</summary>
        public const int StreakLength = 3;

        public static AudioClip Line(string id)
        {
            return Resources.Load<AudioClip>($"Words/Pepi/Audio/{LearningProgress.HomeLanguage}/{id}");
        }

        [System.Serializable]
        private class LineText
        {
            public string id;
            public string bs;
            public string en;
            public string de;
        }

        [System.Serializable]
        private class LineTexts
        {
            public LineText[] lines;
        }

        private static LineTexts _texts;

        /// <summary>What a line says, in the menu language (from Resources/Words/Pepi/lines.json), or "".</summary>
        public static string Text(string id)
        {
            if (_texts == null)
            {
                var asset = Resources.Load<TextAsset>("Words/Pepi/lines");
                _texts = asset != null ? JsonUtility.FromJson<LineTexts>(asset.text) : new LineTexts();
            }

            if (_texts.lines != null)
            {
                foreach (LineText line in _texts.lines)
                {
                    if (line.id == id)
                    {
                        return LearningProgress.Ui(line.bs, line.en, line.de);
                    }
                }
            }

            return string.Empty;
        }

        public static string StoryIntroFor(string pack)
        {
            return "story_intro_" + pack;
        }

        public static string StoryOutroFor(string pack)
        {
            return "story_outro_" + pack;
        }

        /// <summary>"Three in a row!" the first time, a general praise after that.</summary>
        public static string PraiseFor(int streak)
        {
            return streak == StreakLength ? StreakPraise : (Random.value < 0.5f ? "pepi_praise_1" : "pepi_praise_2");
        }

        public static string WelcomeFor(string pack)
        {
            return "pepi_world_" + pack;
        }

        /// <summary>A word missed at least twice and not caught right since: Pepi warns before it.</summary>
        public static bool IsTroubleWord(string wordId)
        {
            WordProgress progress = LearningProgress.Get(wordId);
            return progress.missed >= 2 && progress.box == 0;
        }

        /// <summary>True the first time a world is played in the current learned language; marks it as welcomed.</summary>
        public static bool TakeWelcome(string pack)
        {
            string key = pack + ":" + LearningProgress.Language;

            if (SaveManager.Data.pepiWelcomed.Contains(key))
            {
                return false;
            }

            SaveManager.Data.pepiWelcomed.Add(key);
            SaveManager.Save();
            return true;
        }
    }
}
