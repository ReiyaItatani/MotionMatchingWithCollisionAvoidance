using UnityEngine;
using UnityEngine.AI;
using MotionMatching;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;

namespace CollisionAvoidance{

public class AvatarCreatorRunTime : AvatarCreatorBase
{
    public List<Vector3> pathVerticesEndToStart = new List<Vector3>();

    [Header("Additional Path")]
    public List<Vector3> _additionalPath = new List<Vector3>();

    [Header("Agent Position Parameters")]
    [Range(0.5f, 2)]
    public float initialAgentDistance = 1f;

    [SerializeField]
    private bool onInitialPosOffset = false ;

    [Header("Custom Parameters")]
    public bool onIndividual = false;
    [Range(0,1)]
    public float WalkAnimationProbability = 0.5f;
    private bool SpawnOnStartPos = false;
    private bool _isSpawned = false;
    private bool _onSpawning = false;
    
    public void DeleteAndInstantiate()
    {
        if(_onSpawning == true){
            return;
        }
        _onSpawning = true;
        StartCoroutine(DeleteAndInstantiateWithDelay());
    }

    private IEnumerator DeleteAndInstantiateWithDelay()
    {   
        DeleteAvatars();
        
        yield return new WaitForSeconds(1.0f);
        
        InstantiateAvatars();

        ManageWalkAnimationProbability(WalkAnimationProbability);

        List<GameObject> firstChildObjects = new List<GameObject>();

        foreach (var avatar in instantiatedAvatars)
        {
            if (avatar.transform.childCount > 0)
            {
                firstChildObjects.Add(avatar.transform.GetChild(0).gameObject);
            }
        }

        _onSpawning = false;
    }

