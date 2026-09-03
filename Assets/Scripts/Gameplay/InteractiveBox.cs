using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Caja interactiva que se puede empujar chocandola. Se desliza como una caja real
/// (no se vuelca) y funciona tanto offline como en multijugador.
///
/// En multijugador, la fisica la simula unicamente el servidor (autoridad), y se
/// sincroniza a los clientes con NetworkTransform. Esto evita que la caja quede
/// en una posicion distinta para cada jugador.
///
/// Configuracion del GameObject en el editor:
/// - BoxCollider: NO trigger.
/// - Physic Material recomendado: Friccion dinamica/estatica ~0.6, Bounciness 0
///   (para que deslice de forma natural y no rebote).
/// - NetworkObject + NetworkTransform: requeridos SOLO si el juego es multijugador.
///   En NetworkTransform activa "Sync Position" (rotacion en Y es opcional).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class InteractiveBox : NetworkBehaviour
{
    [Header("Fisica de la caja")]
    [Tooltip("Peso de la caja. Mas alto = mas dificil de empujar.")]
    [SerializeField] private float masa = 8f;

    [Tooltip("Resistencia al movimiento. Mas alto = frena mas rapido al dejar de empujarla.")]
    [SerializeField] private float resistenciaLineal = 1.5f;

    [Tooltip("Resistencia a girar sobre si misma.")]
    [SerializeField] private float resistenciaAngular = 2f;

    [Tooltip("Si esta activo, la caja solo desliza y no se vuelca al empujarla (recomendado).")]
    [SerializeField] private bool evitarQueSeVuelque = true;

    [Tooltip("Fuerza aplicada cuando un jugador choca contra la caja.")]
    [SerializeField] private float fuerzaDeEmpuje = 10f;

    private Rigidbody rb;

    private bool ModoOnline => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = masa;
        rb.linearDamping = resistenciaLineal;
        rb.angularDamping = resistenciaAngular;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (evitarQueSeVuelque)
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Offline: fisica local normal desde el arranque.
        if (!ModoOnline)
            rb.isKinematic = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Online: solo el servidor simula fisica real. Los clientes quedan kinematic
        // y reciben la posicion sincronizada via NetworkTransform.
        rb.isKinematic = !IsServer;
    }

    /// <summary>
    /// Llamar desde el jugador (PlayerController.OnControllerColliderHit) al chocar la caja.
    /// </summary>
    public void Empujar(Vector3 direccion, Vector3 punto)
    {
        if (ModoOnline)
        {
            if (IsServer)
                AplicarFuerza(direccion, punto);
            else
                EmpujarRpc(direccion, punto);
        }
        else
        {
            AplicarFuerza(direccion, punto);
        }
    }

    [Rpc(SendTo.Server)]
    private void EmpujarRpc(Vector3 direccion, Vector3 punto)
    {
        AplicarFuerza(direccion, punto);
    }

    private void AplicarFuerza(Vector3 direccion, Vector3 punto)
    {
        rb.AddForceAtPosition(direccion * fuerzaDeEmpuje, punto, ForceMode.Force);
    }
}
