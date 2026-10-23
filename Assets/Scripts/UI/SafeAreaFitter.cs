using UnityEngine;

namespace FootballRunner.UI
{
    /// <summary>
    /// Shrinks a full-screen RectTransform to the device's safe area, keeping its
    /// children clear of camera cutouts and rounded corners. In landscape the
    /// cutout sits on a long edge, which is exactly where the score and the pause
    /// button live, so without this they can end up partly unreachable.
    /// Attach to a stretched child of the Canvas and parent the HUD to it.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Tooltip("Re-check every frame. Only needed if the device can rotate mid-run.")]
        [SerializeField] private bool trackChanges = true;

        private RectTransform _rect;
        private Rect _appliedSafeArea;
        private int _appliedWidth;
        private int _appliedHeight;

        /// <summary>
        /// Moves interactive HUD children into a safe-area wrapper. Full-screen
        /// overlays named in <paramref name="keepOnRoot"/> stay on the canvas.
        /// Decorative root images such as Scrim must not sit above buttons or
        /// intercept pointer events.
        /// </summary>
        public static void EnsureOnCanvas(Transform canvas, params string[] keepOnRoot)
        {
            if (canvas == null)
            {
                return;
            }

            Transform safe = canvas.Find("SafeArea");
            if (safe == null)
            {
                var go = new GameObject("SafeArea", typeof(RectTransform));
                go.transform.SetParent(canvas, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                go.AddComponent<SafeAreaFitter>();
                safe = go.transform;

                for (int i = canvas.childCount - 1; i >= 0; i--)
                {
                    Transform child = canvas.GetChild(i);

                    if (child == safe)
                    {
                        continue;
                    }

                    bool keep = false;

                    if (keepOnRoot != null)
                    {
                        for (int k = 0; k < keepOnRoot.Length; k++)
                        {
                            if (child.name == keepOnRoot[k])
                            {
                                keep = true;
                                break;
                            }
                        }
                    }

                    if (!keep)
                    {
                        child.SetParent(safe, true);
                    }
                }
            }

            // Buttons live in SafeArea. Keep that above decorative root overlays
            // such as Scrim. Active modals raise themselves when shown.
            safe.SetAsLastSibling();
            UiPointerRepair.DisableDecorativeRaycasts(canvas);
            UiPointerRepair.Repair();
        }

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (trackChanges)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (_rect == null)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            int width = Screen.width;
            int height = Screen.height;

            if (width <= 0 || height <= 0)
            {
                return;
            }

            if (safeArea == _appliedSafeArea && width == _appliedWidth && height == _appliedHeight)
            {
                return;
            }

            _appliedSafeArea = safeArea;
            _appliedWidth = width;
            _appliedHeight = height;

            // Express the safe area as normalised anchors so the result is
            // resolution independent and survives any canvas scaling.
            var anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
            var anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);

            _rect.anchorMin = anchorMin;
            _rect.anchorMax = anchorMax;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
