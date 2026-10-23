using FootballRunner.Audio;
using FootballRunner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.UI
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        private void OnEnable()
        {
            GameManager.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GameManager.StateChanged -= OnStateChanged;
        }

        private void Start()
        {
            resumeButton?.onClick.AddListener(OnResume);
            restartButton?.onClick.AddListener(OnRestart);
            mainMenuButton?.onClick.AddListener(OnMainMenu);

            SetVisible(false);
        }

        private void OnStateChanged(GameState state)
        {
            SetVisible(state == GameState.Paused);
        }

        private void SetVisible(bool visible)
        {
            if (panel != null)
            {
                panel.SetActive(visible);

                if (visible)
                {
                    panel.transform.SetAsLastSibling();
                }
            }
        }

        private void OnResume()
        {
            AudioManager.Instance?.PlayButton();
            UiFeedback.Punch(this, resumeButton);
            GameManager.Instance?.Resume();
        }

        private void OnRestart()
        {
            AudioManager.Instance?.PlayButton();
            GameManager.Instance?.RestartRun();
        }

        private void OnMainMenu()
        {
            AudioManager.Instance?.PlayButton();
            GameManager.Instance?.GoToMainMenu();
        }
    }
}
