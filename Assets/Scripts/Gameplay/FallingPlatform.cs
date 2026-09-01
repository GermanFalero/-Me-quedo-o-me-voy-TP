using UnityEngine;
using Unity.Netcode;

public class FallingPlatform : MonoBehaviour
{
    public float fallDelay = 0.5f;

    private Rigidbody rb;
    private bool falling = false;

    void Start()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            enabled = false;
            return;
        }

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !falling)
        {
            falling = true;
            Invoke(nameof(Fall), fallDelay);
        }
    }

    void Fall()
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        Destroy(gameObject, 3f);
    }
}