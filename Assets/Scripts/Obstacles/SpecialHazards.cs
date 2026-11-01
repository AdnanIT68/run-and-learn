using FootballRunner.Core;
using FootballRunner.Player;
using UnityEngine;

namespace FootballRunner.Obstacles
{
    /// <summary>
    /// Hazards built in code rather than from prefabs, so each world can dress them
    /// in its own picture: flyers at head height that a runner on the ground passes
    /// under ("stay down!"), and a football that rolls slowly towards the runner.
    /// Both carry an <see cref="Obstacle"/>, so damage, word-bubble spacing and
    /// pooling rules treat them like every other hazard.
    /// </summary>
    public static class SpecialHazards
    {
        /// <summary>The runner's capsule is 1.5 tall; flyers start just above it.</summary>
        public const float FlyerBottom = 1.85f;
        public const float FlyerHeight = 0.6f;
        public const int FlyersPerGroup = 3;
        private const float FlyerSize = 0.62f;
        private const float FlyerSpacing = 0.68f;
        public const float BallSize = 0.8f;
        private const int SortingOrder = 5;

        /// <summary>A small flock in a row above the ground at <paramref name="groundPosition"/>.</summary>
        public static GameObject CreateFlyers(Vector3 groundPosition, string world)
        {
            var root = new GameObject("Hazard_Flyers");
            root.transform.position = new Vector3(groundPosition.x, groundPosition.y, 0f);

            Sprite sprite = Load("flyer_" + (string.IsNullOrEmpty(world) ? "default" : world)) ?? Load("flyer_default");
            float width = (FlyersPerGroup - 1) * FlyerSpacing + FlyerSize;

            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(width * 0.9f, FlyerHeight);
            box.offset = new Vector2(0f, FlyerBottom + FlyerHeight * 0.5f);
            root.AddComponent<Obstacle>();

            var formation = root.AddComponent<FlyerFormation>();

            for (int i = 0; i < FlyersPerGroup; i++)
            {
                var flyer = new GameObject("Flyer" + i);
                flyer.transform.SetParent(root.transform, false);
                flyer.transform.localPosition = new Vector3((i - (FlyersPerGroup - 1) * 0.5f) * FlyerSpacing,
                    FlyerBottom + FlyerHeight * 0.5f, 0f);
                SpriteRenderer renderer = flyer.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = SortingOrder;
                Fit(flyer.transform, sprite, FlyerSize);
                formation.Add(flyer.transform);
            }

            // A soft shadow on the ground shows a small child where the flock is.
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            SpriteRenderer shade = shadow.AddComponent<SpriteRenderer>();
            shade.sprite = Utilities.RuntimeSprites.SoftDot();
            shade.color = new Color(0f, 0f, 0f, 0.28f);
            shade.sortingOrder = SortingOrder - 1;
            Fit(shadow.transform, shade.sprite, width);
            shadow.transform.localScale = new Vector3(shadow.transform.localScale.x, shadow.transform.localScale.y * 0.22f, 1f);

            return root;
        }

        /// <summary>A football resting on the ground at <paramref name="groundPosition"/>, rolling once the runner is near.</summary>
        public static GameObject CreateBall(Vector3 groundPosition)
        {
            var root = new GameObject("Hazard_Ball");
            root.transform.position = new Vector3(groundPosition.x, groundPosition.y + BallSize * 0.5f, 0f);

            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var circle = root.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = BallSize * 0.42f;
            root.AddComponent<Obstacle>();

            var picture = new GameObject("Picture");
            picture.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = picture.AddComponent<SpriteRenderer>();
            renderer.sprite = Load("ball");
            renderer.sortingOrder = SortingOrder;
            Fit(picture.transform, renderer.sprite, BallSize);

            root.AddComponent<RollingBall>().Picture = picture.transform;
            return root;
        }

        public const float PropWidth = 0.8f;
        public const float PropHeight = 0.85f;
        private const float PropSize = 1.05f;

