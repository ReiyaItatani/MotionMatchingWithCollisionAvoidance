using UnityEngine;
using UnityEngine.AI;
using MotionMatching;
using System.Collections.Generic;

public class AvatarCreator : MonoBehaviour
{
    public List<GameObject> avatarPrefabs = new List<GameObject>();
    public int spawnCount = 1;
    private List<GameObject> instantiatedAvatars = new List<GameObject>();

    [Tooltip("Path")]
    public Transform startPoint;
    public Transform endPoint;
    public NavMeshPath path;
    public List<Vector3> pathVertices= new List<Vector3>();
    public List<GameObject> InstantiatedAvatars
    {
        get { return instantiatedAvatars; }
    }

    public void InstantiateAvatars()
    {
        CalculatePath();

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject randomAvatar = avatarPrefabs[Random.Range(0, avatarPrefabs.Count)];
            GameObject instance = Instantiate(randomAvatar, this.transform);
            PathController pathController = instance.GetComponentInChildren<PathController>();
            if(pathController != null)
            {
                pathController.Path = pathVertices.ToArray();
            }
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
