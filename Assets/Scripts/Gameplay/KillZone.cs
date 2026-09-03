using UnityEngine;

/// <summary>
/// Zona (trigger) gigante que va debajo de todo el mapa. Si el jugador se cae
/// del recorrido, al entrar en contacto se lo respawnea automaticamente en su
/// ultimo checkpoint (usando la logica que ya existe en PlayerController.Respawn()).
/// Configuracion: BoxCollider con Is Trigger activado, bien grande y abajo de todo.
/// </summary>
[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;

        pc.Respawn();
    }
}