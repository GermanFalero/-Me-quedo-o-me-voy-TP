using UnityEngine;
using UnityEngine.UI;

public class VidasUI : MonoBehaviour
{
    [Header("Configuración")]
    public int maxVidas = 3;

    [Header("Referencias")]
    public Image[] iconosCorazon;

    private PlayerController jugadorLocal;

    void Update()
    {
        if (jugadorLocal == null)
            BuscarJugadorLocal();

        if (jugadorLocal != null)
            ActualizarVidas(jugadorLocal.VidasActuales);
    }

    void BuscarJugadorLocal()
    {
        bool online = Unity.Netcode.NetworkManager.Singleton != null &&
                      Unity.Netcode.NetworkManager.Singleton.IsListening;

        if (online)
        {
            // Online: buscar al dueño local
            foreach (var p in PlayerController.Jugadores)
            {
                if (p != null && p.IsOwner)
                {
                    jugadorLocal = p;
                    return;
                }
            }
        }
        else
        {
            // Offline: tomar el primero disponible
            if (PlayerController.Jugadores.Count > 0 && PlayerController.Jugadores[0] != null)
            {
                jugadorLocal = PlayerController.Jugadores[0];
                return;
            }

            // Fallback: buscar en la escena
            jugadorLocal = FindFirstObjectByType<PlayerController>();
        }
    }

    public void ActualizarVidas(int vidasActuales)
    {
        if (iconosCorazon == null) return;

        for (int i = 0; i < iconosCorazon.Length; i++)
        {
            if (iconosCorazon[i] == null) continue;

            // true = se ve, false = desaparece
            iconosCorazon[i].enabled = i < vidasActuales;
        }
    }
}