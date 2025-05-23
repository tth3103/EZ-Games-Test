using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
        public Transform parent;
    }

    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        InitializePools();
    }

    void Start()
    {
        if (poolDictionary == null)
        {
            InitializePools();
        }
    }

    public void InitializePools()
    {
        if (poolDictionary == null)
            poolDictionary = new Dictionary<string, Queue<GameObject>>();

        poolDictionary.Clear();

        //Debug.Log($"Initializing {pools.Count} pools...");

        foreach (Pool pool in pools)
        {
            if (poolDictionary.ContainsKey(pool.tag))
            {
                //Debug.LogWarning($"Pool with tag {pool.tag} already exists, skipping...");
                continue;
            }

            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);

                if (pool.parent != null)
                    obj.transform.SetParent(pool.parent);

                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
            //Debug.Log($"Created pool '{pool.tag}' with {pool.size} objects");
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (poolDictionary == null)
        {
            //Debug.Log("Pool dictionary was null, initializing...");
            InitializePools();
        }

        if (!poolDictionary.ContainsKey(tag))
        {
            return null;
        }

        if (poolDictionary[tag].Count == 0)
        {
            return null;
        }

        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        if (parent != null)
            objectToSpawn.transform.SetParent(parent);

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        poolDictionary[tag].Enqueue(objectToSpawn);

        return objectToSpawn;
    }

    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            return;
        }

        objectToReturn.SetActive(false);

        objectToReturn.transform.position = Vector3.zero;
        objectToReturn.transform.rotation = Quaternion.identity;
    }

    public void ReturnAllToPool(string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            return;
        }

        Queue<GameObject> pool = poolDictionary[tag];
        foreach (GameObject obj in pool)
        {
            if (obj.activeInHierarchy)
            {
                obj.SetActive(false);
            }
        }
    }
    public void ExpandPool(string tag, int additionalSize)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            return;
        }

        Pool targetPool = pools.Find(p => p.tag == tag);
        if (targetPool == null) return;

        Queue<GameObject> pool = poolDictionary[tag];

        for (int i = 0; i < additionalSize; i++)
        {
            GameObject obj = Instantiate(targetPool.prefab);
            obj.SetActive(false);

            if (targetPool.parent != null)
                obj.transform.SetParent(targetPool.parent);

            pool.Enqueue(obj);
        }
        targetPool.size += additionalSize;
    }
}
