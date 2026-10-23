using UnityEngine;

namespace FootballRunner.Collectibles
{
    /// <summary>
    /// Defines one kind of pickup. Adding a new fruit, ball or toy means creating
    /// one of these assets in the Project window - no new script, no code change.
    /// Right-click in Assets/ScriptableObjects and choose
    /// Create > Football Runner > Collectible Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCollectible", menuName = "Football Runner/Collectible Data")]
    public class CollectibleData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Apple";

        [Header("Appearance")]
        public Sprite sprite;
        public Color tint = Color.white;
        [Tooltip("Uniform size multiplier applied to the pickup in the world.")]
        public float scale = 1f;

        [Header("Reward")]
        [Tooltip("Added to the run score.")]
        public int scorePoints = 10;
        [Tooltip("Added to persistent currency. Leave at zero for non-coin pickups.")]
        public int coinValue;
        [Tooltip("Hit points restored, capped at the player's maximum. " +
                 "Leave at zero for pickups that do not heal.")]
        [Min(0)]
        public int healAmount;

        [Header("Spawning")]
        [Tooltip("Relative likelihood of being chosen. Higher means more common.")]
        [Min(0f)]
        public float spawnWeight = 10f;

        public bool IsCoin => coinValue > 0;
    }
}