    public override void InstantiateAvatars()
    {
        if(_isSpawned == true){
            return;
        }

        SpawnOnStartPos = !SpawnOnStartPos;
        //CalculatePath();
        pathVertices = new List<Vector3>(_additionalPath);
        //CalculatePathEndToStart();
        pathVerticesEndToStart = new List<Vector3>(_additionalPath);
        pathVerticesEndToStart.Reverse();

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

        if(spawnCount >= 4){
            Debug.Log("In this avatarcreator, the number of spawned agents should be lower than 4.");
            spawnCount = 3;
        }

        SocialRelations _socialRelations;
        List<Vector3> _path = new List<Vector3>();
        if(onIndividual){
            _socialRelations = SocialRelations.Individual;
            if(SpawnOnStartPos){
                _path = pathVertices;
                Debug.Log("Spawn on Start Pos");
            }else{
                _path = pathVerticesEndToStart;   
                Debug.Log("Spawn on End Pos");
            }
        }else{
            _socialRelations = SocialRelations.Friend;
            if(SpawnOnStartPos){
                _path = pathVerticesEndToStart;
                Debug.Log("Spawn on Start Pos");
            }else{
                _path = pathVertices;   
                Debug.Log("Spawn on End Pos");
            }
        }

        for (int i = 0; i < spawnCount; i++)
        {
            //Init
            // GameObject randomAvatar = avatarPrefabs[UnityEngine.Random.Range(0, avatarPrefabs.Count)];
            GameObject randomAvatar = avatarPrefabs[i];
            GameObject instance = Instantiate(randomAvatar, this.transform);
            PathController pathController = instance.GetComponentInChildren<PathController>();
            MotionMatchingController motionMatchingController = instance.GetComponentInChildren<MotionMatchingController>();
            CollisionAvoidanceController collisionAvoidanceController = instance.GetComponentInChildren<CollisionAvoidanceController>();
            ConversationalAgentFramework conversationalAgentFramework = instance.GetComponentInChildren<ConversationalAgentFramework>();
            //Set Social Relations
            pathController.socialRelations = _socialRelations;
            categoryCounts[_socialRelations]++;
            instance.name = _socialRelations.ToString()+categoryCounts[_socialRelations].ToString();
            instance.transform.parent = this.transform.Find(_socialRelations.ToString()).transform;
            //Position at the start Pos
            pathController.avatarCreator = this.GetComponent<AvatarCreatorBase>();
            pathController.Path = _path.ToArray();
            //Select Position
            if(spawnCount == 1 && onInitialPosOffset){
                 pathController.Path[0] += new Vector3(0f, 0f, initialAgentDistance);
            }
            if(spawnCount == 2){
                if(i == 0){
                    pathController.Path[0] += new Vector3(0f, 0f, initialAgentDistance);
                }
                if(i == 1){
                    pathController.Path[0] += new Vector3(0f, 0f, -initialAgentDistance);
                }
            }
            if(spawnCount == 3){
                if(i == 1){
                    pathController.Path[0] += new Vector3(0f, 0f, initialAgentDistance);
                }
                if(i == 2){
                    pathController.Path[0] += new Vector3(0f, 0f, -initialAgentDistance);
                }
            }
            //Move the agent to starting pos
            motionMatchingController.transform.position = pathController.Path[0];
            conversationalAgentFramework.transform.position = pathController.Path[0];
            //Set Initial Speed
            pathController.initialSpeed = friendSpeed;
            pathController.minSpeed = minSpeed;
            pathController.maxSpeed = maxSpeed;
            //Set group collider and Save pathmanager
            if(!onIndividual){
                GameObject relationGameObject = transform.Find(_socialRelations.ToString()).gameObject;
                GameObject groupCollider = relationGameObject.transform.Find("GroupCollider").gameObject;
                if (groupCollider != null)
                {
                    GroupParameterManager groupParameterManager = groupCollider.GetComponent<GroupParameterManager>();
                    groupParameterManager.pathControllers.Add(pathController);
                    collisionAvoidanceController.groupCollider = groupCollider.GetComponent<CapsuleCollider>();
                }
            }

            //set group manager to pathmanager
            if (_socialRelations != SocialRelations.Individual)
            {
                GameObject groupRelationsGameObject   = transform.Find(_socialRelations.ToString()).gameObject;
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

        //Set Active 
        foreach(GameObject _agent in instantiatedAvatars){
            _agent.SetActive(true);
        }

        _isSpawned = true;

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
                        Destroy(groupCollider);
                        Destroy(groupColliderManager);
                    }
                }
            }
        }
    }

    public override  void DeleteAvatars()
    {
        if(_isSpawned == false){
            return;
        }

        foreach (GameObject avatar in instantiatedAvatars)
        {
            avatar.SetActive(false);
        }

        foreach (GameObject avatar in instantiatedAvatars)
        {
            Destroy(avatar);
        }
        foreach (GameObject categoryGameObject in categoryGameObjects){
            Destroy(categoryGameObject);
        }
        instantiatedAvatars.Clear();
        categoryGameObjects.Clear();
        pathVertices = new List<Vector3>();
        InitializeDictionaries();

        _isSpawned = false;
    }

    // public List<Vector3> CalculatePathEndToStart()
    // {
    //     path = new NavMeshPath();
    //     pathVerticesEndToStart = new List<Vector3>();

    //     if (NavMesh.CalculatePath(endPoint.position, startPoint.position, NavMesh.AllAreas, path))
    //     {
    //         foreach (var corner in path.corners)
    //         {
    //             pathVerticesEndToStart.Add(corner);
    //         }
    //     }
    //     return pathVerticesEndToStart;
    // }

    private void ManageWalkAnimationProbability(float _probability)
    {
        List<GameObject> _avatarPrefabs = new List<GameObject>();
        _avatarPrefabs = this.GetAgents();
        List<SocialBehaviour> _socialBehaviours = new List<SocialBehaviour>();
        foreach(GameObject avatarprefab in _avatarPrefabs)
        {
            _socialBehaviours.Add(avatarprefab.GetComponent<SocialBehaviour>());
        }

        foreach(SocialBehaviour socialBehaviour in _socialBehaviours){
            socialBehaviour.WalkAnimationProbability = 1;
        }
        
        //Only the first agent will have the probability
        _socialBehaviours[0].WalkAnimationProbability = _probability;

    }

}

}
