using UnityEngine;

public class ScriptPlayerSkinApplier : MonoBehaviour
{
    [Header("Target Renderer")]
    public Renderer targetRenderer;

    [Header("Optional Materials Per Skin Index")]
    public Material[] skinMaterials;

    [Header("Optional 3D Skin Models")]
    public GameObject[] skinVisualObjects;

    void Awake()
    {
        ApplySelectedSkin();
    }

    void Start()
    {
        ApplySelectedSkin();
    }

    public void ApplySelectedSkin()
    {
        int selectedIndex = PlayerPrefs.GetInt(ScriptShopManager.SelectedSkinIndexKey, 0);

        if (skinVisualObjects != null && skinVisualObjects.Length > 0)
        {
            for (int i = 0; i < skinVisualObjects.Length; i++)
            {
                if (skinVisualObjects[i] != null)
                {
                    skinVisualObjects[i].SetActive(i == selectedIndex);
                }
            }
        }

        // ค้นหา Renderer ทั้งหมดบนตัวละคร
        Renderer[] renderers = targetRenderer != null ? new Renderer[] { targetRenderer } : GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return;

        // ดึง Material ที่ตรงกับ index หรือชื่อที่เลือก
        Material chosenMaterial = null;
        string matName = PlayerPrefs.GetString(ScriptShopManager.SelectedSkinMatKey, "");

        if (skinMaterials != null)
        {
            if (!string.IsNullOrEmpty(matName))
            {
                for (int m = 0; m < skinMaterials.Length; m++)
                {
                    if (skinMaterials[m] != null && skinMaterials[m].name == matName)
                    {
                        chosenMaterial = skinMaterials[m];
                        break;
                    }
                }
            }

            if (chosenMaterial == null && selectedIndex >= 0 && selectedIndex < skinMaterials.Length)
            {
                chosenMaterial = skinMaterials[selectedIndex];
            }
        }

        Color savedColor = Color.white;
        bool hasColor = PlayerPrefs.HasKey(ScriptShopManager.SelectedSkinRKey);
        if (hasColor)
        {
            float r = PlayerPrefs.GetFloat(ScriptShopManager.SelectedSkinRKey, 1f);
            float g = PlayerPrefs.GetFloat(ScriptShopManager.SelectedSkinGKey, 1f);
            float b = PlayerPrefs.GetFloat(ScriptShopManager.SelectedSkinBKey, 1f);
            savedColor = new Color(r, g, b, 1f);
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rend = renderers[i];
            if (rend == null) continue;

            if (chosenMaterial != null)
            {
                rend.material = chosenMaterial;
            }

            if (hasColor && chosenMaterial == null)
            {
                Material mat = rend.material;
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", savedColor);
                }
                else if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", savedColor);
                }
            }
        }

        Debug.Log($"[SkinApplier] สวมใส่สกิน Index: {selectedIndex}, Material: {(chosenMaterial != null ? chosenMaterial.name : "None")}");
    }
}