        /// <summary>This world's own ground hazard (a sheep, a snowman, a pile of books...), jumped like a crate.</summary>
        public static GameObject CreateProp(Vector3 groundPosition, string world)
        {
            var root = new GameObject("Hazard_Prop");
            root.transform.position = new Vector3(groundPosition.x, groundPosition.y, 0f);

            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(PropWidth, PropHeight);
            box.offset = new Vector2(0f, PropHeight * 0.5f);
            root.AddComponent<Obstacle>();

            Sprite sprite = Load("prop_" + (string.IsNullOrEmpty(world) ? "World1" : world)) ?? Load("prop_World1");
            var picture = new GameObject("Picture");
            picture.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = picture.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = SortingOrder;
            Fit(picture.transform, sprite, PropSize);
            float height = sprite != null ? sprite.bounds.size.y * picture.transform.localScale.y : PropSize;
            picture.transform.localPosition = new Vector3(0f, height * 0.5f - 0.04f, 0f);

            if (world == "World8")
            {
                // A grey rock becomes a glowing space rock in the night sky.
                renderer.color = new Color(0.82f, 0.7f, 1f);
            }

            return root;
        }

        public const float TrampolineWidth = 1.4f;

        /// <summary>A harmless trampoline that throws the runner up to a row of coins.</summary>
        public static GameObject CreateTrampoline(Vector3 groundPosition)
        {
            var root = new GameObject("Hazard_Trampoline");
            root.transform.position = new Vector3(groundPosition.x, groundPosition.y, 0f);

            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(TrampolineWidth, 0.55f);
            box.offset = new Vector2(0f, 0.3f);
            // Damage 0: word bubbles still keep clear of it, but touching it never hurts.
            root.AddComponent<Obstacle>().Damage = 0;

            var legs = new Color32(70, 80, 100, 255);
            Block(root.transform, "LegLeft", new Vector2(-TrampolineWidth * 0.36f, 0.2f), new Vector2(0.13f, 0.4f), legs, SortingOrder - 1);
            Block(root.transform, "LegRight", new Vector2(TrampolineWidth * 0.36f, 0.2f), new Vector2(0.13f, 0.4f), legs, SortingOrder - 1);
            Transform mat = Block(root.transform, "Mat", new Vector2(0f, 0.42f), new Vector2(TrampolineWidth, 0.28f),
                new Color32(235, 70, 80, 255), SortingOrder);
            Block(mat, "Stripe", Vector2.zero, new Vector2(0.86f, 0.35f), new Color32(255, 220, 90, 255), SortingOrder + 1, true);

            root.AddComponent<Trampoline>().Mat = mat;
            return root;
        }

        public const float MudWidth = 1.7f;

        /// <summary>A puddle that splashes and slows the runner for a moment, but never hurts.</summary>
        public static GameObject CreateMud(Vector3 groundPosition, string world)
        {
            var root = new GameObject("Hazard_Mud");
            root.transform.position = new Vector3(groundPosition.x, groundPosition.y, 0f);

            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(MudWidth * 0.85f, 0.5f);
            box.offset = new Vector2(0f, 0.25f);
            root.AddComponent<Obstacle>().Damage = 0;

            Color color = MudColor(world);
            // A darker rim under a lighter top reads as a puddle, not a flat stain.
            Color rim = Color.Lerp(color, Color.black, 0.3f);
            Transform edge = Block(root.transform, "Rim", new Vector2(0f, 0.06f), Vector2.one, rim, SortingOrder - 2);
            SetSprite(edge, Utilities.RuntimeSprites.Circle(), new Vector2(MudWidth, 0.36f));
            Transform puddle = Block(root.transform, "Puddle", new Vector2(0f, 0.09f), Vector2.one, color, SortingOrder - 1);
            SetSprite(puddle, Utilities.RuntimeSprites.Circle(), new Vector2(MudWidth * 0.92f, 0.26f));

            Color shine = Color.Lerp(color, Color.white, 0.35f);
            for (int i = 0; i < 3; i++)
            {
                float size = 0.14f - i * 0.02f;
                Transform bubble = Block(root.transform, "Bubble" + i, new Vector2(-0.45f + i * 0.42f, 0.1f + (i % 2) * 0.04f),
                    Vector2.one * size, shine, SortingOrder);
                SetSprite(bubble, Utilities.RuntimeSprites.Circle(), Vector2.one * size);
            }

            root.AddComponent<MudPuddle>().Color = color;
            return root;
        }

