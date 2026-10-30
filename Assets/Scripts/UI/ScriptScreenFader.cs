using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScriptScreenFader : MonoBehaviour
{
    private static bool isFading = false;

    public static void FadeAndLoadScene(string sceneName, float duration = 0.6f)
    {
        if (isFading) return;
        isFading = true;

        GameObject oldFader = GameObject.Find("ScreenFader_Persistent");
        if (oldFader != null) Destroy(oldFader);

        GameObject faderObj = new GameObject("ScreenFader_Persistent");
        faderObj.layer = 5;
        DontDestroyOnLoad(faderObj);

        Canvas canvas = faderObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32767; 

        CanvasScaler scaler = faderObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup canvasGroup = faderObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;

        GameObject blackPanel = new GameObject("BlackCurtain");
        blackPanel.layer = 5;
        blackPanel.transform.SetParent(faderObj.transform, false);

        RectTransform rt = blackPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-600, -600);
        rt.offsetMax = new Vector2(600, 600);

        Image img = blackPanel.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        List<CanvasGroup> currentSceneUI = new List<CanvasGroup>();
        Canvas[] sceneCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneCanvases.Length; i++)
        {
            if (sceneCanvases[i] != null && sceneCanvases[i].gameObject != faderObj)
            {
                CanvasGroup cg = sceneCanvases[i].GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = sceneCanvases[i].gameObject.AddComponent<CanvasGroup>();
                }
                currentSceneUI.Add(cg);
            }
        }

        ScriptScreenFader runner = faderObj.AddComponent<ScriptScreenFader>();
        runner.StartCoroutine(runner.RoutineFadeAndLoad(canvasGroup, currentSceneUI, sceneName, duration, faderObj));
    }

    private IEnumerator RoutineFadeAndLoad(CanvasGroup canvasGroup, List<CanvasGroup> currentSceneUI, string sceneName, float duration, GameObject faderObj)
    {
        duration = Mathf.Max(0.1f, duration);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            canvasGroup.alpha = t;

            for (int i = 0; i < currentSceneUI.Count; i++)
            {
                if (currentSceneUI[i] != null)
                {
                    currentSceneUI[i].alpha = 1f - t;
                }
            }

            yield return null;
        }

        canvasGroup.alpha = 1f;
        for (int i = 0; i < currentSceneUI.Count; i++)
        {
            if (currentSceneUI[i] != null)
            {
                currentSceneUI[i].alpha = 0f;
            }
        }

        yield return new WaitForSecondsRealtime(0.1f);

        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncOp.isDone)
        {
            yield return null;
        }

        yield return null;
        yield return null;

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = 1f - t;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        isFading = false;
        Destroy(faderObj);
    }
}
