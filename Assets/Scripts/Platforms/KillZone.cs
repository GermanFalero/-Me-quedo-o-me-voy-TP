using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Zona (trigger) gigante que va debajo de todo el mapa. Si el jugador se cae
/// del recorrido, al entrar en contacto se lo respawnea automaticamente en su
/// ultimo checkpoint (usando la logica que ya existe en PlayerController.Respawn()).
/// Configuracion: BoxCollider con Is Trigger activado, bien grande y abajo de todo.
/// </summary>
[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
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

        // IMPORTANTE: Respawn() modifica vidas.Value, que es un NetworkVariable
        // de escritura exclusiva del dueno. Si no filtramos esto, cualquier
        // cliente que vea la replica de OTRO jugador caer intentaria escribir
        // una variable de red que no le pertenece, y Netcode lo rechaza.
        if (ModoOnline && !pc.IsOwner) return;

        pc.Respawn();
    }
}