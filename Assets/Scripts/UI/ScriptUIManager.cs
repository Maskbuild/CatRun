using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScriptUIManager : MonoBehaviour
{
    public static ScriptUIManager Instance { get; private set; }

    [Header("1. Gameplay HUD")]
    public GameObject hudPanel;
    public TMP_Text hudDistanceText;
    public TMP_Text hudCoinsText;

    [Header("1.1 Power-Up Timer UI")]
    public TMP_Text hudPowerUpStatusText;
    public TMP_Text hudInvincibleTimerText;
    public TMP_Text hudMagnetTimerText;

    [Header("2. Pause Menu UI")]
    public GameObject pausePanel;

    [Header("3. Resume Countdown UI")]
    public GameObject countdownPanel;
    public TMP_Text countdownText;
    public int resumeCountdownSeconds = 3;

    [Header("4. Game Over UI")]
    public GameObject gameOverPanel;
    public TMP_Text gameOverDistanceText;
    public TMP_Text gameOverHighScoreText;
    public TMP_Text gameOverCoinsText;
    public TMP_Text gameOverTotalCoinsText;

    [Header("5. Settings Panel")]
    public GameObject settingsPanel;

    [Header("6. Back To Menu Target")]
    public Object mainMenuScene;

    [Header("Text Formatting")]
    public string hudDistancePrefix = "Distance: ";
    public string hudDistanceSuffix = " m";
    public string hudCoinsPrefix = "Coins: ";
    public string invincibleTimerPrefix = "Invincible: ";
    public string magnetTimerPrefix = "Magnet: ";
    public string timerSuffix = "s";

    public string gameOverDistancePrefix = "Distance: ";
    public string gameOverHighScorePrefix = "Best: ";
    public string gameOverCoinsPrefix = "Coins Earned: ";
    public string gameOverTotalCoinsPrefix = "Total Coins: ";

    private ScriptPlayerController cachedPlayer;
    private bool isCountingDown;
    private Coroutine countdownCoroutine;
    private readonly StringBuilder statusBuilder = new StringBuilder();

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        Time.timeScale = 1f;

        cachedPlayer = FindFirstObjectByType<ScriptPlayerController>();
        ShowGameplayHUD();
        UpdateHUD();
        UpdatePowerUpTimersUI();
    }

    void Update()
    {
        if (ScriptGameManager.Instance == null) return;

        if (Input.GetKeyDown(KeyCode.Escape) && ScriptGameManager.Instance.State != ScriptGameManager.GameState.GameOver)
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
                return;
            }

            if (ScriptGameManager.Instance.State == ScriptGameManager.GameState.Playing)
            {
                OnPauseButtonClicked();
            }
            else if (ScriptGameManager.Instance.State == ScriptGameManager.GameState.Paused && !isCountingDown)
            {
                OnResumeButtonClicked();
            }
        }

        if (ScriptGameManager.Instance.IsPlaying)
        {
            UpdateHUD();
            UpdatePowerUpTimersUI();
        }
    }

    public void UpdateHUD()
    {
        if (ScriptGameManager.Instance == null) return;

        if (hudDistanceText != null)
        {
            hudDistanceText.text = $"{hudDistancePrefix}{ScriptGameManager.Instance.CurrentScore:N0}{hudDistanceSuffix}";
        }

        if (hudCoinsText != null)
        {
            hudCoinsText.text = $"{hudCoinsPrefix}{ScriptGameManager.Instance.SessionCoins:N0}";
        }
    }

    public void UpdatePowerUpTimersUI()
    {
        if (cachedPlayer == null)
        {
            cachedPlayer = FindFirstObjectByType<ScriptPlayerController>();
            if (cachedPlayer == null) return;
        }

        bool hasInvincible = cachedPlayer.IsInvincible;
        bool hasMagnet = cachedPlayer.IsMagnetActive;

        if (hudPowerUpStatusText != null)
        {
            if (!hasInvincible && !hasMagnet)
            {
                hudPowerUpStatusText.gameObject.SetActive(false);
            }
            else
            {
                hudPowerUpStatusText.gameObject.SetActive(true);
                statusBuilder.Clear();

                if (hasInvincible)
                {
                    statusBuilder.Append(invincibleTimerPrefix)
                                 .Append(cachedPlayer.invincibleTimer.ToString("F1"))
                                 .Append(timerSuffix);
                }

                if (hasMagnet)
                {
                    if (statusBuilder.Length > 0) statusBuilder.AppendLine();
                    statusBuilder.Append(magnetTimerPrefix)
                                 .Append(cachedPlayer.magnetTimer.ToString("F1"))
                                 .Append(timerSuffix);
                }

                hudPowerUpStatusText.text = statusBuilder.ToString();
            }
        }

        if (hudInvincibleTimerText != null)
        {
            hudInvincibleTimerText.gameObject.SetActive(hasInvincible);
            if (hasInvincible)
            {
                hudInvincibleTimerText.text = $"{invincibleTimerPrefix}{cachedPlayer.invincibleTimer:F1}{timerSuffix}";
            }
        }

        if (hudMagnetTimerText != null)
        {
            hudMagnetTimerText.gameObject.SetActive(hasMagnet);
            if (hasMagnet)
            {
                hudMagnetTimerText.text = $"{magnetTimerPrefix}{cachedPlayer.magnetTimer:F1}{timerSuffix}";
            }
        }
    }

    public void ShowGameplayHUD()
    {
        ResetCountdown();
        SetPanelActive(hudPanel, true);
        SetPanelsActive(false, pausePanel, countdownPanel, gameOverPanel, settingsPanel);
    }

    private void SetPanelsActive(bool active, params GameObject[] panels)
    {
        if (panels == null) return;
        for (int i = 0; i < panels.Length; i++)
        {
            SetPanelActive(panels[i], active);
        }
    }

    private void ResetCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        isCountingDown = false;
    }

    public void OnPauseButtonClicked()
    {
        if (ScriptGameManager.Instance == null || ScriptGameManager.Instance.State != ScriptGameManager.GameState.Playing) return;

        if (ScriptAudioManager.Instance != null) ScriptAudioManager.Instance.PlayButtonClick();

        ResetCountdown();
        ScriptGameManager.Instance.SetPaused(true);

        SetPanelActive(pausePanel, true);
        SetPanelsActive(false, countdownPanel, settingsPanel);
    }

    public void OnResumeButtonClicked()
    {
        if (ScriptGameManager.Instance == null || isCountingDown) return;

        if (ScriptAudioManager.Instance != null) ScriptAudioManager.Instance.PlayButtonClick();

        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        countdownCoroutine = StartCoroutine(ResumeCountdownRoutine());
    }

    private IEnumerator ResumeCountdownRoutine()
    {
        isCountingDown = true;
        SetPanelsActive(false, pausePanel, settingsPanel);
        SetPanelActive(countdownPanel, true);

        int secondsLeft = Mathf.Max(1, resumeCountdownSeconds);

        while (secondsLeft > 0)
        {
            if (countdownText != null) countdownText.text = secondsLeft.ToString();
            yield return new WaitForSecondsRealtime(1.0f);
            secondsLeft--;
        }

        SetPanelActive(countdownPanel, false);
        isCountingDown = false;
        countdownCoroutine = null;

        if (ScriptGameManager.Instance != null) ScriptGameManager.Instance.SetPaused(false);
    }

    public void OpenSettings()
    {
        if (ScriptAudioManager.Instance != null) ScriptAudioManager.Instance.PlayButtonClick();
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (ScriptAudioManager.Instance != null) ScriptAudioManager.Instance.PlayButtonClick();
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void ShowGameOverUI()
    {
        ResetCountdown();
        SetPanelsActive(false, hudPanel, pausePanel, countdownPanel, settingsPanel);
        SetPanelActive(gameOverPanel, true);

        if (ScriptGameManager.Instance == null) return;

        int currentDistance = ScriptGameManager.Instance.CurrentScore;
        int bestDistance = ScriptGameManager.Instance.HighScore;
        int earnedCoins = ScriptGameManager.Instance.SessionCoins;
        int totalCoins = ScriptGameManager.Instance.TotalCoins;

        if (gameOverDistanceText != null) gameOverDistanceText.text = $"{gameOverDistancePrefix}{currentDistance:N0}{hudDistanceSuffix}";
        if (gameOverHighScoreText != null) gameOverHighScoreText.text = $"{gameOverHighScorePrefix}{bestDistance:N0}{hudDistanceSuffix}";
        if (gameOverCoinsText != null) gameOverCoinsText.text = $"{gameOverCoinsPrefix}{earnedCoins:N0}";
        if (gameOverTotalCoinsText != null) gameOverTotalCoinsText.text = $"{gameOverTotalCoinsPrefix}{totalCoins:N0}";
    }

    public void OnRestartButtonClicked()
    {
        if (ScriptAudioManager.Instance != null)
        {
            ScriptAudioManager.Instance.PlayButtonClick();
            ScriptAudioManager.Instance.FadeOutBGM(0.5f);
        }

        Time.timeScale = 1f;
        ResetCountdown();
        ScriptScreenFader.FadeAndLoadScene(SceneManager.GetActiveScene().name, 0.5f);
    }

    public void OnBackToMenuButtonClicked()
    {
        if (ScriptAudioManager.Instance != null)
        {
            ScriptAudioManager.Instance.PlayButtonClick();
            ScriptAudioManager.Instance.FadeOutBGM(0.5f);
        }

        Time.timeScale = 1f;
        ResetCountdown();

        string sceneName = mainMenuScene != null ? mainMenuScene.name : "00_MainMenu";
        ScriptScreenFader.FadeAndLoadScene(sceneName, 0.5f);
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null && panel.activeSelf != active)
        {
            panel.SetActive(active);
        }
    }
}