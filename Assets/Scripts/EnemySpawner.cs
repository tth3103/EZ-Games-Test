using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum GameMode
{
    OneVsOne,
    OneVsMany,
    ManyVsMany
}
public class EnemySpawner : MonoBehaviour
{
    [Header("Team References")]
    [SerializeField] Transform playerTeamTransform;
    [SerializeField] Transform comTeamTransform;
    [SerializeField] GameObject allyPrefab;

    [Header("Enemy Prefabs")]
    [SerializeField] List<GameObject> bossPrefabs;
    [SerializeField] List<GameObject> mobPrefabs;

    [Header("Spawn Settings")]
    [SerializeField] List<Transform> spawnWayPoints = new List<Transform>();
    [SerializeField] int baseEnemyAmount = 4;
    [SerializeField] int currentEnemyAmount = 0;
    [SerializeField] float baseSpawnInterval = 5f;
    [SerializeField] bool spawnable = true;

    [Header("Difficulty Settings")]
    [SerializeField] int currentLevel = 1;
    [SerializeField] float difficultyMultiplier = 0.3f;
    [SerializeField] int additionalEnemyPerLevel = 2;
    [SerializeField] float spawnIntervalReductionperLevel = 1f;
    [SerializeField] int bossInterval = 5;

    [Header("Components")]
    [SerializeField] LevelManager levelManager;
    [SerializeField] GameMode currentGameMode;
    [SerializeField] ObjectPool objectPool;

    [Header("Pool Settings")]
    [SerializeField] int initialPoolSize = 10;
    [SerializeField] int poolExpandAmount = 5;

    [SerializeField] float enemyAmount;
    float spawnInterval;
    bool bossSpawned = false;

    // Track for cleanup
    private List<GameObject> activeEnemies = new List<GameObject>();
    private GameObject currentAlly;
    private GameObject currentBoss;

    private void Awake()
    {
        LoadGameMode();
        InitializeDifficulty();
    }

    void Start()
    {
        playerTeamTransform = GameObject.Find("Player Team").transform;
        comTeamTransform = GameObject.Find("COM Team").transform;

        SetupObjectPools();

        if (currentGameMode == GameMode.ManyVsMany)
        {
            SpawnAlly();
        }
        if (currentGameMode == GameMode.OneVsOne || (IsBossLevel() && !bossSpawned))
        {
            SpawnBoss();
        }
    }

