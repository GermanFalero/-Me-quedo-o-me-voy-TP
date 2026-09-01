using UnityEngine;
using Unity.Netcode;

public class FinishLine : NetworkBehaviour
{
    public GameObject victoryUI;

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            NotificarVictoriaClientRpc(player.OwnerClientId);
        }
    }

    [ClientRpc]
    void NotificarVictoriaClientRpc(ulong clientId)
    {
        Debug.Log($"¡El jugador {clientId} ganó!");
        if (victoryUI != null) victoryUI.SetActive(true);
        Time.timeScale = 0f;
    }
}
