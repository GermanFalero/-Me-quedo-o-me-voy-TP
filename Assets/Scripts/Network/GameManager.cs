using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("Configuración de escenas")]
    public string escenaOffline = "Nivel1_Neon";
    public string escenaOnline = "Nivel1_Neon";
    public string escenaMenu = "MenuPrincipal";

    [Header("Jugador offline")]
    public GameObject jugadorOfflinePrefab;

    [Header("Red")]
    public string ipPorDefecto = "127.0.0.1";
    public ushort puerto = 7777;

    public NetworkVariable<int> estadoCarrera = new NetworkVariable<int>(0);
    public NetworkVariable<ulong> ganadorId = new NetworkVariable<ulong>(999);

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
        }
    }

    public void JugarOffline()
    {
        SceneManager.LoadScene(escenaOffline);
        StartCoroutine(InstanciarJugadorOffline());
    }

    IEnumerator InstanciarJugadorOffline()
    {
        yield return null;

        if (jugadorOfflinePrefab == null)
        {
            Debug.LogError("No se asignó jugadorOfflinePrefab en GameManager.");
            yield break;
        }

        GameObject jugador = Instantiate(jugadorOfflinePrefab);
        NetworkObject no = jugador.GetComponent<NetworkObject>();
        if (no != null) no.enabled = false;

        Transform spawn = SpawnManager.instance?.GetSpawn(0);
        if (spawn != null)
            jugador.transform.position = spawn.position;

        Camera.main?.GetComponent<CameraController>()?.SetTarget(jugador.transform);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void CrearSala()
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.IsListening) return;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
            transport.SetConnectionData("0.0.0.0", puerto);

        if (NetworkManager.Singleton.StartHost())
        {
            NetworkManager.Singleton.SceneManager.LoadScene(escenaOnline, LoadSceneMode.Single);
        }
    }

    public void UnirseASala(string ip)
    {
        if (string.IsNullOrEmpty(ip)) ip = ipPorDefecto;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
            transport.SetConnectionData(ip, puerto);

        NetworkManager.Singleton.StartClient();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayerFinishedServerRpc(ulong clientId)
    {
        if (estadoCarrera.Value != 0) return;

        estadoCarrera.Value = 1;
        ganadorId.Value = clientId;
        MostrarResultadoClientRpc(clientId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void MostrarResultadoClientRpc(ulong ganador)
    {
        UIManager.Instance?.MostrarVictoria(ganador);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayerEliminatedServerRpc(ulong clientId)
    {
        foreach (var jugador in PlayerController.Jugadores)
        {
            if (jugador.OwnerClientId == clientId)
            {
                jugador.DesactivarJugadorClientRpc();
                break;
            }
        }
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenu);
    }
}