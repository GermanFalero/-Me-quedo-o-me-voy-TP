using UnityEngine;

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
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MovingPlatform : MonoBehaviour
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
    }

    private void FixedUpdate()
    {
        if (puntoA == null || puntoB == null)
        {
            DeltaMovimiento = Vector3.zero;
            return;
        }

        if (temporizadorEspera > 0f)
        {
            temporizadorEspera -= Time.fixedDeltaTime;
            DeltaMovimiento = Vector3.zero;
            return;
        }

        Vector3 nuevaPosicion = Vector3.MoveTowards(rb.position, destino, velocidad * Time.fixedDeltaTime);
        rb.MovePosition(nuevaPosicion);

        DeltaMovimiento = nuevaPosicion - posicionAnterior;
        posicionAnterior = nuevaPosicion;

        if (Vector3.Distance(nuevaPosicion, destino) < 0.01f)
        {
            destino = (destino == puntoA.position) ? puntoB.position : puntoA.position;
            temporizadorEspera = esperaEnPuntos;
        }
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