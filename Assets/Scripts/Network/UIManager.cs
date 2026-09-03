using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Paneles")]
    public GameObject panelMenu;
    public GameObject panelHUD;
    public GameObject panelVictoria;
    public GameObject panelDerrota;
    public GameObject panelPausa;

    public Text textoVictoria;

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
        // Se ejecuta al iniciar el juego
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
        // Detectar tecla Escape
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

    // ====================== VICTORIA / DERROTA ======================

    public void MostrarVictoria(ulong clientId)
    {
        OcultarTodo();
        puedePausar = false;
        estaPausado = false;

        if (panelVictoria != null) panelVictoria.SetActive(true);
        if (textoVictoria != null) textoVictoria.text = $"¡Jugador {clientId} ganó!";

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