using UnityEngine;

/// <summary>
/// Manager de audio central. Otros scripts le piden sonidos por acá en vez de
/// tener cada uno su propio AudioSource repartido por el mapa.
///
/// Uso tipico desde otro script:
///   AudioManager.Instance?.ReproducirSFX(miClip);
///
/// Configuracion: poné este script en un GameObject persistente (junto con
/// GameManager esta bien), con dos AudioSource hijos: uno para musica (loop)
/// y otro para efectos (one-shot).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Fuentes de audio")]
    [SerializeField] private AudioSource fuenteMusica;
    [SerializeField] private AudioSource fuenteSFX;

    [Header("Clips generales (opcional, para no tener que pasarlos siempre)")]
    public AudioClip sonidoMeta;
    public AudioClip sonidoCheckpoint;
    public AudioClip sonidoSalto;
    public AudioClip sonidoCaida;
    public AudioClip sonidoBotonUI;
    public AudioClip musicaMenu;
    public AudioClip musicaNivel;

    private const string CLAVE_VOLUMEN_MUSICA = "VolumenMusica";
    private const string CLAVE_VOLUMEN_SFX = "VolumenSFX";

    private void Awake()
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

        AplicarVolumenGuardado();
    }

    private void AplicarVolumenGuardado()
    {
        SetVolumenMusica(PlayerPrefs.GetFloat(CLAVE_VOLUMEN_MUSICA, 0.7f));
        SetVolumenSFX(PlayerPrefs.GetFloat(CLAVE_VOLUMEN_SFX, 1f));
    }

    // ====================== SFX ======================

    public void ReproducirSFX(AudioClip clip)
    {
        if (clip == null || fuenteSFX == null) return;
        fuenteSFX.PlayOneShot(clip);
    }

    public void ReproducirBotonUI() => ReproducirSFX(sonidoBotonUI);
    public void ReproducirMeta() => ReproducirSFX(sonidoMeta);
    public void ReproducirCheckpoint() => ReproducirSFX(sonidoCheckpoint);
    public void ReproducirSalto() => ReproducirSFX(sonidoSalto);
    public void ReproducirCaida() => ReproducirSFX(sonidoCaida);

    // ====================== Musica ======================

    public void ReproducirMusica(AudioClip clip)
    {
        if (fuenteMusica == null || clip == null) return;
        if (fuenteMusica.clip == clip && fuenteMusica.isPlaying) return;

        fuenteMusica.clip = clip;
        fuenteMusica.loop = true;
        fuenteMusica.Play();
    }

    public void DetenerMusica()
    {
        if (fuenteMusica != null) fuenteMusica.Stop();
    }

    // ====================== Volumen (usado por SettingsManager) ======================

    public void SetVolumenMusica(float volumen)
    {
        if (fuenteMusica != null) fuenteMusica.volume = volumen;
        PlayerPrefs.SetFloat(CLAVE_VOLUMEN_MUSICA, volumen);
    }

    public void SetVolumenSFX(float volumen)
    {
        if (fuenteSFX != null) fuenteSFX.volume = volumen;
        PlayerPrefs.SetFloat(CLAVE_VOLUMEN_SFX, volumen);
    }

    public float VolumenMusicaActual => PlayerPrefs.GetFloat(CLAVE_VOLUMEN_MUSICA, 0.7f);
    public float VolumenSFXActual => PlayerPrefs.GetFloat(CLAVE_VOLUMEN_SFX, 1f);
}