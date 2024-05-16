using UnityEngine;
using UnityEngine.AI;
using MotionMatching;
using System.Collections.Generic;

namespace CollisionAvoidance{
public class AvatarCreatorSparsePos : AvatarCreatorBase
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

    private bool avatarCreateAtStartPos = true;

    public override void InstantiateAvatars()
    {
        CalculatePath();
        CalculatePathEndToStart();
        GenerateWall();

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
            SocialRelations[] allValues = (SocialRelations[])System.Enum.GetValues(typeof(SocialRelations));
            SocialRelations randomRelation;
            do
            {
                randomRelation = allValues[UnityEngine.Random.Range(0, allValues.Length)];
                //change role based on spawn point
                if(avatarCreateAtStartPos){
                    if(randomRelation == SocialRelations.Couple){
                        randomRelation = SocialRelations.Family;
                    }else if(randomRelation == SocialRelations.Coworker){
                        randomRelation = SocialRelations.Friend;
                    }
                }else{ 
                    if(randomRelation == SocialRelations.Family){
                        randomRelation = SocialRelations.Couple;
                    }else if(randomRelation == SocialRelations.Friend){
                        randomRelation = SocialRelations.Coworker;
                    }
                }
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
                if(avatarCreateAtStartPos){
                    pathController.Path = pathVertices.ToArray();
                    avatarCreateAtStartPos = false;
                }else{
                    pathController.Path = pathVerticesEndToStart.ToArray();
                    avatarCreateAtStartPos = true;
                }   
            }

            //Path Noise
            //pathController.Path[0] += GenerateRandomPointInCircle(startPointDeviation);
            pathController.Path[0] += GenerateRandomPointInCircleBasedOnSocialRelations(wallToWallDist, randomRelation);
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

        GameObject leftWall = CreateWall(centerLeft, direction, distance);
        GameObject rightWall =CreateWall(centerRight, direction, distance);

        GameObject corridor = new GameObject("Corridor");
        // WallToWallDistChanger wallToWallDistChanger = corridor.AddComponent<WallToWallDistChanger>();
        // wallToWallDistChanger.WallToWallDist = wallToWallDist;
        // wallToWallDistChanger.leftWall = leftWall;
        // wallToWallDistChanger.rightWall = rightWall;

        corridor.transform.SetParent(wallParent.transform);
        leftWall.transform.SetParent(corridor.transform);
        rightWall.transform.SetParent(corridor.transform);
    }

    private GameObject CreateWall(Vector3 center, Vector3 direction, float distance)
    {
        Quaternion rotation = Quaternion.LookRotation(direction);

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

        boxCollider.size = new Vector3(wallWidth + wallColliderAdditionalWidth, wallHeight, distance);
        boxCollider.center = new Vector3(0, wallHeight/2f, 0);
        Rigidbody rigidBody = wallSegment.AddComponent<Rigidbody>();
        rigidBody.useGravity = false;
        // wallSegment.AddComponent<WallCollisionDetection>();

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-wallWidth/2, 0, -distance/2),
            new Vector3(wallWidth/2, 0, -distance/2),
            new Vector3(wallWidth/2, wallHeight, -distance/2),
            new Vector3(-wallWidth/2, wallHeight, -distance/2),
            new Vector3(-wallWidth/2, 0, distance/2),
            new Vector3(wallWidth/2, 0, distance/2),
            new Vector3(wallWidth/2, wallHeight, distance/2),
            new Vector3(-wallWidth/2, wallHeight, distance/2)
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

        return wallSegment;
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