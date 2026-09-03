using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

/// <summary>
/// Linea de meta de la carrera. Detecta al primer jugador que la toca y dispara
/// un evento con el ganador. Funciona offline y en multijugador (la decision de
/// quien gano siempre la toma el servidor, para evitar que dos clientes se
/// declaren ganadores al mismo tiempo por diferencias de latencia).
///
/// Configuracion: BoxCollider con Is Trigger activado, cubriendo el ancho de la meta.
/// NetworkObject: requerido SOLO si el juego es multijugador.
///
/// Enganchá OnJugadorGano desde el Inspector a tu UI / GameManager para mostrar
/// la pantalla de victoria, sin que este script necesite conocer esos sistemas.
/// </summary>
[RequireComponent(typeof(Collider))]
public class FinishLine : NetworkBehaviour
{
    [Header("Evento")]
    [Tooltip("Se dispara una sola vez, con el primer jugador que cruzo la meta.")]
    public UnityEvent<PlayerController> OnJugadorGano;

    private bool carreraTerminada = false;

    private bool ModoOnline => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (carreraTerminada) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;

        if (ModoOnline)
        {
            // Solo el servidor decide quien gano realmente (autoridad),
            // asi evitamos que cada cliente crea que gano el mismo.
            if (IsServer)
                DeclararGanador(pc);
        }
        else
        {
            DeclararGanador(pc);
        }
    }

    private void DeclararGanador(PlayerController ganador)
    {
        if (carreraTerminada) return;
        carreraTerminada = true;

        if (ModoOnline)
            AvisarGanadorRpc(ganador.OwnerClientId);
        else
            OnJugadorGano?.Invoke(ganador);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void AvisarGanadorRpc(ulong clientIdGanador)
    {
        PlayerController ganador = PlayerController.Jugadores.Find(p => p.OwnerClientId == clientIdGanador);
        if (ganador != null)
            OnJugadorGano?.Invoke(ganador);
    }
}