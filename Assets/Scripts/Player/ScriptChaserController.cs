using UnityEngine;

public class ScriptChaserController : MonoBehaviour
{
    [Header("Target Reference (ตัวละครแมว)")]
    public Transform player;

    [Header("Visual Container (รูปทรงของหมาที่จะซ่อน/แสดง)")]
    [Tooltip("ลากลูกที่เป็นโมเดลหรือรูปทรงของหมามาใส่ ถ้าเว้นว่างไว้ สคริปต์จะซ่อน/แสดง MeshRenderer ทั้งหมดในตัวมันเองอัตโนมัติ")]
    public GameObject visualContainer;

    [Header("Chase Timing (เวลาในการไล่ล่า)")]
    [Tooltip("เวลาที่หมาไล่ตามตอนเริ่มเกมใหม่ๆ (วินาที) ก่อนจะค่อยๆ ผ่อนแรงถอยออกไป")]
    public float initialChaseDuration = 6.0f;

    [Tooltip("เวลาที่หมาจะไล่ตามหลังจากผู้เล่นวิ่งชนสิ่งกีดขวาง (วินาที)")]
    public float chaseDurationAfterHit = 8.0f;

    [Header("Catch Threshold While Retreating (ระยะถอยที่หมายังพุ่งจับได้ทันที)")]
    [Tooltip("หากหมากำลังถอยห่างแต่ยังไม่เกินสัดส่วนนี้ (0.5 = 50%) แล้วผู้เล่นชนสิ่งกีดขวาง หมาจะจับได้และแพ้ทันที!")]
    [Range(0f, 1f)]
    public float catchableRetreatThreshold = 0.5f;

    [Header("Distances & Smooth Speeds (ระยะห่างและความเร็วการพุ่งเข้า/ค่อยๆ ถอย)")]
    [Tooltip("ระยะห่างด้านหลังแมวตอนกำลังไล่จี้ตูด (เมตร) — กล้องห่าง 5m ให้หมาห่าง 2.2m จะเห็นหมาอยู่ในจอพอดี")]
    public float chaseDistanceBehind = 2.2f;

    [Tooltip("ระยะห่างด้านหลังแมวเมื่อถอยพ้นมุมกล้องไปแล้วก่อนจะซ่อนตัว (เมตร)")]
    public float hiddenDistanceBehind = 14.0f;

    [Tooltip("ความเร็วตอนหมาเร่งสปีดพุ่งเข้ามาประชิดแมว (เมตร/วินาที)")]
    public float approachSpeed = 7.0f;

    [Tooltip("ความเร็วที่หมาค่อยๆ ถอยห่างจากแมวตอนหมดเวลาไล่")]
    public float retreatSpeed = 2.2f;

    [Tooltip("ความนุ่มนวลตอนเริ่มผ่อนแรงถอยหลัง")]
    public float retreatAcceleration = 2.5f;

    [Tooltip("ความเร็วในการเปลี่ยนเลนซ้าย-ขวาตามแมว")]
    public float laneFollowSpeed = 10.0f;

    [Header("Run Bobbing Animation (แอนิเมชันกระเด้งตัวตอนวิ่ง)")]
    public float bobFrequency = 14.0f;
    public float bobAmplitude = 0.12f;

    public bool IsChasing { get; private set; }

    public float RetreatProgress
    {
        get
        {
            float totalRetreatSpan = Mathf.Max(0.01f, hiddenDistanceBehind - chaseDistanceBehind);
            return Mathf.Clamp01((currentDistanceBehind - chaseDistanceBehind) / totalRetreatSpan);
        }
    }

    public bool CanCatchPlayer =>
        IsChasing || (!hasCaughtPlayer && IsVisible() && RetreatProgress <= catchableRetreatThreshold);

    private float chaseTimer;
    private float currentDistanceBehind;
    private float currentRetreatRate;
    private float baseHeightY = 1.0f;
    private bool hasCaughtPlayer;
    private Renderer[] chaserRenderers;

    void Awake()
    {
        chaserRenderers = GetComponentsInChildren<Renderer>(true);
    }

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        baseHeightY = transform.position.y;
        if (baseHeightY <= 0.1f && player != null)
        {
            baseHeightY = player.position.y;
        }

