using System;
using System.Collections.Generic;
using System.Globalization;
using FootballRunner.SaveSystem;

namespace FootballRunner.Learning
{
    /// <summary>
    /// How long the child has played, day by day, for the parents' report and the daily
    /// time limit. Only time spent running counts, not menus or pauses. The last 14 days
    /// are kept; the date comes from DailyRewards.Clock so tests can move it.
    /// </summary>
    public static class PlayTime
    {
        private const int DaysKept = 14;

        /// <summary>Seconds not yet written to the save file, so it is not written every frame.</summary>
        private static float _unsaved;

        public static void Add(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            TodayRecord().seconds += seconds;
            _unsaved += seconds;

            if (_unsaved >= 10f)
            {
                Flush();
            }
        }

        /// <summary>Writes what was added since the last save.</summary>
        public static void Flush()
        {
            if (_unsaved > 0f)
            {
                _unsaved = 0f;
                SaveManager.Save();
            }
        }

        public static float TodaySeconds => TodayRecord().seconds;

        /// <summary>Seconds played today and the six days before.</summary>
        public static float WeekSeconds
        {
            get
            {
                DateTime today = DailyRewards.Clock().Date;
                float total = 0f;

                foreach (PlayDay day in SaveManager.Data.playDays)
                {
                    if (DateTime.TryParseExact(day.date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out DateTime date) &&
                        date <= today && (today - date).TotalDays < 7)
                    {
                        total += day.seconds;
                    }
                }

                return total;
            }
        }

        /// <summary>Whole minutes, rounded up once any time was played.</summary>
        public static int Minutes(float seconds)
        {
            return seconds <= 0f ? 0 : (int)Math.Ceiling(seconds / 60f);
        }

        private static PlayDay TodayRecord()
        {
            string today = DailyRewards.Clock().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            List<PlayDay> days = SaveManager.Data.playDays;

            for (int i = days.Count - 1; i >= 0; i--)
            {
                if (days[i].date == today)
                {
                    return days[i];
                }
            }

            var record = new PlayDay { date = today };
            days.Add(record);

            while (days.Count > DaysKept)
            {
                days.RemoveAt(0);
            }

            return record;
        }
    }
}
