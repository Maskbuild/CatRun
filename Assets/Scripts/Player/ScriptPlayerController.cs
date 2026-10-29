using System.Collections;
using UnityEngine;

public class ScriptPlayerController : MonoBehaviour
{
    [Header("Movement & Speed Progression")]
    public float forwardSpeed = 10f;     
    public float maxForwardSpeed = 24f;    
    public float speedIncreaseRate = 0.15f; 
    public float laneChangeSpeed = 14f;  

    [Header("Jump & Gravity")]
    public float jumpHeight = 2.0f;      
    public float gravity = -25f;        
    public float groundY = 0.5f;
    private float verticalVelocity;
    private bool isGrounded;

    [Header("Obstacle Hit & Blink Effect")]
    public float hitCooldown = 0.8f;    
    public float blinkDuration = 1.5f;     
    public float blinkInterval = 0.1f;   
    private float hitTimer;
    private Collider lastHitObstacle;
    private Renderer[] playerRenderers;
    private Coroutine blinkCoroutine;

    [Header("Power-Ups (Invincibility & Coin Magnet)")]
    public float invincibleDuration = 10f;
    public float invincibleSpeedBoost = 8.0f;
    public float magnetDuration = 10f;
    public float magnetRadius = 7.0f;     
    public float magnetPullSpeed = 24f;     

    [Header("Visual Container & Slime Bouncy Animation")]
    [Tooltip("ลาก GameObject ของโมเดลแมว (ลูกของ Player) มาใส่ หรือปล่อยว่างเพื่อให้หาอัตโนมัติ")]
    public Transform visualModel;
    [Tooltip("ความถี่เด้งตอนวิ่งพื้น (ให้เบากว่าหมา)")]
    public float bobFrequency = 12f;
    [Tooltip("ความสูงเด้งตอนวิ่งพื้น (หมา 0.12 แมว 0.06 เบาสบายตา)")]
    public float bobAmplitude = 0.06f;
    public float tiltAmount = 5f;
    public float runSquashAmount = 0.05f;

    [Header("Slime Jump & Fall Stretch")]
    [Tooltip("ค่ายืดตัวสูงสุดตอนพุ่งขึ้นและตกลงมาแบบสไลม์ (ปรับให้ยืดเห็นชัดเจนขึ้นมาก)")]
    public float maxSlimeStretch = 0.85f;
    [Tooltip("ความเร็วในการคืนตัวของสไลม์")]
    public float slimeRecoverySpeed = 7.5f;

    [HideInInspector] public float invincibleTimer;
    [HideInInspector] public float magnetTimer;

    public bool IsInvincible => invincibleTimer > 0f;
    public bool IsMagnetActive => magnetTimer > 0f;

    public float EffectiveForwardSpeed => forwardSpeed + (IsInvincible ? invincibleSpeedBoost : 0f);

    private CharacterController controller;
    private Collider playerCollider;
    private int currentLane;
    private float targetX;
    private Vector3 initialVisualLocalPos;
    private Vector3 initialVisualScale = Vector3.one;
    private float currentRollTilt;
    private bool wasGroundedLastFrame = true;
    private float landImpactSquash = 0f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerCollider = GetComponent<Collider>();

        playerRenderers = GetComponentsInChildren<Renderer>(true);

        if (visualModel == null)
        {
            // หาโมเดลลูกตัวแรกที่มี Renderer
            Renderer r = GetComponentInChildren<Renderer>();
            if (r != null)
            {
                visualModel = r.transform;
            }
        }

