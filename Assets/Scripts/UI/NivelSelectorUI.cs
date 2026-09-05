using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Genera un boton por cada nivel definido en GameManager.niveles y los deja
/// listos para elegir antes de jugar offline o crear sala.
///
/// Uso: poné este script en un panel vacio del menu (ej. "PanelSeleccionNivel"),
/// asignale un prefab de boton (con un componente Text/TMP_Text adentro para
/// el nombre) y un contenedor (puede ser el mismo objeto con un Layout Group).
/// </summary>
public class NivelSelectorUI : MonoBehaviour
{
    [SerializeField] private Button botonPrefab;
    [SerializeField] private Transform contenedor;

    private int nivelElegido = 0;

    private void OnEnable()
    {
        Refrescar();
    }

    public void Refrescar()
    {
        // Limpiar botones previos
        for (int i = contenedor.childCount - 1; i >= 0; i--)
            Destroy(contenedor.GetChild(i).gameObject);

        if (GameManager.Instance == null || GameManager.Instance.niveles == null) return;

        for (int i = 0; i < GameManager.Instance.niveles.Length; i++)
        {
            int index = i; // capturar copia local para el closure del boton
            var nivel = GameManager.Instance.niveles[i];

            Button boton = Instantiate(botonPrefab, contenedor);
            boton.gameObject.SetActive(true);

            Text texto = boton.GetComponentInChildren<Text>();
            if (texto != null) texto.text = nivel.nombreVisible;

            boton.onClick.AddListener(() => Elegir(index));
        }
    }

    private void Elegir(int index)
    {
        nivelElegido = index;
        GameManager.Instance?.SeleccionarNivel(index);
    }

    public int NivelElegido => nivelElegido;
}