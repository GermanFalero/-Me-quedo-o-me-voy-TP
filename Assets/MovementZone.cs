using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class MovementZone : MonoBehaviour
{
    [Tooltip("Multiplicador de velocidad aplicado al entrar. 1 = normal, 0.5 = mitad de velocidad")]
    public float speedMultiplier = 0.5f;

    void Reset()
    {
        BoxCollider c = GetComponent<BoxCollider>();
        c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.SetSpeedMultiplier(speedMultiplier);
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.ResetSpeedMultiplier();
        }
    }
}
