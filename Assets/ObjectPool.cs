using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PoolItem
{
    public GameObject prefab;
    public int amountToPool;
    public bool shouldExpand = true;
}

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool SharedInstance;

    [Header("Pool Settings")]
    public List<PoolItem> objectsToPool; // List of prefabs with their own pool amounts

    private List<GameObject> pooledObjects;

    void Awake()
    {
        SharedInstance = this;
        InitializePool();
    }

    private void InitializePool()
    {
        pooledObjects = new List<GameObject>();

        foreach (PoolItem poolItem in objectsToPool)
        {
            if (poolItem.prefab == null)
            {
                Debug.LogWarning("Null prefab found in objectsToPool list!");
                continue;
            }

            for (int i = 0; i < poolItem.amountToPool; i++)
            {
                GameObject obj = Instantiate(poolItem.prefab);
                obj.SetActive(false);
                pooledObjects.Add(obj);
            }
        }
    }

    public GameObject GetPooledObject()
    {
        // First, try to find any inactive object from any prefab type
        for (int i = 0; i < pooledObjects.Count; i++)
        {
            if (!pooledObjects[i].activeInHierarchy)
            {
                return pooledObjects[i];
            }
        }

        // If no inactive objects found and we can expand, create more from the first expandable pool
        if (objectsToPool.Count > 0)
        {
            // Find the first pool that can expand
            foreach (PoolItem poolItem in objectsToPool)
            {
                if (poolItem.shouldExpand)
                {
                    GameObject newObj = Instantiate(poolItem.prefab);
                    newObj.SetActive(false);
                    pooledObjects.Add(newObj);
                    return newObj;
                }
            }
        }

        Debug.LogWarning("No available objects in pool and no expandable pools!");
        return null;
    }

    // Get a specific type of pooled object (by prefab type)
    public GameObject GetPooledObject(System.Type componentType)
    {
        // First try to find an inactive object of the specific type
        for (int i = 0; i < pooledObjects.Count; i++)
        {
            if (!pooledObjects[i].activeInHierarchy &&
                pooledObjects[i].GetComponent(componentType) != null)
            {
                return pooledObjects[i];
            }
        }

        // If no inactive objects of that type found, try to create one from an expandable pool
        foreach (PoolItem poolItem in objectsToPool)
        {
            if (poolItem.shouldExpand && poolItem.prefab.GetComponent(componentType) != null)
            {
                GameObject newObj = Instantiate(poolItem.prefab);
                newObj.SetActive(false);
                pooledObjects.Add(newObj);
                return newObj;
            }
        }

        return null;
    }

    // Get a pooled object by prefab name
    public GameObject GetPooledObject(string prefabName)
    {
        // First try to find an inactive object with the specific name
        for (int i = 0; i < pooledObjects.Count; i++)
        {
            if (!pooledObjects[i].activeInHierarchy &&
                pooledObjects[i].name.StartsWith(prefabName))
            {
                return pooledObjects[i];
            }
        }

        // If no inactive objects with that name found, try to create one
        foreach (PoolItem poolItem in objectsToPool)
        {
            if (poolItem.shouldExpand && poolItem.prefab.name == prefabName)
            {
                GameObject newObj = Instantiate(poolItem.prefab);
                newObj.SetActive(false);
                pooledObjects.Add(newObj);
                return newObj;
            }
        }

        return null;
    }

    // Get a pooled object from a specific pool item index
    public GameObject GetPooledObject(int poolIndex)
    {
        if (poolIndex < 0 || poolIndex >= objectsToPool.Count)
        {
            Debug.LogError($"Pool index {poolIndex} is out of range!");
            return null;
        }

        PoolItem poolItem = objectsToPool[poolIndex];

        // First try to find an inactive object from this specific prefab type
        for (int i = 0; i < pooledObjects.Count; i++)
        {
            if (!pooledObjects[i].activeInHierarchy &&
                pooledObjects[i].name.StartsWith(poolItem.prefab.name))
            {
                return pooledObjects[i];
            }
        }

        // If none found and pool can expand, create a new one
        if (poolItem.shouldExpand)
        {
            GameObject newObj = Instantiate(poolItem.prefab);
            newObj.SetActive(false);
            pooledObjects.Add(newObj);
            return newObj;
        }

        return null;
    }

    // Optional: Method to return all objects to pool
    public void ReturnAllToPool()
    {
        foreach (GameObject obj in pooledObjects)
        {
            obj.SetActive(false);
        }
    }

    // Get count of active objects (for debugging)
    public int GetActiveObjectCount()
    {
        int count = 0;
        foreach (GameObject obj in pooledObjects)
        {
            if (obj.activeInHierarchy) count++;
        }
        return count;
    }

    // Get count of pooled objects for a specific prefab type
    public int GetPoolCountForPrefab(GameObject prefab)
    {
        int count = 0;
        string prefabName = prefab.name;
        foreach (GameObject obj in pooledObjects)
        {
            if (obj.name.StartsWith(prefabName))
            {
                count++;
            }
        }
        return count;
    }

    // Get count of active objects for a specific prefab type
    public int GetActiveCountForPrefab(GameObject prefab)
    {
        int count = 0;
        string prefabName = prefab.name;
        foreach (GameObject obj in pooledObjects)
        {
            if (obj.name.StartsWith(prefabName) && obj.activeInHierarchy)
            {
                count++;
            }
        }
        return count;
    }
}