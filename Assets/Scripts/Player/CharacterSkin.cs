using System.Collections.Generic;
using UnityEngine;

namespace FootballRunner.Player
{
    /// <summary>
    /// Dresses the runner as the chosen kid. Rukajja's animator keeps running as it
    /// is; after it picks a frame such as "Rukajja_Run_03", this swaps in the same
    /// frame of the chosen kid. Movement, colliders and timing stay Rukajja's.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class CharacterSkin : MonoBehaviour
    {
        private const string SourcePrefix = "Rukajja_";

        private readonly Dictionary<Sprite, Sprite> _swaps = new Dictionary<Sprite, Sprite>();
        private SpriteRenderer _renderer;
        private string _id;

        public string CharacterId => _id;

        public static CharacterSkin EnsureOn(GameObject host)
        {
            CharacterSkin skin = host.GetComponent<CharacterSkin>();
            return skin != null ? skin : host.AddComponent<CharacterSkin>();
        }

        private void Start()
        {
            _id = CharacterRoster.SelectedId;

            if (_id == CharacterRoster.DefaultId)
            {
                enabled = false;
                return;
            }

            var driver = FindFirstObjectByType<RukajjaAnimDriver>();
            Animator animator = driver != null ? driver.GetComponentInChildren<Animator>(true) : null;
            _renderer = animator != null ? animator.GetComponent<SpriteRenderer>() : null;

            if (_renderer == null)
            {
                Debug.LogWarning("[CharacterSkin] No runner sprite found; keeping Rukajja.");
                enabled = false;
                return;
            }

            Swap();
        }

        private void LateUpdate()
        {
            Swap();
        }

        private void Swap()
        {
            Sprite current = _renderer.sprite;

            if (current == null)
            {
                return;
            }

            if (!_swaps.TryGetValue(current, out Sprite replacement))
            {
                replacement = current.name.StartsWith(SourcePrefix)
                    ? CharacterRoster.Frame(_id, current.name.Substring(SourcePrefix.Length))
                    : null;
                _swaps[current] = replacement;
            }

            if (replacement != null)
            {
                _renderer.sprite = replacement;
            }
        }
    }
}
