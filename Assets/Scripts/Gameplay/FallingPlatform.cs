using System.Collections;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Plataforma que se cae al ser pisada por el jugador (compatible con CharacterController).
/// Funciona tanto offline como en multijugador (Unity Netcode for GameObjects).
///
/// IMPORTANTE - Configuracion del GameObject en el editor:
/// - Collider: NO trigger (para que el jugador se pare encima).
/// - Rigidbody: normal, el script controla Is Kinematic / Use Gravity por codigo.
/// - NetworkObject: requerido si el objeto va a usarse en una escena multijugador.
/// - NetworkTransform: agregalo para que la caida se sincronice automaticamente
///   a todos los clientes sin necesidad de escribir RPCs de posicion a mano.
///
/// El jugador (PlayerController) debe llamar a NotificarPisada() desde su
/// OnControllerColliderHit cuando toque esta plataforma desde arriba.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class FallingPlatform : NetworkBehaviour
{
    [Header("Configuracion de caida")]
    [Tooltip("Tiempo (segundos) que tarda en caer despues de ser pisada.")]
    [SerializeField] private float delayAntesDeCaer = 1f;

    [Tooltip("Si esta activo, la plataforma tiembla antes de caer para avisar al jugador.")]
    [SerializeField] private bool sacudirAntesDeCaer = true;

    [Tooltip("Intensidad de la sacudida.")]
    [SerializeField] private float intensidadSacudida = 0.05f;

    [Header("Respawn / Reset")]
    [Tooltip("Si esta activo, la plataforma vuelve a aparecer despues de un tiempo.")]
    [SerializeField] private bool respawnear = true;

    [Tooltip("Tiempo (segundos) despues de caer para que la plataforma reaparezca.")]
    [SerializeField] private float tiempoParaRespawn = 3f;

    [Header("Destruccion")]
    [Tooltip("Si esta activo (y respawnear esta desactivado), la plataforma se destruye despues de caer.")]
    [SerializeField] private bool destruirAlCaer = false;

    [Tooltip("Tiempo (segundos) despues de tocar el suelo/caer para destruir el objeto.")]
    [SerializeField] private float tiempoParaDestruir = 5f;

    private Rigidbody rb;
    private Collider col;
    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;
    private bool activada = false;

    // True si el juego esta corriendo en modo multijugador (con NetworkManager activo).
    private bool ModoOnline => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    /// <summary>
    /// Llamar desde el jugador (PlayerController.OnControllerColliderHit) al pisar la plataforma.
    /// Cualquier cliente puede llamarla; la logica real solo se ejecuta en el servidor (online)
    /// o localmente (offline).
    /// </summary>
    public void NotificarPisada()
    {
        if (activada) return;

        if (ModoOnline)
        {
            if (IsServer)
                EmpezarCaida();
            else
                PedirActivacionRpc();
        }
        else
        {
            EmpezarCaida();
        }
    }

    [Rpc(SendTo.Server)]
    private void PedirActivacionRpc()
    {
        EmpezarCaida();
    }

    private void EmpezarCaida()
    {
        if (activada) return;
        activada = true;
        StartCoroutine(SecuenciaDeCaida());
    }

    private IEnumerator SecuenciaDeCaida()
    {
        if (sacudirAntesDeCaer)
            yield return StartCoroutine(Sacudir(delayAntesDeCaer));
        else
            yield return new WaitForSeconds(delayAntesDeCaer);

        Caer();

        if (destruirAlCaer && !respawnear)
        {
            yield return new WaitForSeconds(tiempoParaDestruir);
            DestruirPlataforma();
        }
        else if (respawnear)
        {
            yield return new WaitForSeconds(tiempoParaRespawn);
            ReiniciarPlataforma();
        }
    }

    private IEnumerator Sacudir(float duracion)
    {
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            float offsetX = Random.Range(-intensidadSacudida, intensidadSacudida);
            float offsetZ = Random.Range(-intensidadSacudida, intensidadSacudida);

            transform.position = posicionInicial + new Vector3(offsetX, 0f, offsetZ);

            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        transform.position = posicionInicial;
    }

    private void Caer()
    {
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    private void DestruirPlataforma()
    {
        if (ModoOnline && NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(); // esto ya destruye el objeto en todos los clientes
        else
            Destroy(gameObject);
    }

    private void ReiniciarPlataforma()
    {
        activada = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = posicionInicial;
        transform.rotation = rotacionInicial;

        rb.isKinematic = true;
        rb.useGravity = false;

        if (col != null)
            col.enabled = true;
    }
}