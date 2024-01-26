using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;
using Unity.VisualScripting;
using Drawing;
using UnityEditor;

namespace CollisionAvoidance{

public enum UpperBodyAnimationState
{
    // The 'Walking' animation state.
    // Used for activities within a group, e.g., smoking, holding a bug, carrying luggage.
    // Utilizes Motion Matching technology.
    Walk,
    
    // The 'Talking' animation state.
    // Used for communication within a group.
    // Utilizes Unity's animation system.
    Talk,
    
    // The 'Using Smartphone' animation state.
    // Used for individual activities, e.g., listening to music, texting, making calls.
    // Utilizes Unity's animation system.
    SmartPhone
}

/// <summary>
/// Manages the social behavior and animation states for a character.
/// </summary>
public class SocialBehaviour : MonoBehaviour
{
    private const float LookAtUpdateTime = 1.5f;
    private const float AnimationStateUpdateMinTime = 10.0f;
    private const float AnimationStateUpdateMaxTime = 20.0f;
    [Range(0,1)]
    private float WalkAnimationProbability = 0.5f;
    private const float FieldOfView = 45f;
    
    [Header("Conversation")]
    public AudioSource audioSource;
    public AudioClip[] audioClips;

    [Header("Animation")]
    private CollisionAvoidance.MotionMatchingSkinnedMeshRenderer motionMatchingRenderer;
    private GazeController gazeController;
    private AvatarMaskData initialAvatarMask;

    [Header("LookAt")]
    private ParameterManager parameterManager;

    [Header("AnimationState")]
    [ReadOnly]
    public UpperBodyAnimationState currentAnimationState = UpperBodyAnimationState.Walk;
    public GameObject smartPhone;
    private Animator animator;
    private bool onSmartPhone = true;

    //For experiment
    public bool onAnimationShift = true;

    private void Awake()
    {
        parameterManager             = GetComponent<ParameterManager>();
        animator                     = GetComponent<Animator>();
        motionMatchingRenderer       = GetComponent<CollisionAvoidance.MotionMatchingSkinnedMeshRenderer>();
        gazeController               = GetComponent<GazeController>();

        if (motionMatchingRenderer != null)
        {
            initialAvatarMask = motionMatchingRenderer.AvatarMask;
        }

        if(smartPhone != null){
            SetSmartPhoneActiveBasedOnSocialRelations(smartPhone);
        }

        parameterManager.GetPathController().OnMutualGaze += OnMutualGaze;

        FollowMotionMatching();
    }

    private void Start()
    {
        StartCoroutine(UpdateCurrentLookAt(LookAtUpdateTime));
        if(onAnimationShift){
            StartCoroutine(UpdateAnimationState());
        }
    }

    #region Animation State Control
    /// <summary>
    /// Continuously updates the current animation state based on social relations and group members.
    /// </summary>
    private IEnumerator UpdateAnimationState()
    {
        while (true)
        {
            List<GameObject> groupAgents = parameterManager.GetAvatarCreatorBase().GetAgentsInCategory(parameterManager.GetSocialRelations());
            //Determine Random Animation State based on social relations
            currentAnimationState = DetermineAnimationState(groupAgents);

            bool isCurrentlyTalking = currentAnimationState == UpperBodyAnimationState.Talk;
            if(isCurrentlyTalking){
                bool areAgentsClose = AreAgentsAndSelfCloseToAveragePos(groupAgents, gameObject);
                currentAnimationState = areAgentsClose ? UpperBodyAnimationState.Talk : UpperBodyAnimationState.Walk;
            }

            TriggerUnityAnimation(currentAnimationState);

            if (currentAnimationState == UpperBodyAnimationState.Walk)
            {
                FollowMotionMatching();
            }

            yield return new WaitForSeconds(UnityEngine.Random.Range(AnimationStateUpdateMinTime, AnimationStateUpdateMaxTime));
        }
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        var style = new GUIStyle()
        {
            fontSize = 20,
            normal = new GUIStyleState() { textColor = Color.black, background = Texture2D.whiteTexture }
        };
        Handles.Label(transform.position + Vector3.up * 2.3f, currentAnimationState.ToString(), style);

    }
    #endif

