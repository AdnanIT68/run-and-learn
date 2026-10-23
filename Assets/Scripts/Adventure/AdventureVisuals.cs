using UnityEngine;

namespace FootballRunner.Adventure
{
    /// <summary>
    /// Sprites used to dress the finite Green Hills course. Filled by the
    /// adventure installer so a device build does not depend on the Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "AdventureVisuals", menuName = "Football Runner/Adventure Visuals")]
    public class AdventureVisuals : ScriptableObject
    {
        public Sprite rukajjaPortrait;
        public Sprite pine;
        public Sprite palm;
        public Sprite bush;
        public Sprite rock;
        public Sprite plantHouse;
        public Sprite grassTuft;
        public Sprite kenneyBush;
        public Sprite kenneyRock;
        public Sprite flagGreen;
        public Sprite flagYellow;
        public Sprite signExit;
        public Sprite sign;
        public Sprite cloud;
        public Sprite smallPlatform;
        public Sprite groundTop;
    }
}
