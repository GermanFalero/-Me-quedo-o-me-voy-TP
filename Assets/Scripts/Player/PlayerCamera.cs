using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [Header("Referencias")]
    public Transform playerTransform;
    public PlayerController playerController;

    [Header("Primera Persona")]
    public float eyeHeight = 1.7f;
    public float mouseSensitivity = 0.12f;
    public float maxLookAngle = 87f;
    public bool invertY = false;

    [Header("Suavizado del Mouse")]
    [Range(0f, 0.2f)]
    public float mouseSmoothing = 0.04f;

    [Header("FOV Dinámico")]
    public float normalFOV = 72f;
    public float runFOV = 84f;
    public float airFOV = 90f;
    public float fovSmoothness = 11f;

    [Header("Head Bob")]
    public bool enableHeadBob = true;
    public float walkBobAmount = 0.035f;
    public float runBobAmount = 0.065f;
    public float bobFrequency = 11.5f;

    [Header("Landing Dip")]
    public bool enableLandingDip = true;
    public float landingDipStrength = 0.22f;
    public float landingDipRecovery = 7.5f;

    [Header("Camera Tilt (al girar)")]
    public bool enableTilt = true;
    public float maxTiltAngle = 3.8f;
    public float tiltSpeed = 9f;

    [Header("Camera Shake")]
    public float shakeDecay = 1.6f;
    public float maxShakeIntensity = 0.35f;

    // ================= INTERNOS =================
    private Camera cam;
    private float pitch;
    private float currentFOV;
    private float targetFOV;

    private Vector2 currentMouseDelta;
    private Vector2 smoothedMouseDelta;

    private float bobTimer;
    private float currentBobOffset;
    private float landingOffset;
    private float lastVerticalVelocity;

    private float currentTilt;
    private float shakeIntensity;
    private Vector3 shakeOffset;

    private Vector3 respawnStartPos;
    private Vector3 respawnTargetPos;
    private float respawnTimer;
    private bool isRespawning;
    private const float RESPAWN_DURATION = 0.28f;

    void Start()
    {
        cam = GetComponent<Camera>();
        currentFOV = normalFOV;
        targetFOV = normalFOV;

        LockCursor();
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        HandleCursor();
        HandleMouseLook();
        HandleFOV();
        HandleHeadBob();
        HandleLandingDip();
        HandleTilt();
        HandleShake();
        HandleRespawnTransition();

        ApplyFinalTransform();
    }

    // ===================== INPUT =====================

    void HandleCursor()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MenuPrincipal") return;
        LockCursor();
    }

    void LockCursor()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            Cursor.lockState = CursorLockMode.Locked;
        if (Cursor.visible)
            Cursor.visible = false;
    }

    void HandleMouseLook()
    {
        if (Mouse.current == null) return;

        Vector2 rawDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;
        if (invertY) rawDelta.y = -rawDelta.y;

        if (mouseSmoothing > 0.001f)
        {
            smoothedMouseDelta = Vector2.Lerp(smoothedMouseDelta, rawDelta, 1f - mouseSmoothing);
            currentMouseDelta = smoothedMouseDelta;
        }
        else
        {
            currentMouseDelta = rawDelta;
        }

        // Rotación horizontal del cuerpo
        playerTransform.Rotate(Vector3.up * currentMouseDelta.x);

        // Pitch
        pitch = Mathf.Clamp(pitch - currentMouseDelta.y, -maxLookAngle, maxLookAngle);
    }

    // ===================== FOV =====================

    void HandleFOV()
    {
        if (playerController == null || cam == null) return;

        float speed = playerController.HorizontalSpeed;
        float speedRatio = Mathf.InverseLerp(playerController.walkSpeed, playerController.runSpeed, speed);

        targetFOV = Mathf.Lerp(normalFOV, runFOV, speedRatio);

        // Más FOV en el aire (sensación de velocidad)
        if (!playerController.IsGrounded)
            targetFOV = Mathf.Max(targetFOV, airFOV);

        currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovSmoothness * Time.deltaTime);
        cam.fieldOfView = currentFOV;
    }

    // ===================== HEAD BOB =====================

    void HandleHeadBob()
    {
        if (!enableHeadBob || playerController == null)
        {
            currentBobOffset = Mathf.Lerp(currentBobOffset, 0f, Time.deltaTime * 12f);
            return;
        }

        if (playerController.IsGrounded && playerController.HorizontalSpeed > 1.8f)
        {
            float speedFactor = playerController.HorizontalSpeed / playerController.walkSpeed;
            float amount = playerController.HorizontalSpeed > playerController.walkSpeed + 1.5f
                ? runBobAmount : walkBobAmount;

            bobTimer += Time.deltaTime * bobFrequency * speedFactor;
            currentBobOffset = Mathf.Sin(bobTimer) * amount;
        }
        else
        {
            bobTimer = 0f;
            currentBobOffset = Mathf.Lerp(currentBobOffset, 0f, Time.deltaTime * 11f);
        }
    }

    // ===================== LANDING DIP =====================

    void HandleLandingDip()
    {
        if (!enableLandingDip || playerController == null) return;

        // Necesitamos la velocidad vertical → expónla desde PlayerController
        float verticalVel = playerController.GetVerticalVelocity(); // ← agregaremos este método

        bool justLandedHard = playerController.IsGrounded && lastVerticalVelocity < -11f;

        if (justLandedHard)
        {
            float impact = Mathf.InverseLerp(-11f, -28f, lastVerticalVelocity);
            landingOffset = -landingDipStrength * Mathf.Clamp01(impact);
            
            // También sacudimos un poco la cámara
            AddShake(0.18f + impact * 0.25f);
        }

        landingOffset = Mathf.Lerp(landingOffset, 0f, Time.deltaTime * landingDipRecovery);
        lastVerticalVelocity = verticalVel;
    }

    // ===================== TILT =====================

    void HandleTilt()
    {
        if (!enableTilt) 
        {
            currentTilt = Mathf.Lerp(currentTilt, 0f, Time.deltaTime * tiltSpeed);
            return;
        }

        float targetTilt = -currentMouseDelta.x * 0.35f;
        targetTilt = Mathf.Clamp(targetTilt, -maxTiltAngle, maxTiltAngle);

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);
    }

    // ===================== SHAKE =====================

    void HandleShake()
    {
        if (shakeIntensity > 0.001f)
        {
            shakeOffset = Random.insideUnitSphere * shakeIntensity;
            shakeIntensity = Mathf.Lerp(shakeIntensity, 0f, Time.deltaTime * shakeDecay);
        }
        else
        {
            shakeOffset = Vector3.zero;
            shakeIntensity = 0f;
        }
    }

    public void AddShake(float intensity)
    {
        shakeIntensity = Mathf.Min(shakeIntensity + intensity, maxShakeIntensity);
    }

    // ===================== RESPAWN SUAVE =====================

    public void PlayRespawnTransition()
    {
        if (playerTransform == null) return;

        isRespawning = true;
        respawnTimer = 0f;
        respawnStartPos = transform.position;
        respawnTargetPos = playerTransform.TransformPoint(new Vector3(0f, eyeHeight, 0f));
    }

    void HandleRespawnTransition()
    {
        if (!isRespawning) return;

        respawnTimer += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, respawnTimer / RESPAWN_DURATION);

        // Solo interpolamos la posición durante el respawn
        // (la rotación se actualiza normal)

        if (respawnTimer >= RESPAWN_DURATION)
            isRespawning = false;
    }

    // ===================== APLICAR TRANSFORM =====================

    void ApplyFinalTransform()
    {
        float finalHeight = eyeHeight + currentBobOffset + landingOffset;

        Vector3 basePosition = playerTransform.TransformPoint(new Vector3(0f, finalHeight, 0f));

        if (isRespawning)
        {
            float t = Mathf.SmoothStep(0f, 1f, respawnTimer / RESPAWN_DURATION);
            transform.position = Vector3.Lerp(respawnStartPos, basePosition, t) + shakeOffset;
        }
        else
        {
            transform.position = basePosition + shakeOffset;
        }

        // Rotación final con pitch + tilt
        Quaternion lookRotation = playerTransform.rotation * Quaternion.Euler(pitch, 0f, currentTilt);
        transform.rotation = lookRotation;
    }

    // ===================== API PÚBLICA =====================

    public void SetTarget(Transform nuevoTarget)
    {
        playerTransform = nuevoTarget;
        playerController = nuevoTarget.GetComponent<PlayerController>();
        pitch = 0f;
        currentBobOffset = 0f;
        landingOffset = 0f;
        currentTilt = 0f;
        shakeIntensity = 0f;
    }
}
