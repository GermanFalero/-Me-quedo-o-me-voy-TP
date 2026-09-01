using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Nombres de paneles")]
    public string nombrePanelMenu = "PanelMenu";
    public string nombrePanelHUD = "PanelHUD";
    public string nombrePanelVictoria = "PanelVictoria";
    public string nombrePanelDerrota = "PanelDerrota";

    private GameObject panelMenu;
    private GameObject panelHUD;
    private GameObject panelVictoria;
    private GameObject panelDerrota;
    private Text textoVictoria;

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
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BuscarPanelesEnEscena();

        if (scene.name == "MenuPrincipal")
        {
            MostrarMenu();
        }
        else
        {
            OcultarTodo();
            // Bloquear cursor en niveles
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void BuscarPanelesEnEscena()
    {
        panelMenu = GameObject.Find(nombrePanelMenu);
        panelHUD = GameObject.Find(nombrePanelHUD);
        panelVictoria = GameObject.Find(nombrePanelVictoria);
        panelDerrota = GameObject.Find(nombrePanelDerrota);

        if (panelVictoria != null)
            textoVictoria = panelVictoria.GetComponentInChildren<Text>();
    }

    void OcultarTodo()
    {
        if (panelMenu != null) panelMenu.SetActive(false);
        if (panelHUD != null) panelHUD.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);
        if (panelDerrota != null) panelDerrota.SetActive(false);
    }

    public void MostrarMenu()
    {
        OcultarTodo();
        if (panelMenu != null) panelMenu.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void MostrarHUD()
    {
        OcultarTodo();
        if (panelHUD != null) panelHUD.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void MostrarVictoria(ulong clientId)
    {
        OcultarTodo();
        if (panelVictoria != null) panelVictoria.SetActive(true);
        if (textoVictoria != null) textoVictoria.text = $"¡Jugador {clientId} ganó!";
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void MostrarDerrota()
    {
        OcultarTodo();
        if (panelDerrota != null) panelDerrota.SetActive(true);
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ReiniciarEscena()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}