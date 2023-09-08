using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

public class AgentManager : MonoBehaviour
{

    [Header("BasicCollisionAvoidance Parameters")]

    [Tooltip("Size of the avoidance collider.")]
    public Vector3 AvoidanceColliderSize = new Vector3(1.5f, 1.5f, 2.0f);
    [Tooltip("Radius to consider as the goal.")]
    [Range(0.1f, 5.0f)]
    public float goalRadius = 0.5f;
    [Tooltip("Radius to start slowing down.")]
    [Range(0.1f, 5.0f)]
    public float slowingRadius = 2.0f;

    [Space]

    [Tooltip("Weight for moving towards the goal.")]
    [Range(0.0f, 5.0f)]
    public float toGoalWeight = 1.7f;
    [Tooltip("Weight to avoid neighbors.")]
    [Range(0.0f, 5.0f)]
    public float avoidNeighborWeight = 1.0f;
    [Tooltip("Weight for general avoidance.")]
    [Range(0.0f, 5.0f)]
    public float avoidanceWeight = 1.5f;

    [Space]

    [Range(0.0f, 2.0f), Tooltip("Max distance between SimulationBone and SimulationObject")] 
    public float MaxDistanceMMAndCharacterController = 0.1f;
    [Range(0.0f, 2.0f), Tooltip("Time needed to move half of the distance between SimulationBone and SimulationObject")] 
    public float PositionAdjustmentHalflife = 0.1f;
    [Range(0.0f, 2.0f), Tooltip("Ratio between the adjustment and the character's velocity to clamp the adjustment")] 
    public float PosMaximumAdjustmentRatio = 0.1f;

    [Header("ControllGizmos Parameters")]
    public bool showAgentSphere = false;
    public bool ShowAvoidanceForce = false;
    public bool ShowUnalignedCollisionAvoidance = false;
    public bool ShowGoalDirection = false;
    public bool ShowCurrentDirection = false;

    [Header("MotionMatchingController Debug")]
    // public float SphereRadius;
    public bool DebugSkeleton = false;
    public bool DebugCurrent = false;
    public bool DebugPose = false;
    public bool DebugTrajectory = false;
    public bool DebugContacts = false;

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

    private List<GameObject> PathControllers = new List<GameObject>();
    private List<GameObject> MotionMatchingControllers = new List<GameObject>();
    private List<GameObject> MotionMatchingSkinnedMeshRendererWithOCEANs = new List<GameObject>();
    private List<GameObject> Avatars = new List<GameObject>();
    private AvatarCreator avatarCreator;

    [Header("Social Behaviour")]
    public bool onTalk = false;
    public bool onAnimation = true;

    void Awake(){
        avatarCreator = this.GetComponent<AvatarCreator>();
        Avatars = avatarCreator.instantiatedAvatars; 
    }

    void Start()
    {
        for (int i = 0; i < Avatars.Count; i++)
        {
            // Get PathController gameobjects
            PathController pathController = Avatars[i].GetComponentInChildren<PathController>();
            if(pathController != null) {
                SetPathControllerParams(pathController);
                PathControllers.Add(pathController.gameObject);
            }

            // Get MotionMatchingController gameobjects
            MotionMatchingController motionMatchingController = Avatars[i].GetComponentInChildren<MotionMatchingController>();
            if(motionMatchingController != null) {
                SetMotionMatchingControllerParams(motionMatchingController);
                MotionMatchingControllers.Add(motionMatchingController.gameObject);
            }

            // Get MotionMatchingSkinnedMeshRendererWithOCEAN gameobjects
            MotionMatchingSkinnedMeshRendererWithOCEAN mmSMRWithOCEAN = Avatars[i].GetComponentInChildren<MotionMatchingSkinnedMeshRendererWithOCEAN>();
            if(mmSMRWithOCEAN != null) {
                SetMotionMatchingSkinnedMeshRendererWithOCEANParams(mmSMRWithOCEAN);
                MotionMatchingSkinnedMeshRendererWithOCEANs.Add(mmSMRWithOCEAN.gameObject);
            }

            // Get SocialBehaviour gameobjects
            SocialBehaviour socialBehaviour = Avatars[i].GetComponentInChildren<SocialBehaviour>();
            if(socialBehaviour != null) {
                SetSocialBehaviourParams(socialBehaviour);
            }     
        }
    }

