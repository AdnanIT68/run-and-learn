using System.Collections.Generic;
using FootballRunner.Utilities;
using UnityEngine;

namespace FootballRunner.Gameplay
{
    /// <summary>
    /// One piece of the endless track. Its origin sits at the left edge, so the
    /// generator can lay chunks end to end by adding <see cref="Length"/>.
    /// Slots are empty child transforms marking where content may be placed.
    /// </summary>
    public class Chunk : MonoBehaviour
    {
        [Tooltip("World units from the left edge of this chunk to the left edge of the next one.")]
        [SerializeField] private float length = 20f;

        [Tooltip("Ground-level positions where an obstacle may stand.")]
        [SerializeField] private Transform[] obstacleSlots;

        [Tooltip("Positions where pickups may appear, including above gaps and platforms.")]
        [SerializeField] private Transform[] collectibleSlots;

        private readonly List<GameObject> _spawnedContent = new List<GameObject>();
        private SpriteRenderer[] _terrainRenderers;

        public float Length => length;

        public IReadOnlyList<Transform> ObstacleSlots => obstacleSlots;

        public IReadOnlyList<Transform> CollectibleSlots => collectibleSlots;

        public void RegisterContent(GameObject instance)
        {
            if (instance != null)
            {
                _spawnedContent.Add(instance);
            }
        }

        /// <summary>Returns every pickup and obstacle this chunk spawned to the pool.</summary>
        public void ReleaseContent()
        {
            for (int i = 0; i < _spawnedContent.Count; i++)
            {
                GameObject instance = _spawnedContent[i];

                if (instance == null)
                {
                    continue;
                }

                PooledObject pooled = instance.GetComponent<PooledObject>();

                if (pooled != null)
                {
                    pooled.ReturnToPool();
                }
                else
                {
                    Destroy(instance);
                }
            }

            _spawnedContent.Clear();
        }

        /// <summary>
        /// Tints only this chunk's own sprites. Pickups and hazards are parented
        /// elsewhere, so they keep their own colours.
        /// </summary>
        public void ApplyTint(Color tint)
        {
            if (_terrainRenderers == null)
            {
                _terrainRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            }

            for (int i = 0; i < _terrainRenderers.Length; i++)
            {
                if (_terrainRenderers[i] != null)
                {
                    _terrainRenderers[i].color = tint;
                }
            }
        }
    }
}
