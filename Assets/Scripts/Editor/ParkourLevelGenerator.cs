#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Netcode;

/// <summary>
/// Genera automaticamente un mapa de parkour largo (dificultad media-alta) con
/// spawn points para 5 jugadores, checkpoints, zona de vacio y linea de meta.
///
/// COMO USARLO:
/// 1. Poné este script en una carpeta llamada "Editor" dentro de Assets
///    (ej: Assets/Editor/ParkourLevelGenerator.cs). Es obligatorio que este
///    en una carpeta "Editor" o Unity va a tirar error al compilar el build.
/// 2. En la barra superior de Unity: Tools > Parkour > Generar Mapa Completo.
/// 3. Se crea todo dentro de un GameObject llamado "Mapa_Parkour_Generado"
///    en la escena activa. Guardá la escena (Ctrl+S) despues.
///
/// El mapa queda armado con geometria simple (cubos) a proposito, para que
/// puedas reemplazar visualmente cada pieza por tus propios modelos/prefabs
/// despues sin tener que rehacer la logica ni las posiciones.
/// </summary>
public static class ParkourLevelGenerator
{
    private static readonly Dictionary<Color, Material> materialCache = new Dictionary<Color, Material>();

    // Colores por zona, para que el mapa se "lea" bien de un vistazo (buena practica de nivel).
    private static readonly Color colorSpawn = new Color(0.3f, 0.8f, 0.4f);
    private static readonly Color colorSprint = new Color(0.6f, 0.6f, 0.65f);
    private static readonly Color colorSaltos = new Color(0.95f, 0.6f, 0.15f);
    private static readonly Color colorCaediza = new Color(0.85f, 0.2f, 0.2f);
    private static readonly Color colorMovil = new Color(0.2f, 0.55f, 0.95f);
    private static readonly Color colorRotante = new Color(0.6f, 0.25f, 0.85f);
    private static readonly Color colorCaja = new Color(0.9f, 0.75f, 0.15f);
    private static readonly Color colorDificil = new Color(0.55f, 0.1f, 0.15f);
    private static readonly Color colorMeta = new Color(1f, 0.85f, 0.2f);
    private static readonly Color colorCheckpoint = new Color(0.2f, 0.9f, 0.9f);

    private static PhysicsMaterial physMatCajas;

