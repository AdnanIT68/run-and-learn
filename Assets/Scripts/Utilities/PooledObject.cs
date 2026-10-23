using UnityEngine;

namespace FootballRunner.Utilities
{
    /// <summary>
    /// Added automatically to every pooled instance so it can find its way home
    /// without the caller needing to remember which prefab it came from.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        public GameObject SourcePrefab { get; set; }

        /// <summary>Returns this instance to its pool, or destroys it if pooling is gone.</summary>
        public void ReturnToPool()
        {
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Release(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
