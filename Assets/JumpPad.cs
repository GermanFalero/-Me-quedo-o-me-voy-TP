using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class JumpPad : MonoBehaviour
{
    public float jumpForce = 20f;

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
            player.ExternalJump(jumpForce);
    }
}
