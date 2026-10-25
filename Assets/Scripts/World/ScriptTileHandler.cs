using System.Collections.Generic;
using UnityEngine;

public class ScriptTileHandler : MonoBehaviour
{
    [Header("Fixed Obstacle Group (กลุ่มสิ่งกีดขวางที่วางเซตไว้ใน Tile นี้)")]
    [Tooltip("ลาก Empty GameObject ที่รวม Obstacle ของ Tile นี้มาใส่ (เพื่อให้ปิดซ่อนได้ใน 2 แผ่นแรกตอนเริ่มเกม)")]
    public GameObject obstacleContainer;

    [Header("Collectible Prefabs (สิ่งที่ต้องสุ่มเกิด)")]
    public GameObject coinPrefab;
    public GameObject[] powerUpPrefabs;

    [Header("Coin Row Settings (สุ่มเกิดเหรียญเป็นแถว)")]
    [Range(0f, 1f)] public float coinRowChance = 0.75f; 
    public int minCoinsInRow = 4;                    
    public int maxCoinsInRow = 6;                   
    public float coinSpacingZ = 2.0f;                  
    public float spawnHeightY = 1.0f;                 

    [Header("Single Power-Up Settings (สุ่มเกิด Power-Up แบบเดี่ยว)")]
    [Range(0f, 1f)] public float powerUpChance = 0.20f; 

    [Header("Obstacle Collision Check (เช็กว่ามีสิ่งกีดขวางขวางจุดเกิดไหม)")]
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

    private bool IsBlockedByObstacle(Vector3 worldPos)
    {
        Collider[] hits = Physics.OverlapSphere(
            worldPos,
            obstacleCheckRadius,
            ~0,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider col in hits)
        {
            if (col.CompareTag("Obstacle"))
            {
                return true; 
            }
        }
        return false;
    }

    private bool IsBlockedByCollectible(Vector3 worldPos)
    {
        Collider[] hits = Physics.OverlapSphere(
            worldPos,
            obstacleCheckRadius,
            ~0,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider col in hits)
        {
            if (col.CompareTag("Coin") || col.CompareTag("PowerUp"))
            {
                return true;
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