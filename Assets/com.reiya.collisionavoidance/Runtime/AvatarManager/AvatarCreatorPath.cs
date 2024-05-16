using UnityEngine;
using MotionMatching;
using System.Collections.Generic;

namespace CollisionAvoidance{
public class AvatarCreatorPath : AvatarCreatorBase
{

    public List<Vector3> agentPath = new List<Vector3>();
    [ReadOnly]
    public List<Vector3> pathVerticesEndToStart = new List<Vector3>();

    public List<SocialRelations> socialRelations = new List<SocialRelations>();

    public override void InstantiateAvatars()
    {
        if(agentPath.Count <= 5){
            Debug.LogError("Path should be more than 5 ");
            return;
        }
        if(avatarPrefabs.Count == 0){
            Debug.LogError("Avatar Prefabs are not set");
            return;
        }
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
        List<int> goalIndexes = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            int goalIndex = UnityEngine.Random.Range(1, agentPath.Count);
            while (goalIndexes.Contains(goalIndex))
            {
                goalIndex = UnityEngine.Random.Range(1, agentPath.Count);
            }
            goalIndexes.Add(goalIndex);
        }

        int goalIndex_Couple = goalIndexes[0];
        int goalIndex_Family = goalIndexes[1];
        int goalIndex_Friend = goalIndexes[2];
        int goalIndex_Coworker = goalIndexes[3];
        
