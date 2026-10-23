using FootballRunner.Player;
using UnityEngine;

namespace FootballRunner.Adventure
{
    /// <summary>
    /// Marks a pickup as one of the three lost stars for this level.
    /// </summary>
    public class AdventureStarMarker : MonoBehaviour
    {
        private bool _counted;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_counted)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            _counted = true;
            AdventureCourse.Instance?.NotifyStarCollected();
        }
    }
}
