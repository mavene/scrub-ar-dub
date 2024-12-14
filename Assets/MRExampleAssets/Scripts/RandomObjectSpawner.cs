using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RandomObjectSpawner : MonoBehaviour
{
    public Material[] planeTextures;        // Array of plane textures
    public GameObject[] objectSet;       // All the Prfabs
    private List<GameObject>[] objectSets;       // Array of prefab sets linked to textures
    private int objectsToSpawn;         // Number of objects to spawn
    public Vector2 spawnAreaSize = new Vector2(10, 10);  // Area size on the plane

    void Start()
    {
        InitialiseObjectsSet();
    }

void InitialiseObjectsSet()
    {
        // Validate input arrays
        if (planeTextures == null || planeTextures.Length == 0)
        {
            Debug.Log("I can see Plane textures array is null or empty!");
            return;
        }

        if (objectSet == null || objectSet.Length == 0)
        {
            Debug.Log("I can see Object set array is null or empty!");
            return;
        }

        // Initialize the objectSets array
        objectSets = new List<GameObject>[planeTextures.Length];

        for (int i = 0; i < objectSets.Length; i++)
        {
            objectSets[i] = new List<GameObject>();
        }
        
        for (int i = 0; i < objectSet.Length; i++)
        {
            if (i < 5)
            {
                objectSets[3].Add(objectSet[i]);
            }
            else if (i < 10)
            {
                objectSets[2].Add(objectSet[i]); 
            }
            else if (i < 16)
            {
                objectSets[1].Add(objectSet[i]); 
            }
            else 
            {
                objectSets[0].Add(objectSet[i]); 
            }
        }

        if (planeTextures.Length != objectSets.Length)
        {
            Debug.Log("I can see Mismatch between textures and object sets!");
        }
    }
public void SpawnObjectsOnPlane(Renderer planeRenderer, int index)
{
    if (objectSets[index] == null)
    {
        Debug.Log("I can see No objects assigned for this texture!");
        return;
    }

    float x = planeRenderer.bounds.size.x;
    float z = planeRenderer.bounds.size.z;
    float area = x*z;
    
    objectsToSpawn = (int)Math.Ceiling(Math.Sqrt(area)); 

    // Clear previous objects (optional)
    DeleteObjects();

    // Spawn objects randomly
    for (int i = 0; i < objectsToSpawn; i++)
    {
        int randomIndex = UnityEngine.Random.Range(0, objectSets[index].Count);

        Vector3 spawnPosition = GetSafeSpawnPosition(planeRenderer);
        Quaternion randomRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);

        GameObject spawnedObj = Instantiate(objectSets[index][randomIndex], spawnPosition, randomRotation, transform);
        
        // Calculate scale based on plane size
        float planeSizeAverage = (planeRenderer.bounds.size.x + planeRenderer.bounds.size.z) / 2;
        float desiredScale = planeSizeAverage * 0.05f; // 5% of plane size average
        
        // Apply scale to object
        spawnedObj.transform.localScale = Vector3.one * desiredScale;
    }
}

Vector3 GetSafeSpawnPosition(Renderer planeRenderer)
{
    float margin = 0.1f;

    Bounds planeBounds = planeRenderer.bounds;
    
    float randomX = UnityEngine.Random.Range(planeBounds.min.x + margin, planeBounds.max.x - margin);
    float randomZ = UnityEngine.Random.Range(planeBounds.min.z + margin, planeBounds.max.z - margin);
    float spawnY = planeBounds.center.y + 0.02f;

    return new Vector3(randomX, spawnY, randomZ);
}

public void DeleteObjects(){
    foreach (Transform child in transform)
    {
        Destroy(child.gameObject);
    }
}

}
