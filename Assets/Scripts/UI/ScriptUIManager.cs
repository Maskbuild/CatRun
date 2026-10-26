using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScriptUIManager : MonoBehaviour
{
    public static ScriptUIManager Instance { get; private set; }

    [Header("1. Gameplay HUD (UI ตอนเล่นเกม)")]
    public GameObject hudPanel;
    [Tooltip("ข้อความแสดงระยะทางขณะวิ่ง (เช่น 120 m)")]
    public TMP_Text hudDistanceText;
    [Tooltip("ข้อความแสดงจำนวนเหรียญที่เก็บได้ในรอบนี้")]
    public TMP_Text hudCoinsText;

    [Header("1.1 Power-Up Timer UI (ตัวบอกเวลาสถานะพิเศษบนหน้าจอ HUD)")]
    [Tooltip("(แนะนำ) ข้อความรวมสำหรับแสดงเวลาสถานะพิเศษทั้งหมดในกล่องเดียว (จะแสดงเฉพาะตอนเก็บไอเทม และซ่อนเองเมื่อหมดเวลา)")]
    public TMP_Text hudPowerUpStatusText;

    [Tooltip("(ทางเลือก) หากต้องการแยกกล่องข้อความเวลาอมตะ (Invincibility) ต่างหาก ลากมาใส่ช่องนี้ได้ (เว้นว่างได้)")]
    public TMP_Text hudInvincibleTimerText;

    [Tooltip("(ทางเลือก) หากต้องการแยกกล่องข้อความเวลาแม่เหล็ก (Coin Magnet) ต่างหาก ลากมาใส่ช่องนี้ได้ (เว้นว่างได้)")]
    public TMP_Text hudMagnetTimerText;

    [Header("2. Pause Menu UI (UI หยุดเกม)")]
    public GameObject pausePanel;

    [Header("3. Resume Countdown UI (UI นับถอยหลัง 3 วิ หลังกด Resume)")]
    [Tooltip("Panel หรือกรอบสำหรับหน้านับถอยหลัง 3 วินาที")]
    public GameObject countdownPanel;
    [Tooltip("ข้อความตัวเลขนับถอยหลัง 3, 2, 1")]
    public TMP_Text countdownText;
    [Tooltip("จำนวนวินาทีที่จะนับถอยหลังหลังกด Resume (ค่าเริ่มต้น 3 วินาที)")]
    public int resumeCountdownSeconds = 3;

    [Header("4. Game Over UI (UI จบเกม)")]
    public GameObject gameOverPanel;
    [Tooltip("ข้อความแสดงระยะทางล่าสุดที่ทำได้ในรอบนี้")]
    public TMP_Text gameOverDistanceText;
    [Tooltip("ข้อความแสดงสถิติระยะทางสูงสุด (High Score)")]
    public TMP_Text gameOverHighScoreText;
    [Tooltip("ข้อความแสดงจำนวนเงินที่เก็บได้ในรอบนี้")]
    public TMP_Text gameOverCoinsText;
    [Tooltip("(ตัวเลือกเสริม) ข้อความแสดงจำนวนเงินสะสมทั้งหมด เว้นว่างได้")]
    public TMP_Text gameOverTotalCoinsText;

    [Header("5. Back To Menu Target (ลากฉากหรือหน้า Menu มาใส่ได้เลย!)")]
    [Tooltip("ลากไฟล์ Scene (.unity) ของหน้า Main Menu จากหน้าต่าง Project มาวางใส่ช่องนี้ได้เลย!")]
    public Object mainMenuScene;

    [Tooltip("(ทางเลือก) หากทำหน้า Main Menu เป็น Panel อยู่ใน Scene เดียวกัน สามารถลาก Panel หน้าเมนูมาใส่ช่องนี้ได้")]
    public GameObject mainMenuPanelInSameScene;

    [Tooltip("ชื่อ Scene ที่จะโหลด (จะถูกดึงชื่อให้อัตโนมัติทันทีเมื่อลากไฟล์ Scene มาใส่ช่องด้านบน)")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Text Formatting (รูปแบบข้อความ ปรับแต่งได้ตามชอบ)")]
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

    void OnValidate()
    {
        if (mainMenuScene != null)
        {
            mainMenuSceneName = mainMenuScene.name;
        }
    }

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
        }

        float invTime = cachedPlayer != null ? cachedPlayer.invincibleTimer : 0f;
        float magTime = cachedPlayer != null ? cachedPlayer.magnetTimer : 0f;

        if (hudPowerUpStatusText != null)
        {
            statusBuilder.Clear();

            if (invTime > 0f)
            {
                statusBuilder.Append($"{invincibleTimerPrefix}{invTime:F1}{timerSuffix}");
            }

            if (magTime > 0f)
            {
                if (statusBuilder.Length > 0) statusBuilder.AppendLine();
                statusBuilder.Append($"{magnetTimerPrefix}{magTime:F1}{timerSuffix}");
            }

            bool hasAnyActive = statusBuilder.Length > 0;
            if (hudPowerUpStatusText.gameObject.activeSelf != hasAnyActive)
            {
                hudPowerUpStatusText.gameObject.SetActive(hasAnyActive);
            }

            if (hasAnyActive)
            {
                hudPowerUpStatusText.text = statusBuilder.ToString();
            }
        }

        if (hudInvincibleTimerText != null)
        {
            bool active = invTime > 0f;
            if (hudInvincibleTimerText.gameObject.activeSelf != active)
            {
                hudInvincibleTimerText.gameObject.SetActive(active);
            }
            if (active)
            {
                hudInvincibleTimerText.text = $"{invincibleTimerPrefix}{invTime:F1}{timerSuffix}";
            }
        }

        if (hudMagnetTimerText != null)
        {
            bool active = magTime > 0f;
            if (hudMagnetTimerText.gameObject.activeSelf != active)
            {
                hudMagnetTimerText.gameObject.SetActive(active);
            }
            if (active)
            {
                hudMagnetTimerText.text = $"{magnetTimerPrefix}{magTime:F1}{timerSuffix}";
            }
        }
    }

    public void ShowGameplayHUD()
    {
        isCountingDown = false;
        SetPanelActive(hudPanel, true);
        SetPanelActive(pausePanel, false);
        SetPanelActive(countdownPanel, false);
        SetPanelActive(gameOverPanel, false);
        SetPanelActive(mainMenuPanelInSameScene, false);
    }

    public void OnPauseButtonClicked()
    {
        if (ScriptGameManager.Instance == null) return;
        if (ScriptGameManager.Instance.State != ScriptGameManager.GameState.Playing) return;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        isCountingDown = false;

        ScriptGameManager.Instance.SetPaused(true);

        SetPanelActive(pausePanel, true);
        SetPanelActive(countdownPanel, false);
    }

    public void OnResumeButtonClicked()
    {
        if (ScriptGameManager.Instance == null) return;
        if (isCountingDown) return;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        countdownCoroutine = StartCoroutine(ResumeCountdownRoutine());
    }

    private IEnumerator ResumeCountdownRoutine()
    {
        isCountingDown = true;

        SetPanelActive(pausePanel, false);
        SetPanelActive(countdownPanel, true);

        int secondsLeft = Mathf.Max(1, resumeCountdownSeconds);

        while (secondsLeft > 0)
        {
            if (countdownText != null)
            {
                countdownText.text = secondsLeft.ToString();
            }

            yield return new WaitForSecondsRealtime(1.0f);
            secondsLeft--;
        }

        SetPanelActive(countdownPanel, false);
        isCountingDown = false;
        countdownCoroutine = null;

        if (ScriptGameManager.Instance != null)
        {
            ScriptGameManager.Instance.SetPaused(false);
        }
    }

    public void ShowGameOverUI()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        isCountingDown = false;

        SetPanelActive(hudPanel, false);
        SetPanelActive(pausePanel, false);
        SetPanelActive(countdownPanel, false);
        SetPanelActive(gameOverPanel, true);

        if (ScriptGameManager.Instance == null) return;

        int currentDistance = ScriptGameManager.Instance.CurrentScore;
        int bestDistance = ScriptGameManager.Instance.HighScore;
        int earnedCoins = ScriptGameManager.Instance.SessionCoins;
        int totalCoins = ScriptGameManager.Instance.TotalCoins;

        if (gameOverDistanceText != null)
        {
            gameOverDistanceText.text = $"{gameOverDistancePrefix}{currentDistance:N0}{hudDistanceSuffix}";
        }

        if (gameOverHighScoreText != null)
        {
            gameOverHighScoreText.text = $"{gameOverHighScorePrefix}{bestDistance:N0}{hudDistanceSuffix}";
        }

        if (gameOverCoinsText != null)
        {
            gameOverCoinsText.text = $"{gameOverCoinsPrefix}{earnedCoins:N0}";
        }

        if (gameOverTotalCoinsText != null)
        {
            gameOverTotalCoinsText.text = $"{gameOverTotalCoinsPrefix}{totalCoins:N0}";
        }
    }

    public void OnRestartButtonClicked()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void OnBackToMenuButtonClicked()
    {
        Time.timeScale = 1f;

        if (mainMenuScene != null)
        {
            mainMenuSceneName = mainMenuScene.name;
        }

        if (mainMenuPanelInSameScene != null)
        {
            SetPanelActive(hudPanel, false);
            SetPanelActive(pausePanel, false);
            SetPanelActive(countdownPanel, false);
            SetPanelActive(gameOverPanel, false);
            SetPanelActive(mainMenuPanelInSameScene, true);
            Time.timeScale = 0f;
            return;
        }

        if (!string.IsNullOrEmpty(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning($"[ScriptUIManager] กรุณาเพิ่ม Scene '{mainMenuSceneName}' เข้าไปในเมนู File > Build Profiles (Scene List) เพื่อให้โหลดข้ามฉากได้!");
        }
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null && panel.activeSelf != active)
        {
            panel.SetActive(active);
        }
    }
}