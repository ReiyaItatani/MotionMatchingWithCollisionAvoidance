using UnityEngine;
using UnityEngine.AI;
using MotionMatching;
using System.Collections.Generic;
using System.IO;

namespace CollisionAvoidance{
public class AgentCreatorPath : AvatarCreatorBase
{

    public List<Vector3> agentPath = new List<Vector3>();
    [ReadOnly]
    public List<Vector3> pathVerticesEndToStart = new List<Vector3>();

    public override void InstantiateAvatars()
    {
        //CalculatePath();
        CalculatePathEndToStart();

        //Set initial speed for each social relations
        float coupleSpeed = UnityEngine.Random.Range(minSpeed, maxSpeed);
        float familySpeed = UnityEngine.Random.Range(minSpeed, maxSpeed);
        float friendSpeed = UnityEngine.Random.Range(minSpeed, maxSpeed);
        float coworkerSpeed = UnityEngine.Random.Range(minSpeed, maxSpeed);

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
                    updateCenterOfMassPos.avatarCreator = this.transform.GetComponent<AvatarCreatorBase>();
                    updateCenterOfMassPos.agentRadius = agentRadius;

                    groupColliderGameObject.AddComponent<GroupParameterManager>();

                    GameObject groupColliderActiveManager = new GameObject("GroupColliderManager");
                    groupColliderActiveManager.transform.SetParent(categoryGameObject.transform);
                    GroupColliderManager groupColliderManager = groupColliderActiveManager.AddComponent<GroupColliderManager>();
                    groupColliderManager.socialRelations = relation;
                    groupColliderManager.avatarCreator = this.transform.GetComponent<AvatarCreatorBase>();
                    groupColliderManager.groupColliderGameObject = groupColliderGameObject;
                }
            }
        }
        
        /****************************************************************/
        /************************Create Individual***********************/
        /****************************************************************/
        for(int i = 0; i < spawnCount; i++)
        {
            //Init
            GameObject randomAvatar = avatarPrefabs[UnityEngine.Random.Range(0, avatarPrefabs.Count)];
            GameObject instance = Instantiate(randomAvatar, this.transform);
            PathController pathController = instance.GetComponentInChildren<PathController>();
            MotionMatchingController motionMatchingController = instance.GetComponentInChildren<MotionMatchingController>();
            CollisionAvoidanceController collisionAvoidanceController = instance.GetComponentInChildren<CollisionAvoidanceController>();
            ConversationalAgentFramework conversationalAgentFramework = instance.GetComponentInChildren<ConversationalAgentFramework>();
            //Set as an individual
            pathController.socialRelations = SocialRelations.Individual;
            categoryCounts[SocialRelations.Individual]++;
            instance.name = SocialRelations.Individual.ToString()+categoryCounts[SocialRelations.Individual].ToString();
            instance.transform.parent = this.transform.Find(SocialRelations.Individual.ToString()).transform;
            //Position at the start Pos
            pathController.avatarCreator = this.GetComponent<AvatarCreatorBase>();
            if(Random.Range(0,2) == 0){
                pathController.Path = pathVerticesEndToStart.ToArray();
            }else{
                pathController.Path = agentPath.ToArray();
            }
            //Move the agent to starting pos
            int randomIndex = UnityEngine.Random.Range(1, agentPath.Count-1);
            pathController.CurrentGoalIndex = randomIndex;
            pathController.Path[randomIndex-1] = GetRandomPointOnCircle(pathController.Path[randomIndex-1], 2f);
            motionMatchingController.transform.position = pathController.Path[randomIndex - 1];
            conversationalAgentFramework.transform.position = pathController.Path[randomIndex - 1];
            //Set Initial Speed
            pathController.initialSpeed = UnityEngine.Random.Range(pathController.minSpeed, pathController.maxSpeed);
            //Save The Agent
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

    public Vector3 GetRandomPointOnCircle(Vector3 center, float radius)
    {
        float angle = Random.Range(0f, Mathf.PI * 2);
        Vector3 point = new Vector3(
            center.x + radius * Mathf.Cos(angle),
            center.y,
            center.z + radius * Mathf.Sin(angle)
        );

        return point;
    }

    public override  void DeleteAvatars()
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
        InitializeDictionaries();
    }

    public List<Vector3> CalculatePathEndToStart()
    {
        pathVerticesEndToStart.Clear();
        for (int i = agentPath.Count - 1; i >= 0; i--)
        {
            pathVerticesEndToStart.Add(agentPath[i]);
        }
        return pathVerticesEndToStart;
    }



    void OnDrawGizmos()
    {
        for (int i = 0; i < agentPath.Count; i++)
        {
            if (i < agentPath.Count - 1)
            {
                Gizmos.DrawLine(transform.position + agentPath[i], transform.position + agentPath[i + 1]);
            }
        }
    }
}
}