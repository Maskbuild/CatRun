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

    private float currentFadeMultiplier = 1.0f;
    private Coroutine bgmFadeCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

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

    void Start()
    {
        if (menuBGM != null && (bgmSource == null || !bgmSource.isPlaying))
        {
            PlayMenuBGM();
        }
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
        go.SetActive(false); 

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
        copy.bgmSource = null;
        copy.sfxSource = null;
        copy.runningSource = null;

        go.SetActive(true); 
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
            bgmSource.spatialBlend = 0f;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
        }

        if (runningSource == null)
        {
            runningSource = gameObject.AddComponent<AudioSource>();
            runningSource.loop = true;
            runningSource.playOnAwake = false;
            runningSource.spatialBlend = 0f;
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
            bgmSource.volume = MasterVolume * MusicVolume * currentFadeMultiplier;
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

    public void PlayMenuBGM(bool smooth = true, float duration = 1.0f)
    {
        if (smooth) CrossfadeBGM(menuBGM, duration);
        else PlayBGMImmediate(menuBGM);
    }

    public void PlayGameplayBGM(bool smooth = true, float duration = 1.0f)
    {
        if (smooth) CrossfadeBGM(gameplayBGM, duration);
        else PlayBGMImmediate(gameplayBGM);
    }

    public void PlayBGMImmediate(AudioClip clip)
    {
        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        if (clip == null || bgmSource == null) return;

        bgmSource.clip = clip;
        currentFadeMultiplier = 1.0f;
        bgmSource.volume = MasterVolume * MusicVolume;
        bgmSource.Play();
    }

    public void FadeOutBGM(float duration = 1.0f, System.Action onComplete = null)
    {
        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(RoutineFadeOutBGM(duration, onComplete));
    }

    public void FadeInBGM(AudioClip clip, float duration = 1.0f)
    {
        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(RoutineFadeInBGM(clip, duration));
    }

    public void CrossfadeBGM(AudioClip newClip, float duration = 1.0f)
    {
        if (newClip == null)
        {
            FadeOutBGM(duration);
            return;
        }

        if (bgmSource == null) return;

        if (bgmSource.clip == newClip && bgmSource.isPlaying)
        {
            if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = StartCoroutine(RoutineFadeInBGM(newClip, duration));
            return;
        }

        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(RoutineCrossfadeBGM(newClip, duration));
    }

    private System.Collections.IEnumerator RoutineCrossfadeBGM(AudioClip newClip, float duration)
    {
        if (bgmSource == null) yield break;

        if (bgmSource.isPlaying && bgmSource.clip != null)
        {
            float halfDuration = Mathf.Max(0.05f, duration * 0.45f);
            float startMultiplier = currentFadeMultiplier;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                currentFadeMultiplier = Mathf.Lerp(startMultiplier, 0f, elapsed / halfDuration);
                bgmSource.volume = MasterVolume * MusicVolume * currentFadeMultiplier;
                yield return null;
            }

            currentFadeMultiplier = 0f;
            bgmSource.volume = 0f;
            bgmSource.Stop();
        }

        if (newClip == null)
        {
            bgmFadeCoroutine = null;
            yield break;
        }

        bgmSource.clip = newClip;
        currentFadeMultiplier = 0f;
        bgmSource.volume = 0f;
        bgmSource.Play();

        float fadeInDuration = Mathf.Max(0.05f, duration * 0.55f);
        float inElapsed = 0f;
        while (inElapsed < fadeInDuration)
        {
            inElapsed += Time.unscaledDeltaTime;
            currentFadeMultiplier = Mathf.Lerp(0f, 1f, inElapsed / fadeInDuration);
            bgmSource.volume = MasterVolume * MusicVolume * currentFadeMultiplier;
            yield return null;
        }

        currentFadeMultiplier = 1f;
        bgmSource.volume = MasterVolume * MusicVolume;
        bgmFadeCoroutine = null;
    }

    private System.Collections.IEnumerator RoutineFadeOutBGM(float duration, System.Action onComplete = null)
    {
        if (bgmSource == null || !bgmSource.isPlaying)
        {
            onComplete?.Invoke();
            yield break;
        }

        float startMultiplier = currentFadeMultiplier;
        float elapsed = 0f;
        duration = Mathf.Max(0.05f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            currentFadeMultiplier = Mathf.Lerp(startMultiplier, 0f, elapsed / duration);
            bgmSource.volume = MasterVolume * MusicVolume * currentFadeMultiplier;
            yield return null;
        }

        currentFadeMultiplier = 0f;
        bgmSource.volume = 0f;
        bgmSource.Stop();
        bgmFadeCoroutine = null;
        onComplete?.Invoke();
    }

    private System.Collections.IEnumerator RoutineFadeInBGM(AudioClip clip, float duration)
    {
        if (bgmSource == null || clip == null) yield break;

        if (bgmSource.clip != clip || !bgmSource.isPlaying)
        {
            bgmSource.clip = clip;
            currentFadeMultiplier = 0f;
            bgmSource.volume = 0f;
            bgmSource.Play();
        }

        float startMultiplier = currentFadeMultiplier;
        float elapsed = 0f;
        duration = Mathf.Max(0.05f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            currentFadeMultiplier = Mathf.Lerp(startMultiplier, 1f, elapsed / duration);
            bgmSource.volume = MasterVolume * MusicVolume * currentFadeMultiplier;
            yield return null;
        }

        currentFadeMultiplier = 1f;
        bgmSource.volume = MasterVolume * MusicVolume;
        bgmFadeCoroutine = null;
    }

    public void StopBGM()
    {
        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
        currentFadeMultiplier = 1.0f;
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