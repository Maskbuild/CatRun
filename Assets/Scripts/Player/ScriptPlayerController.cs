using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
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
    private float verticalVelocity;

    [Header("Obstacle Hit & Blink Effect (ตัวกะพริบเมื่อชนสิ่งกีดขวาง)")]
    public float hitCooldown = 0.8f;     
    public float blinkDuration = 1.5f;    
    public float blinkInterval = 0.1f;   
    private float hitTimer;
    private Collider lastHitObstacle;
    private Renderer[] playerRenderers;
    private Coroutine blinkCoroutine;

    [Header("Power-Ups (Invincibility & Coin Magnet)")]
    public float invincibleDuration = 10f;
    public float magnetDuration = 10f;
    public float magnetRadius = 7.0f;      
    public float magnetPullSpeed = 24f;    

    [Header("Optional Animation Support (ใส่เมื่อมีโมเดลพร้อม Animator)")]
    [Tooltip("ลาก Animator ของโมเดลแมวมาใส่ (ถ้ายังใช้ Capsule ธรรมดา ให้ปล่อยว่างไว้ได้เลย)")]
    public Animator playerAnimator;

    [HideInInspector] public float invincibleTimer;
    [HideInInspector] public float magnetTimer;

    public bool IsInvincible => invincibleTimer > 0f;
    public bool IsMagnetActive => magnetTimer > 0f;

    private CharacterController controller;
    private int currentLane;
    private float targetX;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerRenderers = GetComponentsInChildren<Renderer>(true);
        if (playerAnimator == null)
        {
            playerAnimator = GetComponentInChildren<Animator>();
        }
    }

    void Start()
    {
        currentLane = GameConstants.CenterLane;
        targetX = GameConstants.LaneToX(currentLane);
        SetRenderersVisible(true);
    }

    void Update()
    {
        if (ScriptGameManager.Instance != null && !ScriptGameManager.Instance.IsPlaying)
            return;

        if (forwardSpeed < maxForwardSpeed)
        {
            forwardSpeed = Mathf.Min(maxForwardSpeed, forwardSpeed + speedIncreaseRate * Time.deltaTime);
        }

        HandleLaneInput();
        Move();
        UpdateAnimatorParameters();

        if (invincibleTimer > 0f) invincibleTimer -= Time.deltaTime;
        if (magnetTimer > 0f) magnetTimer -= Time.deltaTime;
        if (hitTimer > 0f) hitTimer -= Time.deltaTime;

        HandleMagnet();
    }

    void HandleLaneInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            currentLane = Mathf.Max(0, currentLane - 1);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            currentLane = Mathf.Min(GameConstants.LaneCount - 1, currentLane + 1);
        }

        targetX = GameConstants.LaneToX(currentLane);
    }

    void Move()
    {
        Vector3 move = Vector3.zero;

        move.z = forwardSpeed;

        float newX = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);
        move.x = (newX - transform.position.x) / Mathf.Max(Time.deltaTime, 0.0001f);

        if (controller.isGrounded)
        {
            verticalVelocity = -2f; 
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (playerAnimator != null)
                {
                    playerAnimator.SetTrigger("Jump");
                }
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    void UpdateAnimatorParameters()
    {
        if (playerAnimator == null) return;
        playerAnimator.SetBool("IsGrounded", controller.isGrounded);
        playerAnimator.SetFloat("Speed", forwardSpeed);
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
                    float pullSpeed = Mathf.Max(magnetPullSpeed, forwardSpeed + 16f);
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
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Hit");
        }

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

            other.gameObject.SetActive(false);
        }
        else if (other.CompareTag("PowerUp"))
        {
            ScriptPowerUp powerUp = other.GetComponent<ScriptPowerUp>();
            if (powerUp != null)
                powerUp.Apply(this);

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