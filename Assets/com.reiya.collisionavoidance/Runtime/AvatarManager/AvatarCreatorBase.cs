using UnityEngine;
using UnityEngine.AI;
using MotionMatching;
using System.Collections.Generic;
using System;

namespace CollisionAvoidance{

[RequireComponent(typeof(AgentManager))]
public class AvatarCreatorBase : AvatarCreator
{
    // Agent's Path Section: Holds information regarding the path of movement for the agent.
    [Header("Agent's Path")]
    // startPoint: The starting point of the path.
    public Transform startPoint;
    // endPoint: The ending point of the path.
    public Transform endPoint;
    // path: The path calculated using the navigation mesh.
    public NavMeshPath path;

    // GetPathControllers: A method to retrieve PathController components from avatars.
    public virtual List<PathController> GetPathControllers()
    {
        List<PathController> pathControllersList = new List<PathController>();
        
        foreach (GameObject avatar in instantiatedAvatars)
        {
            PathController pathController = avatar.GetComponentInChildren<PathController>();
            pathControllersList.Add(pathController);
        }
        
        return pathControllersList;
    }

    // GetPathControllersInCategory: A method to retrieve PathController components from avatars in a specific social relation category.
    protected virtual List<PathController> GetPathControllersInCategory(SocialRelations socialRelations)
    {
        List<PathController> pathControllersList = new List<PathController>();
        
        foreach (GameObject avatar in instantiatedAvatars)
        {
            PathController pathController = avatar.GetComponentInChildren<PathController>();
            if (pathController && pathController.socialRelations == socialRelations)
            {
                pathControllersList.Add(pathController);
            }
        }
        
        return pathControllersList;
    }

    // IsValidRelation: A method to check if a relation is valid based on predefined conditions.
    protected virtual bool IsValidRelation(SocialRelations relation, Dictionary<SocialRelations, int> counts)
    {
        switch (relation)
        {
            case SocialRelations.Couple:
                return counts[relation] < 2;
            case SocialRelations.Family:
                return counts[relation] < 4;
            case SocialRelations.Friend:
                return counts[relation] < 4;
            case SocialRelations.Coworker:
                return counts[relation] < 3;
            default:
                return true;
        }
    }

    // CalculatePath: A method to calculate and return the vertices of a path using the navigation mesh.
    protected virtual List<Vector3> CalculatePath()
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
    
    // OnDrawGizmos: A method used in the Unity Editor to visualize the calculated path.
    #if UNITY_EDITOR
        void OnDrawGizmos()
        {
            // if (pathVertices != null && pathVertices.Count > 0)
            // {
            //     Gizmos.color = Color.red;
            //     for (int i = 0; i < pathVertices.Count - 1; i++)
            //     {
            //         Gizmos.DrawLine(pathVertices[i], pathVertices[i + 1]);
            //     }
            // }
        }
    #endif
}
}