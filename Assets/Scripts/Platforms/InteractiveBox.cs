using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class InteractiveBox : NetworkBehaviour
{
    [Header("Empuje")]
    public float fuerzaEmpuje = 12f;
    public float fuerzaMaxima = 25f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Lo llama el PlayerController cuando choca con la caja.
    /// </summary>
    public void Empujar(Vector3 direccion, Vector3 puntoImpacto)
    {
        if (direccion.sqrMagnitude < 0.001f) return;
        direccion.y = 0f;
        direccion.Normalize();

        Vector3 fuerza = direccion * fuerzaEmpuje;
        if (fuerza.magnitude > fuerzaMaxima)
            fuerza = fuerza.normalized * fuerzaMaxima;

        bool online = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        if (!online)
        {
            // Offline: aplicar directo
            AplicarFuerza(fuerza, puntoImpacto);
            return;
        }

        // Online: solo el servidor mueve la caja
        if (IsServer)
            AplicarFuerza(fuerza, puntoImpacto);
        else
            PedirEmpujeServerRpc(fuerza, puntoImpacto);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void PedirEmpujeServerRpc(Vector3 fuerza, Vector3 punto)
    {
        AplicarFuerza(fuerza, punto);
    }

    private void AplicarFuerza(Vector3 fuerza, Vector3 punto)
    {
        if (rb == null) return;
        rb.AddForceAtPosition(fuerza, punto, ForceMode.Impulse);
    }
}
