using UnityEngine;
using UnityEngine.AI;
using MotionMatching;
using System.Collections.Generic;
using System.IO;
using  System;


public enum SocialRelations
{
    Couple,
    Friend,
    Family,
    Coworker,
    Individual
}

[RequireComponent(typeof(AgentManager))]
public class AvatarCreator : MonoBehaviour
{
    public List<GameObject> avatarPrefabs = new List<GameObject>();
    public List<GameObject> instantiatedAvatars = new List<GameObject>();
    public List<GameObject> categoryGameObjects = new List<GameObject>();
    public int spawnCount = 1;    

    [Header("Agent's Path")]
    public Transform startPoint;
    public Transform endPoint;
    public NavMeshPath path;
    [HideInInspector]
    public List<Vector3> pathVertices= new List<Vector3>();
    [Tooltip("This is a parameter to scatter the start and end positions of the path. The larger the value, the more the start and end positions of the path will deviate.")]
    public float startPointDeviation = 1f;

    [Header("Social Relations")]
    private Dictionary<SocialRelations, int> categoryCounts = new Dictionary<SocialRelations, int>
    {
        { SocialRelations.Couple, 0 },
        { SocialRelations.Family, 0 },
        { SocialRelations.Friend, 0 },
        { SocialRelations.Coworker, 0 },
        { SocialRelations.Individual, 0 }
    };

    [Header("Agent Info")]
    public float agentHeight = 1.8f;
    public float agentRadius = 0.3f;


    public void InstantiateAvatars()
    {
        CalculatePath();

        //Set initial speed for each social relations
        float coupleSpeed = UnityEngine.Random.Range(0.5f, 0.8f);
        float familySpeed = UnityEngine.Random.Range(0.5f, 0.8f);
        float friendSpeed = UnityEngine.Random.Range(0.5f, 0.8f);
        float coworkerSpeed = UnityEngine.Random.Range(0.5f, 0.8f);

        //Create Category Objects
        foreach (SocialRelations relation in System.Enum.GetValues(typeof(SocialRelations)))
        {
            string relationName = relation.ToString();
            Transform child = transform.Find(relationName);
            
            if (child == null)
            {
                GameObject categoryGameObject = new GameObject(relationName);
                categoryGameObjects.Add(categoryGameObject);
                categoryGameObject.transform.SetParent(transform);

                //To make center of mass collider
                if (relation != SocialRelations.Individual){

                    GameObject groupColliderGameObject = new GameObject("GroupCollider");
                    groupColliderGameObject.transform.SetParent(categoryGameObject.transform);

                    groupColliderGameObject.tag = "Group";
                    CapsuleCollider groupCollider = groupColliderGameObject.AddComponent<CapsuleCollider>();
                    groupColliderGameObject.AddComponent<Rigidbody>();
                    groupCollider.height = agentHeight;
                    Vector3 newCenter = groupCollider.center;
                    newCenter.y = agentHeight / 2f;
                    groupCollider.center = newCenter;
                    groupCollider.isTrigger = true;

                    UpdateGroupCollider updateCenterOfMassPos = groupColliderGameObject.AddComponent<UpdateGroupCollider>();
                    updateCenterOfMassPos.avatarCreator = this.transform.GetComponent<AvatarCreator>();
                    updateCenterOfMassPos.agentRadius = agentRadius;

                    groupColliderGameObject.AddComponent<ParameterManager>();

                    GameObject groupColliderActiveManager = new GameObject("GroupColliderManager");
                    groupColliderActiveManager.transform.SetParent(categoryGameObject.transform);
                    GroupColliderManager groupColliderManager = groupColliderActiveManager.AddComponent<GroupColliderManager>();
                    groupColliderManager.socialRelations = relation;
                    groupColliderManager.avatarCreator = this.transform.GetComponent<AvatarCreator>();
                    groupColliderManager.groupColliderGameObject = groupColliderGameObject;
                }
            }
        }
        
        //Create Agents and Change Hierarchy
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject randomAvatar = avatarPrefabs[UnityEngine.Random.Range(0, avatarPrefabs.Count)];
            GameObject instance = Instantiate(randomAvatar, this.transform);
            PathController pathController = instance.GetComponentInChildren<PathController>();
            MotionMatchingController motionMatchingController = instance.GetComponentInChildren<MotionMatchingController>();
            MotionMatchingSkinnedMeshRendererWithOCEAN motionMatchingSkinnedMeshRendererWithOCEAN = instance.GetComponentInChildren<MotionMatchingSkinnedMeshRendererWithOCEAN>();

            //Random Social Relations Allocation 
            Array values = Enum.GetValues(typeof(SocialRelations));
            SocialRelations randomRelation;
            do
            {
                randomRelation = (SocialRelations)values.GetValue(UnityEngine.Random.Range(0, values.Length));
            } while (!IsValidRelation(randomRelation, categoryCounts));

            pathController.socialRelations = randomRelation;
            categoryCounts[randomRelation]++;
            
            //Change object's name and parent object 
            instance.name = randomRelation.ToString()+categoryCounts[randomRelation].ToString();
            instance.transform.parent = this.transform.Find(randomRelation.ToString()).transform;
            
            //Init Path Controller Params
            pathController.avatarCreator = this.GetComponent<AvatarCreator>();
            if(pathController != null)
            {
                pathController.Path = pathVertices.ToArray();
            }

            //Path Noise
            //pathController.Path[0] += GenerateRandomPointInCircle(startPointDeviation);
            pathController.Path[0] += GenerateRandomPointInCircleBasedOnSocialRelations(startPointDeviation, randomRelation);
            // pathController.Path[pathController.Path.Length-1] += GenerateRandomPointInCircle(radius);

                //Move the agent to starting pos
            motionMatchingController.transform.position = pathController.Path[0];
            motionMatchingSkinnedMeshRendererWithOCEAN.transform.position = pathController.Path[0];

            //initial Speed
            if(randomRelation == SocialRelations.Individual){
                pathController.initialSpeed = UnityEngine.Random.Range(pathController.minSpeed, pathController.maxSpeed);
            }else if(randomRelation == SocialRelations.Couple){
                pathController.initialSpeed = coupleSpeed;
            }else if(randomRelation == SocialRelations.Family){
                pathController.initialSpeed = familySpeed;
            }else if(randomRelation == SocialRelations.Friend){
                pathController.initialSpeed = friendSpeed;
            }else if(randomRelation == SocialRelations.Coworker){
                pathController.initialSpeed = coworkerSpeed;
            }

            //set group collider and save pathmanager
            if (randomRelation != SocialRelations.Individual)
            {
                GameObject relationGameObject = transform.Find(randomRelation.ToString()).gameObject;
                GameObject groupCollider = relationGameObject.transform.Find("GroupCollider").gameObject;

                if (groupCollider != null)
                {
                    ParameterManager groupParameterManager = groupCollider.GetComponent<ParameterManager>();
                    groupParameterManager.pathControllers.Add(pathController);
                    pathController.groupCollider = groupCollider.GetComponent<CapsuleCollider>();
                }
            }
            

            instantiatedAvatars.Add(instance);
        }

