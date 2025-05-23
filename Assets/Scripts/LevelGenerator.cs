using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
public class LevelGenerator : MonoBehaviour
{
    [Header("Ring")]
    public List<GameObject> ringPrefabs;
    [SerializeField] float ringRadius = 5f;

    [Header("Props")]
    public List<GameObject> propsPrefabs;

    [Header("Map customization")]
    public Transform plane;
    [SerializeField] float cellSize = 1f;
    [SerializeField] float propDesity = 0.5f;
    [SerializeField] float spawnAreaScale = 0.6f;
    [SerializeField] int minProps, maxProps;

    [Header("Pool Settings")]
    [SerializeField] ObjectPool objectPool;
    [SerializeField] int initialPropPoolSize = 20;
    [SerializeField] int initialRingPoolSize = 5;

    int gridWidth, gridHeight;
    public Transform sceneLayout;
    public NavMeshSurface navMeshSurface;

    Vector3 planeSize;
    Vector3 spawnAreaSize;

    private List<GameObject> activeProps = new List<GameObject>();
    private GameObject currentRing;

    void Awake()
    {
        SetupObjectPools();
    }

    void Start()
    {
        if (objectPool != null && objectPool.poolDictionary == null)
        {
            SetupObjectPools();
        }
        GenerateLevel();
    }

    private void SetupObjectPools()
    {
        if (objectPool == null)
        {
            //Debug.LogError("ObjectPool reference is missing!");
            return;
        }
        if (objectPool.pools == null)
        {
            objectPool.pools = new List<ObjectPool.Pool>();
        }

        if (objectPool.poolDictionary == null)
        {
            objectPool.GetComponent<ObjectPool>().InitializePools();
        }

        // Setup ring pools
        for (int i = 0; i < ringPrefabs.Count; i++)
        {
            string poolTag = "Ring_" + i;
            bool poolExists = false;
            foreach (var pool in objectPool.pools)
            {
                if (pool.tag == poolTag)
                {
                    poolExists = true;
                    break;
                }
            }

            if (!poolExists)
            {
                ObjectPool.Pool newPool = new ObjectPool.Pool
                {
                    tag = poolTag,
                    prefab = ringPrefabs[i],
                    size = initialRingPoolSize,
                    parent = sceneLayout
                };
                objectPool.pools.Add(newPool);
            }
        }

        // Setup prop pools
        for (int i = 0; i < propsPrefabs.Count; i++)
        {
            string poolTag = "Prop_" + i;
            bool poolExists = false;
            foreach (var pool in objectPool.pools)
            {
                if (pool.tag == poolTag)
                {
                    poolExists = true;
                    break;
                }
            }

            if (!poolExists)
            {
                ObjectPool.Pool newPool = new ObjectPool.Pool
                {
                    tag = poolTag,
                    prefab = propsPrefabs[i],
                    size = initialPropPoolSize,
                    parent = sceneLayout
                };
                objectPool.pools.Add(newPool);
            }
        }

        objectPool.GetComponent<ObjectPool>().InitializePools();
    }

    public void GenerateLevel()
    {
        CleanupLevel();

        CalculateDimension();
        GameObject ring = SpawnRing();
        SpawnProps(ring);
        BakeNavMesh();
    }

    public GameObject SpawnRing()
    {
        int ringToSpawn = Random.Range(0, ringPrefabs.Count);
        Vector3 planeCenter = plane.position;
        string poolTag = "Ring_" + ringToSpawn;

        if (currentRing != null)
        {
            string prevPoolTag = GetPoolTagForRing(currentRing);
            if (!string.IsNullOrEmpty(prevPoolTag))
            {
                objectPool.ReturnToPool(prevPoolTag, currentRing);
            }
        }

        currentRing = objectPool.SpawnFromPool(poolTag, planeCenter, Quaternion.identity, sceneLayout);

        if (currentRing != null)
        {
            ResetRingState(currentRing);
            PositionRingCorrectly(currentRing);
            //Debug.Log($"Spawned {ringPrefabs[ringToSpawn].name} from pool!");
        }
        else
        {
            //Debug.LogWarning($"Failed to spawn ring from pool: {poolTag}");
        }

        return currentRing;
    }

    public void SpawnProps(GameObject ring)
    {
        //Debug.Log("Spawning props from pool...");
        Renderer planeRenderer = plane.GetComponent<Renderer>();
        spawnAreaSize = planeSize * spawnAreaScale;
        Vector3 bottomLeft = plane.position - new Vector3(spawnAreaSize.x / 2, 0, spawnAreaSize.z / 2);

        int spawnGridWidth = Mathf.RoundToInt(spawnAreaSize.x / cellSize);
        int spawnGridHeight = Mathf.RoundToInt(spawnAreaSize.z / cellSize);
        Vector3 ringPosition = ring.transform.position;

        int spawnAmount = Random.Range(minProps, maxProps + 1);
        int spawnCount = 0;
        int maxAttempts = spawnAmount * 3;
        int attempt = 0;

        while (spawnCount < spawnAmount && attempt < maxAttempts)
        {
            attempt++;
            float x = Random.Range(0, spawnGridWidth);
            float z = Random.Range(0, spawnGridHeight);
            Vector3 propPosition = bottomLeft + new Vector3((x + 0.5f) * cellSize, 0, (z + 0.5f) * cellSize);
            float distanceToRing = Vector3.Distance(new Vector3(propPosition.x, 0, propPosition.z), new Vector3(ringPosition.x, 0, ringPosition.z));

            if (distanceToRing < ringRadius || IsPositionOccupied(propPosition))
            {
                continue;
            }

            int propsToSpawn = Random.Range(0, propsPrefabs.Count);
            string poolTag = "Prop_" + propsToSpawn;

            GameObject propsGO = objectPool.SpawnFromPool(poolTag, propPosition, Quaternion.identity, sceneLayout);

            if (propsGO != null)
            {
                ResetPropState(propsGO);
                PositionPropCorrectly(propsGO, planeRenderer);

                if (CheckForOverlap(propsGO))
                {
                    objectPool.ReturnToPool(poolTag, propsGO);
                    continue;
                }

                activeProps.Add(propsGO);
                spawnCount++;
            }
            else
            {
                objectPool.ExpandPool(poolTag, 10);
            }
        }
        //Debug.Log($"Attempt to spawn: {attempt}, spawnCount:{spawnCount}");
    }

