using System.Collections.Generic;
using UnityEngine;

namespace FootballRunner.Player
{
    /// <summary>
    /// Draws the worn cap, glasses and scarf on the runner. After the animator and the
    /// kid's skin have picked the frame, each item is placed on that frame's head
    /// (Outfits.TryGetHead), so it follows the run, the jump and the fall.
    /// </summary>
    [DefaultExecutionOrder(1010)]
    public class OutfitRenderer : MonoBehaviour
    {
        private SpriteRenderer _runner;
        private readonly Dictionary<Outfits.Slot, SpriteRenderer> _parts = new Dictionary<Outfits.Slot, SpriteRenderer>();

        public static OutfitRenderer EnsureOn(GameObject host)
        {
            OutfitRenderer renderer = host.GetComponent<OutfitRenderer>();
            return renderer != null ? renderer : host.AddComponent<OutfitRenderer>();
        }

        /// <summary>The drawn item in a place, or null when nothing is worn there; for tests.</summary>
        public SpriteRenderer Part(Outfits.Slot slot)
        {
            return _parts.TryGetValue(slot, out SpriteRenderer part) && part != null && part.enabled ? part : null;
        }

        private void Start()
        {
            var driver = FindFirstObjectByType<RukajjaAnimDriver>();
            Animator animator = driver != null ? driver.GetComponentInChildren<Animator>(true) : null;
            _runner = animator != null ? animator.GetComponent<SpriteRenderer>() : null;

            if (_runner == null)
            {
                enabled = false;
                return;
            }

            foreach (Outfits.Slot slot in System.Enum.GetValues(typeof(Outfits.Slot)))
            {
                var go = new GameObject("Outfit_" + slot);
                go.transform.SetParent(_runner.transform, false);
                var part = go.AddComponent<SpriteRenderer>();
                part.sortingLayerID = _runner.sortingLayerID;
                part.sortingOrder = _runner.sortingOrder + (slot == Outfits.Slot.Eyes ? 3 : slot == Outfits.Slot.Head ? 2 : 1);
                part.sharedMaterial = _runner.sharedMaterial;
                _parts[slot] = part;
            }

            LateUpdate();
        }

        private void LateUpdate()
        {
            Sprite frame = _runner.sprite;
            string key = frame != null ? FrameKey(frame.name) : null;
            bool known = key != null && Outfits.TryGetHead(key, out _);

            foreach (KeyValuePair<Outfits.Slot, SpriteRenderer> pair in _parts)
            {
                Outfits.Item item = Outfits.Worn(pair.Key);
                SpriteRenderer part = pair.Value;

                if (item == null || !known || !_runner.enabled)
                {
                    part.enabled = false;
                    continue;
                }

                Outfits.TryGetHead(key, out Outfits.Head head);
                Outfits.Place(pair.Key, head, out Vector2 centre, out float size);

                Sprite picture = Outfits.Picture(item.Id);
                part.sprite = picture;
                part.enabled = picture != null;

                if (picture == null)
                {
                    continue;
                }

                // Texture pixels to the runner's local space, through the frame's pivot and density.
                float ppu = frame.pixelsPerUnit;
                Vector2 pivot = frame.pivot;
                float x = (centre.x - pivot.x) / ppu;
                part.flipX = _runner.flipX;
                part.transform.localPosition = new Vector3(_runner.flipX ? -x : x, (centre.y - pivot.y) / ppu, 0f);
                float scale = size / ppu / picture.bounds.size.x;
                part.transform.localScale = new Vector3(scale, scale, 1f);
                part.color = _runner.color;
            }
        }

        /// <summary>"Rukajja_Run_03" is Rukajja's own frame; other kids' frames are named "Run_03".</summary>
        private static string FrameKey(string spriteName)
        {
            const string rukajja = "Rukajja_";

            if (spriteName.StartsWith(rukajja))
            {
                return "rukajja/" + spriteName.Substring(rukajja.Length);
            }

            return CharacterRoster.SelectedId + "/" + spriteName;
        }
    }
}
