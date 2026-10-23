using FootballRunner.Utilities;
using UnityEngine;

namespace FootballRunner.UI
{
    /// <summary>
    /// Small service so pickups do not each need their own reference to the
    /// floating text prefab and the pool.
    /// </summary>
    public class FloatingTextSpawner : MonoBehaviour
    {
        public static FloatingTextSpawner Instance { get; private set; }

        [SerializeField] private GameObject floatingTextPrefab;
        [SerializeField] private int prewarmCount = 8;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (floatingTextPrefab != null && PoolManager.Instance != null)
            {
                PoolManager.Instance.Prewarm(floatingTextPrefab, prewarmCount);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Show(string text, Vector3 worldPosition)
        {
            if (floatingTextPrefab == null || PoolManager.Instance == null)
            {
                return;
            }

            GameObject instance = PoolManager.Instance.Get(floatingTextPrefab, worldPosition, Quaternion.identity);
            instance.GetComponent<FloatingText>()?.Show(text);
        }
    }
}
