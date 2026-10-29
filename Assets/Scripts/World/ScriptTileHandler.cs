using System.Collections.Generic;
using UnityEngine;

public class ScriptTileHandler : MonoBehaviour
{
    [Header("Fixed Obstacle Group")]
    public GameObject obstacleContainer;

    [Header("Collectible Prefabs")]
    public GameObject coinPrefab;
    public GameObject[] powerUpPrefabs;

    [Header("Coin Row Settings")]
    [Range(0f, 1f)] public float coinRowChance = 0.75f; 
    public int minCoinsInRow = 4;                    
    public int maxCoinsInRow = 6;                   
    public float coinSpacingZ = 2.0f;                  
    public float spawnHeightY = 1.0f;                 

    [Header("Single Power-Up Settings")]
    [Range(0f, 1f)] public float powerUpChance = 0.20f; 

    [Header("Obstacle Collision Check")]
    public float obstacleCheckRadius = 0.75f;        

    private readonly List<GameObject> spawnedItems = new List<GameObject>();

    public void OnTileActivated(bool allowObstaclesAndItems)
    {
        ActivateTile(allowObstaclesAndItems);
    }

    public void OnTileDeactivated()
    {
        ClearSpawnedItems();
    }

    public void ActivateTile(bool allowObstaclesAndItems)
    {
        ClearSpawnedItems();

        if (obstacleContainer != null)
        {
            obstacleContainer.SetActive(allowObstaclesAndItems);
        }

        if (!allowObstaclesAndItems) return;

        Physics.SyncTransforms();

        int usedCoinLane = -1;
        if (coinPrefab != null && Random.value <= coinRowChance)
        {
            usedCoinLane = Random.Range(0, GameConstants.LaneCount);
            SpawnCoinRow(usedCoinLane);
        }

        if (powerUpPrefabs != null && powerUpPrefabs.Length > 0 && Random.value <= powerUpChance)
        {
            SpawnSinglePowerUp(usedCoinLane);
        }
    }

    private void SpawnCoinRow(int laneIndex)
    {
        float laneX = GameConstants.LaneToX(laneIndex);
        int coinCount = Random.Range(minCoinsInRow, maxCoinsInRow + 1);

        float startLocalZ = Random.Range(3f, 8f);

        for (int i = 0; i < coinCount; i++)
        {
            float localZ = startLocalZ + (i * coinSpacingZ);

            if (localZ > GameConstants.TileLength - 2f) break;

            Vector3 worldPos = transform.position + new Vector3(laneX, spawnHeightY, localZ);

            if (!IsBlockedByObstacle(worldPos))
            {
                GameObject coin = Instantiate(coinPrefab, worldPos, Quaternion.identity, transform);
                spawnedItems.Add(coin);
            }
        }
    }

    private void SpawnSinglePowerUp(int coinLaneToAvoid)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            int laneIndex = Random.Range(0, GameConstants.LaneCount);

            if (laneIndex == coinLaneToAvoid && attempt < 3) continue;

            float laneX = GameConstants.LaneToX(laneIndex);
            float localZ = Random.Range(4f, GameConstants.TileLength - 4f);
            Vector3 worldPos = transform.position + new Vector3(laneX, spawnHeightY, localZ);

            if (!IsBlockedByObstacle(worldPos) && !IsBlockedByCollectible(worldPos))
            {
                GameObject prefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];
                GameObject powerUp = Instantiate(prefab, worldPos, Quaternion.identity, transform);
                spawnedItems.Add(powerUp);
                break; 
            }
        }
    }

    private bool IsBlockedByObstacle(Vector3 worldPos) => IsBlockedByTags(worldPos, "Obstacle");
    private bool IsBlockedByCollectible(Vector3 worldPos) => IsBlockedByTags(worldPos, "Coin", "PowerUp");

    private bool IsBlockedByTags(Vector3 worldPos, params string[] tags)
    {
        Collider[] hits = Physics.OverlapSphere(
            worldPos,
            obstacleCheckRadius,
            ~0,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < hits.Length; i++)
        {
            for (int t = 0; t < tags.Length; t++)
            {
                if (hits[i].CompareTag(tags[t])) return true;
            }
        }
        return false;
    }

    public void ClearSpawnedItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
            {
                Destroy(spawnedItems[i]);
            }
        }
        spawnedItems.Clear();
    }
}