using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float groundAcceleration = 30f;
    public float airAcceleration = 8f;
    public float groundFriction = 25f;
    public float airFriction = 1f;

    [Header("Salto")]
    public float jumpHeight = 2.5f;
    public float gravity = -25f;
    public float lowGravityMultiplier = 0.5f;
    public float highGravityMultiplier = 2f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;
    public float airControlFactor = 0.6f;

    [Header("Aterrizaje")]
    public bool enableLandingBoost = true;
    public float landingBoostMultiplier = 0.8f;

    [Header("Referencias")]
    public Transform cameraTransform;

    // Componentes internos
    private CharacterController cc;
    private Vector3 horizontalVelocity;
    private float verticalVelocity = 0f;
    private float speedMultiplier = 1f;
    private bool isJumping = false;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool wasGrounded = false;

    // Checkpoint
    private Vector3 checkpointPosition;

    // Multiplicadores de superficie
    private float currentAccelMult = 1f;
    private float currentFrictionMult = 1f;
    private float currentSpeedMult = 1f;

    // Propiedades públicas
    public float HorizontalSpeed => horizontalVelocity.magnitude;
    public bool IsGrounded => cc != null && cc.isGrounded;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
        wasGrounded = cc.isGrounded;

        // Checkpoint inicial en la posición de inicio
        checkpointPosition = transform.position;
    }

    void Update()
    {
        HandleMove();

        // Reinicio manual al presionar R
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Respawn();
        }
    }

    void HandleMove()
    {
        // ----- Coyote time y jump buffer -----
        if (cc.isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;

        // ----- Entrada de movimiento -----
        float moveX = 0f;
        float moveZ = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveZ = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveZ = -1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX = 1f;
        }

        Vector3 input = new Vector3(moveX, 0f, moveZ);
        Vector3 desiredDirection = Vector3.zero;

        if (input.magnitude > 0.1f && cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();
            desiredDirection = (forward * input.z + right * input.x).normalized;
        }

        bool isRunning = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        float targetSpeed = (isRunning ? runSpeed : walkSpeed) * speedMultiplier * currentSpeedMult;

        // ----- Aceleración / fricción con multiplicadores de superficie -----
        float accel = (cc.isGrounded ? groundAcceleration : airAcceleration) * currentAccelMult;
        float friction = (cc.isGrounded ? groundFriction : airFriction) * currentFrictionMult;

        if (cc.isGrounded)
        {
            if (desiredDirection != Vector3.zero)
            {
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    desiredDirection * targetSpeed,
                    accel * Time.deltaTime
                );
            }
            else
            {
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    Vector3.zero,
                    friction * Time.deltaTime
                );
            }
        }
        else
        {
            if (desiredDirection != Vector3.zero)
            {
                Vector3 airTarget = desiredDirection * targetSpeed * airControlFactor;
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    airTarget,
                    accel * Time.deltaTime
                );
            }
            else
            {
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    Vector3.zero,
                    friction * Time.deltaTime
                );
            }
        }

        // ----- Detección de aterrizaje -----
        bool justLanded = cc.isGrounded && !wasGrounded;
        if (justLanded && enableLandingBoost)
        {
            horizontalVelocity *= landingBoostMultiplier;
        }
        wasGrounded = cc.isGrounded;

        // ----- Salto -----
        if (jumpBufferTimer > 0 && coyoteTimer > 0)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isJumping = true;
            jumpBufferTimer = 0;
            coyoteTimer = 0;
        }

        // ----- Gravedad -----
        if (cc.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -0.1f;
            isJumping = false;
        }

        if (!cc.isGrounded)
        {
            bool jumpHeld = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;

            if (isJumping && verticalVelocity > 0f)
            {
                float g = jumpHeld ? gravity * lowGravityMultiplier : gravity * highGravityMultiplier;
                verticalVelocity += g * Time.deltaTime;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }
        }

        // ----- Aplicar movimiento -----
        Vector3 motion = horizontalVelocity * Time.deltaTime + Vector3.up * verticalVelocity * Time.deltaTime;

        int steps = 4; // Número de sub-pasos
        for (int i = 0; i < steps; i++)
        {
        cc.Move(motion / steps);
    }
    }

    // ----- Colisiones con objetos físicos -----
    void OnControllerColliderHit(ControllerColliderHit hit){
    // --- Cancelar salto al golpear un techo ---
    // Si la normal del impacto apunta hacia abajo, significa que chocamos contra algo arriba.
    if (hit.normal.y < -0.7f)
    {
        isJumping = false;          // Termina el estado de salto
        verticalVelocity = -2f;     // Pequeño impulso hacia abajo para despegarse del techo
    }

    // --- Empujar objetos con Rigidbody ---
    Rigidbody rb = hit.collider.attachedRigidbody;

    if (rb != null && !rb.isKinematic)
    {
        Vector3 pushDirection = hit.moveDirection;
        pushDirection.y = 0;        // No empujar hacia arriba/abajo

        float pushPower = 10f;      // Ajustá este valor según la masa de los objetos
        rb.AddForceAtPosition(pushDirection * pushPower, hit.point, ForceMode.Force);
    }}

    // ----- Métodos para checkpoints y respawn -----
    public void SetCheckpoint(Vector3 newPosition)
    {
        checkpointPosition = newPosition;
    }

    public void Respawn()
    {
        cc.enabled = false;
        transform.position = checkpointPosition;
        cc.enabled = true;

        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        isJumping = false;
        wasGrounded = cc.isGrounded;
    }

    // ----- Métodos para superficies -----
    public void SetSurfaceMultipliers(float accelMult, float frictionMult, float speedMult)
    {
        currentAccelMult = accelMult;
        currentFrictionMult = frictionMult;
        currentSpeedMult = speedMult;
    }

    public void ResetSurfaceMultipliers()
    {
        currentAccelMult = 1f;
        currentFrictionMult = 1f;
        currentSpeedMult = 1f;
    }

    // ----- Método para trampolines -----
    public void ExternalJump(float force)
    {
        verticalVelocity = force;
        isJumping = true;
    }

    // ----- Métodos para modificar velocidad desde otros scripts -----
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0f, multiplier);
    }

    public void ResetSpeedMultiplier()
    {
        speedMultiplier = 1f;
    }
}