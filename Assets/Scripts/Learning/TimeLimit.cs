using FootballRunner.SaveSystem;

namespace FootballRunner.Learning
{
    /// <summary>
    /// The daily play limit a parent sets in the parents corner (behind its sum gate).
    /// Once today's running time reaches it, Pepi says it is time for a break and
    /// play stops until tomorrow or until a parent changes the limit. Closing and
    /// reopening the game does not help: the time is kept in the save file.
    /// </summary>
    public static class TimeLimit
    {
        /// <summary>The choices the parents corner button steps through, in minutes; 0 is no limit.</summary>
        public static readonly int[] Choices = { 0, 10, 15, 20, 30, 45, 60 };

        public static int Minutes
        {
            get => SaveManager.Data.dailyLimitMinutes;
            set
            {
                SaveManager.Data.dailyLimitMinutes = value < 0 ? 0 : value;
                SaveManager.Save();
            }
        }

        public static bool IsOn => Minutes > 0;

        /// <summary>True once today's play has used up the limit.</summary>
        public static bool IsReached => IsOn && PlayTime.TodaySeconds >= Minutes * 60f;

        /// <summary>Moves to the next choice: off, 10, 15, 20, 30, 45, 60 minutes, then off again.</summary>
        public static void Next()
        {
            int index = System.Array.IndexOf(Choices, Minutes);
            Minutes = Choices[(index + 1) % Choices.Length];
        }
    }
}
