using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScriptSettingsManager : MonoBehaviour
{
    public static ScriptSettingsManager Instance { get; private set; }

    [Header("Settings Panel UI")]
    public GameObject settingsPanel;

    [Header("Audio Sliders")]
    public Slider masterSlider;
    public TMP_Text masterValueText;
    public Slider musicSlider;
    public TMP_Text musicValueText;
    public Slider sfxSlider;
    public TMP_Text sfxValueText;

    [Header("Screen & Display")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown screenModeDropdown;
    public Toggle fullscreenToggle;

    public enum DisplayMode
    {
        Fullscreen = 0,
        Borderless = 1,
        Windowed = 2
    }

    private readonly List<ResolutionOption> resolutionOptions = new List<ResolutionOption>()
    {
        new ResolutionOption(1920, 1080, "1920 x 1080"),
        new ResolutionOption(1600, 900,  "1600 x 900"),
        new ResolutionOption(1366, 768,  "1366 x 768"),
        new ResolutionOption(1280, 720,  "1280 x 720")
    };

    private readonly List<string> screenModeOptions = new List<string>()
    {
        "Fullscreen",
        "Borderless",
        "Windowed"
    };

    private struct ResolutionOption
    {
        public int width;
        public int height;
        public string label;

        public ResolutionOption(int w, int h, string lbl)
        {
            width = w;
            height = h;
            label = lbl;
        }
    }

    public const string ResolutionIndexKey = "Settings_ResolutionIndex";
    public const string FullscreenKey = "Settings_Fullscreen";
    public const string ScreenModeKey = "Settings_ScreenMode";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        InitializeAudioSliders();
        InitializeResolutionDropdown();
        EnsureScreenModeDropdownExists();
        InitializeScreenModeDropdown();
        InitializeFullscreenToggle();

        ApplySavedResolution();
    }

    private void InitializeAudioSliders()
    {
        SetupSlider(masterSlider, masterValueText, ScriptAudioManager.MasterVolKey, 1.0f, OnMasterVolumeChanged);
        SetupSlider(musicSlider, musicValueText, ScriptAudioManager.MusicVolKey, 0.8f, OnMusicVolumeChanged);
        SetupSlider(sfxSlider, sfxValueText, ScriptAudioManager.SfxVolKey, 0.8f, OnSfxVolumeChanged);
    }

    private void SetupSlider(Slider slider, TMP_Text label, string key, float defaultVal, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null) return;
        float val = PlayerPrefs.GetFloat(key, defaultVal);
        slider.value = val;
        UpdateValueLabel(label, val);

        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(callback);
    }

    private void UpdateValueLabel(TMP_Text label, float value)
    {
        if (label != null)
        {
            label.text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f).ToString();
        }
    }

    public void OnMasterVolumeChanged(float value)
    {
        UpdateValueLabel(masterValueText, value);
        if (ScriptAudioManager.Instance != null)
        {
            ScriptAudioManager.Instance.SetMasterVolume(value);
        }
    }

    public void OnMusicVolumeChanged(float value)
    {
        UpdateValueLabel(musicValueText, value);
        if (ScriptAudioManager.Instance != null)
        {
            ScriptAudioManager.Instance.SetMusicVolume(value);
        }
    }

    public void OnSfxVolumeChanged(float value)
    {
        UpdateValueLabel(sfxValueText, value);
        if (ScriptAudioManager.Instance != null)
        {
            ScriptAudioManager.Instance.SetSfxVolume(value);
        }
    }

    private void InitializeResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        for (int i = 0; i < resolutionOptions.Count; i++)
        {
            options.Add(resolutionOptions[i].label);
        }
        resolutionDropdown.AddOptions(options);

        int savedIndex = PlayerPrefs.GetInt(ResolutionIndexKey, 0);
        savedIndex = Mathf.Clamp(savedIndex, 0, resolutionOptions.Count - 1);

        resolutionDropdown.value = savedIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void EnsureScreenModeDropdownExists()
    {
        if (screenModeDropdown != null) return;

        if (resolutionDropdown != null)
        {
            GameObject clonedObj = Instantiate(resolutionDropdown.gameObject, resolutionDropdown.transform.parent);
            clonedObj.name = "screenModeDropdown";

            RectTransform rect = clonedObj.GetComponent<RectTransform>();
            RectTransform origRect = resolutionDropdown.GetComponent<RectTransform>();
            if (rect != null && origRect != null)
            {
                rect.anchoredPosition = new Vector2(origRect.anchoredPosition.x, origRect.anchoredPosition.y - 65f);
            }

            screenModeDropdown = clonedObj.GetComponent<TMP_Dropdown>();
        }
    }

    private void InitializeScreenModeDropdown()
    {
        if (screenModeDropdown == null) return;

        screenModeDropdown.ClearOptions();
        screenModeDropdown.AddOptions(screenModeOptions);

        int savedMode = PlayerPrefs.GetInt(ScreenModeKey, 1);
        savedMode = Mathf.Clamp(savedMode, 0, screenModeOptions.Count - 1);

        screenModeDropdown.value = savedMode;
        screenModeDropdown.RefreshShownValue();

        screenModeDropdown.onValueChanged.RemoveAllListeners();
        screenModeDropdown.onValueChanged.AddListener(OnScreenModeChanged);
    }

    private void InitializeFullscreenToggle()
    {
        if (fullscreenToggle == null && settingsPanel != null)
        {
            fullscreenToggle = settingsPanel.GetComponentInChildren<Toggle>(true);
        }

        if (fullscreenToggle == null) return;

        int savedMode = PlayerPrefs.GetInt(ScreenModeKey, 1);
        bool isFullscreen = savedMode != (int)DisplayMode.Windowed;

        fullscreenToggle.isOn = isFullscreen;
        fullscreenToggle.onValueChanged.RemoveAllListeners();
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
    }

    public void OnResolutionChanged(int index)
    {
        if (index < 0 || index >= resolutionOptions.Count) return;

        PlayerPrefs.SetInt(ResolutionIndexKey, index);
        PlayerPrefs.Save();

        int savedMode = PlayerPrefs.GetInt(ScreenModeKey, 1);
        ApplyResolution(resolutionOptions[index].width, resolutionOptions[index].height, (DisplayMode)savedMode);

        if (ScriptAudioManager.Instance != null)
            ScriptAudioManager.Instance.PlayButtonClick();
    }

    public void OnScreenModeChanged(int modeIndex)
    {
        modeIndex = Mathf.Clamp(modeIndex, 0, 2);
        PlayerPrefs.SetInt(ScreenModeKey, modeIndex);
        PlayerPrefs.SetInt(FullscreenKey, modeIndex != (int)DisplayMode.Windowed ? 1 : 0);
        PlayerPrefs.Save();

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(modeIndex != (int)DisplayMode.Windowed);
        }

        int savedIndex = PlayerPrefs.GetInt(ResolutionIndexKey, 0);
        savedIndex = Mathf.Clamp(savedIndex, 0, resolutionOptions.Count - 1);

        ApplyResolution(resolutionOptions[savedIndex].width, resolutionOptions[savedIndex].height, (DisplayMode)modeIndex);

        if (ScriptAudioManager.Instance != null)
            ScriptAudioManager.Instance.PlayButtonClick();
    }

    public void OnFullscreenToggleChanged(bool isFullscreen)
    {
        int targetMode;
        if (isFullscreen)
        {
            int prevMode = PlayerPrefs.GetInt(ScreenModeKey, 1);
            targetMode = (prevMode == (int)DisplayMode.Windowed) ? (int)DisplayMode.Borderless : prevMode;
        }
        else
        {
            targetMode = (int)DisplayMode.Windowed;
        }

        PlayerPrefs.SetInt(ScreenModeKey, targetMode);
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();

        if (screenModeDropdown != null)
        {
            screenModeDropdown.SetValueWithoutNotify(targetMode);
            screenModeDropdown.RefreshShownValue();
        }

        int savedIndex = PlayerPrefs.GetInt(ResolutionIndexKey, 0);
        savedIndex = Mathf.Clamp(savedIndex, 0, resolutionOptions.Count - 1);

        ApplyResolution(resolutionOptions[savedIndex].width, resolutionOptions[savedIndex].height, (DisplayMode)targetMode);

        if (ScriptAudioManager.Instance != null)
            ScriptAudioManager.Instance.PlayButtonClick();
    }

    private void ApplySavedResolution()
    {
        int savedIndex = PlayerPrefs.GetInt(ResolutionIndexKey, 0);
        savedIndex = Mathf.Clamp(savedIndex, 0, resolutionOptions.Count - 1);

        int savedMode = PlayerPrefs.GetInt(ScreenModeKey, 1);
        savedMode = Mathf.Clamp(savedMode, 0, 2);

        ApplyResolution(resolutionOptions[savedIndex].width, resolutionOptions[savedIndex].height, (DisplayMode)savedMode);
    }

    private void ApplyResolution(int width, int height, DisplayMode mode)
    {
        FullScreenMode unityMode;
        switch (mode)
        {
            case DisplayMode.Fullscreen:
                unityMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case DisplayMode.Borderless:
                unityMode = FullScreenMode.FullScreenWindow;
                break;
            case DisplayMode.Windowed:
            default:
                unityMode = FullScreenMode.Windowed;
                break;
        }

        Screen.SetResolution(width, height, unityMode);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }

        if (ScriptAudioManager.Instance != null)
            ScriptAudioManager.Instance.PlayButtonClick();
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (ScriptAudioManager.Instance != null)
            ScriptAudioManager.Instance.PlayButtonClick();
    }
}
