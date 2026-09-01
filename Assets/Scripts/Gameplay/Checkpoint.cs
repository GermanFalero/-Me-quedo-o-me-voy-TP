using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]   // Aseguramos que tenga Rigidbody
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Punto exacto de reaparición (si está vacío, usa la posición del objeto)")]
    public Transform respawnPoint;

    void Reset()
    {
        // Al añadir el script en editor, configuramos automáticamente
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            Vector3 spawnPos = (respawnPoint != null) ? respawnPoint.position : transform.position;
            player.SetCheckpoint(spawnPos);
            Debug.Log("Checkpoint activado: " + spawnPos);
        }
    }
}
