using NUnit.Framework;
using UnityEngine;

namespace FootballRunner.Tests
{
    /// <summary>
    /// Keeps every test run silent: voices and effects still play (tests check clips and
    /// timing), but the listener volume is 0 for the whole run and restored afterwards.
    /// Nothing is saved to the project or the game's own sound setting.
    /// </summary>
    [SetUpFixture]
    public class SilentTestRun
    {
        private float _volumeBefore = 1f;

        [OneTimeSetUp]
        public void Mute()
        {
            _volumeBefore = AudioListener.volume;
            AudioListener.volume = 0f;
        }

        [OneTimeTearDown]
        public void Unmute()
        {
            AudioListener.volume = _volumeBefore;
        }
    }
}
