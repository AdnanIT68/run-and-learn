using System.Collections.Generic;
using FootballRunner.Utilities;
using UnityEngine;

namespace FootballRunner.Effects
{
    /// <summary>
    /// Tiny sprite particles for dust, sparks and confetti. One component moves
    /// every particle and reuses them from a pool, which is cheaper on phones
    /// than a ParticleSystem per effect and needs no prefab.
    /// </summary>
    public class FxPlayer : MonoBehaviour
    {
        private const int SortingOrder = 28;
        private const int MaxParticles = 160;

        private struct Particle
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Vector3 Velocity;
            public float Gravity;
            public float Age;
            public float Life;
            public float StartSize;
            public float EndSize;
            public float Spin;
            public Color Colour;
        }

        private static FxPlayer _instance;

        private readonly List<Particle> _live = new List<Particle>(MaxParticles);
        private readonly Stack<SpriteRenderer> _free = new Stack<SpriteRenderer>(MaxParticles);
        private Sprite _star;

        public int LiveCount => _live.Count;

        public static FxPlayer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject("FxPlayer").AddComponent<FxPlayer>();
                }

                return _instance;
            }
        }

        public static bool Exists => _instance != null;

        private void Awake()
        {
            _star = Learning.WordPack.Icon("star");
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>Soft puffs at the feet on jump and landing.</summary>
        public void Dust(Vector3 position)
        {
            var colour = new Color(0.86f, 0.8f, 0.68f, 0.85f);

            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                var velocity = new Vector3(side * Random.Range(0.8f, 2.2f), Random.Range(0.2f, 0.9f), 0f);
                Emit(RuntimeSprites.SoftDot(), position, velocity, 0f, Random.Range(0.3f, 0.45f), 0.35f, 0.08f,
                    colour, 0f);
            }
        }

        /// <summary>A white ring of puffs under the runner for the second jump.</summary>
        public void AirRing(Vector3 position)
        {
            var colour = new Color(1f, 1f, 1f, 0.9f);
            const int count = 10;

            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f;
                var velocity = new Vector3(Mathf.Cos(angle) * 2.6f, Mathf.Sin(angle) * 1.1f - 0.6f, 0f);
                Emit(RuntimeSprites.SoftDot(), position, velocity, 0f, 0.3f, 0.3f, 0.05f, colour, 0f);
            }
        }

        /// <summary>Gold stars bursting out of a caught word.</summary>
        public void Sparkle(Vector3 position, int count = 12)
        {
            Sprite sprite = _star != null ? _star : RuntimeSprites.SoftDot();

            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(2.5f, 5f);
                var velocity = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed + 1.5f, 0f);
                Emit(sprite, position, velocity, -9f, Random.Range(0.5f, 0.8f), 0.42f, 0.1f, Color.white,
                    Random.Range(-360f, 360f));
            }
        }

        /// <summary>Colourful confetti above the runner when the goal row fills.</summary>
        public void Confetti(Vector3 position)
        {
            Color[] colours =
            {
                new Color(1f, 0.82f, 0.25f), new Color(0.35f, 0.8f, 0.45f), new Color(0.35f, 0.65f, 1f),
                new Color(1f, 0.5f, 0.55f), new Color(0.75f, 0.55f, 1f)
            };

            for (int i = 0; i < 40; i++)
            {
                var velocity = new Vector3(Random.Range(-5f, 5f), Random.Range(4f, 9f), 0f);
                Emit(RuntimeSprites.SoftDot(), position, velocity, -12f, Random.Range(0.9f, 1.4f), 0.3f, 0.2f,
                    colours[i % colours.Length], 0f);
            }

            Sparkle(position, 8);
        }

        /// <summary>A quick red puff where the runner was hurt.</summary>
        public void Hit(Vector3 position)
        {
            var colour = new Color(1f, 0.35f, 0.3f, 0.9f);

            for (int i = 0; i < 8; i++)
            {
                float angle = i / 8f * Mathf.PI * 2f;
                var velocity = new Vector3(Mathf.Cos(angle) * 3f, Mathf.Sin(angle) * 3f, 0f);
                Emit(RuntimeSprites.SoftDot(), position, velocity, 0f, 0.3f, 0.4f, 0.05f, colour, 0f);
            }
        }

        private void Emit(Sprite sprite, Vector3 position, Vector3 velocity, float gravity, float life,
            float startSize, float endSize, Color colour, float spin)
        {
            if (_live.Count >= MaxParticles)
            {
                return;
            }

            SpriteRenderer renderer = _free.Count > 0 ? _free.Pop() : CreateRenderer();
            renderer.sprite = sprite;
            renderer.color = colour;
            renderer.gameObject.SetActive(true);
            Transform t = renderer.transform;
            t.position = position;
            t.rotation = Quaternion.identity;
            t.localScale = Vector3.one * startSize / SpriteWidth(sprite);

            _live.Add(new Particle
            {
                Transform = t,
                Renderer = renderer,
                Velocity = velocity,
                Gravity = gravity,
                Life = life,
                StartSize = startSize,
                EndSize = endSize,
                Spin = spin,
                Colour = colour
            });
        }

        private SpriteRenderer CreateRenderer()
        {
            var go = new GameObject("Fx");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = SortingOrder;
            return renderer;
        }

        private static float SpriteWidth(Sprite sprite)
        {
            return sprite != null ? Mathf.Max(0.01f, sprite.bounds.size.x) : 1f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            for (int i = _live.Count - 1; i >= 0; i--)
            {
                Particle p = _live[i];
                p.Age += dt;

                if (p.Age >= p.Life || p.Transform == null)
                {
                    if (p.Renderer != null)
                    {
                        p.Renderer.gameObject.SetActive(false);
                        _free.Push(p.Renderer);
                    }

                    _live.RemoveAt(i);
                    continue;
                }

                float t = p.Age / p.Life;
                p.Velocity.y += p.Gravity * dt;
                p.Transform.position += p.Velocity * dt;
                p.Transform.Rotate(0f, 0f, p.Spin * dt);
                float size = Mathf.Lerp(p.StartSize, p.EndSize, t);
                p.Transform.localScale = Vector3.one * size / SpriteWidth(p.Renderer.sprite);
                Color colour = p.Colour;
                colour.a *= 1f - t * t;
                p.Renderer.color = colour;
                _live[i] = p;
            }
        }
    }
}
