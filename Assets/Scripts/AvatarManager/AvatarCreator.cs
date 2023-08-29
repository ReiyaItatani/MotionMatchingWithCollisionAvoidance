using UnityEngine;
using UnityEngine.AI;
using MotionMatching;
using System.Collections.Generic;
using System.IO;

[RequireComponent(typeof(AgentManager))]
public class AvatarCreator : MonoBehaviour
{
    public List<GameObject> avatarPrefabs = new List<GameObject>();
    public int spawnCount = 1;
    public List<GameObject> instantiatedAvatars = new List<GameObject>();

    [Header("Agent's Path")]
    public Transform startPoint;
    public Transform endPoint;
    public NavMeshPath path;
    public List<Vector3> pathVertices= new List<Vector3>();
    [Tooltip("This is a parameter to scatter the start and end positions of the path. The larger the value, the more the start and end positions of the path will deviate.")]
    public float startPointDeviation = 1f;

    [Header("Goal Size")]
    public float GoalRadius = 2f;
    public float SlowingRadius = 3f;

    public void InstantiateAvatars()
    {
        CalculatePath();

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject randomAvatar = avatarPrefabs[Random.Range(0, avatarPrefabs.Count)];
            GameObject instance = Instantiate(randomAvatar, this.transform);
            PathController pathController = instance.GetComponentInChildren<PathController>();
            
            pathController.avatarCreator = this.GetComponent<AvatarCreator>();
            if(pathController != null)
            {
                pathController.Path = pathVertices.ToArray();
            }

            //Path Noise
            pathController.Path[0] += GenerateRandomPointInCircle(startPointDeviation);
            // pathController.Path[pathController.Path.Length-1] += GenerateRandomPointInCircle(radius);

            //initial Speed
            pathController.initialSpeed = Random.Range(0.5f, 1.5f);

            //set goal size
            pathController.goalRadius = GoalRadius;
            pathController.slowingRadius = SlowingRadius;

            instantiatedAvatars.Add(instance);
        }
    }

    public void DeleteAvatars()
    {
        foreach (GameObject avatar in instantiatedAvatars)
        {
            DestroyImmediate(avatar);
        }
        instantiatedAvatars.Clear();
        pathVertices = new List<Vector3>();
    }

    public List<Vector3> CalculatePath()
    {
        path = new NavMeshPath();
        pathVertices = new List<Vector3>();

        if (NavMesh.CalculatePath(startPoint.position, endPoint.position, NavMesh.AllAreas, path))
        {
            foreach (var corner in path.corners)
            {
                pathVertices.Add(corner);
            }
        }

        return pathVertices;
    }

    public List<GameObject> GetAgents()
    {
        List<GameObject> agentsList = new List<GameObject>();

        for (int i = 0; i < instantiatedAvatars.Count; i++)
        {
            MotionMatchingSkinnedMeshRendererWithOCEAN component = instantiatedAvatars[i].GetComponentInChildren<MotionMatchingSkinnedMeshRendererWithOCEAN>();
            if (component != null)
            {
                agentsList.Add(component.gameObject);
            }
        }
        return agentsList;
    }

    Vector3 GenerateRandomPointInCircle(float r)
    {
        float angle = Random.Range(0f, 2f * Mathf.PI); 
        float distance = Mathf.Sqrt(Random.Range(0f, r * r));

        float x = distance * Mathf.Cos(angle);
        float z = distance * Mathf.Sin(angle);

        return new Vector3(x, 0f, z);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (pathVertices != null && pathVertices.Count > 0)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < pathVertices.Count - 1; i++)
            {
                Gizmos.DrawLine(pathVertices[i], pathVertices[i + 1]);
                Gizmos.DrawSphere(pathVertices[i], 0.25f);
            }
            Gizmos.DrawSphere(pathVertices[pathVertices.Count - 1], 0.25f);
        }
    }
#endif
}