    [MenuItem("Tools/Parkour/Generar Mapa Completo")]
    public static void GenerarMapa()
    {
        materialCache.Clear();
        physMatCajas = new PhysicsMaterial("Friccion_Cajas")
        {
            dynamicFriction = 0.6f,
            staticFriction = 0.6f,
            bounciness = 0f
        };

        GameObject root = new GameObject("Mapa_Parkour_Generado");
        Undo.RegisterCreatedObjectUndo(root, "Generar Mapa Parkour");

        Transform tPlataformas = new GameObject("Plataformas").transform;
        tPlataformas.SetParent(root.transform);
        Transform tPuntos = new GameObject("PuntosDeReferencia").transform;
        tPuntos.SetParent(root.transform);
        Transform tSpawns = new GameObject("SpawnPoints").transform;
        tSpawns.SetParent(root.transform);
        Transform tCheckpoints = new GameObject("Checkpoints").transform;
        tCheckpoints.SetParent(root.transform);

        float z = 0f; // cursor que avanza a medida que armamos el recorrido

        // ---------- ZONA 0: Plataforma de inicio + 5 spawn points ----------
        GameObject inicio = CrearPlataforma(tPlataformas, "Inicio", new Vector3(0, 0, 0), new Vector3(16, 1, 10), colorSpawn);
        float[] offsetsX = { -6f, -3f, 0f, 3f, 6f };
        for (int i = 0; i < 5; i++)
        {
            CrearSpawnPoint(tSpawns, $"SpawnPoint_{i + 1}", new Vector3(offsetsX[i], 1f, -1f));
        }
        z = inicio.transform.position.z + inicio.transform.localScale.z / 2f;

        // ---------- ZONA 1: Sprint recto (largo, obliga a correr) ----------
        z += 1.5f;
        float largoSprint1 = 26f;
        GameObject sprint1 = CrearPlataforma(tPlataformas, "Sprint_01", new Vector3(0, 0, z + largoSprint1 / 2f), new Vector3(6, 1, largoSprint1), colorSprint);
        z += largoSprint1;

        // ---------- ZONA 2: Saltos entre plataformas (dificultad media) ----------
        z += 3f;
        int cantSaltos = 7;
        float anchoSalto = 3f;
        float gapSalto = 2.6f;
        for (int i = 0; i < cantSaltos; i++)
        {
            // Alterna un poco en X para que no sea un pasillo recto (mas dificil e interesante)
            float offsetX = (i % 2 == 0) ? -1f : 1f;
            Vector3 pos = new Vector3(offsetX, 0f, z);
            CrearPlataforma(tPlataformas, $"Salto_{i + 1}", pos, new Vector3(anchoSalto, 1, anchoSalto), colorSaltos);
            z += anchoSalto / 2f + gapSalto + anchoSalto / 2f;
        }

        // Checkpoint despues de la primera seccion de habilidad
        z += 1.5f;
        GameObject plataformaCheck1 = CrearPlataforma(tPlataformas, "Plataforma_Checkpoint_1", new Vector3(0, 0, z), new Vector3(5, 1, 4), colorSpawn);
        CrearCheckpoint(tCheckpoints, "Checkpoint_1", new Vector3(0, 1.2f, z), new Vector3(5, 2, 4));
        z += 2f;

        // ---------- ZONA 3: Gauntlet de plataformas caedizas ----------
        z += 3f;
        int cantCaedizas = 5;
        float anchoCaediza = 3.2f;
        float gapCaediza = 2.3f;
        for (int i = 0; i < cantCaedizas; i++)
        {
            Vector3 pos = new Vector3(0f, 0f, z);
            GameObject plat = CrearPlataforma(tPlataformas, $"Caediza_{i + 1}", pos, new Vector3(anchoCaediza, 1, anchoCaediza), colorCaediza);
            HacerCaediza(plat, delay: 0.6f);
            z += anchoCaediza / 2f + gapCaediza + anchoCaediza / 2f;
        }

        // ---------- ZONA 4: Plataformas moviles cruzando un pozo largo ----------
        z += 4f;
        GameObject bordeAntesMovil = CrearPlataforma(tPlataformas, "Borde_Antes_Moviles", new Vector3(0, 0, z), new Vector3(5, 1, 3), colorSprint);
        z += 3f;

        int cantMoviles = 3;
        float espacioEntreMoviles = 8f;
        for (int i = 0; i < cantMoviles; i++)
        {
            float zPlataforma = z + i * espacioEntreMoviles;
            GameObject movil = CrearPlataforma(tPlataformas, $"Movil_{i + 1}", new Vector3(0, 0, zPlataforma), new Vector3(3.5f, 1, 3.5f), colorMovil);
            // Se mueve de lado a lado (eje X), cruzando el pozo
            HacerMovil(movil, tPuntos, new Vector3(4.5f, 0, zPlataforma), velocidad: 2.5f, espera: 0.4f);
        }
        z += (cantMoviles - 1) * espacioEntreMoviles + 4f;

        GameObject bordeDespuesMovil = CrearPlataforma(tPlataformas, "Borde_Despues_Moviles", new Vector3(0, 0, z), new Vector3(5, 1, 3), colorSprint);
        z += 1.5f;

        // Checkpoint 2
        z += 2f;
        GameObject plataformaCheck2 = CrearPlataforma(tPlataformas, "Plataforma_Checkpoint_2", new Vector3(0, 0, z), new Vector3(5, 1, 4), colorSpawn);
        CrearCheckpoint(tCheckpoints, "Checkpoint_2", new Vector3(0, 1.2f, z), new Vector3(5, 2, 4));
        z += 2f;

        // ---------- ZONA 5: Plataformas rotantes ----------
        z += 3.5f;
        int cantRotantes = 4;
        float anchoRotante = 4f;
        float gapRotante = 2.8f;
        for (int i = 0; i < cantRotantes; i++)
        {
            Vector3 pos = new Vector3(0f, 0f, z);
            GameObject plat = CrearPlataforma(tPlataformas, $"Rotante_{i + 1}", pos, new Vector3(anchoRotante, 0.6f, anchoRotante), colorRotante);
            HacerRotante(plat, velocidad: 35f + i * 5f);
            z += anchoRotante / 2f + gapRotante + anchoRotante / 2f;
        }

        // ---------- ZONA 6: Puzzle con caja empujable ----------
        z += 4f;
        GameObject plataformaBaja = CrearPlataforma(tPlataformas, "Puzzle_Plataforma_Baja", new Vector3(0, 0, z), new Vector3(6, 1, 5), colorSaltos);
        Vector3 posCaja = new Vector3(0, 1.5f, z + 1.5f);
        GameObject caja = CrearPlataforma(tPuntos, "Caja_Empujable", posCaja, new Vector3(1.5f, 1.5f, 1.5f), colorCaja);
        HacerCaja(caja, physMatCajas);
        caja.transform.SetParent(tPlataformas);

        float zPlataformaAlta = z + 6.5f;
        GameObject plataformaAlta = CrearPlataforma(tPlataformas, "Puzzle_Plataforma_Alta", new Vector3(0, 1.5f, zPlataformaAlta), new Vector3(5, 1, 4), colorSaltos);
        z = zPlataformaAlta + 2f;

        // Checkpoint 3
        z += 1.5f;
        GameObject plataformaCheck3 = CrearPlataforma(tPlataformas, "Plataforma_Checkpoint_3", new Vector3(0, 1.5f, z), new Vector3(5, 1, 4), colorSpawn);
        CrearCheckpoint(tCheckpoints, "Checkpoint_3", new Vector3(0, 2.7f, z), new Vector3(5, 2, 4));
        z += 2f;

        // ---------- ZONA 7: Seccion dificil combinada (angosta, gaps grandes) ----------
        z += 4f;
        // Mezcla de plataformas angostas, una caediza y una rotante, gaps mas grandes
        Vector3[] offsetsDificil = { Vector3.zero, new Vector3(1.5f, 0, 0), new Vector3(-1.5f, 0, 0), Vector3.zero };
        for (int i = 0; i < 6; i++)
        {
            Vector3 offset = offsetsDificil[i % offsetsDificil.Length];
            Vector3 pos = new Vector3(offset.x, 1.5f, z) ;
            GameObject plat = CrearPlataforma(tPlataformas, $"Dificil_{i + 1}", pos, new Vector3(2.2f, 1, 2.2f), colorDificil);

            if (i == 2) HacerCaediza(plat, delay: 0.5f);
            if (i == 4) HacerRotante(plat, velocidad: 50f);

            z += 1.1f + 3.2f + 1.1f; // gaps mas exigentes que en la zona 2
        }

        // ---------- Sprint final + Meta ----------
        z += 3f;
        float largoSprintFinal = 20f;
        GameObject sprintFinal = CrearPlataforma(tPlataformas, "Sprint_Final", new Vector3(0, 1.5f, z + largoSprintFinal / 2f), new Vector3(6, 1, largoSprintFinal), colorSprint);
        z += largoSprintFinal;

        z += 1.5f;
        GameObject metaPlataforma = CrearPlataforma(tPlataformas, "Plataforma_Meta", new Vector3(0, 1.5f, z), new Vector3(8, 1, 4), colorMeta);
        CrearMeta(tCheckpoints, "FinishLine", new Vector3(0, 2.8f, z + 1f), new Vector3(8, 3, 1));

        float largoTotalMapa = z + 10f;

        // ---------- Zona de vacio (respawn automatico si te caes) ----------
        GameObject killZone = new GameObject("KillZone");
        killZone.transform.SetParent(root.transform);
        killZone.transform.position = new Vector3(0, -25f, largoTotalMapa / 2f);
        BoxCollider killCol = killZone.AddComponent<BoxCollider>();
        killCol.isTrigger = true;
        killCol.size = new Vector3(60, 15, largoTotalMapa + 40);
        killZone.AddComponent<KillZone>();

        // RaceManager: objeto DE ESCENA (no persistente), uno por nivel.
        // Trae su propio NetworkObject via RequireComponent.
        GameObject raceManager = new GameObject("RaceManager");
        raceManager.transform.SetParent(root.transform);
        raceManager.AddComponent<RaceManager>();
        Undo.RegisterCreatedObjectUndo(raceManager, "Crear RaceManager");

        Selection.activeGameObject = root;
        EditorSceneManager_MarkDirty();

        Debug.Log($"Mapa de parkour generado: largo total ~{largoTotalMapa:F0}m, con 5 spawn points, 3 checkpoints, gauntlet de caedizas, moviles, rotantes, puzzle de caja y meta.");
    }

