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
    public Toggle fullscreenToggle;

    private readonly List<ResolutionOption> resolutionOptions = new List<ResolutionOption>()
    {
        new ResolutionOption(1920, 1080, "1920 x 1080 (Full HD)"),
        new ResolutionOption(1600, 900,  "1600 x 900 (HD+)"),
        new ResolutionOption(1366, 768,  "1366 x 768 (Laptop)"),
        new ResolutionOption(1280, 720,  "1280 x 720 (HD)")
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

    private void InitializeFullscreenToggle()
    {
        if (fullscreenToggle == null) return;

        bool isFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        fullscreenToggle.isOn = isFullscreen;

        fullscreenToggle.onValueChanged.RemoveAllListeners();
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
    }

    public void OnResolutionChanged(int index)
    {
        if (index < 0 || index >= resolutionOptions.Count) return;

        PlayerPrefs.SetInt(ResolutionIndexKey, index);
        PlayerPrefs.Save();

        ApplyResolution(resolutionOptions[index].width, resolutionOptions[index].height, Screen.fullScreen);

        if (ScriptAudioManager.Instance != null)
            ScriptAudioManager.Instance.PlayButtonClick();
    }

    public void OnFullscreenToggleChanged(bool isFullscreen)
    {
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();

        int savedIndex = PlayerPrefs.GetInt(ResolutionIndexKey, 0);
        savedIndex = Mathf.Clamp(savedIndex, 0, resolutionOptions.Count - 1);

        ApplyResolution(resolutionOptions[savedIndex].width, resolutionOptions[savedIndex].height, isFullscreen);

        if (ScriptAudioManager.Instance != null)
            ScriptAudioManager.Instance.PlayButtonClick();
    }

    private void ApplySavedResolution()
    {
        int savedIndex = PlayerPrefs.GetInt(ResolutionIndexKey, 0);
        savedIndex = Mathf.Clamp(savedIndex, 0, resolutionOptions.Count - 1);
        bool isFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        ApplyResolution(resolutionOptions[savedIndex].width, resolutionOptions[savedIndex].height, isFullscreen);
    }

    private void ApplyResolution(int width, int height, bool isFullscreen)
    {
        Screen.SetResolution(width, height, isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
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