        if (visualModel != null)
        {
            initialVisualLocalPos = visualModel.localPosition;
            initialVisualScale = visualModel.localScale;
        }
    }

    void Start()
    {
        if (groundY <= 0.05f)
        {
            groundY = transform.position.y > 0.1f ? transform.position.y : 0.5f;
        }

        if (controller == null)
        {
            Vector3 startPos = transform.position;
            startPos.y = groundY;
            transform.position = startPos;
        }

        currentLane = GameConstants.CenterLane; 
        targetX = GameConstants.LaneToX(currentLane);
        SetRenderersVisible(true);

        // ดึงสกินที่เลือกไว้มาใช้กับตัวละครอัตโนมัติ (แม้ไม่ได้ลาก ScriptPlayerSkinApplier ใส่ใน Inspector)
        ScriptPlayerSkinApplier skinApplier = GetComponent<ScriptPlayerSkinApplier>();
        if (skinApplier == null)
        {
            skinApplier = gameObject.AddComponent<ScriptPlayerSkinApplier>();
        }
        else
        {
            skinApplier.ApplySelectedSkin();
        }
    }

    void Update()
    {
        if (ScriptGameManager.Instance != null && !ScriptGameManager.Instance.IsPlaying)
        {
            if (ScriptAudioManager.Instance != null)
                ScriptAudioManager.Instance.SetRunningSound(false);
            return;
        }

        if (forwardSpeed < maxForwardSpeed)
        {
            forwardSpeed = Mathf.Min(maxForwardSpeed, forwardSpeed + speedIncreaseRate * Time.deltaTime);
        }

        HandleLaneInput();
        Move();
        UpdateProceduralAnimation();
        UpdateRunningAudio();

        if (invincibleTimer > 0f) invincibleTimer -= Time.deltaTime;
        if (magnetTimer > 0f) magnetTimer -= Time.deltaTime;
        if (hitTimer > 0f) hitTimer -= Time.deltaTime;

        HandleMagnet();
    }

    void OnDisable()
    {
        if (ScriptAudioManager.Instance != null)
        {
            ScriptAudioManager.Instance.SetRunningSound(false);
        }
    }

    void HandleLaneInput()
    {
        int dir = 0;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) dir = -1;
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) dir = 1;

        if (dir != 0)
        {
            currentLane = Mathf.Clamp(currentLane + dir, 0, GameConstants.LaneCount - 1);
            targetX = GameConstants.LaneToX(currentLane);
        }
    }

    void Move()
    {
        Vector3 move = Vector3.zero;
        move.z = EffectiveForwardSpeed;

        float newX = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);
        move.x = (newX - transform.position.x) / Mathf.Max(Time.deltaTime, 0.0001f);

        isGrounded = controller != null ? controller.isGrounded : transform.position.y <= groundY + 0.05f;

        if (isGrounded)
        {
            verticalVelocity = -2f;
            if (controller == null && transform.position.y < groundY) ClampToGround();

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (ScriptAudioManager.Instance != null) ScriptAudioManager.Instance.PlayJump();
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        move.y = verticalVelocity;

        if (controller != null)
        {
            controller.Move(move * Time.deltaTime);
        }
        else
        {
            transform.position += move * Time.deltaTime;
            if (transform.position.y < groundY)
            {
                ClampToGround();
                verticalVelocity = -2f;
                isGrounded = true;
            }
        }
    }

    private void ClampToGround()
    {
        Vector3 clamped = transform.position;
        clamped.y = groundY;
        transform.position = clamped;
    }

    void UpdateRunningAudio()
    {
        if (ScriptAudioManager.Instance == null) return;

        bool isGroundedAndRunning = isGrounded && Time.timeScale > 0f;
        ScriptAudioManager.Instance.SetRunningSound(isGroundedAndRunning);
    }

    void UpdateProceduralAnimation()
    {
        if (visualModel == null) return;

        // เช็คจังหวะตกกระทบพื้น (Landing Impact Squash)
        if (!wasGroundedLastFrame && isGrounded)
        {
            landImpactSquash = 0.55f; // แปะพื้นแล้วยุบตัวแบะออกกว้างขึ้นแบบสไลม์ตกใส่พื้น
        }
        wasGroundedLastFrame = isGrounded;

        // ค่อยๆ คืนรูปจากการแปะพื้น
        landImpactSquash = Mathf.MoveTowards(landImpactSquash, 0f, slimeRecoverySpeed * Time.deltaTime);

        float targetRoll = 0f;
        float diffX = targetX - transform.position.x;
        if (Mathf.Abs(diffX) > 0.05f)
        {
            targetRoll = -Mathf.Sign(diffX) * tiltAmount;
        }
        currentRollTilt = Mathf.Lerp(currentRollTilt, targetRoll, 14f * Time.deltaTime);

        if (isGrounded)
        {
            // ตอนวิ่งบนพื้น: เด้งเบาๆ นุ่มนวลกว่าหมา (หมา amplitude 0.12, แมว 0.06)
            float speedRatio = EffectiveForwardSpeed / 10f;
            float cycle = Time.time * bobFrequency * speedRatio;

            float bounceY = Mathf.Abs(Mathf.Sin(cycle)) * bobAmplitude;
            visualModel.localPosition = initialVisualLocalPos + new Vector3(0f, bounceY, 0f);

            // เอียงสลับตามก้าวเท้าเบาๆ
            float waddleTilt = Mathf.Sin(cycle * 0.5f) * (tiltAmount * 0.4f);
            visualModel.localRotation = Quaternion.Euler(0f, 0f, currentRollTilt + waddleTilt);

            // รวมแรงยุบวิ่งปกติ + แรงแปะพื้นตอนแลนดิ้ง
            float runSquash = Mathf.Sin(cycle) * runSquashAmount;
            float totalSquash = runSquash + landImpactSquash;

            visualModel.localScale = Vector3.Lerp(
                visualModel.localScale,
                new Vector3(
                    initialVisualScale.x * (1f + totalSquash * 0.7f),
                    initialVisualScale.y * (1f - totalSquash),
                    initialVisualScale.z * (1f + totalSquash * 0.7f)
                ),
                18f * Time.deltaTime
            );
        }
        else
        {
            // ตอนกระโดดลอยกลางอากาศ: ยืดตัวยาวขึ้นแบบสไลม์โดนโยน/ตก
            visualModel.localPosition = initialVisualLocalPos;
            visualModel.localRotation = Quaternion.Euler(0f, 0f, currentRollTilt);

            // คิด stretch จากความเร็วดิ่ง ยืดทันทีตั้งแต่ตอนเริ่มดีดตัวขึ้น
            float verticalSpeed = Mathf.Abs(verticalVelocity);
            float stretchFactor = Mathf.Clamp((verticalSpeed / 10f) * maxSlimeStretch, 0f, maxSlimeStretch);

            // ยืดความสูง Y และบีบด้านข้าง X, Z เข้ามาตามมวลสไลม์
            Vector3 targetAirScale = new Vector3(
                initialVisualScale.x * (1f - stretchFactor * 0.45f),
                initialVisualScale.y * (1f + stretchFactor),
                initialVisualScale.z * (1f - stretchFactor * 0.45f)
            );

            visualModel.localScale = Vector3.Lerp(visualModel.localScale, targetAirScale, 20f * Time.deltaTime);
        }
    }

    void HandleMagnet()
    {
        if (!IsMagnetActive) return;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            magnetRadius,
            ~0,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider c in hits)
        {
            if (c.CompareTag("Coin"))
            {
                ScriptCoin coin = c.GetComponent<ScriptCoin>();
                if (coin != null)
                {
                    coin.StartMagnetPull(this, magnetPullSpeed);
                }
                else
                {
                    float pullSpeed = Mathf.Max(magnetPullSpeed, EffectiveForwardSpeed + 16f);
                    c.transform.position = Vector3.MoveTowards(
                        c.transform.position,
                        transform.position,
                        pullSpeed * Time.deltaTime
                    );
                }
            }
        }
    }

    public void ActivateInvincibility() => invincibleTimer = invincibleDuration;
    public void ActivateMagnet() => magnetTimer = magnetDuration;

    public void TriggerBlinkEffect()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        playerRenderers = GetComponentsInChildren<Renderer>(true);

        float elapsed = 0f;
        bool visible = false;

        while (elapsed < blinkDuration)
        {
            if (ScriptGameManager.Instance != null && !ScriptGameManager.Instance.IsPlaying)
            {
                break;
            }

            SetRenderersVisible(visible);
            visible = !visible;

            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        SetRenderersVisible(true);
        blinkCoroutine = null;
    }

    private void SetRenderersVisible(bool visible)
    {
        if (playerRenderers == null) return;
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] != null)
            {
                playerRenderers[i].enabled = visible;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin"))
        {
            if (ScriptGameManager.Instance != null)
                ScriptGameManager.Instance.AddCoin();

            if (ScriptAudioManager.Instance != null)
                ScriptAudioManager.Instance.PlayCoin();

            other.gameObject.SetActive(false);
        }
        else if (other.CompareTag("PowerUp"))
        {
            ScriptPowerUp powerUp = other.GetComponent<ScriptPowerUp>();
            if (powerUp != null)
                powerUp.Apply(this);

            if (ScriptAudioManager.Instance != null)
                ScriptAudioManager.Instance.PlayPowerUp();

            other.gameObject.SetActive(false);
        }
        else if (other.CompareTag("Obstacle"))
        {
            if (IsInvincible) return;

            if (hitTimer > 0f && other == lastHitObstacle) return;
            if (hitTimer > 0f) return;

            hitTimer = hitCooldown;
            lastHitObstacle = other;

            if (ScriptGameManager.Instance != null)
            {
                ScriptGameManager.Instance.OnPlayerHitObstacle(this);
            }
            else
            {
                TriggerBlinkEffect();
            }
        }
    }
}