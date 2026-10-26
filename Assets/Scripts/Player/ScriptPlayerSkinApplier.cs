using UnityEngine;

public class ScriptPlayerSkinApplier : MonoBehaviour
{
    [Header("Target Renderer (รูปทรงของตัวละครที่จะเปลี่ยนสี/สกิน)")]
    [Tooltip("ลากลูก PlayerVisual (ที่มี MeshRenderer หรือ SkinnedMeshRenderer) มาใส่ ถ้าเว้นว่างไว้ ระบบจะค้นหาจากลูกของ Player ให้อัตโนมัติ")]
    public Renderer targetRenderer;

    [Header("Optional Materials Per Skin Index (ถ้าใช้ Material แยกตามสกิน — เว้นว่างได้)")]
    [Tooltip("หากต้องการให้แต่ละสกินเปลี่ยนเป็น Material เฉพาะ ลากเรียงตามลำดับในร้านค้าได้เลย (ถ้าเว้นว่างไว้ ระบบจะเปลี่ยนสี Color ตามที่เลือกในร้านค้าให้อัตโนมัติ)")]
    public Material[] skinMaterials;

    [Header("Optional 3D Skin Models / Accessories (ถ้ามีโมเดลสกินหรือหมวกแยกชิ้น — เว้นว่างได้)")]
    [Tooltip("หากแต่ละสกินมีโมเดลลูกหรือของตกแต่งแยกกัน สามารถลากใส่ช่องนี้เพื่อให้เปิดเฉพาะชิ้นที่ตรงกับสกินที่เลือกได้")]
    public GameObject[] skinVisualObjects;

    void Start()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

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

        if (targetRenderer == null) return;

        if (skinMaterials != null && selectedIndex >= 0 && selectedIndex < skinMaterials.Length && skinMaterials[selectedIndex] != null)
        {
            targetRenderer.material = skinMaterials[selectedIndex];
        }

        if (PlayerPrefs.HasKey(ScriptShopManager.SelectedSkinRKey))
        {
            float r = PlayerPrefs.GetFloat(ScriptShopManager.SelectedSkinRKey, 0.2f);
            float g = PlayerPrefs.GetFloat(ScriptShopManager.SelectedSkinGKey, 0.65f);
            float b = PlayerPrefs.GetFloat(ScriptShopManager.SelectedSkinBKey, 1.0f);
            Color savedColor = new Color(r, g, b, 1f);

            Material matInstance = targetRenderer.material;
            matInstance.color = savedColor;

            if (matInstance.HasProperty("_BaseColor"))
            {
                matInstance.SetColor("_BaseColor", savedColor);
            }
        }
    }
}