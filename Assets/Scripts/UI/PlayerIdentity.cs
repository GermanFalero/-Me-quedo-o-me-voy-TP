using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Collections;

/// <summary>
/// Identidad visual del jugador: nombre (sincronizado por red) + color por jugador
/// + cartel flotante con el nombre arriba de la cabeza, para poder distinguir a
/// los 5 jugadores entre si durante la carrera.
///
/// Configuracion:
/// - Poné este script en el mismo prefab que PlayerController.
/// - Asignale rendererACoolear (el mesh del personaje) y textoNombreFlotante
///   (un Text/TMP_Text dentro de un Canvas World Space, hijo del jugador,
///   posicionado arriba de la cabeza).
/// </summary>
public class PlayerIdentity : NetworkBehaviour
{
    [Header("Referencias")]
    [Tooltip("El/los renderer del personaje, para pintarlos con el color asignado.")]
    [SerializeField] private Renderer[] renderersACoolear;

    [Tooltip("Texto flotante (Canvas World Space) que muestra el nombre arriba del personaje.")]
    [SerializeField] private Text textoNombreFlotante;

    [Tooltip("Transform del cartel, para que siempre mire a la camara (billboard).")]
    [SerializeField] private Transform carteldeNombre;

    private readonly NetworkVariable<FixedString32Bytes> nombre = new NetworkVariable<FixedString32Bytes>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private bool ModoOnline => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    public string Nombre => nombre.Value.ToString();

    /// <summary>Busca el nombre de un jugador por su clientId, para mostrar en la UI (podio, avisos, etc).</summary>
    public static string BuscarNombre(ulong clientId)
    {
        var jugador = PlayerController.Jugadores.Find(p => p.OwnerClientId == clientId);
        var identidad = jugador != null ? jugador.GetComponent<PlayerIdentity>() : null;
        return (identidad != null && !string.IsNullOrEmpty(identidad.Nombre)) ? identidad.Nombre : $"Jugador {clientId}";
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        ConfigurarEstiloCartel();

        nombre.OnValueChanged += (_, nuevoNombre) => ActualizarCartel(nuevoNombre.ToString());

        if (IsOwner)
        {
            string nombreGuardado = PlayerPrefs.GetString("NombreJugador", $"Jugador{OwnerClientId}");
            nombre.Value = nombreGuardado;

            // El jugador no necesita ver su propio cartel flotando delante suyo.
            if (carteldeNombre != null) carteldeNombre.gameObject.SetActive(false);
        }

        AplicarColor(PlayerColors.GetColor(OwnerClientId));
        ActualizarCartel(nombre.Value.ToString());
    }

    private void Start()
    {
        ConfigurarEstiloCartel();

        // Offline: no hay OnNetworkSpawn, asi que inicializamos aca directamente.
        if (!ModoOnline)
        {
            string nombreGuardado = PlayerPrefs.GetString("NombreJugador", "Jugador");
            ActualizarCartel(nombreGuardado);
            AplicarColor(PlayerColors.GetColor(0));
            if (carteldeNombre != null) carteldeNombre.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Configura el texto para que se lea bien sobre CUALQUIER fondo del mapa:
    /// negrita + contorno oscuro (Outline). Se hace por codigo asi funciona
    /// aunque no lo hayas configurado a mano en el prefab.
    /// </summary>
    private void ConfigurarEstiloCartel()
    {
        if (textoNombreFlotante == null) return;

        textoNombreFlotante.fontStyle = FontStyle.Bold;
        textoNombreFlotante.fontSize = 28;
        textoNombreFlotante.alignment = TextAnchor.MiddleCenter;
        textoNombreFlotante.color = Color.white;
        textoNombreFlotante.horizontalOverflow = HorizontalWrapMode.Overflow;
        textoNombreFlotante.verticalOverflow = VerticalWrapMode.Overflow;

        if (textoNombreFlotante.GetComponent<Outline>() == null)
        {
            Outline contorno = textoNombreFlotante.gameObject.AddComponent<Outline>();
            contorno.effectColor = new Color(0f, 0f, 0f, 0.85f);
            contorno.effectDistance = new Vector2(1.5f, -1.5f);
        }
    }

    private void LateUpdate()
    {
        // Billboard: el cartel siempre mira a la camara activa.
        if (carteldeNombre != null && Camera.main != null)
            carteldeNombre.rotation = Camera.main.transform.rotation;
    }

    private void AplicarColor(Color color)
    {
        if (renderersACoolear == null) return;

        foreach (var r in renderersACoolear)
        {
            if (r != null) r.material.color = color;
        }
    }

    private void ActualizarCartel(string texto)
    {
        if (textoNombreFlotante != null)
            textoNombreFlotante.text = texto;
    }

    /// <summary>Oculta el cartel flotante. Se llama junto con ocultar el resto del personaje (ver PlayerController.OcultarPersonaje), para que no quede el nombre flotando solo sin cuerpo debajo.</summary>
    public void OcultarCartel()
    {
        if (carteldeNombre != null) carteldeNombre.gameObject.SetActive(false);
    }
}