        for(int i = 0; i < spawnCount; i++)
        {
            SocialRelations randomRelation = (SocialRelations)Random.Range(0, socialRelations.Count);
            switch(randomRelation){
                case SocialRelations.Individual:
                    InstantiateAndConfigureAvatar(SocialRelations.Individual, UnityEngine.Random.Range(minSpeed, maxSpeed), UnityEngine.Random.Range(1, agentPath.Count-1), Random.Range(0, 2));
                    break;
                case SocialRelations.Couple:
                    if(goalIndex_Couple !=-1){
                        float speed_Couple = UnityEngine.Random.Range(minSpeed, maxSpeed);
                        int pathDirection_Couple = Random.Range(0, 2);
                        InstantiateAndConfigureAvatar(SocialRelations.Couple, speed_Couple, goalIndex_Couple, pathDirection_Couple);
                        InstantiateAndConfigureAvatar(SocialRelations.Couple, speed_Couple, goalIndex_Couple, pathDirection_Couple);
                        goalIndex_Couple = -1;
                    }else{
                        InstantiateAndConfigureAvatar(SocialRelations.Individual, UnityEngine.Random.Range(minSpeed, maxSpeed), UnityEngine.Random.Range(1, agentPath.Count-1), Random.Range(0, 2));
                    }
                    break;
                case SocialRelations.Family:
                    if(goalIndex_Family !=-1){
                        float speed_Family = UnityEngine.Random.Range(minSpeed, maxSpeed);
                        int pathDirection_Family = Random.Range(0, 2);
                        InstantiateAndConfigureAvatar(SocialRelations.Family, speed_Family, goalIndex_Family, pathDirection_Family);
                        InstantiateAndConfigureAvatar(SocialRelations.Family, speed_Family, goalIndex_Family, pathDirection_Family);
                        InstantiateAndConfigureAvatar(SocialRelations.Family, speed_Family, goalIndex_Family, pathDirection_Family);
                        goalIndex_Family = -1;
                    }else{
                        InstantiateAndConfigureAvatar(SocialRelations.Individual, UnityEngine.Random.Range(minSpeed, maxSpeed), UnityEngine.Random.Range(1, agentPath.Count-1), Random.Range(0, 2));
                    }
                    break;
                case SocialRelations.Friend:
                    if(goalIndex_Friend !=-1){
                        float speed_Friend = UnityEngine.Random.Range(minSpeed, maxSpeed);
                        int pathDirection_Friend = Random.Range(0, 2);
                        InstantiateAndConfigureAvatar(SocialRelations.Friend, speed_Friend, goalIndex_Friend, pathDirection_Friend);
                        InstantiateAndConfigureAvatar(SocialRelations.Friend, speed_Friend, goalIndex_Friend, pathDirection_Friend);
                        goalIndex_Friend = -1;
                    }else{
                        InstantiateAndConfigureAvatar(SocialRelations.Individual, UnityEngine.Random.Range(minSpeed, maxSpeed), UnityEngine.Random.Range(1, agentPath.Count-1), Random.Range(0, 2));
                    }
                    break;
                case SocialRelations.Coworker:
                    if(goalIndex_Coworker !=-1){
                        float speed_Coworker = UnityEngine.Random.Range(minSpeed, maxSpeed);
                        int pathDirection_Coworker = Random.Range(0, 2);
                        InstantiateAndConfigureAvatar(SocialRelations.Coworker, speed_Coworker, goalIndex_Coworker, pathDirection_Coworker);
                        InstantiateAndConfigureAvatar(SocialRelations.Coworker, speed_Coworker, goalIndex_Coworker, pathDirection_Coworker);
                        goalIndex_Coworker = -1;
                    }else{
                        InstantiateAndConfigureAvatar(SocialRelations.Individual, UnityEngine.Random.Range(minSpeed, maxSpeed), UnityEngine.Random.Range(1, agentPath.Count-1), Random.Range(0, 2));
                    }
                    break;
            }
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
    private void InstantiateAndConfigureAvatar(SocialRelations socialRelations, float crowdSpeed, int goalIndex, int pathDirection = 0){
        //Init
        GameObject randomAvatar = avatarPrefabs[UnityEngine.Random.Range(0, avatarPrefabs.Count)];
        GameObject instance = Instantiate(randomAvatar, this.transform);
        PathController pathController = instance.GetComponentInChildren<PathController>();
        MotionMatchingController motionMatchingController = instance.GetComponentInChildren<MotionMatchingController>();
        CollisionAvoidanceController collisionAvoidanceController = instance.GetComponentInChildren<CollisionAvoidanceController>();
        ConversationalAgentFramework conversationalAgentFramework = instance.GetComponentInChildren<ConversationalAgentFramework>();
        //Set as an individual
        pathController.socialRelations = socialRelations;
        categoryCounts[socialRelations]++;
        instance.name = socialRelations.ToString() + categoryCounts[socialRelations].ToString();
        instance.transform.parent = this.transform.Find(socialRelations.ToString()).transform;
        //Position at the start Pos
        pathController.avatarCreator = this.GetComponent<AvatarCreatorBase>();
        if(pathDirection == 0){
            pathController.Path = pathVerticesEndToStart.ToArray();
        }else{
            pathController.Path = agentPath.ToArray();
        }
        //set goal index and move the agent to starting pos
        pathController.CurrentGoalIndex = goalIndex;
        pathController.Path[goalIndex-1] = GetRandomPointOnCircle(pathController.Path[goalIndex-1], 2f);
        motionMatchingController.transform.position = pathController.Path[goalIndex - 1];
        conversationalAgentFramework.transform.position = pathController.Path[goalIndex - 1];
        //Set Initial Speed
        pathController.initialSpeed = crowdSpeed;
        pathController.maxSpeed = maxSpeed;
        pathController.minSpeed = minSpeed;

        //Set group collider and Save pathmanager
        if (socialRelations != SocialRelations.Individual)
        {
            GameObject relationGameObject = transform.Find(socialRelations.ToString()).gameObject;
            GameObject groupCollider = relationGameObject.transform.Find("GroupCollider").gameObject;
            if (groupCollider != null)
            {
                GroupParameterManager groupParameterManager = groupCollider.GetComponent<GroupParameterManager>();
                groupParameterManager.pathControllers.Add(pathController);
                collisionAvoidanceController.groupCollider = groupCollider.GetComponent<CapsuleCollider>();
            }
            //Set group manager to pathmanager
            GameObject groupRelationsGameObject   = transform.Find(socialRelations.ToString()).gameObject;
            GameObject groupColliderManagerObj = groupRelationsGameObject.transform.Find("GroupColliderManager").gameObject;
            if (groupColliderManagerObj != null)
            {
                GroupColliderManager groupColliderManager = groupColliderManagerObj.GetComponent<GroupColliderManager>();
                pathController.groupColliderManager = groupColliderManager;
            }
        }
        //Save The Agent
        instantiatedAvatars.Add(instance);
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
                Gizmos.DrawSphere(transform.position + agentPath[i], 0.1f);
            }
        }
    }
}
}