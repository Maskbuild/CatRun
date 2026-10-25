using UnityEngine;

public class ScriptCoin : MonoBehaviour
{
    public float rotateSpeed = 120f;
    public float bobHeight = 0.15f; 
    public float bobSpeed = 2f;      

    private float baseY;
    private bool isMagnetized;
    private Transform magnetTarget;
    private ScriptPlayerController targetPlayer;
    private float basePullSpeed = 22f;

    void OnEnable()
    {
        baseY = transform.position.y;
        isMagnetized = false;
        magnetTarget = null;
        targetPlayer = null;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        if (isMagnetized && magnetTarget != null)
        {
            if (ScriptGameManager.Instance != null && !ScriptGameManager.Instance.IsPlaying)
                return;

            float playerSpeed = targetPlayer != null ? targetPlayer.forwardSpeed : 12f;
            float effectivePullSpeed = Mathf.Max(basePullSpeed, playerSpeed + 16f);

            Vector3 targetPos = magnetTarget.position;
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                effectivePullSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, targetPos) <= 0.75f)
            {
                if (ScriptGameManager.Instance != null)
                {
                    ScriptGameManager.Instance.AddCoin();
                }
                isMagnetized = false;
                gameObject.SetActive(false);
            }
            return;
        }

        float y = baseY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    public void StartMagnetPull(ScriptPlayerController player, float pullSpeed)
    {
        if (player == null) return;
        isMagnetized = true;
        targetPlayer = player;
        magnetTarget = player.transform;
        basePullSpeed = pullSpeed;
    }
}