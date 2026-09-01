using UnityEngine;
using Unity.Netcode;

public class FallDetector : MonoBehaviour
{
    public Transform player;
    public float minY = -10f;

    void Update()
    {
        if (player == null) return;

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc == null) return;

        bool estaBajoMinimo = player.position.y < minY;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            // Modo offline: respawnear sin restricciones
            if (estaBajoMinimo)
                pc.Respawn();
        }
        else
        {
            // Modo online: solo el dueño
            if (pc.IsOwner && estaBajoMinimo)
                pc.Respawn();
        }
    }
}