    private UpperBodyAnimationState DetermineAnimationState(List<GameObject> groupAgents)
    {
        bool isIndividual = parameterManager.GetSocialRelations() == SocialRelations.Individual || groupAgents.Count <= 1;
        return UnityEngine.Random.value < WalkAnimationProbability ? UpperBodyAnimationState.Walk : (isIndividual ? UpperBodyAnimationState.SmartPhone : UpperBodyAnimationState.Talk);
    }

    public void FollowMotionMatching()
    {
        TriggerUnityAnimation(UpperBodyAnimationState.Walk);
        motionMatchingRenderer.AvatarMask = null;
    }

    public void TriggerUnityAnimation(UpperBodyAnimationState animationState)
    {
        if(onAnimationShift){
            //Update current animation state
            currentAnimationState = animationState;
            motionMatchingRenderer.AvatarMask = initialAvatarMask;

            foreach (UpperBodyAnimationState state in Enum.GetValues(typeof(UpperBodyAnimationState)))
            {
                animator.SetBool(state.ToString(), state == animationState);
            }
            if(animationState == UpperBodyAnimationState.SmartPhone || animationState == UpperBodyAnimationState.Talk){
                TryPlayAudio(1.0f);
            }
        }
    }

    private void SetSmartPhoneActiveBasedOnSocialRelations(GameObject smartPhone)
    {
        List<GameObject> groupAgents = parameterManager.GetAvatarCreatorBase().GetAgentsInCategory(parameterManager.GetSocialRelations());
        bool isIndividual = parameterManager.GetSocialRelations() == SocialRelations.Individual;
        
        if(isIndividual || groupAgents.Count <= 1){
            smartPhone.SetActive(true);
            onSmartPhone = true;
        }
        else{
            smartPhone.SetActive(false);
            onSmartPhone = false;
        }
    }

    public UpperBodyAnimationState GetUpperBodyAnimationState(){
        return currentAnimationState;
    }

    /// <summary>
    /// Determines if the calling object (self) and a group of agents are all sufficiently close to their common average pos.
    /// The distance threshold is set to half the number of agents in the group. If all agents and the self are within this
    /// threshold from the average pos, the function returns true; otherwise, it returns false.
    /// </summary>
    /// <param name="groupAgents">List of agent GameObjects to be checked.</param>
    /// <returns>True if all agents and self are close to the average pos, false otherwise.</returns>
    private bool AreAgentsAndSelfCloseToAveragePos(List<GameObject> groupAgents, GameObject myself)
    {
        // Calculate the center of mass, including the calling object itself
        Vector3 averagePos = CalculateAveragePosition(groupAgents);
        
        // Set the distance threshold to half the number of agents
        float thresholdDistance = groupAgents.Count / 2f;

        // Check if the calling object (self) is within the threshold distance from the center of mass
        if (Vector3.Distance(myself.transform.position, averagePos) > thresholdDistance)
        {
            return false;
        }

        // Check if at least one agent in the group (excluding myself) is within the threshold distance from the average position
        bool isAnyAgentClose = false;
        foreach (GameObject agent in groupAgents)
        {
            if (agent != myself && Vector3.Distance(agent.transform.position, averagePos) <= thresholdDistance)
            {
                isAnyAgentClose = true;
                break;
            }
        }

        // If at least one agent (excluding myself) is close, and myself is also close, return true
        if (isAnyAgentClose)
        {
            return true;
        }

        // If no agent (excluding myself) is close enough, return false
        return false;

    }

    private Vector3 CalculateAveragePosition(List<GameObject> agents)
    {
        Vector3 combinedPosition = Vector3.zero;
        foreach (GameObject agent in agents)
        {
            combinedPosition += agent.transform.position;
        }
        return combinedPosition / agents.Count;
    }

    #endregion

    #region Collide Response
    public void DeleteCollidedTarget()
    {
        collidedTarget = null;
    }

    public void TryPlayAudio(float PlayAudioProbability)
    {
        if (audioSource != null && audioClips.Length > 0 && UnityEngine.Random.value < PlayAudioProbability)
        {
            audioSource.clip = audioClips[UnityEngine.Random.Range(0, audioClips.Length)];
            audioSource.Play();
        }
    }
    #endregion

