using UnityEngine;
using UnityEngine.AI;
using MotionMatching;
using System.Collections.Generic;
using System;

namespace CollisionAvoidance{

public class AvatarCreatorCrossRoad : AvatarCreatorBase
{
    [HideInInspector]
    public List<Vector3> pathVerticesEndToStart = new List<Vector3>();

    [Header("Wall Parameters")]
    public float wallHeight = 3f;
    public float wallWidth = 0.2f;
    public float wallColliderAdditionalWidth = 5.0f;
    public float wallToWallDist = 4.0f;
    [HideInInspector]
    public GameObject wallParent;

    [Header("Agent Position Parameters")]
    [Range(0.5f, 2)]
    public float initialAgentDistance = 1f;
    [Header("Agent Position Parameters")]
    [Range(0.5f, 5)]
    public float interGroupDistance= 5f;

    public override void InstantiateAvatars()
    {
        CalculatePath();
        CalculatePathEndToStart();
        GenerateWall();

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
        /***********************************************************/
        /************************Create Avatars***********************/
        /***********************************************************/
        if(spawnCount >= 5){
            Debug.Log("In this avatarcreator, the number of spawned agents should be lower than 4.");
            spawnCount = 4;
        }

        //To randamize the group positions 
        List<int> randomValue = new List<int>{};
        System.Random rand = new System.Random();

        for (int i = 1; i <= spawnCount; i++)
        {
            randomValue.Add(i);
        }

        for (int i = randomValue.Count - 1; i > 0; i--)
        {
            int swapIndex = rand.Next(i + 1);
            int temp = randomValue[i];
            randomValue[i] = randomValue[swapIndex];
            randomValue[swapIndex] = temp;
        }

        //Define Speed
        float crowdSpeed = UnityEngine.Random.Range(minSpeed, maxSpeed);
        if(spawnCount >= 1){
            for(int i = 1; i <= 1; i++){
                InstantiateAndConfigureAvatar(SocialRelations.Individual, i, randomValue[0], crowdSpeed);
            }
        }
        if(spawnCount >= 2){
            for(int i = 1; i <= 2; i++){
                InstantiateAndConfigureAvatar(SocialRelations.Friend, i, randomValue[1], crowdSpeed);
            }
        }
        if(spawnCount >= 3){
            for(int i = 1; i <= 3; i++){
                InstantiateAndConfigureAvatar(SocialRelations.Family, i, randomValue[2], crowdSpeed);
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

    private void InstantiateAndConfigureAvatar(SocialRelations socialRelations, int agentIndex, int multiplier, float crowdSpeed){
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
        pathController.Path = pathVertices.ToArray();
        //Configure
        //Init
        Vector3 difference = pathController.Path[0] - pathController.Path[1];
        Vector3 upVector = new Vector3(0f, 1f, 0f); // Commonly used up vector
        Vector3 directionNormalized = difference.normalized;
        Vector3 moveAlongDirection = directionNormalized * interGroupDistance;
        Vector3 orthogonal = Vector3.Cross(difference, upVector).normalized;
        Vector3 adjustment = new Vector3(0, 0, 0);

        switch(socialRelations){
            case SocialRelations.Individual :
                pathController.Path[0] += moveAlongDirection * multiplier;
                break;
            case SocialRelations.Friend :

                // Depending on the condition, modify pathController.Path[0]
                if (agentIndex == 1)
                {
                    // Calculate normalized orthogonal vector, then adjust by initialAgentDistance
                    adjustment = orthogonal * initialAgentDistance;
                    pathController.Path[0] += adjustment + moveAlongDirection * multiplier;
                }
                else if (agentIndex == 2)
                {
                    // Calculate normalized orthogonal vector, then adjust by -initialAgentDistance
                    adjustment = orthogonal * -initialAgentDistance;
                    pathController.Path[0] += adjustment + moveAlongDirection * multiplier;
                }
                break;
            case SocialRelations.Family :
                // Depending on the condition, modify pathController.Path[0]
                if (agentIndex == 1)
                {
                    // Calculate normalized orthogonal vector, then adjust by initialAgentDistance
                    adjustment = orthogonal * initialAgentDistance;
                    pathController.Path[0] += adjustment + moveAlongDirection * multiplier;
                }
                else if (agentIndex == 2)
                {
                    // Calculate normalized orthogonal vector, then adjust by -initialAgentDistance
                    adjustment = orthogonal * - initialAgentDistance;
                    pathController.Path[0] += adjustment + moveAlongDirection * multiplier;
                }
                else if (agentIndex == 3)
                {
                    pathController.Path[0] += moveAlongDirection * multiplier;
                }
                break;
        }
        motionMatchingController.transform.position = pathController.Path[0];
        conversationalAgentFramework.transform.position = pathController.Path[0];
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
        pathVertices = new List<Vector3>();
        InitializeDictionaries();
        DestroyWall();
    }

    public List<Vector3> CalculatePathEndToStart()
    {
        path = new NavMeshPath();
        pathVerticesEndToStart = new List<Vector3>();

        if (NavMesh.CalculatePath(endPoint.position, startPoint.position, NavMesh.AllAreas, path))
        {
            foreach (var corner in path.corners)
            {
                pathVerticesEndToStart.Add(corner);
            }
        }
        return pathVerticesEndToStart;
    }

    void GenerateWall()
    {
        wallParent = new GameObject("WallParent");
        wallParent.transform.SetParent(transform);

        for (int i = 0; i < pathVertices.Count - 1; i++)
        {
            Vector3 start = pathVertices[i];
            Vector3 end = pathVertices[i + 1];

            CreateWallSegment(start, end);
        }
    }

    void CreateWallSegment(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        direction.Normalize();

        Vector3 normal = Vector3.up;

        Vector3 offset = Vector3.Cross(direction, normal) * wallToWallDist;

        Vector3 centerLeft = (start + end) / 2 - offset;
        Vector3 centerRight = (start + end) / 2 + offset;

        List<GameObject> leftWalls = CreateWall(centerLeft, direction, distance);
        List<GameObject> rightWalls =CreateWall(centerRight, direction, distance);

        GameObject corridor = new GameObject("Corridor");
        // WallToWallDistChanger wallToWallDistChanger = corridor.AddComponent<WallToWallDistChanger>();
        // wallToWallDistChanger.WallToWallDist = wallToWallDist;
        // wallToWallDistChanger.leftWall = leftWall;
        // wallToWallDistChanger.rightWall = rightWall;

        corridor.transform.SetParent(wallParent.transform);
        foreach(GameObject leftWall in leftWalls){
            leftWall.transform.SetParent(corridor.transform);
        }
        foreach(GameObject rightWall in rightWalls){
            rightWall.transform.SetParent(corridor.transform);
        }
    }

    private List<GameObject> CreateWall(Vector3 center, Vector3 direction, float distance)
    {
        Quaternion rotation = Quaternion.LookRotation(direction);
        List<GameObject> wallSegments = new List<GameObject>{};
        for(int wallPos = 1; wallPos >= -1; wallPos -= 2){
            GameObject wallSegment = new GameObject("WallSegment");
            wallSegment.tag = "Wall";
            wallSegment.transform.position = center;
            wallSegment.transform.rotation = rotation;
            wallSegment.transform.SetParent(wallParent.transform);

            MeshFilter meshFilter = wallSegment.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = wallSegment.AddComponent<MeshRenderer>();
            BoxCollider boxCollider = wallSegment.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            NormalVector wallNormalVector = wallSegment.AddComponent<NormalVector>();

            boxCollider.size = new Vector3(wallWidth + wallColliderAdditionalWidth, wallHeight, distance/2 - wallToWallDist);
            boxCollider.center = new Vector3(0, wallHeight/2f,wallPos * (wallToWallDist+distance/2)/2);
            Rigidbody rigidBody = wallSegment.AddComponent<Rigidbody>();
            rigidBody.useGravity = false;
            // wallSegment.AddComponent<WallCollisionDetection>();

            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
                new Vector3(-wallWidth/2, 0, wallToWallDist * wallPos),
                new Vector3(wallWidth/2, 0, wallToWallDist * wallPos),
                new Vector3(wallWidth/2, wallHeight, wallToWallDist * wallPos),
                new Vector3(-wallWidth/2, wallHeight, wallToWallDist * wallPos),
                new Vector3(-wallWidth/2, 0, distance/2 * wallPos),
                new Vector3(wallWidth/2, 0, distance/2 * wallPos),
                new Vector3(wallWidth/2, wallHeight, distance/2 * wallPos),
                new Vector3(-wallWidth/2, wallHeight, distance/2 * wallPos),
            };
            mesh.triangles = new int[]
            {
                0, 2, 1, 0, 3, 2, // front face
                4, 5, 6, 4, 6, 7, // back face
                0, 1, 5, 0, 5, 4, // bottom face
                2, 3, 7, 2, 7, 6, // top face
                1, 2, 6, 1, 6, 5, // right face
                0, 4, 7, 0, 7, 3  // left face
            };
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;

            //URP
            Material blackMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            //Builtin
            //Material blackMaterial = new Material(Shader.Find("Standard"));
            blackMaterial.color = Color.black;
            blackMaterial.color = Color.black;
            meshRenderer.material = blackMaterial;

            wallSegment.transform.SetParent(wallParent.transform);

            wallSegments.Add(wallSegment);
        }
        return wallSegments;
    }

    public void DestroyWall()
    {
        if (wallParent != null)
        {
            DestroyImmediate(wallParent);
        }
    }
}

}
