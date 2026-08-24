using UnityEngine;

public class FallDetector : MonoBehaviour
{
    public Transform player;
    public float minY = -5f;

    void Update()
    {
        if (player == null) return;

        if (player.position.y < minY)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.Respawn();
        }
    }
}
