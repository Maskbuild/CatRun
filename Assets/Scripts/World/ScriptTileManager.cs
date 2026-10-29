using System.Collections.Generic;
using UnityEngine;

public class ScriptTileManager : MonoBehaviour
{
    [Header("Player Reference")]
    public Transform player;

    [Header("Custom Starting")]
    public GameObject[] startingTilePrefabs;

    [Header("Pre-built Tile Sets")]
    public GameObject[] tileSetPrefabs;
    public int copiesPerSet = 3;

    [Header("Track Dimensions & Pool Settings")]
    public float tileLength = GameConstants.TileLength;
    public int tilesOnScreen = 6;
    public int safeTiles = 2;

    [Header("Delay Before Hiding")]
    public float hideBehindDistance = 25f;

    private float nextSpawnZ = 0f;
    private int tilesSpawned = 0;

    private readonly List<GameObject> inactivePool = new List<GameObject>();

    private readonly Queue<GameObject> activeTiles = new Queue<GameObject>();

    private readonly HashSet<GameObject> customStartingTiles = new HashSet<GameObject>();

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (tileLength <= 1f)
        {
            tileLength = GameConstants.TileLength;
        }

        nextSpawnZ = (player != null) ? (player.position.z - 5f) : -5f;
        tilesSpawned = 0;

        InitializeTilePool();

        if (startingTilePrefabs != null && startingTilePrefabs.Length > 0)
        {
            for (int i = 0; i < startingTilePrefabs.Length; i++)
            {
                if (startingTilePrefabs[i] != null)
                {
                    SpawnCustomStartingTile(startingTilePrefabs[i], i);
                }
            }
        }

        while (activeTiles.Count < tilesOnScreen)
        {
            if (tileSetPrefabs == null || tileSetPrefabs.Length == 0) break;
            ActivateNextTile();
        }
    }

    void Update()
    {
        if (player == null) return;

        while (nextSpawnZ < player.position.z + (tilesOnScreen * tileLength))
        {
            if (tileSetPrefabs == null || tileSetPrefabs.Length == 0) break;
            ActivateNextTile();
        }

        if (activeTiles.Count > 0)
        {
            GameObject oldestTile = activeTiles.Peek();
            float oldestTileEndZ = oldestTile.transform.position.z + tileLength;

            if (player.position.z - oldestTileEndZ >= hideBehindDistance)
            {
                RecycleOldestTile();
            }
        }
    }

    private void SpawnCustomStartingTile(GameObject startPrefab, int index)
    {
        GameObject startTile = Instantiate(startPrefab, new Vector3(0f, 0f, nextSpawnZ), Quaternion.identity, transform);
        startTile.name = $"{startPrefab.name}_Start_{index}";
        startTile.SetActive(true);

        customStartingTiles.Add(startTile);
        activeTiles.Enqueue(startTile);

        nextSpawnZ += tileLength;
        tilesSpawned++;
    }

    private void InitializeTilePool()
    {
        if (tileSetPrefabs == null || tileSetPrefabs.Length == 0)
        {
            Debug.LogWarning("[ScriptTileManager] ยังไม่ได้ใส่ Tile Set Prefabs ใน Inspector!");
            return;
        }

        int countPerPrefab = Mathf.Max(1, copiesPerSet);
        int extraTilesBehind = Mathf.CeilToInt(Mathf.Max(0f, hideBehindDistance) / tileLength) + 2;
        int requiredTotalTiles = tilesOnScreen + extraTilesBehind + 2;

        while (tileSetPrefabs.Length * countPerPrefab < requiredTotalTiles)
        {
            countPerPrefab++;
        }

        for (int i = 0; i < tileSetPrefabs.Length; i++)
        {
            if (tileSetPrefabs[i] == null) continue;

            for (int c = 0; c < countPerPrefab; c++)
            {
                CreateNewPooledTile(tileSetPrefabs[i], c);
            }
        }
    }

    private GameObject CreateNewPooledTile(GameObject prefab, int copyIndex)
    {
        GameObject tileObj = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
        tileObj.name = $"{prefab.name}_Pool_{copyIndex}";
        tileObj.SetActive(false);
        inactivePool.Add(tileObj);
        return tileObj;
    }

    private void ActivateNextTile()
    {
        if (inactivePool.Count == 0)
        {
            if (tileSetPrefabs == null || tileSetPrefabs.Length == 0) return;
            GameObject fallbackPrefab = tileSetPrefabs[Random.Range(0, tileSetPrefabs.Length)];
            if (fallbackPrefab == null) return;
            CreateNewPooledTile(fallbackPrefab, activeTiles.Count + 1);
        }

        int randomIndex = Random.Range(0, inactivePool.Count);
        GameObject tile = inactivePool[randomIndex];
        inactivePool.RemoveAt(randomIndex);

        tile.transform.position = new Vector3(0f, 0f, nextSpawnZ);
        tile.transform.rotation = Quaternion.identity;
        tile.SetActive(true);

        bool hasCustomStart = startingTilePrefabs != null && startingTilePrefabs.Length > 0;
        bool allowObstacles = hasCustomStart || (tilesSpawned >= safeTiles);

        ScriptTileHandler handler = tile.GetComponent<ScriptTileHandler>();
        if (handler != null)
        {
            handler.OnTileActivated(allowObstacles);
        }

        activeTiles.Enqueue(tile);
        nextSpawnZ += tileLength;
        tilesSpawned++;
    }

    private void RecycleOldestTile()
    {
        if (activeTiles.Count == 0) return;

        GameObject oldestTile = activeTiles.Dequeue();

        ScriptTileHandler handler = oldestTile.GetComponent<ScriptTileHandler>();
        if (handler != null)
        {
            handler.OnTileDeactivated();
        }

        oldestTile.SetActive(false);

        if (!customStartingTiles.Contains(oldestTile))
        {
            inactivePool.Add(oldestTile);
        }
    }
}