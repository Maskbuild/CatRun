using UnityEngine;

public class ScriptCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Offset & Smoothness")]
    public Vector3 offset = new Vector3(0f, 3.5f, -5.5f);
    public float followSmooth = 10f;

    [Range(0f, 1f)]
    [Tooltip("0 = กล้องล็อกตรงกลางราง (ไม่ส่ายตามเลน), 0.35 = ขยับตามเลนเล็กน้อยสไตล์")]
    public float laneFollowWeight = 0.3f;

    void LateUpdate()
    {
        if (target == null) return;

        float desiredX = offset.x + (target.position.x * laneFollowWeight);
        Vector3 desiredPosition = new Vector3(desiredX, offset.y, target.position.z + offset.z);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmooth * Time.deltaTime);
    }
}