using System.Collections.Generic;
using UnityEngine;

namespace FootballRunner.Utilities
{
    /// <summary>
    /// Reuses GameObjects instead of instantiating and destroying them during play.
    /// An endless runner spawns and removes objects constantly, and on mid-range
    /// Android hardware that churn is the main cause of frame stutter.
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        private readonly Dictionary<GameObject, Stack<GameObject>> _pools =
            new Dictionary<GameObject, Stack<GameObject>>();

        private readonly Dictionary<GameObject, Transform> _containers =
            new Dictionary<GameObject, Transform>();

        private readonly Dictionary<GameObject, int> _createdCounts =
            new Dictionary<GameObject, int>();

        private const int CreatedCountWarning = 48;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null)
            {
                return;
            }

            Stack<GameObject> pool = GetPool(prefab);

            for (int i = 0; i < count; i++)
            {
                GameObject instance = CreateInstance(prefab);
                instance.SetActive(false);
                pool.Push(instance);
            }
        }

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                return null;
            }

            Stack<GameObject> pool = GetPool(prefab);
            GameObject instance = pool.Count > 0 ? pool.Pop() : CreateInstance(prefab);

            instance.transform.SetParent(parent != null ? parent : _containers[prefab], false);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            return instance;
        }

        public void Release(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            PooledObject pooled = instance.GetComponent<PooledObject>();

            if (pooled == null || pooled.SourcePrefab == null)
            {
                Destroy(instance);
                return;
            }

            // GetPool first: it creates the container as a side effect. Reading
            // _containers before that would throw for any instance this particular
            // PoolManager has not pooled itself, which happens after a scene reload.
            Stack<GameObject> pool = GetPool(pooled.SourcePrefab);

            instance.SetActive(false);
            instance.transform.SetParent(_containers[pooled.SourcePrefab], false);
            pool.Push(instance);
        }

        private Stack<GameObject> GetPool(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out Stack<GameObject> pool))
            {
                return pool;
            }

            pool = new Stack<GameObject>();
            _pools.Add(prefab, pool);

            var container = new GameObject($"Pool_{prefab.name}").transform;
            container.SetParent(transform, false);
            _containers.Add(prefab, container);

            return pool;
        }

        private GameObject CreateInstance(GameObject prefab)
        {
            GameObject instance = Instantiate(prefab, _containers[prefab]);
            instance.name = prefab.name;

            PooledObject pooled = instance.GetComponent<PooledObject>();

            if (pooled == null)
            {
                pooled = instance.AddComponent<PooledObject>();
            }

            pooled.SourcePrefab = prefab;

            if (!_createdCounts.TryGetValue(prefab, out int created))
            {
                created = 0;
            }

            created++;
            _createdCounts[prefab] = created;

            if (created == CreatedCountWarning)
            {
                Debug.LogWarning(
                    $"[PoolManager] Created {created} instances of {prefab.name}. " +
                    "Recycling should keep this count flat during a long run.");
            }

            return instance;
        }
    }
}
