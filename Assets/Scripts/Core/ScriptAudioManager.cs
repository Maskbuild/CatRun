using UnityEngine;

public class ScriptAudioManager : MonoBehaviour
{
    public static ScriptAudioManager Instance { get; private set; }

    [Header("BGM Clips")]
    public AudioClip menuBGM;
    public AudioClip gameplayBGM;

    [Header("SFX Clips")]
    public AudioClip jumpSFX;
    public AudioClip runningSFX;
    public AudioClip hitSFX;
    public AudioClip catchSFX;
    public AudioClip coinSFX;
    public AudioClip powerUpSFX;
    public AudioClip buttonClickSFX;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioSource runningSource;

    public const string MasterVolKey = "Settings_MasterVol";
    public const string MusicVolKey = "Settings_MusicVol";
    public const string SfxVolKey = "Settings_SfxVol";

    public float MasterVolume { get; private set; } = 1.0f;
    public float MusicVolume { get; private set; } = 0.8f;
    public float SfxVolume { get; private set; } = 0.8f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // ลบเฉพาะคอมโพเนนต์นี้ ไม่ลบทั้ง GameObject (กันลบ GameManager/สคริปต์อื่นที่อยู่ร่วมกัน)
            Destroy(this);
            return;
        }

        // ถ้าอยู่ร่วม GameObject กับสคริปต์อื่น (เช่น GameManager) ให้ย้ายตัวเองไป GameObject ใหม่
        // เพื่อไม่ให้ DontDestroyOnLoad พาสคริปต์อื่นข้ามฉากไปด้วย
        if (SharesGameObjectWithOtherScripts() || transform.parent != null)
        {
            RelocateToOwnGameObject();
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupAudioSources();

        LoadVolumeSettings();
    }

    private bool SharesGameObjectWithOtherScripts()
    {
        foreach (MonoBehaviour mb in GetComponents<MonoBehaviour>())
        {
            if (mb != null && mb != this) return true;
        }
        return false;
    }

    private void RelocateToOwnGameObject()
    {
        GameObject go = new GameObject("AudioManager (Persistent)");
        go.SetActive(false); // กัน Awake ทำงานก่อนคัดลอกค่าเสร็จ

        ScriptAudioManager copy = go.AddComponent<ScriptAudioManager>();
        copy.menuBGM = menuBGM;
        copy.gameplayBGM = gameplayBGM;
        copy.jumpSFX = jumpSFX;
        copy.runningSFX = runningSFX;
        copy.hitSFX = hitSFX;
        copy.catchSFX = catchSFX;
        copy.coinSFX = coinSFX;
        copy.powerUpSFX = powerUpSFX;
        copy.buttonClickSFX = buttonClickSFX;
        // AudioSource เดิมอยู่บน GameObject ที่จะถูกลบตอนเปลี่ยนฉาก จึงให้สร้างใหม่บนตัวใหม่
        copy.bgmSource = null;
        copy.sfxSource = null;
        copy.runningSource = null;

        go.SetActive(true); // Awake ของตัวใหม่จะตั้ง Instance + DontDestroyOnLoad
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void SetupAudioSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        if (runningSource == null)
        {
            runningSource = gameObject.AddComponent<AudioSource>();
            runningSource.loop = true;
            runningSource.playOnAwake = false;
        }
    }

    private void LoadVolumeSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat(MasterVolKey, 1.0f);
        MusicVolume = PlayerPrefs.GetFloat(MusicVolKey, 0.8f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolKey, 0.8f);
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = MasterVolume * MusicVolume;
        }

        if (runningSource != null)
        {
            runningSource.volume = MasterVolume * SfxVolume;
        }
    }

    public void SetMasterVolume(float volume) => SetVolume(MasterVolKey, volume, v => MasterVolume = v);
    public void SetMusicVolume(float volume) => SetVolume(MusicVolKey, volume, v => MusicVolume = v);
    public void SetSfxVolume(float volume) => SetVolume(SfxVolKey, volume, v => SfxVolume = v);

    private void SetVolume(string key, float volume, System.Action<float> assign)
    {
        assign(Mathf.Clamp01(volume));
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(volume));
        ApplyVolumes();
    }

    public void PlayMenuBGM() => PlayBGM(menuBGM);
    public void PlayGameplayBGM() => PlayBGM(gameplayBGM);

    private void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null || (bgmSource.clip == clip && bgmSource.isPlaying)) return;
        bgmSource.clip = clip;
        bgmSource.volume = MasterVolume * MusicVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    public void PlayJump() => PlaySFX(jumpSFX);
    public void PlayHit() => PlaySFX(hitSFX);
    public void PlayCatch() => PlaySFX(catchSFX);
    public void PlayCoin() => PlaySFX(coinSFX);
    public void PlayPowerUp() => PlaySFX(powerUpSFX);
    public void PlayButtonClick() => PlaySFX(buttonClickSFX);

    public void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
    {
        if (clip == null || sfxSource == null) return;
        float finalVolume = MasterVolume * SfxVolume * volumeScale;
        sfxSource.PlayOneShot(clip, finalVolume);
    }

    public void SetRunningSound(bool isRunning)
    {
        if (runningSource == null || runningSFX == null) return;

        if (runningSource.clip != runningSFX)
        {
            runningSource.clip = runningSFX;
        }

        if (isRunning)
        {
            runningSource.volume = MasterVolume * SfxVolume;
            if (!runningSource.isPlaying)
            {
                runningSource.Play();
            }
        }
        else
        {
            if (runningSource.isPlaying)
            {
                runningSource.Stop();
            }
        }
    }
}