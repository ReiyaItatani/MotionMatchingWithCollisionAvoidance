using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;
using Unity.VisualScripting;
using Drawing;

public enum UpperBodyAnimatiomState{
    Walk,//Smoke, Hold a bug, Carry a buggage For Group People //Motion Matching
    Talk,//For Group People //Unity Animation
    SmartPhone//ListenToMusic, Texting, Calling, For Individual// Unity Animation
}

[RequireComponent(typeof(ParameterManager))]
[RequireComponent(typeof(Animator))]
public class SocialBehaviour : MonoBehaviour
{
    [Header("Conversation")]
    public bool onTalk = false;
    public AudioSource audioSource;
    public AudioClip[] audioClips;
    [Range(0, 1)] 
    public float playProbability = 0.1f; 

    [Header("Animation")]
    public MotionMatchingSkinnedMeshRendererWithOCEAN motionMatchingSkinnedMeshRendererWithOCEAN;
    private AvatarMaskData initialAvatarMask;

    [Header("LookAt")]
    private ParameterManager parameterManager;
    private float fieldOfView = 45f;

    [Header("AnimationState")]
    [ReadOnly]
    public UpperBodyAnimatiomState currentAnimationState = UpperBodyAnimatiomState.Walk;
    private Animator animator;

    void Awake()
    {
        parameterManager = this.GetComponent<ParameterManager>();
        animator = this.GetComponent<Animator>();
        if(motionMatchingSkinnedMeshRendererWithOCEAN!=null){
            initialAvatarMask = motionMatchingSkinnedMeshRendererWithOCEAN.AvatarMask;
        }
        FollowMotionMatching();
    }

    void Start(){
        StartCoroutine(UpdateCurrentLookAt(0.2f, parameterManager.GetSocialRelations(), this.gameObject));  
        StartCoroutine(UpdateAnimationState(parameterManager.GetSocialRelations()));
    }

    #region AnimationStateControl
    private IEnumerator UpdateAnimationState(SocialRelations _socialRelations)
    {
        while (true)
        {
            List<GameObject> groupAgents = parameterManager.GetAvatarCreatorBase().GetAgentsInCategory(_socialRelations);
            currentAnimationState = DetermineAnimationState(_socialRelations, groupAgents);

            TriggerUnityAnimation(currentAnimationState);
            
            if (currentAnimationState == UpperBodyAnimatiomState.Walk)
            {
                FollowMotionMatching();
            }

            yield return new WaitForSeconds(UnityEngine.Random.Range(10.0f, 20.0f));
        }
    }

    private UpperBodyAnimatiomState DetermineAnimationState(SocialRelations _socialRelations, List<GameObject> groupAgents)
    {
        float randomValue = UnityEngine.Random.value;
        if (_socialRelations == SocialRelations.Individual || groupAgents.Count <= 1)
        {
            return randomValue < 0.5f ? UpperBodyAnimatiomState.Walk : UpperBodyAnimatiomState.SmartPhone;
        }
        else
        {
            return randomValue < 0.5f ? UpperBodyAnimatiomState.Talk : UpperBodyAnimatiomState.Walk;
        }
    }

    public void FollowMotionMatching()
    {
        TriggerUnityAnimation(UpperBodyAnimatiomState.Walk);
        motionMatchingSkinnedMeshRendererWithOCEAN.AvatarMask = null;
    }

    public void TriggerUnityAnimation(UpperBodyAnimatiomState animationState)
    {
        motionMatchingSkinnedMeshRendererWithOCEAN.AvatarMask = initialAvatarMask;

        foreach (UpperBodyAnimatiomState animState in Enum.GetValues(typeof(UpperBodyAnimatiomState)))
        {
            if (animState == animationState)
            {
                animator.SetBool(animState.ToString(), true);
            }
            else
            {
                animator.SetBool(animState.ToString(), false);
            }
        }
    }
    #endregion

    public void DeleteCollidedTarget(){
        motionMatchingSkinnedMeshRendererWithOCEAN.SetCollidedTarget(null);
    }
    public void TryPlayAudio()
    {
        if (onTalk && audioSource != null && UnityEngine.Random.value < playProbability && audioClips.Length >= 1)
        {
            int randomIndex = UnityEngine.Random.Range(0, audioClips.Length);
            audioSource.clip = audioClips[randomIndex];
            audioSource.Play();
        }
    }

    #region UpdateLookAt
    private IEnumerator UpdateCurrentLookAt(float updateTime, SocialRelations socialRelations, GameObject agentGameObject)
    {
        List<GameObject> groupAgents = parameterManager.GetAvatarCreatorBase().GetAgentsInCategory(socialRelations);
        SocialRelations mySocialRelations = parameterManager.GetSocialRelations();
        bool isIndividual = groupAgents.Count <= 1 || socialRelations == SocialRelations.Individual;

        IfIndividual(isIndividual);

        while (true)
        {
            UpdateDirectionAndAvoidance(mySocialRelations);

            if (!isIndividual)
            {
                UpdateGroupAgentLookAt(groupAgents, agentGameObject);
            }

            yield return new WaitForSeconds(updateTime);
        }
    }

