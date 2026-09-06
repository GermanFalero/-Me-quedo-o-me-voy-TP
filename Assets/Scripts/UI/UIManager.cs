using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Text;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Paneles")]
    public GameObject panelMenu;
    public GameObject panelHUD;
    public GameObject panelVictoria;
    public GameObject panelDerrota;
    public GameObject panelPausa;
    public GameObject panelPodio;
    public GameObject panelTiempoAgotado;
    public GameObject panelEsperandoOtros;
    public GameObject panelDesconexion;
    public GameObject panelCargando;
    public GameObject panelCuentaRegresiva;

    public Text textoVictoria;
    public Text textoPodio;
    public Text textoTiempoAgotado;
    public Text textoAvisoLlegada; // texto pequeño tipo "toast" en el HUD, ej: "Jugador 2 llego 3ro!"
    public Text textoEsperandoOtros;
    public Text textoDesconexion;
    public Text textoTimer; // cuenta regresiva de la carrera, en el HUD
    public Text textoCargando;
    public Text textoCuentaRegresiva;

    private bool estaPausado = false;
    private bool puedePausar = true;

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

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name == "MenuPrincipal")
        {
            MostrarMenu();
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (puedePausar)
            {
                if (estaPausado)
                    Reanudar();
                else
                    Pausar();
            }
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        estaPausado = false;
        Time.timeScale = 1f;

        if (scene.name == "MenuPrincipal")
        {
            MostrarMenu();
            puedePausar = false;
        }
        else
        {
            MostrarHUD();
            puedePausar = true;
        }
    }

    void OcultarTodo()
    {
        if (panelMenu != null) panelMenu.SetActive(false);
        if (panelHUD != null) panelHUD.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);
        if (panelDerrota != null) panelDerrota.SetActive(false);
        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelPodio != null) panelPodio.SetActive(false);
        if (panelTiempoAgotado != null) panelTiempoAgotado.SetActive(false);
        if (panelEsperandoOtros != null) panelEsperandoOtros.SetActive(false);
        if (panelCuentaRegresiva != null) panelCuentaRegresiva.SetActive(false);
        if (panelDesconexion != null) panelDesconexion.SetActive(false);
    }

    // ====================== MENÚ PRINCIPAL ======================

    public void MostrarMenu()
    {
        OcultarTodo();
        if (panelMenu != null) panelMenu.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
        estaPausado = false;
        puedePausar = false;
    }

    public void MostrarHUD()
    {
        OcultarTodo();
        if (panelHUD != null) panelHUD.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        puedePausar = true;
        estaPausado = false;
    }

    // ====================== PAUSA ======================
    // Podes llamar a Pausar() desde el Escape (ya esta en Update) O desde el
    // OnClick() de un boton de UI en el HUD - las dos formas usan el mismo metodo.

    public void Pausar()
    {
        if (panelPausa == null) return;

        estaPausado = true;
        Time.timeScale = 0f;

        if (panelHUD != null) panelHUD.SetActive(false);
        panelPausa.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Reanudar()
    {
        estaPausado = false;
        Time.timeScale = 1f;

        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelHUD != null) panelHUD.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ====================== VICTORIA / DERROTA / PODIO ======================

    public void MostrarVictoria(ulong clientId)
    {
        OcultarTodo();
        puedePausar = false;
        estaPausado = false;

        if (panelVictoria != null) panelVictoria.SetActive(true);
        if (textoVictoria != null) textoVictoria.text = $"¡{PlayerIdentity.BuscarNombre(clientId)} ganó!";

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Aviso corto (no bloqueante) para cuando un jugador que NO es el primero cruza la meta.
    /// El juego sigue corriendo para los que faltan llegar.
    /// </summary>
    public void MostrarLlegadaJugador(ulong clientId, int puesto)
    {
        if (textoAvisoLlegada == null) return;

        string sufijo = puesto switch
        {
            2 => "2do",
            3 => "3ro",
            4 => "4to",
            5 => "5to",
            _ => $"{puesto}to"
        };

        textoAvisoLlegada.text = $"{PlayerIdentity.BuscarNombre(clientId)} llegó {sufijo}!";
        CancelInvoke(nameof(OcultarAvisoLlegada));
        Invoke(nameof(OcultarAvisoLlegada), 3f);
    }

    /// <summary>Toast generico de una linea, para avisos que no requieren pausar nada (ej: "Un jugador se desconectó").</summary>
    public void MostrarAviso(string mensaje)
    {
        if (textoAvisoLlegada == null) return;

        textoAvisoLlegada.text = mensaje;
        CancelInvoke(nameof(OcultarAvisoLlegada));
        Invoke(nameof(OcultarAvisoLlegada), 3f);
    }

    /// <summary>Cuenta regresiva de la carrera. Se llama ~1 vez por segundo desde GameManager.</summary>
    public void ActualizarTimer(float segundosRestantes)
    {
        if (textoTimer == null) return;

        int minutos = Mathf.FloorToInt(segundosRestantes / 60f);
        int segundos = Mathf.FloorToInt(segundosRestantes % 60f);
        textoTimer.text = $"{minutos:00}:{segundos:00}";
    }

    /// <summary>Se te cayo la conexion con el host. Bloqueante: no tiene sentido seguir jugando solo.</summary>
    public void MostrarMensajeDesconexion(string mensaje)
    {
        OcultarTodo();
        puedePausar = false;
        estaPausado = false;

        if (panelDesconexion != null) panelDesconexion.SetActive(true);
        if (textoDesconexion != null) textoDesconexion.text = mensaje;

        Time.timeScale = 1f; // no pausamos: ya estamos volviendo al menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Cuenta regresiva antes de que arranque la carrera (esperando jugadores). No pausa el juego.</summary>
    public void MostrarCuentaRegresiva(int segundosRestantes)
    {
        if (panelCuentaRegresiva != null) panelCuentaRegresiva.SetActive(true);
        if (textoCuentaRegresiva != null) textoCuentaRegresiva.text = $"La carrera empieza en {segundosRestantes}...";
    }

    public void OcultarCuentaRegresiva()
    {
        if (panelCuentaRegresiva != null) panelCuentaRegresiva.SetActive(false);
    }

    /// <summary>Mostrar mientras se conecta y/o carga el nivel (offline u online).</summary>
    public void MostrarCargando(string mensaje = "Cargando...")
    {
        if (panelCargando != null) panelCargando.SetActive(true);
        if (textoCargando != null) textoCargando.text = mensaje;
    }

    public void OcultarCargando()
    {
        if (panelCargando != null) panelCargando.SetActive(false);
    }

    void OcultarAvisoLlegada()
    {
        if (textoAvisoLlegada != null) textoAvisoLlegada.text = "";
    }

    /// <summary>
    /// Para cuando ESTE jugador ya cruzo la meta pero todavia faltan otros.
    /// A proposito NO toca Time.timeScale ni oculta el HUD del todo: el resto
    /// de la carrera tiene que seguir corriendo con normalidad para los demas.
    /// </summary>
    public void MostrarEsperandoAOtros(int puesto)
    {
        if (panelEsperandoOtros != null) panelEsperandoOtros.SetActive(true);
        if (textoEsperandoOtros != null) textoEsperandoOtros.text = $"¡Llegaste {puesto}°! Esperando a los demás jugadores...";
    }

    /// <summary>Se muestra cuando TODOS los jugadores conectados ya cruzaron la meta.</summary>
    public void MostrarPodioFinal(ulong[] ordenLlegada)
    {
        OcultarTodo();
        puedePausar = false;
        estaPausado = false;

        if (panelPodio != null) panelPodio.SetActive(true);

        if (textoPodio != null)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ordenLlegada.Length; i++)
            {
                sb.AppendLine($"{i + 1}° - {PlayerIdentity.BuscarNombre(ordenLlegada[i])}");
            }
            textoPodio.text = sb.ToString();
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Se cumplio el tiempo limite de carrera y nadie llego a la meta (offline,
    /// o multijugador sin ningun jugador finalizado). Evita mostrar un podio
    /// vacio que se ve como si "no pasara nada".
    /// </summary>
    public void MostrarTiempoAgotado()
    {
        OcultarTodo();
        puedePausar = false;
        estaPausado = false;

        if (panelTiempoAgotado != null) panelTiempoAgotado.SetActive(true);
        if (textoTiempoAgotado != null) textoTiempoAgotado.text = "¡Se acabó el tiempo!";

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void MostrarDerrota()
    {
        OcultarTodo();
        puedePausar = false;
        estaPausado = false;

        if (panelDerrota != null) panelDerrota.SetActive(true);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ====================== BOTONES ======================

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        estaPausado = false;

        if (Unity.Netcode.NetworkManager.Singleton != null &&
            Unity.Netcode.NetworkManager.Singleton.IsListening)
        {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("MenuPrincipal");
    }

    public void SalirDelJuego()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ReiniciarEscena()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}