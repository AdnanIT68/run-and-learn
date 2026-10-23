using FootballRunner.SaveSystem;
using UnityEngine;

namespace FootballRunner.Audio
{
    /// <summary>
    /// Survives scene loads so sound settings stay consistent between the menu
    /// and the game. Background music is disabled. SFX clip fields may be left
    /// empty: every call is null-safe.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Music")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip gameMusic;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip collectClip;
        [SerializeField] private AudioClip coinClip;
        [SerializeField] private AudioClip hurtClip;
        [SerializeField] private AudioClip powerUpClip;
        [SerializeField] private AudioClip buttonClip;
        [SerializeField] private AudioClip gameOverClip;

        public bool MusicEnabled => SaveManager.Data.musicEnabled;

        public bool SoundEnabled => SaveManager.Data.soundEnabled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            StopMusic();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetMusicEnabled(bool enabled)
        {
            SaveManager.SetMusicEnabled(enabled);
            StopMusic();
        }

        public void SetSoundEnabled(bool enabled)
        {
            SaveManager.SetSoundEnabled(enabled);
        }

        public void PlayMenuMusic()
        {
            PlayMusic(menuMusic);
        }

        public void PlayGameMusic()
        {
            PlayMusic(gameMusic);
        }

        public void PlayJump() => PlaySfx(jumpClip);

        public void PlayCollect() => PlaySfx(collectClip);

        public void PlayCoin() => PlaySfx(coinClip);

        public void PlayHurt() => PlaySfx(hurtClip);

        public void PlayPowerUp() => PlaySfx(powerUpClip);

        public void PlayButton() => PlaySfx(buttonClip);

        public void PlayGameOver() => PlaySfx(gameOverClip);

        private void StopMusic()
        {
            if (musicSource == null)
            {
                return;
            }

            musicSource.Stop();
            musicSource.loop = false;
            musicSource.clip = null;
        }

        private void PlayMusic(AudioClip clip)
        {
            // Background music is disabled for this product. Clip fields stay
            // serialized so a later build can restore playback without new wiring.
            if (clip != null && musicSource != null)
            {
                musicSource.clip = null;
            }

            StopMusic();
        }

        private void PlaySfx(AudioClip clip)
        {
            if (sfxSource == null || clip == null || !SoundEnabled)
            {
                return;
            }

            sfxSource.PlayOneShot(clip);
        }
    }
}