    private static void EditorSceneManager_MarkDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }

    // ---------------- Helpers ----------------

    private static GameObject CrearPlataforma(Transform parent, string nombre, Vector3 posicion, Vector3 tamano, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(parent);
        go.transform.position = posicion;
        go.transform.localScale = tamano;

        Material mat = GetMaterial(color);
        if (mat != null)
            go.GetComponent<Renderer>().sharedMaterial = mat;

        Undo.RegisterCreatedObjectUndo(go, "Crear plataforma");
        return go;
    }

    private static Transform CrearSpawnPoint(Transform parent, string nombre, Vector3 posicion)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent);
        go.transform.position = posicion;
        Undo.RegisterCreatedObjectUndo(go, "Crear spawn point");
        return go.transform;
    }

    private static Transform CrearPuntoVacio(Transform parent, string nombre, Vector3 posicion)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent);
        go.transform.position = posicion;
        return go.transform;
    }

    private static void CrearCheckpoint(Transform parent, string nombre, Vector3 posicion, Vector3 tamano)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent);
        go.transform.position = posicion;
        BoxCollider col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = tamano;
        go.AddComponent<CheckpointZone>();
    }

    private static void CrearMeta(Transform parent, string nombre, Vector3 posicion, Vector3 tamano)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent);
        go.transform.position = posicion;
        BoxCollider col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = tamano;
        go.AddComponent<FinishLine>();
    }

    private static void HacerCaediza(GameObject plataforma, float delay)
    {
        FallingPlatform fp = plataforma.AddComponent<FallingPlatform>();
        SerializedObject so = new SerializedObject(fp);
        so.FindProperty("delayAntesDeCaer").floatValue = delay;
        so.FindProperty("sacudirAntesDeCaer").boolValue = true;
        so.FindProperty("respawnear").boolValue = true;
        so.FindProperty("tiempoParaRespawn").floatValue = 3f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void HacerMovil(GameObject plataforma, Transform puntosParent, Vector3 posicionB, float velocidad, float espera)
    {
        Transform puntoA = CrearPuntoVacio(puntosParent, plataforma.name + "_PuntoA", plataforma.transform.position);
        Transform puntoB = CrearPuntoVacio(puntosParent, plataforma.name + "_PuntoB", posicionB);

        MovingPlatform mp = plataforma.AddComponent<MovingPlatform>();
        SerializedObject so = new SerializedObject(mp);
        so.FindProperty("puntoA").objectReferenceValue = puntoA;
        so.FindProperty("puntoB").objectReferenceValue = puntoB;
        so.FindProperty("velocidad").floatValue = velocidad;
        so.FindProperty("esperaEnPuntos").floatValue = espera;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void HacerRotante(GameObject plataforma, float velocidad)
    {
        RotatingPlatform rp = plataforma.AddComponent<RotatingPlatform>();
        rp.rotationSpeed = velocidad;
        rp.rotationAxis = Vector3.up;
    }

    private static void HacerCaja(GameObject cubo, PhysicsMaterial mat)
    {
        cubo.AddComponent<InteractiveBox>();
        BoxCollider col = cubo.GetComponent<BoxCollider>();
        col.sharedMaterial = mat;
    }

    private static Material GetMaterial(Color color)
    {
        if (materialCache.TryGetValue(color, out Material cached))
            return cached;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return null;

        Material mat = new Material(shader) { color = color };
        materialCache[color] = mat;
        return mat;
    }
}
#endif