using UnityEngine;

/// <summary>
/// Paleta de colores fija para identificar visualmente a cada jugador (hasta 5).
/// No hace falta sincronizar esto por red: cada cliente calcula el mismo color
/// de forma independiente a partir del OwnerClientId.
/// </summary>
public static class PlayerColors
{
    private static readonly Color[] paleta = new Color[]
    {
        new Color(0.95f, 0.25f, 0.25f), // rojo
        new Color(0.25f, 0.55f, 0.95f), // azul
        new Color(0.30f, 0.85f, 0.35f), // verde
        new Color(0.95f, 0.85f, 0.20f), // amarillo
        new Color(0.75f, 0.30f, 0.90f), // violeta
    };

    public static Color GetColor(ulong clientId)
    {
        return paleta[(int)(clientId % (ulong)paleta.Length)];
    }
}
