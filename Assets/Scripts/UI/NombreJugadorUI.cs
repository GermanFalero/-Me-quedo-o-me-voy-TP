using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Input de nombre en el menu principal. Se guarda en PlayerPrefs y lo lee
/// PlayerIdentity al spawnear. Poné este script junto a un InputField del menu.
/// </summary>
public class NombreJugadorUI : MonoBehaviour
{
    [SerializeField] private InputField campoNombre;

    private const string CLAVE_NOMBRE = "NombreJugador";

    private void OnEnable()
    {
        if (campoNombre == null) return;

        campoNombre.text = PlayerPrefs.GetString(CLAVE_NOMBRE, "Jugador");
        campoNombre.onEndEdit.AddListener(GuardarNombre);
    }

    private void OnDisable()
    {
        campoNombre?.onEndEdit.RemoveListener(GuardarNombre);
    }

    private void GuardarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) nombre = "Jugador";
        nombre = nombre.Trim();
        if (nombre.Length > 16) nombre = nombre.Substring(0, 16);

        PlayerPrefs.SetString(CLAVE_NOMBRE, nombre);
    }
}
