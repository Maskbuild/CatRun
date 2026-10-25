using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScriptMainMenuManager : MonoBehaviour
{
    [Header("Gameplay Scene Target (ลากไฟล์ Scene ด่านเล่นเกมมาใส่ได้เลย!)")]
    [Tooltip("ลากไฟล์ Scene (.unity) ของด่านเล่นเกมจากหน้าต่าง Project มาวางในช่องนี้ได้เลย!")]
    public Object gameplayScene;

    [Tooltip("ชื่อ Scene ด่านเล่นเกม (จะถูกดึงชื่อให้อัตโนมัติเมื่อลากไฟล์ Scene มาใส่ช่องด้านบน)")]
    public string gameplaySceneName = "SampleScene";

    [Header("Optional Same-Scene Mode (กรณีวาง Main Menu ไว้ใน Scene เดียวกับตัวเกม)")]
    [Tooltip("ถ้าทำหน้าเมนูเป็น Panel ในฉากเดียวกับตอนวิ่ง ให้ลาก Panel หน้าเมนูมาใส่ช่องนี้ (ถ้าแยกคนละ Scene ให้เว้นว่างไว้)")]
    public GameObject mainMenuPanel;

    [Header("Optional Stats Display (แสดงสถิติบนหน้าเมนู — เว้นว่างได้)")]
    public TMP_Text highScoreText;
    public TMP_Text totalCoinsText;
    public string highScorePrefix = "Best Distance: ";
    public string highScoreSuffix = " m";
    public string totalCoinsPrefix = "Coins: ";

    void OnValidate()
    {
        if (gameplayScene != null)
        {
            gameplaySceneName = gameplayScene.name;
        }
    }

    void Start()
    {
        Time.timeScale = 1f;
        RefreshMenuStats();
    }

    public void RefreshMenuStats()
    {
        int bestScore = PlayerPrefs.GetInt("EndlessRun_HighScore", 0);
        int totalCoins = PlayerPrefs.GetInt("EndlessRun_TotalCoins", 0);

        if (highScoreText != null)
        {
            highScoreText.text = $"{highScorePrefix}{bestScore:N0}{highScoreSuffix}";
        }

        if (totalCoinsText != null)
        {
            totalCoinsText.text = $"{totalCoinsPrefix}{totalCoins:N0}";
        }
    }

    public void OnStartGameButtonClicked()
    {
        Time.timeScale = 1f;

        if (gameplayScene != null)
        {
            gameplaySceneName = gameplayScene.name;
        }

        if (mainMenuPanel != null && SceneManager.GetActiveScene().name == gameplaySceneName)
        {
            mainMenuPanel.SetActive(false);
            if (ScriptGameManager.Instance != null)
            {
                ScriptGameManager.Instance.StartGame();
            }
            return;
        }

        if (!string.IsNullOrEmpty(gameplaySceneName) && Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            Debug.LogError($"[ScriptMainMenuManager] ไม่พบฉาก '{gameplaySceneName}' ใน Build Profiles! กรุณาเปิดเมนู File > Build Profiles แล้วลาก Scene ทั้งสองใส่ใน Scene List");
        }
    }

    public void OnQuitGameButtonClicked()
    {
        Debug.Log("[ScriptMainMenuManager] ออกจากเกม (Quit Game)!");
        Application.Quit();
    }
}