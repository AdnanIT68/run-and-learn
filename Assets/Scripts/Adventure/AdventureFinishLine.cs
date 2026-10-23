using FootballRunner.Core;
using FootballRunner.Player;
using UnityEngine;

namespace FootballRunner.Adventure
{
    /// <summary>
    /// Ends Green Hills Level 1 when the runner reaches the finish flags.
    /// </summary>
    public class AdventureFinishLine : MonoBehaviour
    {
        private bool _triggered;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            _triggered = true;
            GameManager.Instance?.CompleteAdventureLevel();
        }
    }
}
