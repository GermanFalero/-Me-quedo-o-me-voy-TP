using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
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

    private CharacterController cc;
    private Vector3 horizontalVelocity;
    private float verticalVelocity = 0f;
    private float speedMultiplier = 1f;
    private bool isJumping = false;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool wasGrounded = false;

    private Vector3 checkpointPosition;

    private float currentAccelMult = 1f;
    private float currentFrictionMult = 1f;
    private float currentSpeedMult = 1f;

    public float HorizontalSpeed => horizontalVelocity.magnitude;
    public bool IsGrounded => cc != null && cc.isGrounded;

    // Propiedad que determina si este jugador es el dueño (controla input y cámara)
    private bool EsDueno
    {
        get
        {
            // Si no hay NetworkManager o no está escuchando (offline), siempre es dueño
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return true;
            // Si hay red, solo el dueño real
            return IsOwner;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            enabled = true;

            // Asignar cámara local
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
                Camera.main.GetComponent<CameraController>()?.SetTarget(transform);
            }
            else
            {
                Debug.LogError("No se encontró Camera.main en OnNetworkSpawn.");
            }

            // Posicionar en spawn
            Transform spawn = SpawnManager.instance?.GetSpawn((int)OwnerClientId);
            if (spawn != null)
                transform.position = spawn.position;
        }
        else
        {
            enabled = false; // Los jugadores remotos no procesan input
        }
    }

    void Start()
    {
        cc = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (EsDueno && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            Camera.main.GetComponent<CameraController>()?.SetTarget(transform);
        }

        wasGrounded = cc.isGrounded;
        checkpointPosition = transform.position;
    }

    void Update()
    {
        if (!EsDueno) return;

        // Asegurar que la cámara esté asignada
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            Camera.main.GetComponent<CameraController>()?.SetTarget(transform);
        }

        HandleMove();

        // Respawn local con tecla R
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Respawn();
        }
    }

    void HandleMove()
    {
        if (cc.isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;

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

        if (input.magnitude > 0.1f)
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (cameraTransform != null)
            {
                Vector3 forward = cameraTransform.forward;
                forward.y = 0f;
                forward.Normalize();
                Vector3 right = cameraTransform.right;
                right.y = 0f;
                right.Normalize();
                desiredDirection = (forward * input.z + right * input.x).normalized;
            }
        }

        bool isRunning = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        float targetSpeed = (isRunning ? runSpeed : walkSpeed) * speedMultiplier * currentSpeedMult;

        float accel = (cc.isGrounded ? groundAcceleration : airAcceleration) * currentAccelMult;
        float friction = (cc.isGrounded ? groundFriction : airFriction) * currentFrictionMult;

        if (cc.isGrounded)
        {
            if (desiredDirection != Vector3.zero)
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredDirection * targetSpeed, accel * Time.deltaTime);
            else
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, friction * Time.deltaTime);
        }
        else
        {
            if (desiredDirection != Vector3.zero)
            {
                Vector3 airTarget = desiredDirection * targetSpeed * airControlFactor;
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, airTarget, accel * Time.deltaTime);
            }
            else
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, friction * Time.deltaTime);
        }

        bool justLanded = cc.isGrounded && !wasGrounded;
        if (justLanded && enableLandingBoost)
            horizontalVelocity *= landingBoostMultiplier;
        wasGrounded = cc.isGrounded;

        if (jumpBufferTimer > 0 && coyoteTimer > 0)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isJumping = true;
            jumpBufferTimer = 0;
            coyoteTimer = 0;
        }

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
                verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 motion = horizontalVelocity * Time.deltaTime + Vector3.up * verticalVelocity * Time.deltaTime;
        cc.Move(motion);
    }

   void OnControllerColliderHit(ControllerColliderHit hit)
{
    Debug.Log("Colisión con: " + hit.collider.name);
    Rigidbody rb = hit.collider.attachedRigidbody;
    if (rb != null && !rb.isKinematic)
    {
        Debug.Log("Empujando objeto con Rigidbody");
        Vector3 pushDirection = hit.moveDirection;
        pushDirection.y = 0;
        float pushPower = 30f;
        rb.AddForceAtPosition(pushDirection * pushPower, hit.point, ForceMode.Force);
    }
    else
    {
        Debug.Log("El objeto no tiene Rigidbody o es kinematic");
    }
}
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

    public void ExternalJump(float force)
    {
        verticalVelocity = force;
        isJumping = true;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0f, multiplier);
    }

    public void ResetSpeedMultiplier()
    {
        speedMultiplier = 1f;
    }
}