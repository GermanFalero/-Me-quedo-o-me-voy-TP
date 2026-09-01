using UnityEngine;
using Unity.Netcode;

public class KillZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        // Si no hay red activa (offline), respawnear directamente
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            player.Respawn();
            return;
        }

        // Si hay red, solo el dueño puede respawnearse a sí mismo
        if (player.IsOwner)
        {
            player.Respawn();
        }
    }
}