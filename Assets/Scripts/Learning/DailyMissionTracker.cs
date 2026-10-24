using FootballRunner.Core;
using FootballRunner.Gameplay;
using UnityEngine;

namespace FootballRunner.Learning
{
    /// <summary>
    /// Watches a run for what today's tasks ask (words caught, new words, coins,
    /// finished levels and three-star levels) and reports it to DailyRewards.
    /// Lives on the GameManager; adds nothing to the scene.
    /// </summary>
    public class DailyMissionTracker : MonoBehaviour
    {
        private int _lastCoins;

        public static DailyMissionTracker EnsureOn(GameObject host)
        {
            var tracker = host.GetComponent<DailyMissionTracker>();
            return tracker != null ? tracker : host.AddComponent<DailyMissionTracker>();
        }

        private void OnEnable()
        {
            WordChallengeDirector.ChallengeEnded += OnChallengeEnded;
            ScoreManager.CoinsChanged += OnCoinsChanged;
            GameManager.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            WordChallengeDirector.ChallengeEnded -= OnChallengeEnded;
            ScoreManager.CoinsChanged -= OnCoinsChanged;
            GameManager.StateChanged -= OnStateChanged;
        }

        private static void OnChallengeEnded(WordEntry word, bool caught)
        {
            if (!caught || word == null)
            {
                return;
            }

            DailyRewards.Report(DailyRewards.Kind.CatchWords);

            // The first time a plain word is caught it counts as newly learned
            // (the director has already recorded this catch).
            bool plainWord = !word.IsCounting && !word.IsCommand && !word.IsSize && !word.IsSentence &&
                             !word.IsLetter;
            if (plainWord && LearningProgress.Get(word.id).correct == 1)
            {
                DailyRewards.Report(DailyRewards.Kind.NewWord);
            }
        }

        private void OnCoinsChanged(int runCoins)
        {
            // A new run starts again from zero.
            if (runCoins < _lastCoins)
            {
                _lastCoins = 0;
            }

            DailyRewards.Report(DailyRewards.Kind.CollectCoins, runCoins - _lastCoins);
            _lastCoins = runCoins;
        }

        private static void OnStateChanged(GameState state)
        {
            WordChallengeDirector director = WordChallengeDirector.Instance;

            if (state != GameState.LevelComplete || director == null || !director.LevelDone)
            {
                return;
            }

            DailyRewards.Report(DailyRewards.Kind.FinishLevel);

            if (director.LevelStars >= 3)
            {
                DailyRewards.Report(DailyRewards.Kind.ThreeStars);
            }
        }
    }
}