    private void UpdateDirectionAndAvoidance(SocialRelations mySocialRelations)
    {
        SetCurrentDirection(parameterManager.GetCurrentDirection());
        GameObject currentAvoidanceTarget = parameterManager.GetCurrentAvoidanceTarget();
        
        if (currentAvoidanceTarget != null)
        {
            SocialRelations avoidanceTargetSocialRelations = currentAvoidanceTarget.GetComponent<IParameterManager>().GetSocialRelations();
            SetCurrentAvoidanceTarget(avoidanceTargetSocialRelations != mySocialRelations ? currentAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentPosition() : Vector3.zero);
        }
        else
        {
            SetCurrentAvoidanceTarget(Vector3.zero);
        }
    }

    private void UpdateGroupAgentLookAt(List<GameObject> groupAgents, GameObject agentGameObject)
    {
        Vector3 headDirection = GetCurrentLookAt();
        if (headDirection != Vector3.zero)
        {
            Vector3 currentPosition = parameterManager.GetCurrentPosition();
            Vector3 gazeAngleDirection = CalculateGazingDirectionToCOM(groupAgents, currentPosition, headDirection, agentGameObject, fieldOfView);
            SetCurrentCenterOfMass(gazeAngleDirection);
        }
    }

    public void IfIndividual(bool isIndividual)
    {
        motionMatchingSkinnedMeshRendererWithOCEAN.IfIndividual(isIndividual);
    }

    public Vector3 GetCurrentLookAt()
    {
        return motionMatchingSkinnedMeshRendererWithOCEAN.GetCurrentLookAt();
    }

    private Vector3 CalculateGazingDirectionToCOM(List<GameObject> groupAgents, Vector3 currentPos, Vector3 currentLookDir, GameObject myself, float angleLimit)
    {
        Vector3            centerOfMass = CalculateCenterOfMass(groupAgents, myself);
        Vector3 directionToCenterOfMass = (centerOfMass - currentPos).normalized;    

        float             angle = Vector3.Angle(currentLookDir, directionToCenterOfMass);
        float neckRotationAngle = 0f;

        if (angle > angleLimit)
        {
            neckRotationAngle = angle - angleLimit;
        }

        Vector3 crossProduct = Vector3.Cross(currentLookDir, directionToCenterOfMass);
        Quaternion  rotation = Quaternion.identity;
        if (crossProduct.y > 0)
        {
            // directionToCenterOfMass is on your right side
            rotation = Quaternion.Euler(0, neckRotationAngle, 0);
        }
        else if (crossProduct.y <= 0)
        {
            // directionToCenterOfMass is on your left side
            rotation = Quaternion.Euler(0, -neckRotationAngle, 0);
        }

        Vector3 rotatedVector = rotation * currentLookDir;

        return rotatedVector.normalized;
    }

