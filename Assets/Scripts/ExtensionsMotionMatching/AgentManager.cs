using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

public class AgentManager : MonoBehaviour
{

    [Header("BasicCollisionAvoidance Parameters")]
    public Vector3 AvoidanceColliderSize;
    public float GoalRadius;
    public float SlowingRadius;

    [Header("ControllGizmos Parameters")]
    public bool ShowAvoidanceForce;
    public bool ShowUnalignedCollisionAvoidance;
    public bool ShowGoalDirection;
    public bool ShowCurrentDirection;

    [Header("MotionMatchingController Debug")]
    public float SphereRadius;
    public bool DebugSkeleton;
    public bool DebugCurrent;
    public bool DebugPose;
    public bool DebugTrajectory;
    public bool DebugContacts;

    [Header("OCEAN Parameters")]
    [Range(-1f, 1f)] public float openness = 0f;
    [Range(-1f, 1f)] public float conscientiousness = 0f;
    [Range(-1f, 1f)] public float extraversion = 0f;
    [Range(-1f, 1f)] public float agreeableness = 0f;
    [Range(-1f, 1f)] public float neuroticism = 0f;

    [Header("Emotion Parameters")]
    [Range(0f, 1f)] public float e_happy = 0f;
    [Range(0f, 1f)] public float e_sad = 0f;
    [Range(0f, 1f)] public float e_angry = 0f;
    [Range(0f, 1f)] public float e_disgust = 0f;
    [Range(0f, 1f)] public float e_fear = 0f;
    [Range(0f, 1f)] public float e_shock = 0f;

    public List<GameObject> Agents = new List<GameObject>();
    private List<GameObject> PathControllers = new List<GameObject>();
    private List<GameObject> MotionMatchingControllers = new List<GameObject>();
    private List<GameObject> MotionMatchingSkinnedMeshRendererWithOCEANs = new List<GameObject>();
    private List<GameObject> MotionMatchingSkinnedMeshRenderers = new List<GameObject>();
    private List<GameObject> Avatars = new List<GameObject>();


    void Start()
    {
        for (int i = 0; i < Agents.Count; i++)
        {
            // Instantiate the Prefab at the randomPosition and with no rotation
            GameObject newPrefab = Instantiate(Agents[i], Vector3.zero, Quaternion.identity);

            // Get PathController gameobjects
            PathController pathController = newPrefab.GetComponentInChildren<PathController>();
            if(pathController != null) {
                // Read initial values
                AvoidanceColliderSize = pathController.avoidanceColliderSize;
                GoalRadius = pathController.goalRadius;
                SlowingRadius = pathController.slowingRadius;
                ShowAvoidanceForce = pathController.showAvoidanceForce;
                ShowUnalignedCollisionAvoidance = pathController.showUnalignedCollisionAvoidance;
                ShowGoalDirection = pathController.showGoalDirection;
                ShowCurrentDirection = pathController.showCurrentDirection;

                PathControllers.Add(pathController.gameObject);
            }

            // Get MotionMatchingController gameobjects
            MotionMatchingController motionMatchingController = newPrefab.GetComponentInChildren<MotionMatchingController>();
            if(motionMatchingController != null) {
                SphereRadius = motionMatchingController.SpheresRadius;
                DebugSkeleton = motionMatchingController.DebugSkeleton;
                DebugCurrent = motionMatchingController.DebugCurrent;
                DebugPose = motionMatchingController.DebugPose;
                DebugTrajectory = motionMatchingController.DebugTrajectory;
                DebugContacts = motionMatchingController.DebugContacts;
                MotionMatchingControllers.Add(motionMatchingController.gameObject);
            }

            // Get MotionMatchingSkinnedMeshRendererWithOCEAN gameobjects
            MotionMatchingSkinnedMeshRendererWithOCEAN mmSMRWithOCEAN = newPrefab.GetComponentInChildren<MotionMatchingSkinnedMeshRendererWithOCEAN>();
            if(mmSMRWithOCEAN != null) {
                // Read initial values
                openness = mmSMRWithOCEAN.openness;
                conscientiousness = mmSMRWithOCEAN.conscientiousness;
                extraversion = mmSMRWithOCEAN.extraversion;
                agreeableness = mmSMRWithOCEAN.agreeableness;
                neuroticism = mmSMRWithOCEAN.neuroticism;
                e_happy = mmSMRWithOCEAN.e_happy;
                e_sad = mmSMRWithOCEAN.e_sad;
                e_angry = mmSMRWithOCEAN.e_angry;
                e_disgust = mmSMRWithOCEAN.e_disgust;
                e_fear = mmSMRWithOCEAN.e_fear;
                e_shock = mmSMRWithOCEAN.e_shock;
                MotionMatchingSkinnedMeshRendererWithOCEANs.Add(mmSMRWithOCEAN.gameObject);
                Avatars.Add(mmSMRWithOCEAN.gameObject);
            }

            MotionMatchingSkinnedMeshRenderer mmSMR = newPrefab.GetComponentInChildren<MotionMatchingSkinnedMeshRenderer>();
            if(mmSMR != null) {
                MotionMatchingSkinnedMeshRenderers.Add(mmSMR.gameObject);
                Avatars.Add(mmSMR.gameObject);
            }
        }

        foreach(GameObject controllerObject in PathControllers) 
        {
            PathController pathController = controllerObject.GetComponent<PathController>();
            if(pathController != null) 
            {
                StartCoroutine(pathController.UpdateAvoidNeighborsVector(Avatars,0.1f, 0.3f));
            }
        }
        
    }

