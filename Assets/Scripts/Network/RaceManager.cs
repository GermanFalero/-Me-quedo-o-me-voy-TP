using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Estado de LA CARRERA (quien va llegando, timer, podio). Es un NetworkBehaviour
/// separado de GameManager a proposito: Netcode prohibe que un NetworkBehaviour
/// comparta GameObject (o jerarquia) con el propio NetworkManager, y GameManager
/// vive junto a el en el objeto "Managers" persistente.
///
/// Este script va como objeto DE ESCENA en cada nivel (NO persistente, NO en
/// Managers) - por ejemplo, lo agrega el ParkourLevelGenerator automaticamente.
/// Al ser un objeto de escena normal, Netcode lo auto-spawnea solo al cargar
/// el nivel, sin configuracion extra. Como se recrea con cada nivel, tampoco
/// hace falta resetear manualmente listas ni contadores entre carreras.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class RaceManager : NetworkBehaviour
{
    public static RaceManager Instance;

    [Header("Espera inicial de jugadores (solo multijugador)")]
    [Tooltip("Segundos de espera al crear la sala antes de que todos puedan moverse.")]
    public float tiempoEsperaInicial = 30f;

    [Header("Tiempo límite de carrera (solo multijugador)")]
    [Tooltip("Si se cumple, la carrera se corta y se muestra el podio con lo que haya. Evita que quede colgada por un jugador AFK o desconectado sin avisar.")]
    public float tiempoLimiteCarreraSegundos = 600f; // 10 minutos

    public NetworkVariable<int> estadoCarrera = new NetworkVariable<int>(0);
    public NetworkVariable<ulong> ganadorId = new NetworkVariable<ulong>(999);

    private readonly List<ulong> ordenLlegada = new List<ulong>();
    private readonly HashSet<ulong> jugadoresEliminados = new HashSet<ulong>();

    private float tiempoTranscurridoCarrera = 0f;
    // Arranca en false: en online, la carrera "de verdad" (con su timer limite)
    // recien empieza cuando termina la cuenta regresiva inicial. Offline la
    // activamos enseguida en Start() (no hay a quien esperar).
    private bool carreraActiva = false;
    private float temporizadorAvisoTiempo = 0f;

    private bool ModoOnline => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (!ModoOnline)
            carreraActiva = true; // offline: no hay espera de jugadores, arranca directo
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
            StartCoroutine(EsperarJugadoresYArrancar());
    }

    private IEnumerator EsperarJugadoresYArrancar()
    {
        float restante = tiempoEsperaInicial;

        while (restante > 0f)
        {
            ActualizarCuentaRegresivaClientRpc(Mathf.CeilToInt(restante));
            yield return new WaitForSeconds(1f);
            restante -= 1f;
        }

        carreraActiva = true;
        EmpezarCarreraClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ActualizarCuentaRegresivaClientRpc(int segundosRestantes)
    {
        UIManager.Instance?.MostrarCuentaRegresiva(segundosRestantes);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void EmpezarCarreraClientRpc()
    {
        UIManager.Instance?.OcultarCuentaRegresiva();
        PlayerController.Jugadores.Find(p => p.IsOwner)?.PermitirMovimiento(true);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!carreraActiva) return;

        // Offline: sos el unico jugador, asi que vos mismo sos la "autoridad".
        // Online: solo el servidor corre el timer real (el resto lo recibe por RPC).
        bool esAutoridad = !ModoOnline || IsServer;
        if (!esAutoridad) return;

        tiempoTranscurridoCarrera += Time.deltaTime;

        temporizadorAvisoTiempo -= Time.deltaTime;
        if (temporizadorAvisoTiempo <= 0f)
        {
            temporizadorAvisoTiempo = 1f;
            float restante = Mathf.Max(0f, tiempoLimiteCarreraSegundos - tiempoTranscurridoCarrera);

            if (ModoOnline)
                ActualizarTiempoClientRpc(restante);
            else
                UIManager.Instance?.ActualizarTimer(restante); // offline: llamada directa, no hace falta RPC
        }

        // El tiempo limite aplica en los dos modos: offline corta tu propia
        // sesion (no tiene sentido dejarte corriendo para siempre), online
        // corta la carrera para todos si alguien quedo AFK o desconectado.
        if (tiempoTranscurridoCarrera >= tiempoLimiteCarreraSegundos)
        {
            carreraActiva = false;
            TerminarCarreraPorTiempo();
        }
    }

    private void TerminarCarreraPorTiempo()
    {
        if (ModoOnline)
        {
            if (ordenLlegada.Count > 0)
                MostrarPodioClientRpc(ordenLlegada.ToArray()); // mostrar a los que si llegaron
            else
                TiempoAgotadoClientRpc(); // nadie llego - un podio vacio no se ve como si pasara algo
        }
        else
        {
            UIManager.Instance?.MostrarTiempoAgotado();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TiempoAgotadoClientRpc()
    {
        UIManager.Instance?.MostrarTiempoAgotado();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ActualizarTiempoClientRpc(float restante)
    {
        UIManager.Instance?.ActualizarTimer(restante);
    }

    /// <summary>Llamar desde FinishLine cuando un jugador cruza la meta (online).</summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayerFinishedServerRpc(ulong clientId)
    {
        if (ordenLlegada.Contains(clientId)) return;

        ordenLlegada.Add(clientId);
        int puesto = ordenLlegada.Count;

        if (puesto == 1)
        {
            ganadorId.Value = clientId;
            estadoCarrera.Value = 1;
        }

        AvisarLlegadaClientRpc(clientId, puesto);
        VerificarFinDeCarrera();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void AvisarLlegadaClientRpc(ulong clientId, int puesto)
    {
        // Aviso NO bloqueante: no pausa nada, la carrera sigue para los que faltan.
        UIManager.Instance?.MostrarLlegadaJugador(clientId, puesto);

        bool esMiPropioJugador = NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == clientId;
        if (esMiPropioJugador)
        {
            PlayerController.Jugadores.Find(p => p.IsOwner)?.DetenerAlLlegar();
            UIManager.Instance?.MostrarEsperandoAOtros(puesto);
        }
        else
        {
            // En la pantalla de los DEMAS jugadores tambien hay que ocultar
            // al que llego - si no, se sigue viendo parado ahi como una estatua.
            PlayerController.Jugadores.Find(p => p.OwnerClientId == clientId)?.OcultarPersonaje();
        }
    }

    /// <summary>
    /// La carrera termina (podio final, pausa para todos) cuando todos los jugadores
    /// que TODAVIA pueden llegar (conectados menos eliminados) ya llegaron.
    /// </summary>
    private void VerificarFinDeCarrera()
    {
        if (NetworkManager.Singleton == null) return;

        int totalJugadores = NetworkManager.Singleton.ConnectedClientsIds.Count;
        int jugadoresQuePuedenLlegar = totalJugadores - jugadoresEliminados.Count;

        if (jugadoresQuePuedenLlegar <= 0) return; // todos eliminados, caso raro

        if (ordenLlegada.Count >= jugadoresQuePuedenLlegar)
        {
            carreraActiva = false;
            MostrarPodioClientRpc(ordenLlegada.ToArray());
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void MostrarPodioClientRpc(ulong[] ordenFinal)
    {
        UIManager.Instance?.MostrarPodioFinal(ordenFinal);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayerEliminatedServerRpc(ulong clientId)
    {
        jugadoresEliminados.Add(clientId);

        foreach (var jugador in PlayerController.Jugadores)
        {
            if (jugador.OwnerClientId == clientId)
            {
                jugador.DesactivarJugadorClientRpc();
                break;
            }
        }

        VerificarFinDeCarrera();
    }

    /// <summary>Llamado por GameManager (server) cuando un jugador se desconecta a mitad de carrera.</summary>
    public void MarcarJugadorDesconectado(ulong clientId)
    {
        if (!IsServer) return;

        jugadoresEliminados.Add(clientId);
        AvisoGeneralClientRpc("Un jugador se desconectó.");
        VerificarFinDeCarrera();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void AvisoGeneralClientRpc(string mensaje)
    {
        UIManager.Instance?.MostrarAviso(mensaje);
    }
}
