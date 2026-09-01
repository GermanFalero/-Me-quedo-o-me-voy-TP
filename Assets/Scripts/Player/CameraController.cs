using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform playerTransform;
    public PlayerController playerController;

    [Header("Configuración de primera persona")]
    public float eyeHeight = 1.7f;
    public float mouseSensitivity = 0.1f;
    public float maxLookAngle = 85f;
    public bool invertY = false;

    [Header("FOV dinámico")]
    public float normalFOV = 60f;
    public float runFOV = 70f;
    public float fovSmoothness = 8f;

    private float pitch = 0f;
    private float currentFOV;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        currentFOV = normalFOV;
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MenuPrincipal")
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        float mx = 0f;
        float my = 0f;

        if (Mouse.current != null)
        {
            mx = Mouse.current.delta.x.ReadValue() * mouseSensitivity;
            my = Mouse.current.delta.y.ReadValue() * mouseSensitivity;
            if (invertY) my = -my;
        }

        playerTransform.Rotate(Vector3.up * mx);

        pitch = Mathf.Clamp(pitch - my, -maxLookAngle, maxLookAngle);

        Vector3 headPosition = playerTransform.TransformPoint(new Vector3(0, eyeHeight, 0));
        transform.position = headPosition;

        transform.rotation = playerTransform.rotation * Quaternion.Euler(pitch, 0, 0);

        if (playerController != null && cam != null)
        {
            float currentSpeed = playerController.HorizontalSpeed;
            float speedRatio = 0f;
            if (playerController.runSpeed > playerController.walkSpeed)
                speedRatio = Mathf.InverseLerp(playerController.walkSpeed, playerController.runSpeed, currentSpeed);

            float targetFOV = Mathf.Lerp(normalFOV, runFOV, speedRatio);
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovSmoothness * Time.deltaTime);
            cam.fieldOfView = currentFOV;
        }
    }

    public void SetTarget(Transform nuevoTarget)
    {
        playerTransform = nuevoTarget;
        playerController = nuevoTarget.GetComponent<PlayerController>();
        pitch = 0f;
    }
}