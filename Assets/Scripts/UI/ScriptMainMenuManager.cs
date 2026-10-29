using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScriptMainMenuManager : MonoBehaviour
{
    [Header("Gameplay Scene Target")]
    public Object gameplayScene;

    public void OnStartGameButtonClicked()
    {
        if (ScriptAudioManager.Instance != null)
            ScriptAudioManager.Instance.PlayButtonClick();

        Time.timeScale = 1f;

        string targetScene = gameplayScene != null ? gameplayScene.name : "01_Game";
        if (Application.CanStreamedLevelBeLoaded(targetScene))
        {
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            Debug.LogError($"[ScriptMainMenuManager] ไม่พบฉาก '{targetScene}' ใน Build Settings! กรุณาเปิด File > Build Settings แล้วลากฉากใส่ลงไป");
        }
    }

    public void OnQuitGameButtonClicked()
    {
        if (ScriptAudioManager.Instance != null)
            ScriptAudioManager.Instance.PlayButtonClick();

        Application.Quit();
    }
}