    void Update()
    {
        if (currentGameMode != GameMode.OneVsOne && !IsBossLevel())
        {
            if (currentEnemyAmount < enemyAmount)
            {
                StartCoroutine(SpawnEnemyCOM());
            }
        }
        if (currentGameMode != GameMode.OneVsOne && IsBossLevel() && !bossSpawned)
        {
            SpawnBoss();
        }
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

        for (int i = 0; i < mobPrefabs.Count; i++)
        {
            string poolTag = "Mob_" + i;
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
                    prefab = mobPrefabs[i],
                    size = initialPoolSize,
                    parent = comTeamTransform
                };
                objectPool.pools.Add(newPool);
                //Debug.Log($"Added mob pool: {poolTag}");
            }
        }

        // Setup boss pools
        for (int i = 0; i < bossPrefabs.Count; i++)
        {
            string poolTag = "Boss_" + i;
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
                    prefab = bossPrefabs[i],
                    size = 2, 
                    parent = comTeamTransform
                };
                objectPool.pools.Add(newPool);
            }
        }

        // Setup ally pool
        if (allyPrefab != null)
        {
            bool poolExists = false;
            foreach (var pool in objectPool.pools)
            {
                if (pool.tag == "Ally")
                {
                    poolExists = true;
                    break;
                }
            }

            if (!poolExists)
            {
                ObjectPool.Pool newPool = new ObjectPool.Pool
                {
                    tag = "Ally",
                    prefab = allyPrefab,
                    size = 2,
                    parent = playerTeamTransform
                };
                objectPool.pools.Add(newPool);
                
            }
        }
        objectPool.InitializePools();

    }

    private IEnumerator SpawnEnemyCOM()
    {
        if (!spawnable) yield break;
        spawnable = false;
        currentEnemyAmount++;

        int prefabIndex = Random.Range(0, mobPrefabs.Count);
        int wayPointIndex = Random.Range(0, spawnWayPoints.Count);
        string poolTag = "Mob_" + prefabIndex;

        GameObject enemy = objectPool.SpawnFromPool(poolTag, spawnWayPoints[wayPointIndex].position, Quaternion.identity, comTeamTransform);

        if (enemy != null)
        {
            // Reset enemy state
            ResetEnemyState(enemy);

            ModifyStat(enemy, false);
            enemy.GetComponent<AI>().enemyTeam = playerTeamTransform;
            enemy.GetComponent<AI>().levelManager = levelManager;

            activeEnemies.Add(enemy);

            
            var ai = enemy.GetComponent<AI>();
            if (ai != null)
            {
                ai.OnEnemyDeath += OnEnemyDied;
            }
        }
        else
        {
            objectPool.ExpandPool(poolTag, poolExpandAmount);
        }

        yield return new WaitForSeconds(spawnInterval);
        spawnable = true;
    }

    private void SpawnAlly()
    {
        if (allyPrefab != null)
        {

            if (currentAlly != null)
            {
                objectPool.ReturnToPool("Ally", currentAlly);
            }

            currentAlly = objectPool.SpawnFromPool("Ally", Vector3.zero, Quaternion.identity, playerTeamTransform);

            if (currentAlly != null)
            {
                ResetAllyState(currentAlly);
                currentAlly.GetComponent<AllyAI>().enemyTeam = comTeamTransform;
                currentAlly.GetComponent<AllyAI>().levelManager = levelManager;
                ModifyStat(currentAlly, false);
                //Debug.Log("Ally spawned from pool");
            }
        }
    }

    private void SpawnBoss()
    {
        if (bossPrefabs.Count == 0) return;

        //Debug.Log("Spawning Boss");
        int prefabIndex = Random.Range(0, bossPrefabs.Count);
        string poolTag = "Boss_" + prefabIndex;

        if (currentBoss != null)
        {
            objectPool.ReturnToPool(poolTag, currentBoss);
        }

        currentBoss = objectPool.SpawnFromPool(poolTag, spawnWayPoints[prefabIndex].position, Quaternion.identity, comTeamTransform);

        if (currentBoss != null)
        {
            currentEnemyAmount++;
            ResetEnemyState(currentBoss);
            currentBoss.GetComponent<AI>().enemyTeam = playerTeamTransform;
            currentBoss.GetComponent<AI>().levelManager = levelManager;
            ModifyStat(currentBoss, true);

            var ai = currentBoss.GetComponent<AI>();
            if (ai != null)
            {
                ai.OnEnemyDeath += OnBossDied;
            }

            bossSpawned = true;
        }
    }

    private void OnEnemyDied(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);

            string poolTag = GetPoolTagForEnemy(enemy);
            if (!string.IsNullOrEmpty(poolTag))
            {
                objectPool.ReturnToPool(poolTag, enemy);
            }
        }
    }

    private void OnBossDied(GameObject boss)
    {
        if (boss == currentBoss)
        {
            string poolTag = GetPoolTagForEnemy(boss);
            if (!string.IsNullOrEmpty(poolTag))
            {
                objectPool.ReturnToPool(poolTag, boss);
            }
        }
    }

    private string GetPoolTagForEnemy(GameObject enemy)
    {
        for (int i = 0; i < mobPrefabs.Count; i++)
        {
            if (enemy.name.Contains(mobPrefabs[i].name))
            {
                return "Mob_" + i;
            }
        }

        for (int i = 0; i < bossPrefabs.Count; i++)
        {
            if (enemy.name.Contains(bossPrefabs[i].name))
            {
                return "Boss_" + i;
            }
        }

        return null;
    }

    private void ResetEnemyState(GameObject enemy)
    {
        var ai = enemy.GetComponent<AI>();
        if (ai != null)
        {
            ai.ResetToInitialState(); 
        }

        enemy.transform.rotation = Quaternion.identity;
    }

    private void ResetAllyState(GameObject ally)
    {
        // Reset ally state
        var allyAI = ally.GetComponent<AllyAI>();
        if (allyAI != null)
        {
            allyAI.ResetToInitialState();
        }

        ally.transform.rotation = Quaternion.identity;
    }

    private void LoadGameMode()
    {
        currentGameMode = (GameMode)PlayerPrefs.GetInt("GameMode", 0);
        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
    }

    private void InitializeDifficulty()
    {
        enemyAmount = baseEnemyAmount + (currentLevel - 1) * additionalEnemyPerLevel;
        spawnInterval = Mathf.Max(baseSpawnInterval - (currentLevel - 1) * spawnIntervalReductionperLevel);
    }

    private void ModifyStat(GameObject target, bool isBoss)
    {
        AI targetAI = target.GetComponent<AI>();
        if (targetAI != null)
        {
            float multiplier = 1 + (currentLevel - 1) * difficultyMultiplier;
            if (isBoss) multiplier += 0.5f;
            targetAI.ScaleStat(multiplier);
        }
    }

    private bool IsBossLevel()
    {
        return currentLevel % bossInterval == 0;
    }

    public float GetEnemyAmountThisLevel()
    {
        if (currentGameMode != GameMode.OneVsOne || IsBossLevel())
        {
            return enemyAmount;
        }
        else
        {
            return 1;
        }
    }
    public void CleanupLevel()
    {
        // Return all active enemies to pool
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null && enemy.activeInHierarchy)
            {
                string poolTag = GetPoolTagForEnemy(enemy);
                if (!string.IsNullOrEmpty(poolTag))
                {
                    objectPool.ReturnToPool(poolTag, enemy);
                }
            }
        }
        activeEnemies.Clear();

        // Return ally to pool
        if (currentAlly != null)
        {
            objectPool.ReturnToPool("Ally", currentAlly);
            currentAlly = null;
        }

        // Return boss to pool
        if (currentBoss != null)
        {
            string poolTag = GetPoolTagForEnemy(currentBoss);
            if (!string.IsNullOrEmpty(poolTag))
            {
                objectPool.ReturnToPool(poolTag, currentBoss);
            }
            currentBoss = null;
        }

        currentEnemyAmount = 0;
        bossSpawned = false;
    }
    private void OnDestroy()
    {
        CleanupLevel();
    }
}
