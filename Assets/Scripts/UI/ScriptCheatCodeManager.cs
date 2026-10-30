using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScriptCheatCodeManager : MonoBehaviour
{
    [Header("Cheat Settings")]
    public int coinsToAdd = 99999;
    public float keyInputTimeout = 3.0f;

    private readonly KeyCode[] cheatSequenceAB = new KeyCode[]
    {
        KeyCode.UpArrow, KeyCode.UpArrow,
        KeyCode.DownArrow, KeyCode.DownArrow,
        KeyCode.LeftArrow, KeyCode.RightArrow,
        KeyCode.LeftArrow, KeyCode.RightArrow,
        KeyCode.A, KeyCode.B
    };

    private readonly KeyCode[] cheatSequenceBA = new KeyCode[]
    {
        KeyCode.UpArrow, KeyCode.UpArrow,
        KeyCode.DownArrow, KeyCode.DownArrow,
        KeyCode.LeftArrow, KeyCode.RightArrow,
        KeyCode.LeftArrow, KeyCode.RightArrow,
        KeyCode.B, KeyCode.A
    };

    private int currentIndexAB = 0;
    private int currentIndexBA = 0;
    private float lastKeyTime;

    void Update()
    {
        if (Time.unscaledTime - lastKeyTime > keyInputTimeout)
        {
            currentIndexAB = 0;
            currentIndexBA = 0;
        }

        if (Input.anyKeyDown)
        {
            CheckKeys();
        }
    }

    private void CheckKeys()
    {
        KeyCode pressed = KeyCode.None;
        foreach (KeyCode k in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(k))
            {
                pressed = k;
                break;
            }
        }

        if (pressed == KeyCode.None) return;

        lastKeyTime = Time.unscaledTime;

        if (IsMatchingKey(pressed, cheatSequenceAB[currentIndexAB]))
        {
            currentIndexAB++;
            if (currentIndexAB >= cheatSequenceAB.Length)
            {
                ActivateCheat();
                currentIndexAB = 0;
                currentIndexBA = 0;
                return;
            }
        }
        else
        {
            currentIndexAB = IsMatchingKey(pressed, cheatSequenceAB[0]) ? 1 : 0;
        }

        if (IsMatchingKey(pressed, cheatSequenceBA[currentIndexBA]))
        {
            currentIndexBA++;
            if (currentIndexBA >= cheatSequenceBA.Length)
            {
                ActivateCheat();
                currentIndexAB = 0;
                currentIndexBA = 0;
                return;
            }
        }
        else
        {
            currentIndexBA = IsMatchingKey(pressed, cheatSequenceBA[0]) ? 1 : 0;
        }
    }

    private bool IsMatchingKey(KeyCode pressed, KeyCode target)
    {
        if (pressed == target) return true;

        if (target == KeyCode.UpArrow && (pressed == KeyCode.W || pressed == KeyCode.Keypad8)) return true;
        if (target == KeyCode.DownArrow && (pressed == KeyCode.S || pressed == KeyCode.Keypad2)) return true;
        if (target == KeyCode.LeftArrow && pressed == KeyCode.Keypad4) return true;
        if (target == KeyCode.RightArrow && (pressed == KeyCode.D || pressed == KeyCode.Keypad6)) return true;

        return false;
    }

    private void ActivateCheat()
    {
        int currentCoins = PlayerPrefs.GetInt(ScriptGameManager.TotalCoinsKey, 0);
        currentCoins += coinsToAdd;
        PlayerPrefs.SetInt(ScriptGameManager.TotalCoinsKey, currentCoins);
        PlayerPrefs.Save();

        if (ScriptAudioManager.Instance != null)
        {
            ScriptAudioManager.Instance.PlayPowerUp();
        }

        ScriptShopManager shop = ScriptShopManager.Instance != null ? ScriptShopManager.Instance : FindAnyObjectByType<ScriptShopManager>();
        if (shop != null)
        {
            shop.RefreshShopUI();
        }

        ShowCheatNotification($"+{coinsToAdd:N0} COINS!");
        Debug.Log($"[CheatCode] Activated! Added {coinsToAdd:N0} coins. Total: {currentCoins:N0}");
    }

    private void ShowCheatNotification(string message)
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject banner = new GameObject("CheatNotificationBanner");
        banner.transform.SetParent(canvas.transform, false);
        banner.transform.SetAsLastSibling();

        RectTransform rt = banner.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.85f);
        rt.anchorMax = new Vector2(0.5f, 0.85f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(460, 75);

        Image bg = banner.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.12f, 0.16f, 0.95f);

        Outline outline = banner.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.82f, 0.2f, 1f);
        outline.effectDistance = new Vector2(3, -3);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(banner.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        TMP_Text tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = $"CHEAT CODE ACTIVATED\n<color=#FFD700>{message}</color>";
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        CanvasGroup cg = banner.AddComponent<CanvasGroup>();
        StartCoroutine(RoutineAnimateBanner(banner, cg, rt));
    }

    private IEnumerator RoutineAnimateBanner(GameObject banner, CanvasGroup cg, RectTransform rt)
    {
        float timer = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;

        while (timer < 0.25f)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / 0.25f;
            rt.localScale = Vector3.LerpUnclamped(startScale, targetScale, 1f + 0.2f * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        rt.localScale = targetScale;

        yield return new WaitForSecondsRealtime(2.0f);

        timer = 0f;
        while (timer < 0.5f)
        {
            timer += Time.unscaledDeltaTime;
            cg.alpha = 1f - (timer / 0.5f);
            yield return null;
        }

        Destroy(banner);
    }
}
