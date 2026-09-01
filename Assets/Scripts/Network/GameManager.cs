using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Configuración de escenas")]
    public string escenaOffline = "Nivel1_Neon";
    public string escenaOnline = "Nivel1_Neon";
    public string escenaMenu = "MenuPrincipal";

    [Header("Jugador offline")]
    public GameObject jugadorOfflinePrefab;

    [Header("Red")]
    public string ipPorDefecto = "127.0.0.1";
    public ushort puerto = 7777;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void JugarOffline()
    {
        Debug.Log("Cargando modo offline...");
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
        Debug.Log("Iniciando host...");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton es null.");
            return;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("Ya hay una sesión activa. Ignorando CrearSala.");
            return;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
            transport.SetConnectionData("0.0.0.0", puerto);

        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("Host iniciado correctamente.");
            NetworkManager.Singleton.SceneManager.LoadScene(escenaOnline, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("No se pudo iniciar el host.");
        }
    }

    public void UnirseASala(string ip)
    {
        if (string.IsNullOrEmpty(ip))
        {
            ip = ipPorDefecto;
            Debug.LogWarning($"IP vacía, usando {ip}");
        }

        Debug.Log($"Intentando unirse a {ip}:{puerto}...");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton es null.");
            return;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
            transport.SetConnectionData(ip, puerto);
        else
        {
            Debug.LogError("UnityTransport no encontrado.");
            return;
        }

        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("Cliente iniciado correctamente.");
        }
        else
        {
            Debug.LogError("No se pudo iniciar el cliente. Verificá IP, puerto y host activo.");
        }
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenu);
    }
}