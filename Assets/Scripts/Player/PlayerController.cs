using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    public static List<PlayerController> Jugadores = new List<PlayerController>();

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

    [Header("Vidas")]
    public int vidasIniciales = 3;
    public NetworkVariable<int> vidas = new NetworkVariable<int>(
        3,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private int vidasOffline;

    [Header("Referencias")]
    public Transform cameraTransform;

    private CharacterController cc;
    private Vector3 horizontalVelocity;
    private float verticalVelocity = 0f;
    private float speedMultiplier = 1f;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool wasGrounded = false;
    private bool invulnerable = false;

    private Vector3 checkpointPosition;

    private float currentAccelMult = 1f;
    private float currentFrictionMult = 1f;
    private float currentSpeedMult = 1f;

    [Header("Plataformas moviles")]
    public float distanciaDeteccionPlataforma = 0.3f;
    public LayerMask capaPlataformas = ~0;

    private MovingPlatform plataformaActual;

    // Offline: true. Online: false hasta que RaceManager permita moverse.
    private bool carreraEmpezada = true;

    public float HorizontalSpeed => horizontalVelocity.magnitude;
    public bool IsGrounded => cc != null && cc.isGrounded;

    public int VidasActuales
    {
        get
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                return vidas.Value;
            return vidasOffline;
        }
    }

    private bool EsDueno
    {
        get
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return true;
            return IsOwner;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!Jugadores.Contains(this))
            Jugadores.Add(this);

        if (IsOwner)
        {
            enabled = true;
            carreraEmpezada = false; // espera cuenta regresiva / RaceManager
            vidas.Value = vidasIniciales;
            vidasOffline = vidasIniciales;

            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
                Camera.main.GetComponent<CameraController>()?.SetTarget(transform);
            }

            Transform spawn = SpawnManager.instance?.GetSpawn((int)OwnerClientId);
            if (spawn != null)
                transform.position = spawn.position;
        }
        else
        {
            enabled = false;
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

        if (cc != null)
            wasGrounded = cc.isGrounded;

        checkpointPosition = transform.position;

        // Offline
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            vidasOffline = vidasIniciales;
            carreraEmpezada = true;

            if (!Jugadores.Contains(this))
                Jugadores.Add(this);
        }
    }

    public void PermitirMovimiento(bool permitido)
    {
        carreraEmpezada = permitido;

        if (!permitido)
        {
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }
    }

    void Update()
    {
        if (!EsDueno) return;
        if (!carreraEmpezada) return;
        if (cc == null || !cc.enabled) return;
        if (Time.timeScale == 0f) return;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            Camera.main.GetComponent<CameraController>()?.SetTarget(transform);
        }

        HandleMove();

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            Respawn();
    }

    void HandleMove()
    {
        if (cc == null || !cc.enabled) return;

        DetectarPlataformaMovil();

        if (cc.isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;

        float moveX = 0f, moveZ = 0f;
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
            {
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, friction * Time.deltaTime);
            }
        }

        bool justLanded = cc.isGrounded && !wasGrounded;
        if (justLanded && enableLandingBoost)
            horizontalVelocity *= landingBoostMultiplier;
        wasGrounded = cc.isGrounded;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        if (cc.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -0.1f;

        if (!cc.isGrounded)
        {
            bool jumpHeld = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;

            if (verticalVelocity > 0f)
            {
                float g = jumpHeld ? gravity * lowGravityMultiplier : gravity * highGravityMultiplier;
                verticalVelocity += g * Time.deltaTime;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }
        }

        Vector3 motion = horizontalVelocity * Time.deltaTime + Vector3.up * verticalVelocity * Time.deltaTime;

        if (plataformaActual != null)
            motion += plataformaActual.DeltaMovimiento;

        cc.Move(motion);
    }

    void DetectarPlataformaMovil()
    {
        plataformaActual = null;
        if (cc == null || !cc.enabled || !cc.isGrounded) return;

        Vector3 origen = transform.position + cc.center + Vector3.up * 0.05f;
        float distancia = (cc.height / 2f) + distanciaDeteccionPlataforma;

        if (Physics.Raycast(origen, Vector3.down, out RaycastHit hit, distancia, capaPlataformas, QueryTriggerInteraction.Ignore))
            plataformaActual = hit.collider.GetComponent<MovingPlatform>();
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y < -0.7f)
            verticalVelocity = -2f;

        InteractiveBox caja = hit.collider.GetComponent<InteractiveBox>();
        if (caja != null)
        {
            Vector3 pushDirection = hit.moveDirection;
            pushDirection.y = 0f;
            if (pushDirection.sqrMagnitude > 0.0001f)
                pushDirection.Normalize();

            caja.Empujar(pushDirection, hit.point);
        }
        else
        {
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                Vector3 pushDirection = hit.moveDirection;
                pushDirection.y = 0f;
                if (pushDirection.sqrMagnitude > 0.0001f)
                    pushDirection.Normalize();

                rb.AddForceAtPosition(pushDirection * 10f, hit.point, ForceMode.Force);
            }
        }

        FallingPlatform plataforma = hit.collider.GetComponent<FallingPlatform>();
        if (plataforma != null && hit.normal.y > 0.5f)
            plataforma.NotificarPisada();
    }

    public void SetCheckpoint(Vector3 newPosition)
    {
        checkpointPosition = newPosition;
    }

    public void Respawn()
    {
        if (invulnerable) return;
        if (VidasActuales <= 0) return;
        if (cc == null) return;

        invulnerable = true;
        Invoke(nameof(QuitarInvulnerable), 1f);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            vidas.Value--;
        else
            vidasOffline--;

        if (VidasActuales <= 0)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                RaceManager.Instance?.PlayerEliminatedServerRpc(OwnerClientId);
            else
                UIManager.Instance?.MostrarDerrota();

            enabled = false;
            OcultarPersonaje();
            return;
        }

        cc.enabled = false;
        transform.position = checkpointPosition;
        cc.enabled = true;

        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        wasGrounded = cc.isGrounded;

        Camera.main?.GetComponent<PlayerCamera>()?.PlayRespawnTransition();
        // Si solo usás CameraController, esta línea puede quedar o borrarse.
    }

    public void OcultarPersonaje()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = false;

        if (cc != null)
            cc.enabled = false;

        GetComponent<PlayerIdentity>()?.OcultarCartel();
    }

    public void DetenerAlLlegar()
    {
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        enabled = false;
        OcultarPersonaje();
    }

    void QuitarInvulnerable()
    {
        invulnerable = false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void DesactivarJugadorClientRpc()
    {
        OcultarPersonaje();

        if (IsOwner)
        {
            enabled = false;
            UIManager.Instance?.MostrarDerrota();
        }
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
        if (!carreraEmpezada) return;
        if (cc == null || !cc.enabled) return;
        verticalVelocity = force;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0f, multiplier);
    }

    public void ResetSpeedMultiplier()
    {
        speedMultiplier = 1f;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        Jugadores.Remove(this);
    }

    public float GetVerticalVelocity()
    {
        return verticalVelocity;
    }
}