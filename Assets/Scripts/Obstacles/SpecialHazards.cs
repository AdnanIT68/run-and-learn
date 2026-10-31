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
}
