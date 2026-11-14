using FootballRunner.Core;
using NUnit.Framework;

namespace FootballRunner.Tests
{
    /// <summary>The display stays on only while a run is going.</summary>
    public class ScreenSleepTests
    {
        [TestCase(GameState.Ready, true)]
        [TestCase(GameState.Playing, true)]
        [TestCase(GameState.Dying, true)]
        [TestCase(GameState.Paused, false)]
        [TestCase(GameState.GameOver, false)]
        [TestCase(GameState.LevelComplete, false)]
        public void ScreenStaysAwakeOnlyDuringARun(GameState state, bool awake)
        {
            Assert.AreEqual(awake, GameManager.KeepsScreenAwake(state));
        }
    }
}
