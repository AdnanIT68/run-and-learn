using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FootballRunner.UI
{
    /// <summary>
    /// Keeps pointer UI alive in every scene. Does not disable EventSystem,
    /// InputSystemUIInputModule, GraphicRaycaster, or Canvas.
    /// </summary>
    public static class UiPointerRepair
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Repair();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Repair();
        }

        public static void Repair()
        {
            EventSystem keep = EnsureSingleEventSystem();
            if (keep == null)
            {
                return;
            }

            keep.enabled = true;

            var module = keep.GetComponent<InputSystemUIInputModule>();
            if (module == null)
            {
                module = keep.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            module.enabled = true;

            if (module.actionsAsset == null ||
                module.point == null || module.point.action == null ||
                module.leftClick == null || module.leftClick.action == null)
            {
                module.AssignDefaultActions();
            }

            InputActionAsset asset = module.actionsAsset != null
                ? module.actionsAsset
                : InputSystem.actions;
            asset?.FindActionMap("UI")?.Enable();

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null)
                {
                    continue;
                }

                canvas.enabled = true;
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                {
                    raycaster.enabled = true;
                }

                DisableDecorativeRaycasts(canvas.transform);
            }
        }

        public static void DisableDecorativeRaycasts(Transform canvas)
        {
            if (canvas == null)
            {
                return;
            }

            DisableImageRaycast(canvas.Find("Scrim"));
        }

        public static void RaiseOverlay(Transform overlay)
        {
            overlay?.SetAsLastSibling();
        }

        private static void DisableImageRaycast(Transform target)
        {
            if (target == null)
            {
                return;
            }

            var image = target.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
            }
        }

        private static EventSystem EnsureSingleEventSystem()
        {
            EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            EventSystem keep = EventSystem.current;

            if (keep == null)
            {
                for (int i = 0; i < systems.Length; i++)
                {
                    if (systems[i] != null)
                    {
                        keep = systems[i];
                        break;
                    }
                }
            }

            if (keep == null)
            {
                return null;
            }

            keep.enabled = true;
            keep.gameObject.SetActive(true);

            for (int i = 0; i < systems.Length; i++)
            {
                EventSystem extra = systems[i];
                if (extra == null || extra == keep)
                {
                    continue;
                }

                extra.enabled = false;
            }

            EventSystem.current = keep;
            return keep;
        }
    }
}
