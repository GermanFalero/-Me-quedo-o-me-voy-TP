using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

        // Si está pausado, no tocamos ni el cursor ni la cámara
        if (Time.timeScale == 0f)
            return;

        // Si estamos en el menú, no bloqueamos el cursor
        if (SceneManager.GetActiveScene().name == "MenuPrincipal")
            return;

        // Bloquear cursor solo cuando se está jugando
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Lectura del mouse
        float mx = 0f;
        float my = 0f;

        if (Mouse.current != null)
        {
            mx = Mouse.current.delta.x.ReadValue() * mouseSensitivity;
            my = Mouse.current.delta.y.ReadValue() * mouseSensitivity;
            if (invertY) my = -my;
        }

        // Rotación del jugador (horizontal)
        playerTransform.Rotate(Vector3.up * mx);

        // Mirar arriba/abajo
        pitch = Mathf.Clamp(pitch - my, -maxLookAngle, maxLookAngle);

        // Posición de la cámara
        Vector3 headPosition = playerTransform.TransformPoint(new Vector3(0f, eyeHeight, 0f));
        transform.position = headPosition;

        // Rotación de la cámara
        transform.rotation = playerTransform.rotation * Quaternion.Euler(pitch, 0f, 0f);

        // FOV dinámico
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