        StartInitialChase();
    }

    void Update()
    {
        if (player == null) return;

        if (hasCaughtPlayer)
        {
            currentDistanceBehind = Mathf.MoveTowards(
                currentDistanceBehind,
                0.5f,
                approachSpeed * 2.0f * Time.unscaledDeltaTime
            );
            UpdateChaserPosition(Time.unscaledDeltaTime, false);
            return;
        }

        if (ScriptGameManager.Instance != null && !ScriptGameManager.Instance.IsPlaying)
            return;

        if (IsChasing)
        {
            chaseTimer -= Time.deltaTime;
            currentRetreatRate = 0f;

            currentDistanceBehind = Mathf.MoveTowards(
                currentDistanceBehind,
                chaseDistanceBehind,
                approachSpeed * Time.deltaTime
            );

            if (chaseTimer <= 0f)
            {
                IsChasing = false;
                Debug.Log("[Chaser] หมาเริ่มเหนื่อยและกำลังถอยห่าง (แต่ถ้ายังไม่พ้นระยะ 50% แล้วชนซ้ำ ยังโดนจับได้นะ!)");
            }
        }
        else
        {
            currentRetreatRate = Mathf.MoveTowards(
                currentRetreatRate,
                retreatSpeed,
                retreatAcceleration * Time.deltaTime
            );

            currentDistanceBehind = Mathf.MoveTowards(
                currentDistanceBehind,
                hiddenDistanceBehind,
                currentRetreatRate * Time.deltaTime
            );

            if (Mathf.Approximately(currentDistanceBehind, hiddenDistanceBehind))
            {
                SetChaserVisible(false);
            }
        }

        UpdateChaserPosition(Time.deltaTime, true);
    }

    private void UpdateChaserPosition(float deltaTime, bool applyBobbing)
    {
        float newX = Mathf.Lerp(transform.position.x, player.position.x, laneFollowSpeed * deltaTime);

        float newZ = player.position.z - currentDistanceBehind;

        float bobOffset = applyBobbing ? Mathf.Abs(Mathf.Sin(Time.time * bobFrequency)) * bobAmplitude : 0f;
        float newY = baseHeightY + bobOffset;

        transform.position = new Vector3(newX, newY, newZ);
    }

    public void StartInitialChase()
    {
        hasCaughtPlayer = false;
        IsChasing = true;
        chaseTimer = initialChaseDuration;
        currentDistanceBehind = chaseDistanceBehind;
        currentRetreatRate = 0f;

        SetChaserVisible(true);

        if (player != null)
        {
            transform.position = new Vector3(player.position.x, baseHeightY, player.position.z - currentDistanceBehind);
        }

        Debug.Log($"[Chaser] เริ่มเกม! หมากำลังไล่ตามเป็นเวลา {initialChaseDuration} วินาที");
    }

    public void StartChaseAfterMistake()
    {
        hasCaughtPlayer = false;
        IsChasing = true;
        chaseTimer = chaseDurationAfterHit;
        currentRetreatRate = 0f;

        if (!IsVisible() || currentDistanceBehind > 8.5f)
        {
            currentDistanceBehind = 8.5f;
        }

        SetChaserVisible(true);
        Debug.Log($"[Chaser] แมวสะดุดสิ่งกีดขวาง! หมาพุ่งกลับเข้ามาไล่ตามเป็นเวลา {chaseDurationAfterHit} วินาที");
    }

    public void CatchPlayer()
    {
        hasCaughtPlayer = true;
        IsChasing = true;
        currentRetreatRate = 0f;
        SetChaserVisible(true);
        Debug.Log($"[Chaser] หมาพุ่งตะครุบแมวได้! (ระยะถอยขณะชน: {RetreatProgress * 100f:F0}%) GAME OVER!");
    }

    private void SetChaserVisible(bool visible)
    {
        if (visualContainer != null)
        {
            if (visualContainer.activeSelf != visible)
                visualContainer.SetActive(visible);
            return;
        }

        if (chaserRenderers == null) return;
        for (int i = 0; i < chaserRenderers.Length; i++)
        {
            if (chaserRenderers[i] != null)
                chaserRenderers[i].enabled = visible;
        }
    }

    private bool IsVisible()
    {
        if (visualContainer != null) return visualContainer.activeSelf;
        if (chaserRenderers != null && chaserRenderers.Length > 0 && chaserRenderers[0] != null)
            return chaserRenderers[0].enabled;
        return false;
    }
}