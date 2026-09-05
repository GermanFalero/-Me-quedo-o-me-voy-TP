using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Plataforma que rota continuamente sobre un eje. Funciona offline y en multijugador.
///
/// IMPORTANTE - Configuracion del GameObject en el editor:
/// - Collider: NO trigger (solido).
/// - NetworkObject + NetworkTransform: requeridos SOLO si el juego es multijugador.
///   NetworkTransform debe tener "Sync Rotation" activado (la posicion no cambia,
///   asi que podes dejar la sincronizacion de posicion desactivada si queres ahorrar ancho de banda).
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class RotatingPlatform : NetworkBehaviour
{
    [Header("Rotacion")]
    public float rotationSpeed = 25f;
    public Vector3 rotationAxis = Vector3.up;

    // Solo la instancia "autoritativa" (servidor online, o cualquiera si es offline) rota de verdad.
    // El resto ve la rotacion sincronizada por NetworkTransform.
    private bool debeRotar;

    private bool ModoOnline => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Awake()
    {
        // Offline: no hay spawn de red, así que decidimos aca directamente.
        debeRotar = !ModoOnline;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (ModoOnline)
            debeRotar = IsServer;
    }

    private void FixedUpdate()
    {
        if (!debeRotar) return;
        transform.Rotate(rotationAxis, rotationSpeed * Time.fixedDeltaTime, Space.Self);
    }
}