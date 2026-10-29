using UnityEngine;

public class ScriptPowerUp : MonoBehaviour
{
    public enum PowerUpType { Invincibility, CoinMagnet }

    public PowerUpType type = PowerUpType.Invincibility;
    public float rotateSpeed = 60f;

    void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }

    public void Apply(ScriptPlayerController player)
    {
        switch (type)
        {
            case PowerUpType.Invincibility:
                player.ActivateInvincibility();
                break;

            case PowerUpType.CoinMagnet:
                player.ActivateMagnet();
                break;
        }
    }
}