    public List<GameObject> GetAgents() {
        return Agents;
    }

    public List<GameObject> GetPathControllers() {
        return PathControllers;
    }

    public List<GameObject> GetMotionMatchingControllers() {
        return MotionMatchingControllers;
    }

    public List<GameObject> GetMotionMatchingSkinnedMeshRendererWithOCEANs() {
        return MotionMatchingSkinnedMeshRendererWithOCEANs;
    }

    void Update()
    {
        foreach(GameObject controllerObject in PathControllers) 
        {
            PathController pathController = controllerObject.GetComponent<PathController>();
            if(pathController != null) 
            {
                pathController.avoidanceColliderSize = AvoidanceColliderSize;
                pathController.goalRadius = GoalRadius;
                pathController.slowingRadius = SlowingRadius;
                pathController.showAvoidanceForce = ShowAvoidanceForce;
                pathController.showUnalignedCollisionAvoidance = ShowUnalignedCollisionAvoidance;
                pathController.showGoalDirection = ShowGoalDirection;
                pathController.showCurrentDirection = ShowCurrentDirection;
            }
        }

        foreach(GameObject controllerObject in MotionMatchingControllers) 
        {
            MotionMatchingController motionMatchingController = controllerObject.GetComponent<MotionMatchingController>();
            if(motionMatchingController != null) 
            {
                motionMatchingController.SpheresRadius = SphereRadius;
                motionMatchingController.DebugSkeleton = DebugSkeleton;
                motionMatchingController.DebugCurrent = DebugCurrent;
                motionMatchingController.DebugPose = DebugPose;
                motionMatchingController.DebugTrajectory = DebugTrajectory;
                motionMatchingController.DebugContacts = DebugContacts;
            }
        }

        foreach(GameObject controllerObject in MotionMatchingSkinnedMeshRendererWithOCEANs) 
        {
            MotionMatchingSkinnedMeshRendererWithOCEAN mmOCEAN = controllerObject.GetComponent<MotionMatchingSkinnedMeshRendererWithOCEAN>();
            if(mmOCEAN != null) 
            {
                mmOCEAN.openness = openness;
                mmOCEAN.conscientiousness = conscientiousness;
                mmOCEAN.extraversion = extraversion;
                mmOCEAN.agreeableness = agreeableness;
                mmOCEAN.neuroticism = neuroticism;
                mmOCEAN.e_happy = e_happy;
                mmOCEAN.e_sad = e_sad;
                mmOCEAN.e_angry = e_angry;
                mmOCEAN.e_disgust = e_disgust;
                mmOCEAN.e_fear = e_fear;
                mmOCEAN.e_shock = e_shock;
            }
        }
    }
}