        //Destroy Group Collider and its manager if the number of agents is less than 1
        foreach (KeyValuePair<SocialRelations, int> entry in categoryCounts)
        {
            if(entry.Key == SocialRelations.Individual) return; 
            if (entry.Value <= 1)
            {
                GameObject relationGameObject = transform.Find(entry.Key.ToString()).gameObject;

                if (relationGameObject != null)
                {
                    GameObject groupCollider = relationGameObject.transform.Find("GroupCollider").gameObject;
                    GameObject groupColliderManager = relationGameObject.transform.Find("GroupColliderManager").gameObject;

                    if (groupCollider != null)
                    {
                        DestroyImmediate(groupCollider);
                        DestroyImmediate(groupColliderManager);
                    }
                }
            }
        }

    }

    public void DeleteAvatars()
    {
        foreach (GameObject avatar in instantiatedAvatars)
        {
            DestroyImmediate(avatar);
        }
        foreach (GameObject categoryGameObject in categoryGameObjects){
            DestroyImmediate(categoryGameObject);
        }
        instantiatedAvatars.Clear();
        categoryGameObjects.Clear();
        pathVertices = new List<Vector3>();
        InitializeDictionaries();
    }
    private void InitializeDictionaries()
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

    public List<GameObject> GetAgentsInCategory(SocialRelations socialRelations){
        List<GameObject> agentsList = new List<GameObject>();
        for (int i = 0; i < instantiatedAvatars.Count; i++)
        {
            PathController pathController = instantiatedAvatars[i].GetComponentInChildren<PathController>();
            if(pathController.socialRelations == socialRelations){
                GameObject agent = instantiatedAvatars[i].GetComponentInChildren<MotionMatchingSkinnedMeshRendererWithOCEAN>().gameObject;
                agentsList.Add(agent);
            }
        }
        return agentsList;
    }

    public List<PathController> GetPathControllersInCategory(SocialRelations socialRelations)
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

    Vector3 GenerateRandomPointInCircle(float r)
    {
        float angle = UnityEngine.Random.Range(0f, 2f * Mathf.PI); 
        float distance = Mathf.Sqrt(UnityEngine.Random.Range(0f, r * r));

        float x = distance * Mathf.Cos(angle);
        float z = distance * Mathf.Sin(angle);

        return new Vector3(x, 0f, z);
    }

    public Vector3 GenerateRandomPointInCircleBasedOnSocialRelations(float r, SocialRelations relation)
    {
        int enumCount = System.Enum.GetValues(typeof(SocialRelations)).Length;
        float segmentAngle = 360f / enumCount; 

        float baseAngle = (int)relation * segmentAngle;
        float angle = UnityEngine.Random.Range(baseAngle, baseAngle + segmentAngle) * Mathf.Deg2Rad;

        float distance = Mathf.Sqrt(UnityEngine.Random.Range(0f, r * r));

        float x = distance * Mathf.Cos(angle);
        float z = distance * Mathf.Sin(angle);

        return new Vector3(x, 0f, z);
    }

    private bool IsValidRelation(SocialRelations relation, Dictionary<SocialRelations, int> counts)
    {
        switch (relation)
        {
            case SocialRelations.Couple:
                return counts[relation] < 2;
            case SocialRelations.Family:
            case SocialRelations.Friend:
            case SocialRelations.Coworker:
                return counts[relation] < 3;
            default:
                return true;
        }
    }

    public SocialRelations StringToSocialRelations(string relationName)
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

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (pathVertices != null && pathVertices.Count > 0)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < pathVertices.Count - 1; i++)
            {
                Gizmos.DrawLine(pathVertices[i], pathVertices[i + 1]);
                //Gizmos.DrawSphere(pathVertices[i], 0.25f);
            }
            //Gizmos.DrawSphere(pathVertices[pathVertices.Count - 1], 0.25f);
        }
    }
#endif
}