    private Vector3 CalculateCenterOfMass(List<GameObject> groupAgents, GameObject myself)
    {
        if (groupAgents == null || groupAgents.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 sumOfPositions = Vector3.zero;
        int count = 0;

        foreach (GameObject go in groupAgents)
        {
            if (go != null && go != myself) 
            {
                sumOfPositions += go.transform.position;
                count++; 
            }
        }

        if (count == 0) 
        {
            return Vector3.zero;
        }

        return sumOfPositions / count;
    }
    #endregion

    //  #region UpdateLookAt
    // private IEnumerator UpdateCurrentLookAt(float updateTime, SocialRelations _socialRelations, GameObject agentGameObject){

    //     List<GameObject> groupAgents = parameterManager.GetAvatarCreatorBase().GetAgentsInCategory(_socialRelations);
    //     SocialRelations mySocialRelations = parameterManager.GetSocialRelations();

    //     if(groupAgents.Count <= 1 || _socialRelations == SocialRelations.Individual){
    //         //For Individual Agent
    //         IfIndividual(true);
    //         while(true){
    //             Vector3 currentDirection = parameterManager.GetCurrentDirection();
    //             SetCurrentDirection(currentDirection);
            
    //             GameObject currentAvoidanceTarget = parameterManager.GetCurrentAvoidanceTarget();
    //             if(currentAvoidanceTarget != null){
    //                 SocialRelations avoidanceTargetSocialRelations = currentAvoidanceTarget.GetComponent<IParameterManager>().GetSocialRelations();
    //                 if(avoidanceTargetSocialRelations != mySocialRelations){
    //                     SetCurrentAvoidanceTarget(currentAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentPosition());
    //                 }else{
    //                     SetCurrentAvoidanceTarget(Vector3.zero);
    //                 }
    //             }else{
    //                 SetCurrentAvoidanceTarget(Vector3.zero);
    //             }

    //             yield return new WaitForSeconds(updateTime);
    //         }
    //     }else{
    //         //For Group Agents
    //         IfIndividual(false);
    //         while(true){
    //             Vector3  currentDirection = parameterManager.GetCurrentDirection();     
    //             SetCurrentDirection(currentDirection);

    //             GameObject currentAvoidanceTarget = parameterManager.GetCurrentAvoidanceTarget();
    //             if(currentAvoidanceTarget != null){
    //                 SocialRelations avoidanceTargetSocialRelations = currentAvoidanceTarget.GetComponent<IParameterManager>().GetSocialRelations();
    //                 if(avoidanceTargetSocialRelations != mySocialRelations){
    //                     SetCurrentAvoidanceTarget(currentAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentPosition());
    //                 }else{
    //                     SetCurrentAvoidanceTarget(Vector3.zero);
    //                 }
    //             }else{
    //                 SetCurrentAvoidanceTarget(Vector3.zero);
    //             }

    //             Vector3     headDirection = GetCurrentLookAt();
    //             if(headDirection!=null){
    //                 Vector3    currentPosition = parameterManager.GetCurrentPosition();
    //                 Vector3 GazeAngleDirection = CalculateGazingDirectionToCOM(groupAgents, currentPosition, headDirection, agentGameObject, fieldOfView);
    //                 SetCurrentCenterOfMass(GazeAngleDirection);
    //             }

    //             yield return new WaitForSeconds(updateTime);
    //         }
    //     }
    // }

    // public void IfIndividual(bool ifIndividual){
    //     motionMatchingSkinnedMeshRendererWithOCEAN.IfIndividual(ifIndividual);
    // }

    // public Vector3 GetCurrentLookAt(){
    //     return motionMatchingSkinnedMeshRendererWithOCEAN.GetCurrentLookAt();
    // }

    // private Vector3 CalculateGazingDirectionToCOM(List<GameObject> groupAgents, Vector3 currentPos, Vector3 currentLookDir, GameObject myself, float angleLimit)
    // {
    //     Vector3            centerOfMass = CalculateCenterOfMass(groupAgents, myself);
    //     Vector3 directionToCenterOfMass = (centerOfMass - currentPos).normalized;    

    //     float             angle = Vector3.Angle(currentLookDir, directionToCenterOfMass);
    //     float neckRotationAngle = 0f;

    //     if (angle > angleLimit)
    //     {
    //         neckRotationAngle = angle - angleLimit;
    //     }

    //     Vector3 crossProduct = Vector3.Cross(currentLookDir, directionToCenterOfMass);
    //     Quaternion  rotation = Quaternion.identity;
    //     if (crossProduct.y > 0)
    //     {
    //         // directionToCenterOfMass is on your right side
    //         rotation = Quaternion.Euler(0, neckRotationAngle, 0);
    //     }
    //     else if (crossProduct.y <= 0)
    //     {
    //         // directionToCenterOfMass is on your left side
    //         rotation = Quaternion.Euler(0, -neckRotationAngle, 0);
    //     }

    //     Vector3 rotatedVector = rotation * currentLookDir;

    //     return rotatedVector.normalized;
    // }

    // private Vector3 CalculateCenterOfMass(List<GameObject> groupAgents, GameObject myself)
    // {
    //     if (groupAgents == null || groupAgents.Count == 0)
    //     {
    //         return Vector3.zero;
    //     }

    //     Vector3 sumOfPositions = Vector3.zero;
    //     int count = 0;

    //     foreach (GameObject go in groupAgents)
    //     {
    //         if (go != null && go != myself) 
    //         {
    //             sumOfPositions += go.transform.position;
    //             count++; 
    //         }
    //     }

    //     if (count == 0) 
    //     {
    //         return Vector3.zero;
    //     }

    //     return sumOfPositions / count;
    // }
    // #endregion

    #region SET
    public void SetCollidedTarget(GameObject collidedTarget){
        motionMatchingSkinnedMeshRendererWithOCEAN.SetCollidedTarget(collidedTarget);
    }
    public void SetCurrentDirection(Vector3 currentDirection){
        motionMatchingSkinnedMeshRendererWithOCEAN.SetCurrentAgentDirection(currentDirection);
    }

    public void SetCurrentCenterOfMass(Vector3 lookAtCenterOfMass){
        motionMatchingSkinnedMeshRendererWithOCEAN.SetCurrentCenterOfMass(lookAtCenterOfMass);
    }

    public void SetCurrentAvoidanceTarget(Vector3 currentAvoidanceTarget){
        motionMatchingSkinnedMeshRendererWithOCEAN.SetCurrentAvoidanceTarget(currentAvoidanceTarget);
    }
    #endregion
}
