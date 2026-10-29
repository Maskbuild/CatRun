using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScriptShopManager : MonoBehaviour
{
    [Serializable]
    public class ShopSkinItem
    {
        public string skinName = "Cat Skin";

        public int price = 100;

        public Color skinColor = Color.cyan;

        public Material customMaterial;
    }

    public const string SelectedSkinIndexKey = "EndlessRun_SelectedSkinIndex";
    public const string SelectedSkinRKey = "EndlessRun_SkinColor_R";
    public const string SelectedSkinGKey = "EndlessRun_SkinColor_G";
    public const string SelectedSkinBKey = "EndlessRun_SkinColor_B";
    public const string SkinUnlockedPrefix = "EndlessRun_SkinUnlocked_";

    [Header("Shop UI Panels")]
    public GameObject shopPanel;

    [Header("Skin Catalog")]
    public ShopSkinItem[] skins = new ShopSkinItem[]
    {

    };

    [Header("Shop UI")]
    public TMP_Text totalCoinsText;
    public TMP_Text feedbackMessageText;
    public TMP_Text[] slotButtonTexts;

    private int currentPreviewIndex = 0;

    void Start()
    {
        EnsureDefaultSkinUnlocked();

        currentPreviewIndex = GetSelectedSkinIndex();
        if (skins != null && skins.Length > 0)
        {
            currentPreviewIndex = Mathf.Clamp(currentPreviewIndex, 0, skins.Length - 1);
            SaveSelectedSkinData(currentPreviewIndex);
        }

        if (feedbackMessageText != null)
        {
            feedbackMessageText.text = "";
        }

        RefreshShopUI();
    }

    private void EnsureDefaultSkinUnlocked()
    {
        if (skins == null || skins.Length == 0) return;

        for (int i = 0; i < skins.Length; i++)
        {
            if (i == 0 || skins[i].price <= 0)
            {
                PlayerPrefs.SetInt(SkinUnlockedPrefix + i, 1);
            }
        }
        PlayerPrefs.Save();
    }

    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }
        currentPreviewIndex = GetSelectedSkinIndex();
        if (feedbackMessageText != null) feedbackMessageText.text = "";
        RefreshShopUI();
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    public void OnNextSkinButtonClicked()
    {
        if (skins == null || skins.Length == 0) return;
        currentPreviewIndex = (currentPreviewIndex + 1) % skins.Length;
        if (feedbackMessageText != null) feedbackMessageText.text = "";
        RefreshShopUI();
    }

    public void OnPreviousSkinButtonClicked()
    {
        if (skins == null || skins.Length == 0) return;
        currentPreviewIndex = (currentPreviewIndex - 1 + skins.Length) % skins.Length;
        if (feedbackMessageText != null) feedbackMessageText.text = "";
        RefreshShopUI();
    }

    public void OnBuyOrEquipButtonClicked()
    {
        SelectOrBuySkinByIndex(currentPreviewIndex);
    }

    public void SelectOrBuySkinByIndex(int index)
    {
        if (skins == null || index < 0 || index >= skins.Length) return;

        ScriptAudioManager.Instance?.PlayButtonClick();

        currentPreviewIndex = index;
        ShopSkinItem item = skins[index];

        if (IsSkinUnlocked(index))
        {
            EquipSkin(index);
            if (feedbackMessageText != null)
            {
                feedbackMessageText.text = $"Equipped: {item.skinName}!";
            }
        }
        else
        {
            int totalCoins = GetTotalCoins();
            if (totalCoins >= item.price)
            {
                totalCoins -= item.price;
                PlayerPrefs.SetInt(ScriptGameManager.TotalCoinsKey, totalCoins);
                PlayerPrefs.SetInt(SkinUnlockedPrefix + index, 1);

                EquipSkin(index);

                if (feedbackMessageText != null)
                {
                    feedbackMessageText.text = $"Unlocked & Equipped: {item.skinName}!";
                }
            }
            else
            {
                int needed = item.price - totalCoins;
                if (feedbackMessageText != null)
                {
                    feedbackMessageText.text = $"Not enough coins! Need {needed:N0} more.";
                }
            }
        }

        RefreshShopUI();
    }

    public const string SelectedSkinMatKey = "EndlessRun_SelectedSkinMat";

    private void EquipSkin(int index)
    {
        SaveSelectedSkinData(index);
    }

    private void SaveSelectedSkinData(int index)
    {
        if (skins == null || index < 0 || index >= skins.Length) return;

        ShopSkinItem chosen = skins[index];
        PlayerPrefs.SetInt(SelectedSkinIndexKey, index);
        PlayerPrefs.SetFloat(SelectedSkinRKey, chosen.skinColor.r);
        PlayerPrefs.SetFloat(SelectedSkinGKey, chosen.skinColor.g);
        PlayerPrefs.SetFloat(SelectedSkinBKey, chosen.skinColor.b);

        if (chosen.customMaterial != null)
        {
            PlayerPrefs.SetString(SelectedSkinMatKey, chosen.customMaterial.name);
        }
        else
        {
            PlayerPrefs.DeleteKey(SelectedSkinMatKey);
        }

        PlayerPrefs.Save();
    }

    public void RefreshShopUI()
    {
        int totalCoins = GetTotalCoins();
        if (totalCoinsText != null) totalCoinsText.text = $"{totalCoins:N0}";

        int selectedIndex = GetSelectedSkinIndex();

        if (slotButtonTexts != null && skins != null)
        {
            for (int i = 0; i < slotButtonTexts.Length; i++)
            {
                if (slotButtonTexts[i] == null || i >= skins.Length) continue;

                if (i == selectedIndex)
                {
                    slotButtonTexts[i].text = "EQUIPPED";
                }
                else if (IsSkinUnlocked(i))
                {
                    slotButtonTexts[i].text = "USE";
                }
                else
                {
                    slotButtonTexts[i].text = skins[i].price > 0 ? $"{skins[i].price:N0}" : "USE";
                }
            }
        }
    }

    public bool IsSkinUnlocked(int index)
    {
        if (index == 0) return true;
        return PlayerPrefs.GetInt(SkinUnlockedPrefix + index, 0) == 1;
    }

    public int GetSelectedSkinIndex()
    {
        return PlayerPrefs.GetInt(SelectedSkinIndexKey, 0);
    }

    public int GetTotalCoins()
    {
        return PlayerPrefs.GetInt(ScriptGameManager.TotalCoinsKey, 0);
    }
}