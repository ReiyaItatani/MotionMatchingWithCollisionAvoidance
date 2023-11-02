using UnityEngine;
using UnityEngine.AI;
using MotionMatching;
using System.Collections.Generic;
using System;

// Enum to define social relations. This will be used later to categorize relationships between characters.
public enum SocialRelations
{
    Couple,
    Friend,
    Family,
    Coworker,
    Individual
}

// AvatarCreatorBase is a base class for creating and managing avatars.
[RequireComponent(typeof(AgentManager))]
public class AvatarCreatorBase : MonoBehaviour
{
    // avatarPrefabs: A list of available avatar prefabs.
    public List<GameObject> avatarPrefabs = new List<GameObject>();
    // categoryGameObjects: A list of game objects per category (might not be used).
    public List<GameObject> categoryGameObjects = new List<GameObject>();  
    // instantiatedAvatars: A list of instantiated avatars.
    public List<GameObject> instantiatedAvatars = new List<GameObject>();
    // spawnCount: The number of avatars to spawn.
    public int spawnCount = 1;  

    public float maxSpeed = 0.8f;
    public float minSpeed = 0.5f;

    // pathVertices: A list to store vertices of the path that avatars will move along.
    [HideInInspector]
    public List<Vector3> pathVertices= new List<Vector3>();

    // Social Relations Section: Categorizes the possible social relations avatars might have.
    [Header("Social Relations")]
    // categoryCounts: A dictionary to hold the number of avatars belonging to each category.
    protected Dictionary<SocialRelations, int> categoryCounts = new Dictionary<SocialRelations, int>
    {
        { SocialRelations.Couple, 0 },
        { SocialRelations.Family, 0 },
        { SocialRelations.Friend, 0 },
        { SocialRelations.Coworker, 0 },
        { SocialRelations.Individual, 0 }
    };

    // Agent's Path Section: Holds information regarding the path of movement for the agent.
    [Header("Agent's Path")]
    // startPoint: The starting point of the path.
    public Transform startPoint;
    // endPoint: The ending point of the path.
    public Transform endPoint;
    // path: The path calculated using the navigation mesh.
    public NavMeshPath path;

    // Agent Info Section: Holds information regarding the physical characteristics of the agent.
    [Header("Agent Info")]
    // agentHeight: The height of the agent.
    public float agentHeight = 1.8f;
    // agentRadius: The radius of the agent.
    public float agentRadius = 0.3f;

    // InstantiateAvatars: A method to instantiate avatars (to be implemented in derived classes).
    public virtual void InstantiateAvatars(){}

    // DeleteAvatars: A method to delete avatars (to be implemented in derived classes).
    public virtual void DeleteAvatars(){}

    // GetAgents: A method to retrieve agent game objects from instantiated avatars.
    public virtual List<GameObject> GetAgents(){
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

    // GetAgentsInCategory: A method to retrieve agents belonging to a specific social relation category.
    public virtual List<GameObject> GetAgentsInCategory(SocialRelations socialRelations){
        List<GameObject> agentsList = new List<GameObject>();
        for (int i = 0; i < instantiatedAvatars.Count; i++)
        {
            PathController pathController = instantiatedAvatars[i].GetComponentInChildren<PathController>();
            if(pathController.GetSocialRelations() == socialRelations){
                GameObject agent = instantiatedAvatars[i].GetComponentInChildren<MotionMatchingSkinnedMeshRendererWithOCEAN>().gameObject;
                agentsList.Add(agent);
            }
        }
        return agentsList;
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

    // StringToSocialRelations: A method to convert a string to a SocialRelations enum.
    public virtual SocialRelations StringToSocialRelations(string relationName)
    {
        if (Enum.IsDefined(typeof(SocialRelations), relationName))
        {
            return (SocialRelations)Enum.Parse(typeof(SocialRelations), relationName);
        }
        else
        {
            throw new ArgumentException($"'{relationName}' is not a valid SocialRelations name.");
        }
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

    // InitializeDictionaries: A method to initialize dictionaries, resetting the counts of each category.
    protected virtual void InitializeDictionaries()
    {
        categoryCounts = new Dictionary<SocialRelations, int>
        {
            { SocialRelations.Couple, 0 },
            { SocialRelations.Family, 0 },
            { SocialRelations.Friend, 0 },
            { SocialRelations.Coworker, 0 },
            { SocialRelations.Individual, 0 }
        };
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
            if (pathVertices != null && pathVertices.Count > 0)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < pathVertices.Count - 1; i++)
                {
                    Gizmos.DrawLine(pathVertices[i], pathVertices[i + 1]);
                }
            }
        }
    #endif
}
