using UnityEngine;

public class ScriptGameManager : MonoBehaviour
{
    public static ScriptGameManager Instance { get; private set; }

    public enum GameState { MainMenu, Playing, Paused, GameOver }
    public GameState State { get; private set; } = GameState.Playing;

    [Header("Player, Chaser & UI References")]
    public Transform playerTransform;
    [Tooltip("ลากวัตถุ DogChaser (ที่มี ScriptChaserController) มาใส่ หรือปล่อยว่างไว้ให้ระบบค้นหาเองอัตโนมัติ")]
    public ScriptChaserController chaserController;
    [Tooltip("ลากวัตถุ UIManager (ที่มี ScriptUIManager) มาใส่ หรือปล่อยว่างไว้ให้ระบบค้นหาเองอัตโนมัติ")]
    public ScriptUIManager uiManager;

    [Header("Score Settings")]
    public float scoreMultiplier = 1f;

    public int SessionCoins { get; private set; }
    public int TotalCoins { get; private set; }
    public int CurrentScore { get; private set; }
    public int HighScore { get; private set; }

    public bool IsBeingChased => chaserController != null && chaserController.IsChasing;

    private float startZ;

    private const string HighScoreKey = "EndlessRun_HighScore";
    private const string TotalCoinsKey = "EndlessRun_TotalCoins";

    public bool IsPlaying => State == GameState.Playing;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        TotalCoins = PlayerPrefs.GetInt(TotalCoinsKey, 0);
    }

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        if (chaserController == null)
        {
            chaserController = FindFirstObjectByType<ScriptChaserController>();
        }

        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<ScriptUIManager>();
        }

        StartGame();
    }

    void Update()
    {
        if (!IsPlaying || playerTransform == null) return;

        float distance = Mathf.Max(0f, playerTransform.position.z - startZ);
        CurrentScore = Mathf.FloorToInt(distance * scoreMultiplier);

        if (CurrentScore > HighScore)
        {
            HighScore = CurrentScore;
        }
    }

    public void StartGame()
    {
        SessionCoins = 0;
        CurrentScore = 0;

        if (playerTransform != null)
            startZ = playerTransform.position.z;

        State = GameState.Playing;
        Time.timeScale = 1f;

        if (chaserController != null)
        {
            chaserController.StartInitialChase();
        }

        if (uiManager != null)
        {
            uiManager.ShowGameplayHUD();
            uiManager.UpdateHUD();
        }

        Debug.Log($"[Game Start] เริ่มวิ่ง! (หมากำลังไล่ตามช่วงแรก) | High Score: {HighScore}m | Total Coins: {TotalCoins}");
    }

    public void OnPlayerHitObstacle(ScriptPlayerController player)
    {
        if (!IsPlaying) return;

        if (chaserController != null)
        {
            if (chaserController.IsChasing)
            {
                chaserController.CatchPlayer();
                GameOver();
            }
            else
            {
                if (player != null)
                {
                    player.TriggerBlinkEffect();
                }
                chaserController.StartChaseAfterMistake();
            }
        }
        else
        {
            if (player != null)
            {
                player.TriggerBlinkEffect();
            }
            Debug.LogWarning("[ScriptGameManager] ยังไม่ได้สร้าง DogChaser (ScriptChaserController) ในฉาก!");
        }
    }

    public void TakeDamage(int amount = 1)
    {
        ScriptPlayerController player = playerTransform != null
            ? playerTransform.GetComponent<ScriptPlayerController>()
            : FindFirstObjectByType<ScriptPlayerController>();
        OnPlayerHitObstacle(player);
    }

    public void AddCoin()
    {
        if (!IsPlaying) return;

        SessionCoins++;
        TotalCoins++;

        if (uiManager != null)
        {
            uiManager.UpdateHUD();
        }

        Debug.Log($"[Coin Collected] เหรียญรอบนี้: {SessionCoins} (สะสมทั้งหมด: {TotalCoins}) | ระยะทาง: {CurrentScore}m");
    }

    public void GameOver()
    {
        State = GameState.GameOver;
        Time.timeScale = 0f;

        if (CurrentScore >= HighScore)
        {
            HighScore = CurrentScore;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
        }
        PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins);
        PlayerPrefs.Save();

        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<ScriptUIManager>();
        }
        if (uiManager != null)
        {
            uiManager.ShowGameOverUI();
        }

        Debug.Log($"=== GAME OVER (โดนหมาจับได้!) === ระยะทาง: {CurrentScore}m | High Score: {HighScore}m | เหรียญรอบนี้: {SessionCoins} (รวมทั้งหมด: {TotalCoins})");
    }

    public void SetPaused(bool paused)
    {
        if (State == GameState.GameOver) return;

        State = paused ? GameState.Paused : GameState.Playing;
        Time.timeScale = paused ? 0f : 1f;
    }
}