    #region Update Look At
    private IEnumerator UpdateCurrentLookAt(float updateTime)
    {
        List<GameObject> groupAgents = parameterManager.GetAvatarCreatorBase().GetAgentsInCategory(parameterManager.GetSocialRelations());
        SocialRelations mySocialRelations = parameterManager.GetSocialRelations();
        bool _isIndividual = groupAgents.Count <= 1 || mySocialRelations == SocialRelations.Individual;

        while (true)
        {
            UpdateDirectionAndAvoidance(mySocialRelations, _isIndividual);

            if (!_isIndividual)
            {
                UpdateGroupAgentLookAt(groupAgents);
            }

            yield return new WaitForSeconds(updateTime);
        }
    }

    //For individual
    private void UpdateDirectionAndAvoidance(SocialRelations mySocialRelations, bool _isIndividual)
    {
        SetCurrentDirection(parameterManager.GetCurrentDirection());
        GameObject _potentialAvoidanceTarget = parameterManager.GetPotentialAvoidanceTarget();

        if (_potentialAvoidanceTarget != null)
        {
            SocialRelations avoidanceTargetSocialRelations = _potentialAvoidanceTarget.GetComponent<IParameterManager>().GetSocialRelations();
            if(_isIndividual == true){
                SetPotentialAvoidanceTarget(_potentialAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentPosition(), _potentialAvoidanceTarget);
            }else{
                SetPotentialAvoidanceTarget(avoidanceTargetSocialRelations != mySocialRelations ? _potentialAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentPosition() : Vector3.zero, _potentialAvoidanceTarget);
            }
        }
        else
        {
            SetPotentialAvoidanceTarget(Vector3.zero);
        }
    }

    // UpdateGroupAgentLookAt: Updates the direction group agents are looking at.
    // This includes making agents look at the talking agent in the group.
    private void UpdateGroupAgentLookAt(List<GameObject> groupAgents)
    {
        // Get the current direction the agent is looking at
        Vector3 headDirection = GetCurrentLookAt();

        // Only proceed if there is a valid head direction
        if (headDirection != Vector3.zero)
        {
            // Get the current position of this agent
            Vector3 currentPosition = parameterManager.GetCurrentPosition();
            // Check if any other agent in the group is talking
            GameObject otherAgent = IsAnyAgentInAnimationState(groupAgents, UpperBodyAnimationState.Talk);

            if (otherAgent != null &&
            Vector3.Distance(currentPosition, otherAgent.GetComponent<ParameterManager>().GetCurrentPosition()) < groupAgents.Count / 2f)
            {
                // If another agent is talking, calculate and set the gaze direction towards them
                Vector3 gazeDirectionToTalkingAgent = (otherAgent.GetComponent<ParameterManager>().GetCurrentPosition() - currentPosition).normalized;
                SetCurrentCenterOfMass(gazeDirectionToTalkingAgent);
            }
            else
            {
                // If no agent is talking, calculate and set the gaze direction based on group's center of mass
                Vector3 gazeAngleDirection = CalculateGazingDirectionToCOM(groupAgents, currentPosition, headDirection, gameObject, FieldOfView);
                SetCurrentCenterOfMass(gazeAngleDirection);
            }
        }
    }


    private GameObject IsAnyAgentInAnimationState(List<GameObject> groupAgents, UpperBodyAnimationState targetUpperBodyState){
        foreach(GameObject agent in groupAgents){
            if(agent != gameObject && agent.GetComponent<SocialBehaviour>().GetUpperBodyAnimationState() == targetUpperBodyState){
                return agent;
            }
        }
        return null;
    }

