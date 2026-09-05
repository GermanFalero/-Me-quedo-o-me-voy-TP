using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pantalla de opciones: volumen de musica/efectos y sensibilidad del mouse.
/// Todo se guarda en PlayerPrefs y se puede leer desde cualquier lado con
/// SettingsManager.SensibilidadMouse (estatico), sin necesidad de tener
/// una referencia a este componente.
///
/// Uso en tu script de camara (CameraController/PlayerCamera):
///   float sensibilidad = SettingsManager.SensibilidadMouse;
///   rotacion += input * sensibilidad;
/// </summary>
public class SettingsManager : MonoBehaviour
{
    [Header("UI (opcional, asignar si este panel tiene sliders)")]
    [SerializeField] private Slider sliderVolumenMusica;
    [SerializeField] private Slider sliderVolumenSFX;
    [SerializeField] private Slider sliderSensibilidad;

    private const string CLAVE_SENSIBILIDAD = "SensibilidadMouse";
    private const float SENSIBILIDAD_POR_DEFECTO = 2f;

    public static float SensibilidadMouse => PlayerPrefs.GetFloat(CLAVE_SENSIBILIDAD, SENSIBILIDAD_POR_DEFECTO);

    private void OnEnable()
    {
        // Sincronizar los sliders con los valores guardados cada vez que se abre el panel.
        if (sliderVolumenMusica != null)
        {
            sliderVolumenMusica.value = AudioManager.Instance != null ? AudioManager.Instance.VolumenMusicaActual : 0.7f;
            sliderVolumenMusica.onValueChanged.AddListener(CambiarVolumenMusica);
        }

        if (sliderVolumenSFX != null)
        {
            sliderVolumenSFX.value = AudioManager.Instance != null ? AudioManager.Instance.VolumenSFXActual : 1f;
            sliderVolumenSFX.onValueChanged.AddListener(CambiarVolumenSFX);
        }

        if (sliderSensibilidad != null)
        {
            sliderSensibilidad.value = SensibilidadMouse;
            sliderSensibilidad.onValueChanged.AddListener(CambiarSensibilidad);
        }
    }

    private void OnDisable()
    {
        sliderVolumenMusica?.onValueChanged.RemoveListener(CambiarVolumenMusica);
        sliderVolumenSFX?.onValueChanged.RemoveListener(CambiarVolumenSFX);
        sliderSensibilidad?.onValueChanged.RemoveListener(CambiarSensibilidad);
    }

    public void CambiarVolumenMusica(float valor) => AudioManager.Instance?.SetVolumenMusica(valor);
    public void CambiarVolumenSFX(float valor) => AudioManager.Instance?.SetVolumenSFX(valor);

    public void CambiarSensibilidad(float valor)
    {
        PlayerPrefs.SetFloat(CLAVE_SENSIBILIDAD, valor);
    }
}