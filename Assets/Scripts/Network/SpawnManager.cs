using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager instance;

    [Tooltip("Si es true, busca automáticamente hijos con nombre 'SpawnX' al iniciar cada escena.")]
    public bool buscarAutomatico = true;

    private Transform[] spawnPoints;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        BuscarSpawnsEnEscena();
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (buscarAutomatico)
            BuscarSpawnsEnEscena();
    }

    public void BuscarSpawnsEnEscena()
    {
        // Buscar objetos con nombres Spawn0, Spawn1, etc.
        System.Collections.Generic.List<Transform> lista = new System.Collections.Generic.List<Transform>();
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name.StartsWith("Spawn") && go.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene())
            {
                lista.Add(go.transform);
            }
        }
        spawnPoints = lista.ToArray();
    }

    public Transform GetSpawn(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;
        return spawnPoints[index % spawnPoints.Length];
    }
}
