using System.Collections.Generic;
using UnityEngine;

public class ScriptBackgroundManager : MonoBehaviour
{
    public enum SpawnOrderMode
    {
        Sequential,
        Random      
    }

    [Header("Player Reference")]
    public Transform player;

    [Header("Custom Starting Backgrounds")]
    public GameObject[] startingBackgroundPrefabs;

    [Header("Background Sets")]
    public GameObject[] backgroundPrefabs;
    public SpawnOrderMode orderMode = SpawnOrderMode.Sequential;
    public int copiesPerPrefab = 3;

    [Header("Background Dimensions & Pool Settings")]
    public float backgroundLength = 20f;
    public int backgroundsOnScreen = 6;
    public float hideBehindDistance = 30f;

    [Header("Position Offset")]
    public Vector2 positionOffsetXY = Vector2.zero;

    private float nextSpawnZ = 0f;
    private int currentSequenceIndex = 0;

    private List<GameObject>[] inactivePoolsByType;

    private readonly Queue<PooledBackgroundItem> activeBackgrounds = new Queue<PooledBackgroundItem>();

    private struct PooledBackgroundItem
    {
        public GameObject instance;
        public int typeIndex; 
    }

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (backgroundLength <= 1f)
        {
            backgroundLength = GameConstants.TileLength;
        }

        nextSpawnZ = (player != null) ? (player.position.z - 5f) : -5f;
        currentSequenceIndex = 0;

        InitializeBackgroundPool();

        if (startingBackgroundPrefabs != null && startingBackgroundPrefabs.Length > 0)
        {
            for (int i = 0; i < startingBackgroundPrefabs.Length; i++)
            {
                if (startingBackgroundPrefabs[i] != null)
                {
                    SpawnStartingBackground(startingBackgroundPrefabs[i], i);
                }
            }
        }

        while (activeBackgrounds.Count < backgroundsOnScreen)
        {
            if (backgroundPrefabs == null || backgroundPrefabs.Length == 0) break;
            ActivateNextBackground();
        }
    }

    void Update()
    {
        if (player == null) return;

        while (nextSpawnZ < player.position.z + (backgroundsOnScreen * backgroundLength))
        {
            if (backgroundPrefabs == null || backgroundPrefabs.Length == 0) break;
            ActivateNextBackground();
        }

        if (activeBackgrounds.Count > 0)
        {
            PooledBackgroundItem oldest = activeBackgrounds.Peek();
            if (oldest.instance != null)
            {
                float oldestEndZ = oldest.instance.transform.position.z + backgroundLength;
                if (player.position.z - oldestEndZ >= hideBehindDistance)
                {
                    RecycleOldestBackground();
                }
            }
            else
            {
                activeBackgrounds.Dequeue();
            }
        }
    }

    private void InitializeBackgroundPool()
    {
        if (backgroundPrefabs == null || backgroundPrefabs.Length == 0)
        {
            Debug.LogWarning("[ScriptBackgroundManager] ยังไม่ได้ใส่ Background Prefabs ใน Inspector!");
            return;
        }

        inactivePoolsByType = new List<GameObject>[backgroundPrefabs.Length];

        int countPerPrefab = Mathf.Max(1, copiesPerPrefab);
        int extraBehind = Mathf.CeilToInt(Mathf.Max(0f, hideBehindDistance) / backgroundLength) + 2;
        int requiredTotal = backgroundsOnScreen + extraBehind + 2;

        while (backgroundPrefabs.Length * countPerPrefab < requiredTotal)
        {
            countPerPrefab++;
        }

        for (int i = 0; i < backgroundPrefabs.Length; i++)
        {
            inactivePoolsByType[i] = new List<GameObject>();
            if (backgroundPrefabs[i] == null) continue;

            for (int c = 0; c < countPerPrefab; c++)
            {
                CreateNewPooledBackground(i, c);
            }
        }
    }

    private GameObject CreateNewPooledBackground(int typeIndex, int copyNumber)
    {
        GameObject prefab = backgroundPrefabs[typeIndex];
        GameObject bgObj = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
        bgObj.name = $"{prefab.name}_BGPool_{copyNumber}";
        bgObj.SetActive(false);
        inactivePoolsByType[typeIndex].Add(bgObj);
        return bgObj;
    }

    private void SpawnStartingBackground(GameObject startPrefab, int index)
    {
        Vector3 spawnPos = new Vector3(positionOffsetXY.x, positionOffsetXY.y, nextSpawnZ);
        GameObject startBg = Instantiate(startPrefab, spawnPos, Quaternion.identity, transform);
        startBg.name = $"{startPrefab.name}_BGStart_{index}";
        startBg.SetActive(true);

        activeBackgrounds.Enqueue(new PooledBackgroundItem
        {
            instance = startBg,
            typeIndex = -1
        });

        nextSpawnZ += backgroundLength;
    }

    private void ActivateNextBackground()
    {
        if (backgroundPrefabs == null || backgroundPrefabs.Length == 0) return;

        int chosenType = 0;
        if (orderMode == SpawnOrderMode.Sequential)
        {
            chosenType = currentSequenceIndex % backgroundPrefabs.Length;
            currentSequenceIndex = (currentSequenceIndex + 1) % backgroundPrefabs.Length;
        }
        else
        {
            chosenType = Random.Range(0, backgroundPrefabs.Length);
        }

        if (backgroundPrefabs[chosenType] == null) return;

        List<GameObject> pool = inactivePoolsByType[chosenType];

        if (pool.Count == 0)
        {
            CreateNewPooledBackground(chosenType, activeBackgrounds.Count + 1);
        }

        int lastIdx = pool.Count - 1;
        GameObject bgObj = pool[lastIdx];
        pool.RemoveAt(lastIdx);

        bgObj.transform.position = new Vector3(positionOffsetXY.x, positionOffsetXY.y, nextSpawnZ);
        bgObj.transform.rotation = Quaternion.identity;
        bgObj.SetActive(true);

        activeBackgrounds.Enqueue(new PooledBackgroundItem
        {
            instance = bgObj,
            typeIndex = chosenType
        });

        nextSpawnZ += backgroundLength;
    }

    private void RecycleOldestBackground()
    {
        if (activeBackgrounds.Count == 0) return;

        PooledBackgroundItem oldest = activeBackgrounds.Dequeue();
        if (oldest.instance == null) return;

        oldest.instance.SetActive(false);

        if (oldest.typeIndex >= 0 && oldest.typeIndex < inactivePoolsByType.Length)
        {
            inactivePoolsByType[oldest.typeIndex].Add(oldest.instance);
        }
    }
}