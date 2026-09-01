using UnityEngine;
using Unity.Netcode;

public class RotatingPlatform : MonoBehaviour
{
    public float rotationSpeed = 25f;

    void Start()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            enabled = false;
            return;
        }
    }

    void FixedUpdate()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.fixedDeltaTime);
    }
}