    private void ResetRingState(GameObject ring)
    {
        ring.transform.rotation = Quaternion.identity;
        ring.transform.localScale = Vector3.one;
    }

    private void ResetPropState(GameObject prop)
    {
        prop.transform.rotation = Quaternion.identity;
        prop.transform.localScale = Vector3.one;
    }

    private void PositionRingCorrectly(GameObject ring)
    {
        Renderer ringRenderer = ring.GetComponent<Renderer>();
        if (ringRenderer == null)
        {
            ringRenderer = ring.GetComponentInChildren<Renderer>();
        }
        if (ringRenderer != null)
        {
            float ringHeight = ringRenderer.bounds.size.y;
            Vector3 currentRingPos = ring.transform.position;
            ring.transform.position = new Vector3(currentRingPos.x, ringHeight, currentRingPos.z);
        }
    }

    private void PositionPropCorrectly(GameObject prop, Renderer planeRenderer)
    {
        Renderer propsRenderer = prop.GetComponent<Renderer>();
        if (propsRenderer == null)
        {
            propsRenderer = prop.GetComponentInChildren<Renderer>();
        }

        if (propsRenderer != null)
        {
            // Calculate bottom
            float objectBottomY = propsRenderer.bounds.min.y;
            float objectHeight = propsRenderer.bounds.size.y;

            float planeTopY = plane.position.y + (planeRenderer.bounds.size.y / 2);

            Vector3 currentPropPos = prop.transform.position;
            float yOffset = planeTopY - objectBottomY;
            prop.transform.position = new Vector3(currentPropPos.x, currentPropPos.y + yOffset, currentPropPos.z);
        }
    }

    private string GetPoolTagForRing(GameObject ring)
    {
        for (int i = 0; i < ringPrefabs.Count; i++)
        {
            if (ring.name.Contains(ringPrefabs[i].name))
            {
                return "Ring_" + i;
            }
        }
        return null;
    }

    private string GetPoolTagForProp(GameObject prop)
    {
        for (int i = 0; i < propsPrefabs.Count; i++)
        {
            if (prop.name.Contains(propsPrefabs[i].name))
            {
                return "Prop_" + i;
            }
        }
        return null;
    }

    private void CalculateDimension()
    {
        Renderer planeRenderer = plane.GetComponent<Renderer>();
        planeSize = planeRenderer.bounds.size;
        gridWidth = Mathf.RoundToInt(planeSize.x / cellSize);
        gridHeight = Mathf.RoundToInt(planeSize.z / cellSize);
    }

    private void BakeNavMesh()
    {
        navMeshSurface.BuildNavMesh();
        //Debug.Log("NavMesh baked successfully!");
    }

    private bool IsPositionOccupied(Vector3 position)
    {
        Vector3 checkPosition = new Vector3(position.x, position.y + 0.1f, position.z);
        float checkRadius = cellSize * 0.3f;
        Collider[] colliders = Physics.OverlapSphere(checkPosition, checkRadius);
        foreach (Collider collider in colliders)
        {
            if (collider.transform != plane)
            {
                return true;
            }
        }
        return false;
    }

    private bool CheckForOverlap(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0) return false;

        foreach (Collider col in colliders)
        {
            Vector3 center = col.bounds.center;
            Vector3 halfExtents = col.bounds.extents;
            Quaternion rotation = col.transform.rotation;

            Collider[] overlapColliders = Physics.OverlapBox(center, halfExtents, rotation);

            foreach (Collider overlap in overlapColliders)
            {
                if (overlap.transform == obj.transform ||
                   overlap.transform.IsChildOf(obj.transform) ||
                   obj.transform.IsChildOf(overlap.transform) ||
                   overlap.transform == plane)
                {
                    continue;
                }
                return true;
            }
        }
        return false;
    }
    public void CleanupLevel()
    {
        // Return all active props to pool
        foreach (GameObject prop in activeProps)
        {
            if (prop != null && prop.activeInHierarchy)
            {
                string poolTag = GetPoolTagForProp(prop);
                if (!string.IsNullOrEmpty(poolTag))
                {
                    objectPool.ReturnToPool(poolTag, prop);
                }
            }
        }
        activeProps.Clear();

        // Return current ring to pool
        if (currentRing != null && currentRing.activeInHierarchy)
        {
            string poolTag = GetPoolTagForRing(currentRing);
            if (!string.IsNullOrEmpty(poolTag))
            {
                objectPool.ReturnToPool(poolTag, currentRing);
            }
            currentRing = null;
        }
    }

    public void RegenerateLevel()
    {
        GenerateLevel();
    }

    private void OnDrawGizmos()
    {
        if (plane != null)
        {
            Gizmos.color = Color.red;
            Renderer planeRenderer = plane.GetComponent<Renderer>();
            Gizmos.DrawWireCube(plane.position, planeSize * spawnAreaScale);
        }
    }
    private void OnDestroy()
    {
        CleanupLevel();
    }
}
