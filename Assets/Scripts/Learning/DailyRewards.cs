using System;
using System.Collections.Generic;
using System.Globalization;
using FootballRunner.SaveSystem;

namespace FootballRunner.Learning
{
    /// <summary>
    /// A daily gift and three small daily tasks, made for young children: every
    /// reward is fixed and shown in advance (no lucky draws), and missing a day
    /// never takes anything away; the gift track simply waits for the next visit.
    /// </summary>
    public static class DailyRewards
    {
        /// <summary>Coins for gift days one to seven; after day seven the track starts again.</summary>
        public static readonly int[] GiftTrack = { 10, 15, 20, 25, 30, 40, 60 };

        public enum Kind
        {
            CatchWords,
            FinishLevel,
            ThreeStars,
            CollectCoins,
            NewWord
        }

        private struct Template
        {
            public Kind Kind;
            public int Target;
            public int Reward;
        }

        private static readonly Template[] Templates =
        {
            new Template { Kind = Kind.CatchWords, Target = 8, Reward = 15 },
            new Template { Kind = Kind.FinishLevel, Target = 1, Reward = 20 },
            new Template { Kind = Kind.ThreeStars, Target = 1, Reward = 25 },
            new Template { Kind = Kind.CollectCoins, Target = 20, Reward = 15 },
            new Template { Kind = Kind.NewWord, Target = 2, Reward = 20 }
        };

        /// <summary>Today's date source; tests move it to another day.</summary>
        public static Func<DateTime> Clock = () => DateTime.Now;

        /// <summary>Raised when a task is finished; its coins are already added.</summary>
        public static event Action<DailyMission> MissionCompleted;

        /// <summary>Raised after the gift is taken or a task moves on.</summary>
        public static event Action Changed;

        private static string Today => Clock().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public static bool GiftReady => SaveManager.Data.dailyGiftDate != Today;

        /// <summary>Position on the seven-day track of the next gift to take.</summary>
        public static int GiftStep => Math.Abs(SaveManager.Data.dailyGiftStep) % GiftTrack.Length;

        public static int NextGift => GiftTrack[GiftStep];

        /// <summary>Takes today's gift. Returns the coins added, or 0 if it was already taken today.</summary>
        public static int ClaimGift()
        {
            if (!GiftReady)
            {
                return 0;
            }

            SaveData data = SaveManager.Data;
            int coins = NextGift;
            data.totalCoins += coins;
            data.dailyGiftStep = (GiftStep + 1) % GiftTrack.Length;
            data.dailyGiftDate = Today;
            SaveManager.Save();
            Changed?.Invoke();
            return coins;
        }

        /// <summary>Today's three tasks, made fresh on the first look each day.</summary>
        public static IReadOnlyList<DailyMission> Missions
        {
            get
            {
                EnsureToday();
                return SaveManager.Data.missions;
            }
        }

        /// <summary>Counts something the child did towards today's tasks.</summary>
        public static void Report(Kind kind, int amount = 1)
        {
            if (amount <= 0)
            {
                return;
            }

            EnsureToday();
            bool changed = false;

            foreach (DailyMission mission in SaveManager.Data.missions)
            {
                if (mission.done || mission.kind != kind.ToString())
                {
                    continue;
                }

                mission.progress = Math.Min(mission.target, mission.progress + amount);
                changed = true;

                if (mission.progress >= mission.target)
                {
                    mission.done = true;
                    SaveManager.Data.totalCoins += mission.reward;
                    MissionCompleted?.Invoke(mission);
                }
            }

            if (changed)
            {
                SaveManager.Save();
                Changed?.Invoke();
            }
        }

        /// <summary>The task in the language the child already speaks.</summary>
        public static string Describe(DailyMission mission, bool bosnian)
        {
            int n = mission.target;
            Enum.TryParse(mission.kind, out Kind kind);

            switch (kind)
            {
                case Kind.CatchWords:
                    return bosnian ? $"Uhvati {n} riječi" : $"Catch {n} words";
                case Kind.FinishLevel:
                    return bosnian ? "Završi jedan nivo" : "Finish a level";
                case Kind.ThreeStars:
                    return bosnian ? "Osvoji 3 zvjezdice na nivou" : "Win 3 stars on a level";
                case Kind.CollectCoins:
                    return bosnian ? $"Skupi {n} novčića u igri" : $"Collect {n} coins in a run";
                case Kind.NewWord:
                    return bosnian ? $"Nauči {n} nove riječi" : $"Learn {n} new words";
                default:
                    return mission.kind;
            }
        }

        private static void EnsureToday()
        {
            SaveData data = SaveManager.Data;
            string today = Today;

            if (data.missionDate == today && data.missions.Count > 0)
            {
                return;
            }

            data.missionDate = today;
            data.missions.Clear();

            // One task about words every day, then one about levels and one about coins
            // or new words. The day picks which, so the mix changes but never surprises.
            int seed = Clock().DayOfYear;
            Add(data, Templates[0]);
            Add(data, Templates[seed % 2 == 0 ? 1 : 2]);
            Add(data, Templates[seed % 3 == 0 ? 3 : 4]);
            SaveManager.Save();
        }

        private static void Add(SaveData data, Template template)
        {
            data.missions.Add(new DailyMission
            {
                kind = template.Kind.ToString(),
                target = template.Target,
                reward = template.Reward
            });
        }
    }
}
