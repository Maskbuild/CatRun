using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScriptShopManager : MonoBehaviour
{
    [Serializable]
    public class ShopSkinItem
    {
        [Tooltip("ชื่อสกินที่จะแสดงในร้านค้า")]
        public string skinName = "Cat Skin";

        [Tooltip("ราคาเหรียญที่ใช้ซื้อ (ตั้งเป็น 0 สำหรับสกินเริ่มต้นที่ปลดล็อกฟรีตั้งแต่แรก)")]
        public int price = 100;

        [Tooltip("สีของสกินนี้ (ใช้เปลี่ยนสีตัวละครและแสดงบนกรอบตัวอย่าง UI)")]
        public Color skinColor = Color.cyan;

        [Tooltip("(ทางเลือก) หากต้องการเปลี่ยนเป็น Material พิเศษ ลาก Material มาใส่ได้ (ถ้าเว้นว่างจะใช้การเปลี่ยนสี skinColor แทน)")]
        public Material customMaterial;
    }

    public const string SelectedSkinIndexKey = "EndlessRun_SelectedSkinIndex";
    public const string SelectedSkinRKey = "EndlessRun_SkinColor_R";
    public const string SelectedSkinGKey = "EndlessRun_SkinColor_G";
    public const string SelectedSkinBKey = "EndlessRun_SkinColor_B";
    public const string SkinUnlockedPrefix = "EndlessRun_SkinUnlocked_";

    [Header("Shop UI Panels (หน้าต่างร้านค้า เปิด/ปิดจากหน้าเมนูได้)")]
    [Tooltip("ลาก Panel ของหน้าร้านค้า (Shop_Panel) มาใส่")]
    public GameObject shopPanel;

    [Header("Skin Catalog (รายการสกินและสีทั้งหมดในร้านค้า)")]
    public ShopSkinItem[] skins = new ShopSkinItem[]
    {
        new ShopSkinItem { skinName = "Sky Blue Cat (Default)", price = 0,   skinColor = new Color(0.2f, 0.65f, 1.0f, 1f) },
        new ShopSkinItem { skinName = "Golden Cat",             price = 50,  skinColor = new Color(1.0f, 0.80f, 0.15f, 1f) },
        new ShopSkinItem { skinName = "Ruby Pink Cat",          price = 100, skinColor = new Color(1.0f, 0.30f, 0.55f, 1f) },
        new ShopSkinItem { skinName = "Emerald Cat",            price = 150, skinColor = new Color(0.2f, 0.90f, 0.45f, 1f) },
        new ShopSkinItem { skinName = "Amethyst Cat",           price = 250, skinColor = new Color(0.65f, 0.35f, 1.0f, 1f) }
    };

    [Header("Shop UI Texts & Preview (องค์ประกอบ UI ในหน้าร้านค้า)")]
    [Tooltip("ข้อความแสดงจำนวนเหรียญสะสมทั้งหมดที่มีอยู่")]
    public TMP_Text totalCoinsText;
    [Tooltip("ข้อความแสดงชื่อสกินที่กำลังเลือกดูอยู่")]
    public TMP_Text skinNameText;
    [Tooltip("ข้อความแสดงราคา หรือสถานะ (เช่น 'Price: 100 Coins', 'Owned', 'Equipped')")]
    public TMP_Text skinPriceOrStatusText;
    [Tooltip("ข้อความบนปุ่มกดซื้อ/สวมใส่ (จะเปลี่ยนคำว่า BUY / EQUIP / EQUIPPED อัตโนมัติ)")]
    public TMP_Text buyEquipButtonText;
    [Tooltip("ข้อความแจ้งเตือนผลการซื้อ (เช่น 'ปลดล็อกสำเร็จ!' หรือ 'เหรียญไม่พอ!') — เว้นว่างได้")]
    public TMP_Text feedbackMessageText;

    [Header("Visual Preview (แสดงสีตัวอย่างบน UI หรือบนหุ่นตัวละคร 3D)")]
    [Tooltip("ลาก UI Image ที่ใช้แสดงสีตัวอย่างของสกินมาใส่ (เว้นว่างได้)")]
    public Image previewColorImage;
    [Tooltip("(ทางเลือก) หากมีหุ่นตัวละคร 3D โชว์ในหน้าร้านค้า ลาก MeshRenderer ของหุ่นมาใส่เพื่อให้เปลี่ยนสีตามสกินที่ดูได้")]
    public Renderer preview3DRenderer;
    [Tooltip("(ทางเลือก) หากวางร้านค้าไว้ในฉากเดียวกับตัวผู้เล่น ลาก ScriptPlayerSkinApplier มาใส่เพื่อเปลี่ยนสีทันที")]
    public ScriptPlayerSkinApplier playerSkinApplier;

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
                Debug.Log($"[Shop] ซื้อสกิน '{item.skinName}' สำเร็จ! เหรียญคงเหลือ: {totalCoins}");
            }
            else
            {
                int needed = item.price - totalCoins;
                if (feedbackMessageText != null)
                {
                    feedbackMessageText.text = $"Not enough coins! Need {needed:N0} more.";
                }
                Debug.Log($"[Shop] เหรียญไม่พอซื้อ '{item.skinName}' (ขาดอีก {needed} เหรียญ)");
            }
        }

        RefreshShopUI();
    }

    private void EquipSkin(int index)
    {
        SaveSelectedSkinData(index);

        if (playerSkinApplier == null)
        {
            playerSkinApplier = FindFirstObjectByType<ScriptPlayerSkinApplier>();
        }
        if (playerSkinApplier != null)
        {
            playerSkinApplier.ApplySelectedSkin();
        }
    }

    private void SaveSelectedSkinData(int index)
    {
        if (skins == null || index < 0 || index >= skins.Length) return;

        ShopSkinItem chosen = skins[index];
        PlayerPrefs.SetInt(SelectedSkinIndexKey, index);
        PlayerPrefs.SetFloat(SelectedSkinRKey, chosen.skinColor.r);
        PlayerPrefs.SetFloat(SelectedSkinGKey, chosen.skinColor.g);
        PlayerPrefs.SetFloat(SelectedSkinBKey, chosen.skinColor.b);
        PlayerPrefs.Save();
    }

    public void RefreshShopUI()
    {
        int totalCoins = GetTotalCoins();
        if (totalCoinsText != null)
        {
            totalCoinsText.text = $"Coins: {totalCoins:N0}";
        }

        if (skins == null || skins.Length == 0) return;

        currentPreviewIndex = Mathf.Clamp(currentPreviewIndex, 0, skins.Length - 1);
        ShopSkinItem currentItem = skins[currentPreviewIndex];
        bool isUnlocked = IsSkinUnlocked(currentPreviewIndex);
        bool isEquipped = GetSelectedSkinIndex() == currentPreviewIndex;

        if (skinNameText != null)
        {
            skinNameText.text = currentItem.skinName;
        }

        if (skinPriceOrStatusText != null)
        {
            if (isEquipped)
                skinPriceOrStatusText.text = "Status: EQUIPPED";
            else if (isUnlocked)
                skinPriceOrStatusText.text = "Status: OWNED";
            else
                skinPriceOrStatusText.text = $"Price: {currentItem.price:N0} Coins";
        }

        if (buyEquipButtonText != null)
        {
            if (isEquipped)
                buyEquipButtonText.text = "EQUIPPED";
            else if (isUnlocked)
                buyEquipButtonText.text = "EQUIP";
            else
                buyEquipButtonText.text = $"BUY ({currentItem.price:N0})";
        }

        if (previewColorImage != null)
        {
            previewColorImage.color = currentItem.skinColor;
        }

        if (preview3DRenderer != null)
        {
            if (currentItem.customMaterial != null)
            {
                preview3DRenderer.material = currentItem.customMaterial;
            }
            preview3DRenderer.material.color = currentItem.skinColor;
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