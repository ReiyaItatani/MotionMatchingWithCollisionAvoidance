using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

public class AgentManager : MonoBehaviour
{

    [Header("BasicCollisionAvoidance Parameters")]
    public Vector3 AvoidanceColliderSize;

    [Header("ControllGizmos Parameters")]
    public bool ShowAvoidanceForce;
    public bool ShowUnalignedCollisionAvoidance;
    public bool ShowGoalDirection;
    public bool ShowCurrentDirection;

    [Header("MotionMatchingController Debug")]
    // public float SphereRadius;
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

    private List<GameObject> PathControllers = new List<GameObject>();
    private List<GameObject> MotionMatchingControllers = new List<GameObject>();
    private List<GameObject> MotionMatchingSkinnedMeshRendererWithOCEANs = new List<GameObject>();
    private List<GameObject> Avatars = new List<GameObject>();
    private AvatarCreator avatarCreator;

    [Header("Others")]
    public bool onConversation = false;
    private List<GameObject> soundObjects = new List<GameObject>();

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
                // Read initial values
                AvoidanceColliderSize = pathController.avoidanceColliderSize;;
                ShowAvoidanceForce = pathController.showAvoidanceForce;
                ShowUnalignedCollisionAvoidance = pathController.showUnalignedCollisionAvoidance;
                ShowGoalDirection = pathController.showGoalDirection;
                ShowCurrentDirection = pathController.showCurrentDirection;

                PathControllers.Add(pathController.gameObject);
            }

            // Get MotionMatchingController gameobjects
            MotionMatchingController motionMatchingController = Avatars[i].GetComponentInChildren<MotionMatchingController>();
            if(motionMatchingController != null) {
                // SphereRadius = motionMatchingController.SpheresRadius;
                DebugSkeleton = motionMatchingController.DebugSkeleton;
                DebugCurrent = motionMatchingController.DebugCurrent;
                DebugPose = motionMatchingController.DebugPose;
                DebugTrajectory = motionMatchingController.DebugTrajectory;
                DebugContacts = motionMatchingController.DebugContacts;
                MotionMatchingControllers.Add(motionMatchingController.gameObject);
            }

            // Get MotionMatchingSkinnedMeshRendererWithOCEAN gameobjects
            MotionMatchingSkinnedMeshRendererWithOCEAN mmSMRWithOCEAN = Avatars[i].GetComponentInChildren<MotionMatchingSkinnedMeshRendererWithOCEAN>();
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
                soundObjects.Add(mmSMRWithOCEAN.gameObject.GetComponentInChildren<AudioSource>().gameObject);
            }
        }
    }

    private void OnValidate() {
        foreach(GameObject controllerObject in PathControllers) 
        {
            PathController pathController = controllerObject.GetComponent<PathController>();
            if(pathController != null) 
            {
                pathController.avoidanceColliderSize = AvoidanceColliderSize;
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
                // motionMatchingController.SpheresRadius = SphereRadius;
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
        foreach(GameObject controllerObject in soundObjects){
            if(onConversation == true){
                controllerObject.SetActive(true);
            }else{
                controllerObject.SetActive(false);
            }
        }
        
    }
}
