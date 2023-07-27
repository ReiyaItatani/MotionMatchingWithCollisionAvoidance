using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentManager : MonoBehaviour
{
    public List<GameObject> Agents;

    public List<GameObject> GetAgents(){
        return Agents;
    }

    //     public List<GameObject> prefabsToSpawn;
    // public int numberOfPrefabs = 10;
    // private List<GameObject> Agents = new List<GameObject>();

    // void Start()
    // {
    //     for (int i = 0; i < numberOfPrefabs; i++)
    //     {
    //         GameObject prefabToSpawn = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Count)];

    //         // Instantiate the Prefab at the randomPosition and with no rotation
    //         GameObject newPrefab = Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity);

    //         // Add the newPrefab to the spawnedPrefabs list
    //         Agents.Add(newPrefab);
    //     }
    // }
    // public List<GameObject> GetAgents(){
    //     return Agents;
    // }
}
