using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.UI
{
    /// <summary>
    /// Tiny press-scale for buttons. One shared coroutine pattern so menus do not
    /// each invent their own punch.
    /// </summary>
    public static class UiFeedback
    {
        public static void Punch(MonoBehaviour host, Selectable button)
        {
            if (host == null || button == null || !host.isActiveAndEnabled)
            {
                return;
            }

            host.StartCoroutine(PunchRoutine(button.transform));
        }

        private static IEnumerator PunchRoutine(Transform target)
        {
            Vector3 baseScale = Vector3.one;
            const float duration = 0.12f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float scale = 1f + (1f - t) * 0.08f;
                target.localScale = baseScale * scale;
                yield return null;
            }

            target.localScale = baseScale;
        }
    }
}
