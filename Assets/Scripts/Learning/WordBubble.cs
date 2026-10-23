using System.Collections;
using FootballRunner.Player;
using FootballRunner.Utilities;
using UnityEngine;

namespace FootballRunner.Learning
{
    /// <summary>
    /// A floating bubble holding one word's picture. The runner jumps into the
    /// bubble that matches the spoken word. Built in code so no prefab is needed.
    /// </summary>
    public class WordBubble : MonoBehaviour
    {
        private const int SortingOrder = 30;


        private WordChallengeDirector _owner;
        private SpriteRenderer _back;
        private SpriteRenderer _picture;
        private CircleCollider2D _collider;
        private Vector3 _anchor;
        private Vector3 _baseScale;
        private float _phase;
        private bool _finished;
        private bool _highlighted;

        public WordEntry Word { get; private set; }

        public bool IsFinished => _finished;

        public static WordBubble Create(Transform parent, Vector3 position, WordEntry word, Sprite picture,
            float radius, Material material, WordChallengeDirector owner)
        {
            var go = new GameObject("WordBubble_" + word.id);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var bubble = go.AddComponent<WordBubble>();
            bubble.Word = word;
            bubble._owner = owner;
            bubble._anchor = position;
            bubble._phase = Random.Range(0f, Mathf.PI * 2f);

            bubble._back = go.AddComponent<SpriteRenderer>();
            bubble._back.sprite = RuntimeSprites.Circle();
            bubble._back.color = new Color(1f, 1f, 1f, 0.9f);
            bubble._back.sortingOrder = SortingOrder;
            go.transform.localScale = Vector3.one * radius * 2f;
            bubble._baseScale = go.transform.localScale;

            var pictureGo = new GameObject("Picture");
            pictureGo.transform.SetParent(go.transform, false);
            bubble._picture = pictureGo.AddComponent<SpriteRenderer>();
            bubble._picture.sprite = picture;
            bubble._picture.sortingOrder = SortingOrder + 1;

            if (picture != null)
            {
                // Fit the picture inside the bubble regardless of its pixels-per-unit.
                float largest = Mathf.Max(picture.bounds.size.x, picture.bounds.size.y);
                pictureGo.transform.localScale = Vector3.one * (0.72f / Mathf.Max(0.01f, largest));
            }

            if (material != null)
            {
                bubble._back.sharedMaterial = material;
                bubble._picture.sharedMaterial = material;
            }

            bubble._collider = go.AddComponent<CircleCollider2D>();
            bubble._collider.isTrigger = true;
            bubble._collider.radius = 0.5f;
            return bubble;
        }

        private void Update()
        {
            if (_finished)
            {
                return;
            }

            float bob = Mathf.Sin(Time.time * 2.2f + _phase) * 0.12f;
            transform.position = _anchor + Vector3.up * bob;

            if (_highlighted)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 9f) * 0.08f;
                transform.localScale = _baseScale * pulse;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_finished || other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            _owner?.OnBubbleTouched(this);
        }

        /// <summary>Draws attention to the right answer after a wrong one.</summary>
        public void Highlight()
        {
            if (_finished)
            {
                return;
            }

            _highlighted = true;
            _back.color = new Color(1f, 0.92f, 0.45f, 0.95f);
        }

        public void PopCorrect()
        {
            Finish();
            _back.color = new Color(0.55f, 0.95f, 0.55f, 0.95f);
            StartCoroutine(Animate(1.6f, 0.35f));
        }

        public void PopWrong()
        {
            Finish();
            _back.color = new Color(1f, 0.6f, 0.6f, 0.9f);
            StartCoroutine(Animate(0.4f, 0.3f));
        }

        public void FadeAway()
        {
            if (_finished)
            {
                return;
            }

            Finish();
            StartCoroutine(Animate(0.8f, 0.4f));
        }

        private void Finish()
        {
            _finished = true;
            _highlighted = false;

            if (_collider != null)
            {
                _collider.enabled = false;
            }
        }

        private IEnumerator Animate(float endScale, float seconds)
        {
            Vector3 start = transform.localScale;
            Vector3 end = _baseScale * endScale;
            Color back = _back.color;
            Color picture = _picture.color;
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                transform.localScale = Vector3.Lerp(start, end, t);
                back.a = Mathf.Lerp(0.95f, 0f, t);
                picture.a = Mathf.Lerp(1f, 0f, t);
                _back.color = back;
                _picture.color = picture;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
