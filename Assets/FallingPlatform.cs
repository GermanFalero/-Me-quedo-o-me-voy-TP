using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    public float fallDelay = 0.5f;
    private Rigidbody rb;
    private bool falling = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !falling)
        {
            falling = true;
            Invoke("Fall", fallDelay);
        }
    }

    void Fall()
    {
        if (rb != null) rb.isKinematic = false;
        Destroy(gameObject, 3f);
    }
}
