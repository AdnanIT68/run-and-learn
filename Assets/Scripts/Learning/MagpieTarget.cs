using UnityEngine;

namespace FootballRunner.Learning
{
    /// <summary>
    /// Brbljavka at the end of a world's last level: she flies ahead of the runner at
    /// jumping height, carrying the stolen letters, a little slower than the run, so
    /// the child catches up and jumps into her. She never escapes for good.
    /// </summary>
    public class MagpieTarget : MonoBehaviour
    {
        private const int SortingOrder = 31;

        /// <summary>How far ahead of the runner she appears (and reappears after a miss).</summary>
        public const float LeadDistance = 11f;

        /// <summary>She drifts forward at this share of the run speed, so the runner gains on her.</summary>
        private const float SpeedShare = 0.45f;

        private const float Size = 1.6f;

        private SpriteRenderer _renderer;
        private Sprite _up;
        private Sprite _down;
        private float _baseY;
        private float _time;
        private bool _caught;

        public Bounds Bounds => _renderer != null ? _renderer.bounds : new Bounds(transform.position, Vector3.one);

        public static MagpieTarget Create(Transform parent, Vector3 position)
        {
            var go = new GameObject("Brbljavka");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var magpie = go.AddComponent<MagpieTarget>();
            magpie._renderer = go.AddComponent<SpriteRenderer>();
            magpie._renderer.sortingOrder = SortingOrder;
            magpie._up = StoryArt.Magpie(carryingLetters: true);
            magpie._down = StoryArt.MagpieWingsDown();
            magpie._renderer.sprite = magpie._up;
            magpie._baseY = position.y;

            if (magpie._up != null)
            {
                float width = magpie._up.bounds.size.x;
                float scale = width > 0f ? Size / width : 1f;
                go.transform.localScale = new Vector3(scale, scale, 1f);
            }

            return magpie;
        }

        /// <summary>Moves her to a spot; her bobbing then centres on it.</summary>
        public void PlaceAt(Vector3 position)
        {
            _baseY = position.y;
            transform.position = position;
        }

        public void Caught()
        {
            _caught = true;
            _renderer.sprite = StoryArt.Magpie(carryingLetters: false) ?? _up;
            Destroy(gameObject, 1.2f);
        }

        private void Update()
        {
            _time += Time.deltaTime;

            if (_caught)
            {
                // Lets go of the letters and flutters up and away.
                transform.position += new Vector3(2.5f, 3f, 0f) * Time.deltaTime;
                return;
            }

            float speed = Gameplay.DifficultyManager.Instance != null ? Gameplay.DifficultyManager.Instance.CurrentSpeed : 6f;
            Vector3 p = transform.position;
            p.x += speed * SpeedShare * Time.deltaTime;
            p.y = _baseY + Mathf.Sin(_time * 4f) * 0.25f;
            transform.position = p;

            // Flapping: wings up and down a few times a second.
            if (_down != null)
            {
                _renderer.sprite = Mathf.Repeat(_time * 6f, 1f) < 0.5f ? _up : _down;
            }
        }
    }
}
