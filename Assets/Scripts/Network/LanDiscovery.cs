using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Descubrimiento de partidas en LAN por UDP broadcast. Corre en un puerto
/// DISTINTO al del juego (Netcode/UnityTransport), asi no interfiere.
///
/// Host: llama a EmpezarAAnunciar(nombreSala) cuando crea la sala.
/// Cliente: llama a EmpezarABuscar() en la pantalla de "Unirse a sala",
/// y lee SalasEncontradas para mostrar una lista con boton "Unirse" por sala,
/// que llama a GameManager.Instance.UnirseASala(sala.ip).
///
/// Poné este script en el mismo GameObject persistente que tiene GameManager
/// (o cualquier objeto con DontDestroyOnLoad).
/// </summary>
public class LanDiscovery : MonoBehaviour
{
    public struct SalaEncontrada
    {
        public string ip;
        public string nombre;
        public float ultimoAviso;
    }

    [Header("Configuracion")]
    [SerializeField] private int puertoDescubrimiento = 47777;
    [SerializeField] private float intervaloAnuncio = 1f;
    [SerializeField] private float tiempoExpiracion = 3f;

    private UdpClient udpAnunciante;
    private UdpClient udpEscucha;
    private Thread hiloEscucha;
    private volatile bool escuchando = false;

    private readonly Dictionary<string, SalaEncontrada> salasEncontradas = new Dictionary<string, SalaEncontrada>();
    private readonly Queue<(string ip, string mensaje)> mensajesRecibidos = new Queue<(string, string)>();
    private readonly object lockMensajes = new object();

    private float temporizadorAnuncio;
    private string nombreSalaActual;

    public IReadOnlyCollection<SalaEncontrada> SalasEncontradas => salasEncontradas.Values;

    private void Update()
    {
        // Reenviar el anuncio del host periodicamente
        if (udpAnunciante != null)
        {
            temporizadorAnuncio -= Time.unscaledDeltaTime;
            if (temporizadorAnuncio <= 0f)
            {
                temporizadorAnuncio = intervaloAnuncio;
                EnviarAnuncio();
            }
        }

        // Procesar mensajes recibidos (el hilo de escucha los encola, aca los consumimos)
        lock (lockMensajes)
        {
            while (mensajesRecibidos.Count > 0)
            {
                var (ip, mensaje) = mensajesRecibidos.Dequeue();
                salasEncontradas[ip] = new SalaEncontrada
                {
                    ip = ip,
                    nombre = mensaje,
                    ultimoAviso = Time.unscaledTime
                };
            }
        }

        // Limpiar salas que dejaron de anunciarse (el host se cerro o se fue de la LAN)
        List<string> vencidas = null;
        foreach (var kvp in salasEncontradas)
        {
            if (Time.unscaledTime - kvp.Value.ultimoAviso > tiempoExpiracion)
            {
                vencidas ??= new List<string>();
                vencidas.Add(kvp.Key);
            }
        }
        if (vencidas != null)
            foreach (var ip in vencidas) salasEncontradas.Remove(ip);
    }

    // ====================== HOST: anunciar la sala ======================

    public void EmpezarAAnunciar(string nombreSala)
    {
        DetenerTodo();
        nombreSalaActual = string.IsNullOrEmpty(nombreSala) ? "Sala sin nombre" : nombreSala;

        udpAnunciante = new UdpClient();
        udpAnunciante.EnableBroadcast = true;
        temporizadorAnuncio = 0f; // que mande el primero ya
    }

    private void EnviarAnuncio()
    {
        try
        {
            byte[] datos = Encoding.UTF8.GetBytes(nombreSalaActual);
            udpAnunciante.Send(datos, datos.Length, new IPEndPoint(IPAddress.Broadcast, puertoDescubrimiento));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"LanDiscovery: no se pudo anunciar la sala ({e.Message})");
        }
    }

    // ====================== CLIENTE: buscar salas ======================

    public void EmpezarABuscar()
    {
        DetenerTodo();
        salasEncontradas.Clear();

        udpEscucha = new UdpClient();
        udpEscucha.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udpEscucha.Client.Bind(new IPEndPoint(IPAddress.Any, puertoDescubrimiento));

        escuchando = true;
        hiloEscucha = new Thread(EscucharAnuncios) { IsBackground = true };
        hiloEscucha.Start();
    }

    private void EscucharAnuncios()
    {
        IPEndPoint remoto = new IPEndPoint(IPAddress.Any, 0);

        while (escuchando)
        {
            try
            {
                byte[] datos = udpEscucha.Receive(ref remoto);
                string mensaje = Encoding.UTF8.GetString(datos);

                lock (lockMensajes)
                {
                    mensajesRecibidos.Enqueue((remoto.Address.ToString(), mensaje));
                }
            }
            catch (SocketException)
            {
                // El socket se cerro (DetenerTodo) - salimos del hilo tranquilos.
                break;
            }
        }
    }

    // ====================== Limpieza ======================

    public void DetenerTodo()
    {
        escuchando = false;

        udpAnunciante?.Close();
        udpAnunciante = null;

        udpEscucha?.Close();
        udpEscucha = null;

        if (hiloEscucha != null && hiloEscucha.IsAlive)
            hiloEscucha.Join(200);
        hiloEscucha = null;
    }

    private void OnDestroy() => DetenerTodo();
    private void OnApplicationQuit() => DetenerTodo();
}
