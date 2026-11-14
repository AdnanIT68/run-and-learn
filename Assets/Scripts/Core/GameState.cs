namespace FootballRunner.Core
{
    public enum GameState
    {
        Ready,
        Playing,
        Paused,
        Dying,
        GameOver,
        LevelComplete,

        /// <summary>The daily time limit is used up: play stops until a parent changes it.</summary>
        Resting
    }
}
