using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    // Lista estática para que GameManager pueda encontrar a los jugadores
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

    // Variable local para offline
    private int vidasOffline;

    [Header("Identidad del jugador")]
    [Tooltip("Colores asignados por orden de llegada (indice = OwnerClientId % cantidad). Poné al menos 5 para que nunca se repitan con 5 jugadores.")]
    public Color[] paletaColores = new Color[]
    {
        new Color(0.95f, 0.3f, 0.3f),  // rojo
        new Color(0.3f, 0.55f, 0.95f), // azul
        new Color(0.3f, 0.9f, 0.4f),   // verde
        new Color(0.95f, 0.85f, 0.2f), // amarillo
        new Color(0.75f, 0.3f, 0.95f), // violeta
    };

    public NetworkVariable<FixedString32Bytes> nombreJugador = new NetworkVariable<FixedString32Bytes>(
        "Jugador",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public NetworkVariable<Color> colorJugador = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    // Version offline (no hay NetworkVariable funcionando sin NetworkManager activo)
    public string NombreOffline { get; private set; } = "Jugador";
    public Color ColorOffline { get; private set; } = Color.white;

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
    [Tooltip("Distancia del raycast hacia abajo para detectar si hay una plataforma movil debajo.")]
    public float distanciaDeteccionPlataforma = 0.3f;

    [Tooltip("Layer(s) donde estan las plataformas moviles. Dejalo en Everything si no usas layers separadas.")]
    public LayerMask capaPlataformas = ~0;

    private MovingPlatform plataformaActual;

    public float HorizontalSpeed => horizontalVelocity.magnitude;
    public bool IsGrounded => cc != null && cc.isGrounded;

    // Propiedad que devuelve las vidas actuales según el modo
    public int VidasActuales
    {
        get
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                return vidas.Value;
            else
                return vidasOffline;
        }
    }

    // Determina si este jugador es controlable localmente
    private bool EsDueno
    {
        get
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return true; // Offline
            return IsOwner;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Jugadores.Add(this);

        if (IsOwner)
        {
            enabled = true;
            vidas.Value = vidasIniciales;
            vidasOffline = vidasIniciales;

            string nombreElegido = PlayerPrefs.GetString("NombreJugador", $"Jugador {OwnerClientId}");
            nombreJugador.Value = nombreElegido;
            colorJugador.Value = paletaColores[(int)(OwnerClientId % (ulong)paletaColores.Length)];

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

        wasGrounded = cc.isGrounded;
        checkpointPosition = transform.position;

        // Inicializar vidas en offline
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            vidasOffline = vidasIniciales;
            NombreOffline = PlayerPrefs.GetString("NombreJugador", "Jugador");
            ColorOffline = paletaColores.Length > 0 ? paletaColores[0] : Color.white;
        }
    }

    /// <summary>Nombre a mostrar, sea online (NetworkVariable) u offline (campo local).</summary>
    public string NombreActual => (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        ? nombreJugador.Value.ToString()
        : NombreOffline;

    /// <summary>Color a mostrar, sea online (NetworkVariable) u offline (campo local).</summary>
    public Color ColorActual => (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        ? colorJugador.Value
        : ColorOffline;

    void Update()
    {
        if (!EsDueno) return;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            Camera.main.GetComponent<CameraController>()?.SetTarget(transform);
        }

        HandleMove();

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Respawn();
        }
    }

    void HandleMove()
    {
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
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, friction * Time.deltaTime);
        }

        bool justLanded = cc.isGrounded && !wasGrounded;
        if (justLanded && enableLandingBoost)
            horizontalVelocity *= landingBoostMultiplier;
        wasGrounded = cc.isGrounded;

        if (jumpBufferTimer > 0 && coyoteTimer > 0)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferTimer = 0;
            coyoteTimer = 0;
        }

        if (cc.isGrounded && verticalVelocity < 0)
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
                verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 motion = horizontalVelocity * Time.deltaTime + Vector3.up * verticalVelocity * Time.deltaTime;

        // Si estamos parados sobre una plataforma movil, sumamos su desplazamiento
        // para que el jugador se mueva junto con ella (CharacterController no lo hace solo).
        if (plataformaActual != null)
            motion += plataformaActual.DeltaMovimiento;

        cc.Move(motion);
    }

    /// <summary>
    /// Chequea con un raycast hacia abajo si el jugador esta parado sobre una MovingPlatform.
    /// </summary>
    void DetectarPlataformaMovil()
    {
        plataformaActual = null;

        if (!cc.isGrounded) return;

        Vector3 origen = transform.position + cc.center + Vector3.up * 0.05f;
        float distancia = (cc.height / 2f) + distanciaDeteccionPlataforma;

        if (Physics.Raycast(origen, Vector3.down, out RaycastHit hit, distancia, capaPlataformas, QueryTriggerInteraction.Ignore))
        {
            plataformaActual = hit.collider.GetComponent<MovingPlatform>();
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y < -0.7f)
            verticalVelocity = -2f;

        InteractiveBox caja = hit.collider.GetComponent<InteractiveBox>();
        if (caja != null)
        {
            // OJO: hit.moveDirection NO es un vector normalizado (es el desplazamiento
            // de este frame, muy chico). Hay que normalizarlo o la fuerza real es casi nula.
            Vector3 pushDirection = hit.moveDirection;
            pushDirection.y = 0;
            if (pushDirection.sqrMagnitude > 0.0001f)
                pushDirection.Normalize();

            caja.Empujar(pushDirection, hit.point);
        }
        else
        {
            // Fallback generico para otros Rigidbodies empujables no controlados por red.
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                Vector3 pushDirection = hit.moveDirection;
                pushDirection.y = 0;
                if (pushDirection.sqrMagnitude > 0.0001f)
                    pushDirection.Normalize();
                float pushPower = 10f;
                rb.AddForceAtPosition(pushDirection * pushPower, hit.point, ForceMode.Force);
            }
        }

        // Avisar a plataformas que se caen al ser pisadas
        FallingPlatform plataforma = hit.collider.GetComponent<FallingPlatform>();
        if (plataforma != null && hit.normal.y > 0.5f) // solo si lo pisamos desde arriba
        {
            plataforma.NotificarPisada();
        }
    }

    public void SetCheckpoint(Vector3 newPosition)
    {
        checkpointPosition = newPosition;
    }

    public void Respawn()
    {
        if (invulnerable) return;
        if (VidasActuales <= 0) return;

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
            return;
        }

        cc.enabled = false;
        transform.position = checkpointPosition;
        cc.enabled = true;

        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        wasGrounded = cc.isGrounded;

        Camera.main?.GetComponent<PlayerCamera>()?.PlayRespawnTransition();
    }

    /// <summary>
    /// Llamar cuando ESTE jugador cruza la meta: deja de poder moverse mientras
    /// espera a que terminen los demas, sin pausar el juego para nadie mas.
    /// </summary>
    public void DetenerAlLlegar()
    {
        enabled = false;
    }

    void QuitarInvulnerable()
    {
        invulnerable = false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void DesactivarJugadorClientRpc()
    {
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