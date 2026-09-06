using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Linea de meta. Detecta cuando un jugador la cruza y le avisa a GameManager,
/// que es quien decide el orden de llegada (offline: sos el unico jugador;
/// online: el servidor es la autoridad y arma el podio).
///
/// Configuracion: BoxCollider con Is Trigger activado.
/// </summary>
[RequireComponent(typeof(Collider))]
public class FinishLine : MonoBehaviour
{
    private bool ModoOnline => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;

        AudioManager.Instance?.ReproducirMeta();

        if (ModoOnline)
        {
            // Que cada cliente solo reporte la llegada de SU PROPIO jugador
            // (evita RPCs redundantes por las replicas de otros jugadores en el trigger local).
            if (!pc.IsOwner) return;

            if (RaceManager.Instance == null)
            {
                Debug.LogError("FinishLine: no se encontro un RaceManager en esta escena. Agregalo (o regenera el nivel) para que la meta funcione en multijugador.");
                return;
            }

            RaceManager.Instance.PlayerFinishedServerRpc(pc.OwnerClientId);
        }
        else
        {
            UIManager.Instance?.MostrarVictoria(pc.OwnerClientId);
            pc.OcultarPersonaje();
        }
    }
}