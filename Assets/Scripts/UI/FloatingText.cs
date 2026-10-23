using FootballRunner.Utilities;
using TMPro;
using UnityEngine;

namespace FootballRunner.UI
{
    /// <summary>
    /// The little "+10" that rises and fades where a pickup was collected.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float lifetimeSeconds = 0.7f;
        [SerializeField] private float riseSpeed = 1.8f;

        private float _elapsed;
        private Color _baseColour;
        private PooledObject _pooled;

        private void Awake()
        {
            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>();
            }

            if (label != null)
            {
                _baseColour = label.color;
            }

            _pooled = GetComponent<PooledObject>();
        }

        public void Show(string text)
        {
            _elapsed = 0f;

            if (label != null)
            {
                label.text = text;
                label.color = _baseColour;
            }
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

            if (label != null)
            {
                Color colour = _baseColour;
                colour.a = Mathf.Lerp(1f, 0f, _elapsed / lifetimeSeconds);
                label.color = colour;
            }

            if (_elapsed < lifetimeSeconds)
            {
                return;
            }

            if (_pooled == null)
            {
                _pooled = GetComponent<PooledObject>();
            }

            if (_pooled != null)
            {
                _pooled.ReturnToPool();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
