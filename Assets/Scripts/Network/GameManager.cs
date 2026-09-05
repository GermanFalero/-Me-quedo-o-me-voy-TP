using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections;

/// <summary>
/// Manager de sesion/menu: navegacion, seleccion de nivel, arrancar offline u
/// online, conexion y desconexion. A PROPOSITO NO es NetworkBehaviour: Netcode
/// prohibe que cualquier NetworkBehaviour comparta GameObject (o jerarquia) con
/// el propio NetworkManager, y este objeto vive junto a el en "Managers".
///
/// El estado de LA CARRERA en si (quien va llegando, timer, podio) vive en
/// RaceManager, que es un NetworkBehaviour aparte, ubicado como objeto DE ESCENA
/// en cada nivel (no persistente) para no chocar con esta restriccion.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Niveles disponibles")]
    public NivelInfo[] niveles;
    private int nivelSeleccionado = 0;

    [Header("Configuración de escenas")]
    public string escenaMenu = "MenuPrincipal";

    [Header("Jugador offline")]
    public GameObject jugadorOfflinePrefab;

    [Header("Red")]
    public string ipPorDefecto = "127.0.0.1";
    public ushort puerto = 7777;
    public int maxJugadores = 5;

    [Header("Descubrimiento LAN (opcional)")]
    public LanDiscovery lanDiscovery;
    public string nombreSala = "Sala de Parkour";

    [System.Serializable]
    public class NivelInfo
    {
        public string nombreVisible;
        public string nombreEscena;
        public Sprite miniatura;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += AprobarConexion;
            NetworkManager.Singleton.OnClientDisconnectCallback += ManejarDesconexion;
            NetworkManager.Singleton.OnClientConnectedCallback += ManejarConexionExitosa;

            if (NetworkManager.Singleton.SceneManager != null)
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += ManejarCargaCompleta;
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= AprobarConexion;
            NetworkManager.Singleton.OnClientDisconnectCallback -= ManejarDesconexion;
            NetworkManager.Singleton.OnClientConnectedCallback -= ManejarConexionExitosa;

            if (NetworkManager.Singleton.SceneManager != null)
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= ManejarCargaCompleta;
        }
    }

    private void ManejarConexionExitosa(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
            UIManager.Instance?.MostrarCargando("Conectado. Cargando nivel...");
    }

    private void ManejarCargaCompleta(string nombreEscena, LoadSceneMode modo, System.Collections.Generic.List<ulong> completados, System.Collections.Generic.List<ulong> conTimeout)
    {
        UIManager.Instance?.OcultarCargando();
    }

    // ====================== SELECCION DE NIVEL ======================

    /// <summary>Llamar desde la UI de seleccion de nivel antes de jugar offline o crear sala.</summary>
    public void SeleccionarNivel(int index)
    {
        if (niveles == null || niveles.Length == 0) return;
        nivelSeleccionado = Mathf.Clamp(index, 0, niveles.Length - 1);
    }

    public NivelInfo NivelActual => (niveles != null && niveles.Length > 0) ? niveles[nivelSeleccionado] : null;

    // ====================== OFFLINE ======================

    public void JugarOffline()
    {
        if (NivelActual == null)
        {
            Debug.LogError("No hay niveles configurados en GameManager.");
            return;
        }

        StartCoroutine(CargarNivelOfflineAsync());
    }

    private IEnumerator CargarNivelOfflineAsync()
    {
        UIManager.Instance?.MostrarCargando("Cargando nivel...");

        AsyncOperation carga = SceneManager.LoadSceneAsync(NivelActual.nombreEscena);
        while (!carga.isDone)
            yield return null;

        yield return InstanciarJugadorOffline();

        UIManager.Instance?.OcultarCargando();
    }

    IEnumerator InstanciarJugadorOffline()
    {
        if (jugadorOfflinePrefab == null)
        {
            Debug.LogError("No se asignó jugadorOfflinePrefab en GameManager.");
            yield break;
        }

        // Salvaguarda: esperamos hasta 1 segundo a que el SpawnManager tenga
        // los spawn points de la escena nueva listos.
        float esperaMaxima = 1f;
        while (SpawnManager.instance == null || SpawnManager.instance.GetSpawn(0) == null)
        {
            esperaMaxima -= Time.unscaledDeltaTime;
            if (esperaMaxima <= 0f) break;
            yield return null;
        }

        GameObject jugador = Instantiate(jugadorOfflinePrefab);
        NetworkObject no = jugador.GetComponent<NetworkObject>();
        if (no != null) no.enabled = false;

        Transform spawn = SpawnManager.instance?.GetSpawn(0);
        if (spawn != null)
            jugador.transform.position = spawn.position;
        else
            Debug.LogWarning("No se encontraron spawn points en la escena. El jugador aparecio en (0,0,0).");

        Camera.main?.GetComponent<CameraController>()?.SetTarget(jugador.transform);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ====================== MULTIJUGADOR ======================

    public void CrearSala()
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.IsListening) return;
        if (NivelActual == null)
        {
            Debug.LogError("No hay niveles configurados en GameManager.");
            return;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
            transport.SetConnectionData("0.0.0.0", puerto);

        if (NetworkManager.Singleton.StartHost())
        {
            UIManager.Instance?.MostrarCargando("Creando sala...");
            NetworkManager.Singleton.SceneManager.LoadScene(NivelActual.nombreEscena, LoadSceneMode.Single);
            lanDiscovery?.EmpezarAAnunciar(nombreSala);
        }
    }

    public void UnirseASala(string ip)
    {
        if (string.IsNullOrEmpty(ip)) ip = ipPorDefecto;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
            transport.SetConnectionData(ip, puerto);

        UIManager.Instance?.MostrarCargando("Conectando...");
        NetworkManager.Singleton.StartClient();
    }

    /// <summary>
    /// Limita la sala a maxJugadores. El host elige el nivel, asi que los clientes
    /// no necesitan (ni pueden) elegir nivel por su cuenta.
    /// </summary>
    private void AprobarConexion(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        int conectadosActuales = NetworkManager.Singleton.ConnectedClientsIds.Count;

        if (conectadosActuales >= maxJugadores)
        {
            response.Approved = false;
            response.Reason = "Sala llena";
            response.CreatePlayerObject = false;
            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = true;
    }

    /// <summary>
    /// Se dispara para CUALQUIER desconexion (la tuya propia o la de otro jugador).
    /// </summary>
    private void ManejarDesconexion(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;

        bool soyYo = NetworkManager.Singleton.LocalClientId == clientId;

        if (soyYo && !NetworkManager.Singleton.IsServer)
        {
            // Perdi la conexion con el host (se cerro la sala, cayo la red, etc).
            UIManager.Instance?.MostrarMensajeDesconexion("Se perdió la conexión con el host.");
            SceneManager.LoadScene(escenaMenu);
            return;
        }

        if (NetworkManager.Singleton.IsServer && !soyYo)
        {
            // Se fue OTRO jugador: se lo delegamos a RaceManager (el que sabe
            // de red) para que cuente como "ya no puede llegar" y avise a todos.
            RaceManager.Instance?.MarcarJugadorDesconectado(clientId);
        }
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;

        lanDiscovery?.DetenerTodo();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(escenaMenu);
    }
}