    public Vector3 GetCurrentLookAt()
    {
        return gazeController.GetCurrentLookAt();
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

    #region GET and SET
    public bool GetOnSmartPhone(){
        return onSmartPhone;
    }

    GameObject collidedTarget;
    Vector3 currentDirection;
    Vector3 lookAtCenterOfMass;
    Vector3 potentialAvoidanceTarget;
    GameObject potentialAvoidanceObject;
    GameObject avoidanceCoordinateTarget;

    public void SetCollidedTarget(GameObject _collidedTarget){
        collidedTarget = _collidedTarget;
    }
    private void SetCurrentDirection(Vector3 _currentDirection){
        currentDirection = _currentDirection;
    }

    private void SetCurrentCenterOfMass(Vector3 _lookAtCenterOfMass){
        lookAtCenterOfMass = _lookAtCenterOfMass;
    }

    private void SetPotentialAvoidanceTarget(Vector3 _potentialAvoidanceTarget, GameObject avoidanceTargetObject = null){
        if (_potentialAvoidanceTarget != Vector3.zero){

            float distance = Vector3.Distance(transform.position, _potentialAvoidanceTarget);
            //TODO: this maxdistance should be considered with unalinged collision avoidance area
            // Define the maximum distance (threshold)
            float maxDistance = 7.0f; // Adjust this value based on game requirements
            // Calculate the probability based on distance (linearly decreasing)
            float probability = distance / maxDistance;

            // Random.value returns a random number between 0 and 1
            if (UnityEngine.Random.value < probability){
                // Use _potentialAvoidanceTarget based on probability
                potentialAvoidanceTarget = _potentialAvoidanceTarget;
                potentialAvoidanceObject = avoidanceTargetObject;
                //this is for adjusting duration of looking at potential avoidance target
                StartCoroutine(CheckMutualGaze(LookAtUpdateTime-0.1f, avoidanceTargetObject));
            } else {
                // Otherwise, set to Vector3.zero
                potentialAvoidanceTarget = Vector3.zero;
                potentialAvoidanceObject = null;
            }
        } else {
            potentialAvoidanceTarget = Vector3.zero;
            potentialAvoidanceObject = null;
        }
    }

    //TODO: potential avoidance target > getcurrentlookat→ 180° → potential avoidance target = Vector3.zero
    //implement mutual gaze
    private IEnumerator CheckMutualGaze(float duration, GameObject avoidanceTargetObject){
        if(duration > LookAtUpdateTime){
            duration = LookAtUpdateTime;
        }

        float elapsedTime = 0f;

        SocialBehaviour targetSocialBehaviour = avoidanceTargetObject.GetComponent<SocialBehaviour>();
        IParameterManager targetParameterManager = avoidanceTargetObject.GetComponent<IParameterManager>();

        while (duration > elapsedTime)
        {
            elapsedTime += Time.deltaTime;

            if (targetSocialBehaviour != null && targetParameterManager != null)
            {
                Vector3 targetLookAt = targetSocialBehaviour.GetCurrentLookAt();
                Vector3 myLookAt     = GetCurrentLookAt();

                Vector3 targetPosition = targetParameterManager.GetCurrentPosition();
                Vector3 myPosition     = parameterManager.GetCurrentPosition(); 

                Vector3 myPositionToTarget = (targetPosition - myPosition).normalized;

                // Normalize vectors (if they are not already normalized in their methods)
                targetLookAt.Normalize();
                myLookAt.Normalize();

                // Calculate the dot products
                float dotProductLookAt = Vector3.Dot(targetLookAt, myLookAt);
                float dotProductPosition = Vector3.Dot(myPositionToTarget, myLookAt);

                if (dotProductLookAt < -0.99 && dotProductPosition > 0.99)
                {
                    // This indicates mutual gaze
                    potentialAvoidanceTarget = Vector3.zero;
                    Debug.Log("Detect Mutual Gaze");
                }
            }

            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    private bool isMutualGazeRunning = false;

    // Handle the mutual gaze event
    private void OnMutualGaze(GameObject currentAvoidanceTarget){
        // Skip the process if it's already running
        if (isMutualGazeRunning) return;

        // Set the flag indicating the process is running
        isMutualGazeRunning = true;

        avoidanceCoordinateTarget = currentAvoidanceTarget;

        // Call ResetAvoidanceCoordinateTarget method after 1 second
        Invoke("ResetAvoidanceCoordinateTarget", 2f);
    }

    // Reset the avoidance coordinate target
    private void ResetAvoidanceCoordinateTarget(){
        // Set avoidanceCoordinateTarget to null
        avoidanceCoordinateTarget = null;

        // Reset the flag indicating the process is running
        isMutualGazeRunning = false;
    }

    public GameObject GetAvoidanceCoordinationTarget(){
        return avoidanceCoordinateTarget;
    }

    public GameObject GetCollidedTarget(){
        return collidedTarget;
    }
    public Vector3 GetCurrentDirection(){
        return currentDirection;
    }

    public Vector3 GetCurrentCenterOfMass(){
        return lookAtCenterOfMass;
    }

    public Vector3 GetPotentialAvoidanceTarget(){
        return potentialAvoidanceTarget;
    }


    #endregion
}
}