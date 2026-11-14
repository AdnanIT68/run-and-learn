using UnityEngine;

namespace FootballRunner.Learning
{
    /// <summary>Brbljavka's pictures from Tools/make_story_art.py.</summary>
    public static class StoryArt
    {
        public static Sprite Magpie(bool carryingLetters)
        {
            return Resources.Load<Sprite>(carryingLetters ? "Story/magpie_letters" : "Story/magpie");
        }

        public static Sprite MagpieWingsDown()
        {
            return Resources.Load<Sprite>("Story/magpie_down_letters");
        }
    }
}
