using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class SurfaceZone : MonoBehaviour
{
    public enum SurfaceType { Ice, Slow, Boost, Normal }
    public SurfaceType surface = SurfaceType.Normal;

    public float accelerationMultiplier = 1f;
    public float frictionMultiplier = 1f;
    public float speedMultiplier = 1f;

    void Reset()
    {
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
            player.SetSurfaceMultipliers(accelerationMultiplier, frictionMultiplier, speedMultiplier);
    }

    void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
            player.ResetSurfaceMultipliers();
    }
}