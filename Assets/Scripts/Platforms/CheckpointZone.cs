using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Zona (trigger) que actualiza el checkpoint del jugador al atravesarla.
/// Configuracion: BoxCollider con Is Trigger activado.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CheckpointZone : MonoBehaviour
{
    [Tooltip("Punto exacto donde va a reaparecer el jugador. Si lo dejas vacio, usa la posicion de este objeto.")]
    [SerializeField] private Transform puntoDeRespawn;

    private bool ModoOnline => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;

        // SetCheckpoint solo afecta una variable local (no es de red), pero igual
        // filtramos para no hacer trabajo de mas con las replicas de otros jugadores.
        if (ModoOnline && !pc.IsOwner) return;

        Vector3 posicion = puntoDeRespawn != null ? puntoDeRespawn.position : transform.position;
        pc.SetCheckpoint(posicion);
    }
}
