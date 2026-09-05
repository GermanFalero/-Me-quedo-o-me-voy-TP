using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Plataforma que se mueve en loop entre dos puntos (A y B).
/// Expone DeltaMovimiento (cuanto se movio en el ultimo FixedUpdate) para que
/// el jugador (con CharacterController) pueda sumarlo a su propio movimiento
/// y no se "resbale" al quedar parado encima.
///
/// Configuracion en el editor:
/// - Collider: NO trigger (solido, para que el jugador se pare encima).
/// - Rigidbody: se agrega y configura solo por codigo (kinematic).
/// - Poné esta plataforma en una Layer separada (ej. "Platforms") para
///   que el raycast del jugador la detecte facil sin chocar con otras cosas.
/// - NetworkObject + NetworkTransform (Sync Position activado): requeridos
///   SOLO si el juego es multijugador, para que el movimiento se vea igual
///   en todos los clientes (el calculo real lo hace unicamente el servidor).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class MovingPlatform : NetworkBehaviour
{
    [Header("Puntos de movimiento")]
    [Tooltip("Punto de inicio. La plataforma arranca en esta posicion.")]
    [SerializeField] private Transform puntoA;

    [Tooltip("Punto de destino.")]
    [SerializeField] private Transform puntoB;

    [Header("Configuracion")]
    [SerializeField] private float velocidad = 3f;
    [Tooltip("Segundos que espera parada en cada punto antes de volver.")]
    [SerializeField] private float esperaEnPuntos = 0.5f;

    private Rigidbody rb;
    private Vector3 destino;
    private Vector3 posicionAnterior;
    private float temporizadorEspera;

    /// <summary>
    /// Cuanto se movio la plataforma en el ultimo FixedUpdate.
    /// El jugador debe sumar esto a su propio movimiento cuando esta parado encima.
    /// </summary>
    public Vector3 DeltaMovimiento { get; private set; }

    // Solo la instancia autoritativa (servidor online, o cualquiera si es offline) mueve de verdad.
    private bool debeMoverse;

    private bool ModoOnline => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (puntoA != null)
            transform.position = puntoA.position;

        destino = puntoB != null ? puntoB.position : transform.position;
        posicionAnterior = transform.position;

        // Offline: no hay spawn de red, asi que decidimos aca directamente.
        debeMoverse = !ModoOnline;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (ModoOnline)
            debeMoverse = IsServer;
    }

    private void FixedUpdate()
    {
        // Solo la instancia con autoridad (servidor online, o cualquiera si es offline)
        // calcula el movimiento real.
        if (debeMoverse && puntoA != null && puntoB != null)
        {
            if (temporizadorEspera > 0f)
            {
                temporizadorEspera -= Time.fixedDeltaTime;
            }
            else
            {
                Vector3 nuevaPosicion = Vector3.MoveTowards(rb.position, destino, velocidad * Time.fixedDeltaTime);
                rb.MovePosition(nuevaPosicion);

                if (Vector3.Distance(nuevaPosicion, destino) < 0.01f)
                {
                    destino = (destino == puntoA.position) ? puntoB.position : puntoA.position;
                    temporizadorEspera = esperaEnPuntos;
                }
            }
        }

        // Esto corre siempre, en TODOS los clientes: mide el desplazamiento real que tuvo
        // el transform (ya sea porque lo movimos nosotros, o porque NetworkTransform lo
        // sincronizo desde el servidor). Asi el jugador se puede "pegar" a la plataforma
        // sin importar en que maquina se esta ejecutando.
        DeltaMovimiento = transform.position - posicionAnterior;
        posicionAnterior = transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        if (puntoA == null || puntoB == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(puntoA.position, puntoB.position);
        Gizmos.DrawWireSphere(puntoA.position, 0.3f);
        Gizmos.DrawWireSphere(puntoB.position, 0.3f);
    }
}