    private void OnValidate() {
        foreach(GameObject controllerObject in PathControllers) 
        {
            PathController pathController = controllerObject.GetComponent<PathController>();
            if(pathController != null) 
            {
                SetPathControllerParams(pathController);
            }
        }

        foreach(GameObject controllerObject in MotionMatchingControllers) 
        {
            MotionMatchingController motionMatchingController = controllerObject.GetComponent<MotionMatchingController>();
            if(motionMatchingController != null) 
            {
                SetMotionMatchingControllerParams(motionMatchingController);
            }
        }

        foreach(GameObject controllerObject in MotionMatchingSkinnedMeshRendererWithOCEANs) 
        {
            MotionMatchingSkinnedMeshRendererWithOCEAN mmSMRWithOCEAN = controllerObject.GetComponent<MotionMatchingSkinnedMeshRendererWithOCEAN>();
            if(mmSMRWithOCEAN != null) 
            {
                SetMotionMatchingSkinnedMeshRendererWithOCEANParams(mmSMRWithOCEAN);
            }
            SocialBehaviour socialBehaviour = controllerObject.GetComponent<SocialBehaviour>();
            if(socialBehaviour != null) {
                SetSocialBehaviourParams(socialBehaviour);
            }    
        }
    }

    private void SetPathControllerParams(PathController pathController){
        pathController.avoidanceColliderSize = AvoidanceColliderSize;

        pathController.goalRadius = goalRadius;
        pathController.slowingRadius = slowingRadius;

        pathController.toGoalWeight = toGoalWeight;
        pathController.avoidanceWeight = avoidanceWeight;
        pathController.avoidNeighborWeight = avoidNeighborWeight;

        pathController.MaxDistanceMMAndCharacterController = MaxDistanceMMAndCharacterController;
        pathController.PositionAdjustmentHalflife = PositionAdjustmentHalflife;
        pathController.PosMaximumAdjustmentRatio = PosMaximumAdjustmentRatio;

        pathController.showAgentSphere = showAgentSphere;
        pathController.showAvoidanceForce = ShowAvoidanceForce;
        pathController.showUnalignedCollisionAvoidance = ShowUnalignedCollisionAvoidance;
        pathController.showGoalDirection = ShowGoalDirection;
        pathController.showCurrentDirection = ShowCurrentDirection;
    }

    private void SetMotionMatchingControllerParams(MotionMatchingController motionMatchingController){
        // motionMatchingController.SpheresRadius = SphereRadius;
        motionMatchingController.DebugSkeleton = DebugSkeleton;
        motionMatchingController.DebugCurrent = DebugCurrent;
        motionMatchingController.DebugPose = DebugPose;
        motionMatchingController.DebugTrajectory = DebugTrajectory;
        motionMatchingController.DebugContacts = DebugContacts;
    }

    private void SetMotionMatchingSkinnedMeshRendererWithOCEANParams(MotionMatchingSkinnedMeshRendererWithOCEAN mmSMRWithOCEAN){
        mmSMRWithOCEAN.openness = openness;
        mmSMRWithOCEAN.conscientiousness = conscientiousness;
        mmSMRWithOCEAN.extraversion = extraversion;
        mmSMRWithOCEAN.agreeableness = agreeableness;
        mmSMRWithOCEAN.neuroticism = neuroticism;
        mmSMRWithOCEAN.e_happy = e_happy;
        mmSMRWithOCEAN.e_sad = e_sad;
        mmSMRWithOCEAN.e_angry = e_angry;
        mmSMRWithOCEAN.e_disgust = e_disgust;
        mmSMRWithOCEAN.e_fear = e_fear;
        mmSMRWithOCEAN.e_shock = e_shock;      
    }

    private void SetSocialBehaviourParams(SocialBehaviour socialBehaviour){
        socialBehaviour.onTalk = onTalk;
        socialBehaviour.onAnimation = onAnimation;
    }

}