        private static Color MudColor(string world)
        {
            switch (world)
            {
                case "World5": return new Color32(80, 150, 215, 235);  // river water
                case "World6": return new Color32(175, 210, 235, 235); // slush
                case "World8": return new Color32(140, 90, 200, 235);  // space goo
                default: return new Color32(120, 80, 45, 235);
            }
        }

        /// <summary>A coloured rounded box of <paramref name="size"/> world units (or parent units when local).</summary>
        private static Transform Block(Transform parent, string name, Vector2 position, Vector2 size, Color color,
            int order, bool local = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.color = color;
            renderer.sortingOrder = order;

            if (local)
            {
                // Inside a parent box of the same sprite, size is a share of that box.
                renderer.sprite = Utilities.RuntimeSprites.RoundedRect();
                go.transform.localScale = new Vector3(size.x, size.y, 1f);
            }
            else
            {
                SetSprite(go.transform, Utilities.RuntimeSprites.RoundedRect(), size);
            }

            return go.transform;
        }

        private static void SetSprite(Transform target, Sprite sprite, Vector2 size)
        {
            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            Vector2 native = sprite != null ? (Vector2)sprite.bounds.size : Vector2.one;
            target.localScale = new Vector3(size.x / Mathf.Max(0.01f, native.x), size.y / Mathf.Max(0.01f, native.y), 1f);
        }

        private static Sprite Load(string name)
        {
            return Resources.Load<Sprite>("Hazards/" + name);
        }

        private static void Fit(Transform target, Sprite sprite, float worldSize)
        {
            float size = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
            target.localScale = Vector3.one * (worldSize / Mathf.Max(0.01f, size));
        }
    }

    /// <summary>Flyers bob gently out of step, so the flock looks alive but stays at the same height.</summary>
    public class FlyerFormation : MonoBehaviour
    {
        private const float Bob = 0.07f;
        private readonly System.Collections.Generic.List<Transform> _flyers = new System.Collections.Generic.List<Transform>();
        private readonly System.Collections.Generic.List<float> _homes = new System.Collections.Generic.List<float>();

        public void Add(Transform flyer)
        {
            _flyers.Add(flyer);
            _homes.Add(flyer.localPosition.y);
        }

        private void Update()
        {
            for (int i = 0; i < _flyers.Count; i++)
            {
                Vector3 p = _flyers[i].localPosition;
                p.y = _homes[i] + Mathf.Sin(Time.time * 4f + i * 1.7f) * Bob;
                _flyers[i].localPosition = p;
            }
        }
    }

    /// <summary>
    /// Waits until the runner is close, then rolls slowly towards them. Starting late
    /// keeps the meeting point near where the ball was placed, so spacing to other
    /// hazards still holds.
    /// </summary>
    public class RollingBall : MonoBehaviour
    {
        public const float StartDistance = 14f;
        public const float Speed = 2f;

        public Transform Picture;

        public bool IsRolling { get; private set; }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (!IsRolling)
            {
                PlayerController runner = PlayerController.Instance;
                IsRolling = runner != null && transform.position.x - runner.transform.position.x < StartDistance;
                return;
            }

            float step = Speed * Time.deltaTime;
            transform.position += Vector3.left * step;

