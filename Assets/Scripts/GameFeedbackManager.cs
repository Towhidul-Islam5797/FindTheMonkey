using UnityEngine;

[DisallowMultipleComponent]
public class GameFeedbackManager : MonoBehaviour
{
    public static GameFeedbackManager Instance
    {
        get;
        private set;
    }

    // =======================================================
    // AUDIO SOURCES
    // =======================================================

    [Header("Audio Sources")]
    [Tooltip("AudioSource used only for background music.")]
    [SerializeField]
    private AudioSource musicSource;

    [Tooltip("AudioSource used for sound effects.")]
    [SerializeField]
    private AudioSource sfxSource;

    // =======================================================
    // AUDIO CLIPS
    // =======================================================

    [Header("Music")]
    [SerializeField]
    private AudioClip backgroundMusic;

    [Header("Sound Effects")]
    [SerializeField]
    private AudioClip buttonTapSound;

    [SerializeField]
    private AudioClip monkeyFoundSound;

    // =======================================================
    // SETTINGS
    // =======================================================

    [Header("Settings")]
    [SerializeField]
    private bool musicEnabled = true;

    [SerializeField]
    private bool soundEffectsEnabled = true;

    [SerializeField]
    private bool vibrationEnabled = true;

    [Range(0f, 1f)]
    [SerializeField]
    private float musicVolume = 0.5f;

    [Range(0f, 1f)]
    [SerializeField]
    private float soundEffectsVolume = 1f;

    // =======================================================
    // UNITY
    // =======================================================

    private void Awake()
    {
        /*
         * Only one manager can exist.
         *
         * When changing Jungle -> Beach -> Snow,
         * the original manager survives.
         */
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        ConfigureAudioSources();
    }

    private void Start()
    {
        PlayBackgroundMusic();
    }

    // =======================================================
    // AUDIO SETUP
    // =======================================================

    private void ConfigureAudioSources()
    {
        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = soundEffectsVolume;
        }
    }

    // =======================================================
    // BACKGROUND MUSIC
    // =======================================================

    public void PlayBackgroundMusic()
    {
        if (!musicEnabled)
        {
            return;
        }

        if (musicSource == null ||
            backgroundMusic == null)
        {
            return;
        }

        /*
         * Do not restart music after scene changes.
         */
        if (musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        musicSource.volume = musicVolume;

        musicSource.Play();
    }

    // =======================================================
    // UI TAP
    // =======================================================

    public void PlayButtonTap()
    {
        PlaySound(
            buttonTapSound
        );
    }

    // =======================================================
    // WRONG OBJECT
    // =======================================================

    public void WrongObjectFeedback()
    {
        /*
         * User clicked the wrong hiding object.
         */
        Vibrate();
    }

    // =======================================================
    // MONKEY FOUND
    // =======================================================

    public void MonkeyFoundFeedback()
    {
        PlaySound(
            monkeyFoundSound
        );

        Vibrate();
    }

    // =======================================================
    // GENERIC SOUND
    // =======================================================

    private void PlaySound(
        AudioClip clip
    )
    {
        if (!soundEffectsEnabled)
        {
            return;
        }

        if (clip == null ||
            sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(
            clip,
            soundEffectsVolume
        );
    }

    // =======================================================
    // VIBRATION
    // =======================================================

    private void Vibrate()
    {
        if (!vibrationEnabled)
        {
            return;
        }

        /*
         * No effect in Unity Editor/Desktop.
         * Works on supported mobile devices.
         */
        if (Application.isMobilePlatform)
        {
            Handheld.Vibrate();
        }
    }

    // =======================================================
    // OPTIONAL SETTINGS
    // =======================================================

    public void SetMusicEnabled(
        bool enabled
    )
    {
        musicEnabled = enabled;

        if (musicSource == null)
        {
            return;
        }

        if (musicEnabled)
        {
            PlayBackgroundMusic();
        }
        else
        {
            musicSource.Stop();
        }
    }

    public void SetSoundEffectsEnabled(
        bool enabled
    )
    {
        soundEffectsEnabled = enabled;
    }

    public void SetVibrationEnabled(
        bool enabled
    )
    {
        vibrationEnabled = enabled;
    }
}