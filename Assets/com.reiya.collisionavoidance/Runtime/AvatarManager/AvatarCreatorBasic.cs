using UnityEngine;
using MotionMatching;
using System.Collections.Generic;
using System;

namespace CollisionAvoidance{
public class AvatarCreatorBasic : AvatarCreatorBase
{
    [Tooltip("This is a parameter to scatter the start and end positions of the path. The larger the value, the more the start and end positions of the path will deviate.")]
    public float startPointDeviation = 1f;

    public override void InstantiateAvatars()
    {
        
        CalculatePath();

        //Set initial speed for each social relations
        float coupleSpeed = UnityEngine.Random.Range(minSpeed,maxSpeed);
        float familySpeed = UnityEngine.Random.Range(minSpeed,maxSpeed);
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
        
        //Create Agents and Change Hierarchy
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject randomAvatar = avatarPrefabs[UnityEngine.Random.Range(0, avatarPrefabs.Count)];
            GameObject instance = Instantiate(randomAvatar, this.transform);
            PathController pathController = instance.GetComponentInChildren<PathController>();
            MotionMatchingController motionMatchingController = instance.GetComponentInChildren<MotionMatchingController>();
            CollisionAvoidanceController collisionAvoidanceController = instance.GetComponentInChildren<CollisionAvoidanceController>();
            ConversationalAgentFramework conversationalAgentFramework = instance.GetComponentInChildren<ConversationalAgentFramework>();

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
            pathController.avatarCreator = this.GetComponent<AvatarCreatorBase>();
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
            conversationalAgentFramework.transform.position = pathController.Path[0];

            //initial Speed
            if(randomRelation == SocialRelations.Individual){
                pathController.initialSpeed = UnityEngine.Random.Range(minSpeed, maxSpeed);
            }else if(randomRelation == SocialRelations.Couple){
                pathController.initialSpeed = coupleSpeed;
            }else if(randomRelation == SocialRelations.Family){
                pathController.initialSpeed = familySpeed;
            }else if(randomRelation == SocialRelations.Friend){
                pathController.initialSpeed = friendSpeed;
            }else if(randomRelation == SocialRelations.Coworker){
                pathController.initialSpeed = coworkerSpeed;
            }

            pathController.maxSpeed = maxSpeed;
            pathController.minSpeed = minSpeed;

            //set group collider and save pathmanager
            if (randomRelation != SocialRelations.Individual)
            {
                GameObject relationGameObject = transform.Find(randomRelation.ToString()).gameObject;
                GameObject groupCollider = relationGameObject.transform.Find("GroupCollider").gameObject;

                if (groupCollider != null)
                {
                    GroupParameterManager groupParameterManager = groupCollider.GetComponent<GroupParameterManager>();
                    groupParameterManager.pathControllers.Add(pathController);
                    collisionAvoidanceController.groupCollider = groupCollider.GetComponent<CapsuleCollider>();
                }
            }

            //set group manager to pathmanager
            if (randomRelation != SocialRelations.Individual)
            {
                GameObject relationGameObject   = transform.Find(randomRelation.ToString()).gameObject;
                GameObject groupColliderManagerObj = relationGameObject.transform.Find("GroupColliderManager").gameObject;
                if (groupColliderManagerObj != null)
                {
                    GroupColliderManager groupColliderManager = groupColliderManagerObj.GetComponent<GroupColliderManager>();
                    pathController.groupColliderManager = groupColliderManager;
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

    public override void DeleteAvatars()
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
}
}