            if (Picture != null)
            {
                // Roll without slipping: distance / radius, in degrees.
                Picture.Rotate(0f, 0f, step / (SpecialHazards.BallSize * 0.5f) * Mathf.Rad2Deg);
            }
        }
    }

    /// <summary>Throws the runner up once, with a squash of the mat so the bounce reads.</summary>
    public class Trampoline : MonoBehaviour
    {
        /// <summary>Rises about 3.7 above the ground: up to the coins and over anything after.</summary>
        public const float BounceVelocity = 17f;

        public Transform Mat;

        public bool Used { get; private set; }

        private float _squash;
        private Vector3 _matScale;

        private void Start()
        {
            if (Mat != null)
            {
                _matScale = Mat.localScale;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController runner = other.GetComponentInParent<PlayerController>();

            if (Used || runner == null)
            {
                return;
            }

            // A runner still rising from a jump flies past; one on the ground or coming down bounces.
            Rigidbody2D body = runner.GetComponent<Rigidbody2D>();

            if (body != null && body.linearVelocity.y > 1f)
            {
                return;
            }

            Used = true;
            _squash = 1f;
            runner.Bounce(BounceVelocity);
            Utilities.Haptics.Light();

            Effects.FxPlayer.Instance.Sparkle(transform.position + Vector3.up * 0.4f, 8);
        }

        private void Update()
        {
            if (Mat == null || _squash <= 0f)
            {
                return;
            }

            _squash = Mathf.Max(0f, _squash - Time.deltaTime * 4f);
            float wobble = Mathf.Sin((1f - _squash) * Mathf.PI * 3f) * _squash;
            Mat.localScale = new Vector3(_matScale.x * (1f + wobble * 0.08f), _matScale.y * (1f - wobble * 0.45f), 1f);
        }
    }

    /// <summary>Slows the runner while they wade through, with a splash on the way in.</summary>
    public class MudPuddle : MonoBehaviour
    {
        /// <summary>Share of normal speed kept in the mud: slow enough to notice, never a stop.</summary>
        public const float SlowFactor = 0.6f;

        public Color Color = Color.white;

        public bool Splashed { get; private set; }

        private void OnTriggerStay2D(Collider2D other)
        {
            PlayerController runner = other.GetComponentInParent<PlayerController>();

            // Only feet in the puddle slow down; jumping over it keeps full speed.
            if (runner == null || !runner.IsGrounded)
            {
                return;
            }

            runner.SlowDown(SlowFactor);

            if (!Splashed)
            {
                Splashed = true;
                Utilities.Haptics.Light();
                Splash(new Vector3(runner.transform.position.x, transform.position.y + 0.1f, 0f));
            }
        }

        private void Splash(Vector3 at)
        {
            for (int i = 0; i < 7; i++)
            {
                var drop = new GameObject("MudDrop");
                drop.transform.position = at;
                SpriteRenderer renderer = drop.AddComponent<SpriteRenderer>();
                renderer.sprite = Utilities.RuntimeSprites.Circle();
                renderer.color = Color;
                renderer.sortingOrder = 12;
                float size = Random.Range(0.1f, 0.2f);
                float largest = Mathf.Max(0.01f, Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y));
                drop.transform.localScale = Vector3.one * (size / largest);
                drop.AddComponent<MudDrop>().Velocity = new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(3f, 5.5f));
            }
        }
    }

    /// <summary>One splash drop: flies up, falls and fades.</summary>
    public class MudDrop : MonoBehaviour
    {
        private const float Life = 0.6f;

        public Vector2 Velocity;

        private float _age;
        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            _age += Time.deltaTime;

            if (_age >= Life)
            {
                Destroy(gameObject);
                return;
            }

            Velocity += Vector2.down * 18f * Time.deltaTime;
            transform.position += (Vector3)(Velocity * Time.deltaTime);
            Color c = _renderer.color;
            c.a = Mathf.Clamp01((Life - _age) / 0.3f) * 0.9f;
            _renderer.color = c;
        }
    }
}
