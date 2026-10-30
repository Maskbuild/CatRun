using UnityEngine;

public class ScriptMainMenuManager : MonoBehaviour
{
    [Header("Gameplay Scene Target")]
    public Object gameplayScene;

    [Header("Fade Transition")]
    [Tooltip("ระยะเวลาในการเฟดดำ (วินาที)")]
    public float fadeDuration = 0.6f;

    private bool isLoadingStarted = false;

    void Start()
    {
        if (ScriptAudioManager.Instance != null)
        {
            ScriptAudioManager.Instance.PlayMenuBGM(smooth: true, duration: 1.0f);
        }
    }

    public void OnStartGameButtonClicked()
    {
        if (isLoadingStarted) return;

        if (ScriptAudioManager.Instance != null)
        {
            ScriptAudioManager.Instance.PlayButtonClick();
            ScriptAudioManager.Instance.FadeOutBGM(fadeDuration);
        }

        Time.timeScale = 1f;

        string targetScene = gameplayScene != null ? gameplayScene.name : "01_Game";
        if (Application.CanStreamedLevelBeLoaded(targetScene))
        {
            isLoadingStarted = true;
            ScriptScreenFader.FadeAndLoadScene(targetScene, fadeDuration);
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
