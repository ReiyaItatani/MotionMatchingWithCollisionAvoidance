using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Drawing;

using MotionMatching;
using TrajectoryFeature = MotionMatching.MotionMatchingData.TrajectoryFeature;
using UnityEngine.Events;

namespace CollisionAvoidance{

public class PathController : MotionMatchingCharacterController
{
    public string TrajectoryPositionFeatureName = "FuturePosition";
    public string TrajectoryDirectionFeatureName = "FutureDirection";
    public Vector3[] Path;
    //Warning:current position is not the current position of the agent itself when the parent transform is not (0.0f, 0.0f, 0.0f);
    //To get current position of the agent you have to use GetCurrentPosition()
    protected Vector3 CurrentPosition;
    protected Vector3 CurrentDirection;
    protected Vector3[] PredictedPositions;
    protected Vector3[] PredictedDirections;
    [HideInInspector, Range(0.0f, 2.0f), Tooltip("Max distance between SimulationBone and SimulationObject")] 
    public float MaxDistanceMMAndCharacterController = 0.1f; // Max distance between SimulationBone and SimulationObject
    [HideInInspector, Range(0.0f, 2.0f), Tooltip("Time needed to move half of the distance between SimulationBone and SimulationObject")] 
    public float PositionAdjustmentHalflife = 0.1f; // Time needed to move half of the distance between SimulationBone and SimulationObject
    [HideInInspector, Range(0.0f, 2.0f), Tooltip("Ratio between the adjustment and the character's velocity to clamp the adjustment")] 
    public float PosMaximumAdjustmentRatio = 0.1f; // Ratio between the adjustment and the character's velocity to clamp the adjustment
    // Speed Of Agents -----------------------------------------------------------------
    [Header("Speed")]
    public float currentSpeed = 1.0f; //Current speed of the agent
    [Range (0.0f, 1.0f)]
    public float initialSpeed = 0.7f; //Initial speed of the agent
    public float minSpeed = 0.0f; //Minimum speed of the agent
    public float maxSpeed = 1.0f; //Maximum speed of the agent
    private UnityAction OnGoalReached; //the event when the agent reaches the goal
    public bool onInSlowingArea = false; //the event when the agent is in the slowing radius
    // --------------------------------------------------------------------------
    // To Mange Agents -----------------------------------------------------------------
    public AvatarCreatorBase avatarCreator; //Manager for all of the agents
    // --------------------------------------------------------------------------
    // Features -----------------------------------------------------------------
    [Header("Features For Motion Matching")]
    protected int TrajectoryPosFeatureIndex;
    protected int TrajectoryRotFeatureIndex;
    protected int[] TrajectoryPosPredictionFrames;
    protected int[] TrajectoryRotPredictionFrames;
    protected int NumberPredictionPos { get { return TrajectoryPosPredictionFrames.Length; } }
    protected int NumberPredictionRot { get { return TrajectoryRotPredictionFrames.Length; } }
    // --------------------------------------------------------------------------
    // Collision Avoidance Force ------------------------------------------------
    [Header("Parameters For Basic Collision Avoidance"), HideInInspector]
    protected Vector3 avoidanceVector = Vector3.zero;//Direction of basic collision avoidance
    [HideInInspector]
    public float avoidanceWeight = 2.0f;//Weight for basic collision avoidance
    [ReadOnly]
    public GameObject currentAvoidanceTarget;
    // --------------------------------------------------------------------------
    // Collision Response -------------------------------------------------------
    public event EventDelegate OnMutualGaze;
    public delegate void EventDelegate(GameObject targetAgent);
    // --------------------------------------------------------------------------
    // To Goal Direction --------------------------------------------------------
    [Header("Parameters For Goal Direction")]
    protected Vector3 currentGoal;
    protected Vector3 toGoalVector = Vector3.zero;//Direction to goal
    [HideInInspector]
    public float toGoalWeight = 2.0f;//Weight for goal direction
    [ReadOnly]
    public int currentGoalIndex = 1;//Current goal index num
    public int CurrentGoalIndex 
    { 
        get { return currentGoalIndex; } 
        set { currentGoalIndex = value; } 
    }
    public float goalRadius = 0.5f;
    public float slowingRadius = 2.0f;
    // --------------------------------------------------------------------------
    // Anticipated Collision Avoidance -------------------------------------------
    [Header("Parameters For Anticipated Collision Avoidance"), HideInInspector]
    protected Vector3 avoidNeighborsVector = Vector3.zero;//Direction for Anticipated collision avoidance
    [ReadOnly]
    public GameObject potentialAvoidanceTarget;
    [HideInInspector]
    public float avoidNeighborWeight = 2.0f;//Weight for Anticipated collision avoidance
    protected float minTimeToCollision =5.0f;
    protected float collisionDangerThreshold = 4.0f;
    // --------------------------------------------------------------------------
    // When Collide each other -----------------------------------------------------
    [Header("Social Behaviour, Non-verbal Communication")]
    protected bool onCollide = false;
    protected bool onMoving = false;
    protected GameObject collidedAgent;
    // --------------------------------------------------------------------------
    // Gizmo Parameters -------------------------------------------------------------
    [Header("Controll Gizmos")]
    [HideInInspector]
    public bool showAvoidanceForce = false;
    [HideInInspector]
    public bool showAnticipatedCollisionAvoidance = false;
    [HideInInspector]
    public bool showGoalDirection = false;
    [HideInInspector]
    public bool showCurrentDirection = false;
    [HideInInspector]
    public bool showGroupForce = false;
    [HideInInspector]
    public bool showWallForce = false;
    [HideInInspector]
    public bool showSyntheticVisionForce = false;
    // --------------------------------------------------------------------------
    // Force From Group --------------------------------------------------------
    [Header("Group Force, Group Category")]
    protected Vector3 groupForce = Vector3.zero;
    [ReadOnly]
    public SocialRelations socialRelations;
    [HideInInspector]
    public float groupForceWeight = 0.5f;
    // --------------------------------------------------------------------------
    // Collision Avoidance Controller --------------------------------------------
    public CollisionAvoidanceController collisionAvoidance;
    // --------------------------------------------------------------------------
    // Repulsion Force From Wall ------------------------------------------------
    protected Vector3 wallRepForce;
    [HideInInspector]
    public float wallRepForceWeight = 0.2f;
    // --------------------------------------------------------------------------
    // Group Collider Manager For Group Behaviour--------------------------------
    public GroupColliderManager groupColliderManager;   
    // --------------------------------------------------------------------------
    // For experiment -----------------------------------------------------------
    public bool onAvoidanceCoordination = true;
    

    protected virtual void Start()
    {
        //Init
        currentSpeed = initialSpeed;
        if (initialSpeed < minSpeed)
        {
            initialSpeed = minSpeed;
            // minSpeed = initialSpeed;
        }
        int currentPositionIndex = currentGoalIndex - 1;
        CurrentPosition = Path[currentPositionIndex];
        currentGoal = Path[currentGoalIndex];        

        //Init
        AgentCollisionDetection agentCollisionDetection = collisionAvoidance.GetAgentCollisionDetection();
        agentCollisionDetection.OnEnterTrigger += HandleAgentCollision;     

        // Get the feature indices
        TrajectoryPosFeatureIndex = -1;
        TrajectoryRotFeatureIndex = -1;
        for (int i = 0; i < MotionMatching.MMData.TrajectoryFeatures.Count; ++i)
        {
            if (MotionMatching.MMData.TrajectoryFeatures[i].Name == TrajectoryPositionFeatureName) TrajectoryPosFeatureIndex = i;
            if (MotionMatching.MMData.TrajectoryFeatures[i].Name == TrajectoryDirectionFeatureName) TrajectoryRotFeatureIndex = i;
        }
        Debug.Assert(TrajectoryPosFeatureIndex != -1, "Trajectory Position Feature not found");
        Debug.Assert(TrajectoryRotFeatureIndex != -1, "Trajectory Direction Feature not found");

        TrajectoryPosPredictionFrames = MotionMatching.MMData.TrajectoryFeatures[TrajectoryPosFeatureIndex].FramesPrediction;
        TrajectoryRotPredictionFrames = MotionMatching.MMData.TrajectoryFeatures[TrajectoryRotFeatureIndex].FramesPrediction;
        // TODO: generalize this, allow for different number of prediction frames
        Debug.Assert(TrajectoryPosPredictionFrames.Length == TrajectoryRotPredictionFrames.Length, "Trajectory Position and Trajectory Direction Prediction Frames must be the same for PathCharacterController");
        for (int i = 0; i < TrajectoryPosPredictionFrames.Length; ++i)
        {
            Debug.Assert(TrajectoryPosPredictionFrames[i] == TrajectoryRotPredictionFrames[i], "Trajectory Position and Trajectory Direction Prediction Frames must be the same for PathCharacterController");
        }

        PredictedPositions  = new Vector3[NumberPredictionPos];
        PredictedDirections = new Vector3[NumberPredictionRot];
        
        UpdateCollisionAvoidance();
    }

    protected virtual void UpdateCollisionAvoidance(){
        //Update the forces
        StartCoroutine(UpdateToGoalVector(0.1f));
        StartCoroutine(UpdateAvoidanceVector(0.1f, 0.3f));
        StartCoroutine(UpdateAvoidNeighborsVector(0.1f, 0.3f));
        StartCoroutine(UpdateGroupForce(0.1f, GetSocialRelations()));
        StartCoroutine(UpdateWallForce(0.2f, 0.5f));
        //StartCoroutine(UpdateAngularVelocityControl(0.2f));   

        //Update the speed of the agent based on the distance to the goal
        StartCoroutine(UpdateSpeed(avatarCreator.GetAgentsInCategory(GetSocialRelations()), collisionAvoidance.GetAgentGameObject()));
        StartCoroutine(UpdateSpeedBasedOnGoalDist(0.1f));

        StartCoroutine(CheckForGoalReached(0.1f));
            
        //If you wanna consider all of the other agents for anticipated collision avoidance use below
        //StartCoroutine(UpdateAvoidNeighborsVector(avatarCreator.GetAgents(), 0.1f, 0.3f));
    }

    #region UPDATE SIMULATION
    protected override void OnUpdate(){
        // Predict the future positions and directions
        for (int i = 0; i < NumberPredictionPos; i++)
        {
            SimulatePath(DatabaseDeltaTime * TrajectoryPosPredictionFrames[i], CurrentPosition, out PredictedPositions[i], out PredictedDirections[i]);
        }
        
        UpdateGoalTarget(CurrentPosition, currentGoal);

        //Update Current Position and Direction
        SimulatePath(Time.deltaTime, CurrentPosition, out CurrentPosition, out CurrentDirection);

        //Prevent agents from intersection
        AdjustCharacterPosition();
        ClampSimulationBone();

        //Draw Gizmos
        DrawInfo();
    }

    protected virtual void SimulatePath(float time, Vector3 _currentPosition, out Vector3 nextPosition, out Vector3 direction)
    {
        //Gradually decrease speed
        // float distanceToGoal = Vector3.Distance(_currentPosition, currentGoal);
        // currentSpeed = distanceToGoal < slowingRadius ? Mathf.Lerp(minSpeed, currentSpeed, distanceToGoal / slowingRadius) : currentSpeed;

        //Move Agent
        direction = (      toGoalWeight    *            toGoalVector + 
                        avoidanceWeight    *         avoidanceVector + 
                    avoidNeighborWeight    *    avoidNeighborsVector + 
                    groupForceWeight       *              groupForce +
                    wallRepForceWeight     *            wallRepForce +
            syntheticVisionForceWeight     *    syntheticVisionForce
                    ).normalized;
        direction = new Vector3(direction.x, 0f, direction.z);

        //Check collision
        if(onCollide){
            Vector3    myDir = GetCurrentDirection();
            Vector3    myPos = GetCurrentPosition();
            Vector3 otherDir = collidedAgent.GetComponent<IParameterManager>().GetCurrentDirection();
            Vector3 otherPos = collidedAgent.GetComponent<IParameterManager>().GetCurrentPosition();
            Vector3   offset = otherPos - myPos;

            offset = new Vector3(offset.x, 0f, offset.z);
            float dotProduct = Vector3.Dot(myDir, otherDir);
            float angle = 0.1f;

            if(onMoving){
                if (dotProduct <= -angle){
                    //anti-parallel
                    bool isParallel = false;
                    direction = CheckOppoentDir(myDir, myPos, otherDir, otherPos, out isParallel);
                    nextPosition = _currentPosition + direction * 0.1f * time;
                }else{
                    //parallel
                    if(Vector3.Dot(offset, GetCurrentDirection()) > 0){
                        //If the other agent is in front of you
                        nextPosition = _currentPosition;
                    }else{
                        //If you are in front of the other agent
                        nextPosition = _currentPosition + direction * currentSpeed * time;
                    }
                }
            }else{
                //Take a step back
                float speedOfStepBack = 0.3f;
                nextPosition = _currentPosition - offset * speedOfStepBack * time;
            }
        }else{
            nextPosition = _currentPosition + direction * currentSpeed * time;
        }
    }

    //when the agent collide with the agent in front of it, it will take a step back
    protected virtual void HandleAgentCollision(Collider other){
        //Check the social realtionship between the collided agent and the agent
        SocialRelations  mySocialRelations          = GetSocialRelations();
        IParameterManager otherAgentParameterManager = other.GetComponent<IParameterManager>();
        SocialRelations  otherAgentSocialRelations  = otherAgentParameterManager.GetSocialRelations();

        if(onCollide == false){
            collidedAgent = other.gameObject;
            if(mySocialRelations != SocialRelations.Individual && mySocialRelations == otherAgentSocialRelations){
                //If the collided agent is in the same group
                float distance = Vector3.Distance(GetCurrentPosition(), otherAgentParameterManager.GetCurrentPosition());
                if(distance < 0.4f){
                    //If the collided agent is too close
                    StartCoroutine(ReactionToCollision(0.5f, 0.0f));
                }
            }else{
                //If the collided agent is not in the same group
                StartCoroutine(ReactionToCollision(1.0f, 1.0f));
            }
        }
    }

    protected virtual IEnumerator ReactionToCollision(float stepBackDuration, float goDifferentDuration)
    {
        onCollide = true;
        yield return new WaitForSeconds(stepBackDuration);
        onMoving = true;
        yield return new WaitForSeconds(goDifferentDuration);
        onCollide = false;
        onMoving = false;
    }

    protected virtual Vector3 CheckOppoentDir(Vector3 myDirection, Vector3 myPosition, Vector3 otherDirection, Vector3 otherPosition, out bool isParallel){
        Vector3 offset = (otherPosition - myPosition).normalized;
        Vector3 right= Vector3.Cross(Vector3.up, offset).normalized;
        if(Vector3.Dot(right, myDirection)>0 && Vector3.Dot(right, otherDirection)>0 || Vector3.Dot(right, myDirection)<0 && Vector3.Dot(right, otherDirection)<0){
            //Potential to collide
            isParallel = true;
            return GetReflectionVector(myDirection, offset);
        }
        isParallel = false;
        return myDirection;
    }

    protected virtual Vector3 GetReflectionVector(Vector3 targetVector, Vector3 baseVector)
    {
        targetVector = targetVector.normalized;
        baseVector = baseVector.normalized;
        float cosTheta = Vector3.Dot(targetVector, baseVector); // p・x = cos θ
        Vector3 q = 2 * cosTheta * baseVector - targetVector;   // q = 2cos θ・x - p
        return q;
    }
    
    protected virtual void AdjustCharacterPosition()
    {
        float3 characterController = GetCurrentPosition();
        float3 motionMatching = MotionMatching.transform.position;
        float3 differencePosition = characterController - motionMatching;
        // Damp the difference using the adjustment halflife and dt
        float3 adjustmentPosition = Spring.DampAdjustmentImplicit(differencePosition, PositionAdjustmentHalflife, Time.deltaTime);
        // Clamp adjustment if the length is greater than the character velocity
        // multiplied by the ratio
        float maxLength = PosMaximumAdjustmentRatio * math.length(MotionMatching.Velocity) * Time.deltaTime;
        if (math.length(adjustmentPosition) > maxLength)
        {
            adjustmentPosition = maxLength * math.normalize(adjustmentPosition);
        }
        // Move the simulation bone towards the simulation object
        MotionMatching.SetPosAdjustment(adjustmentPosition);
    }

    protected virtual void ClampSimulationBone()
    {
        // Clamp Position
        float3 characterController = GetCurrentPosition();
        float3 motionMatching = MotionMatching.transform.position;
        if (math.distance(characterController, motionMatching) > MaxDistanceMMAndCharacterController)
        {
            float3 newMotionMatchingPos = MaxDistanceMMAndCharacterController * math.normalize(motionMatching - characterController) + characterController;
            MotionMatching.SetPosAdjustment(newMotionMatchingPos - motionMatching);
        }
    }
    #endregion

    /**********************************************************************************************
    * Goal Direction Update:
    * This section of the code is responsible for recalculating and adjusting the target direction.
    * It ensures that the object is always oriented or moving towards its intended goal or target.
    ***********************************************************************************************/
    #region TO GOAL FORCE
    protected virtual IEnumerator UpdateToGoalVector(float updateTime){
        while(true){
            toGoalVector = (GetCurrentGoal() - (Vector3)GetCurrentPosition()).normalized;
            yield return new WaitForSeconds(updateTime);
        }
    }

    protected virtual void UpdateGoalTarget(Vector3 _currentPosition, Vector3 _currentGoal)
    {
        float distanceToGoal = Vector3.Distance(_currentPosition, _currentGoal);
        if(distanceToGoal < goalRadius) {
            SelectRandomGoal();
        }
    }

    protected bool isIncreasing = true;

    protected virtual void SelectRandomGoal(){

        if(isIncreasing)
        {
            //Path[index]→Path[index+1]
            currentGoalIndex++;
            if(currentGoalIndex >= Path.Length - 1) isIncreasing = false;
        }
        else
        {
            //Path[index]→Path[index-1]
            currentGoalIndex--;
            if(currentGoalIndex <= 0) isIncreasing = true;
        }

        //Adjustment
        if(currentGoalIndex > Path.Length-1) currentGoalIndex = Path.Length-2;
        if(currentGoalIndex < 0) currentGoalIndex = -currentGoalIndex;

        currentGoal = Path[currentGoalIndex];
        OnGoalReached?.Invoke();
    }
    #endregion

    /***********************************************************************************************
    * Collision Avoidance Logic[Nuria HiDAC]:
    * This section of the code ensures that objects do not overlap or intersect with each other.
    * It provides basic mechanisms to detect potential collisions and take preventive actions.
    ***********************************************************************************************/
    #region BASIC COLLISION AVOIDANCE FORCE
    protected virtual IEnumerator UpdateAvoidanceVector(float updateTime, float transitionTime)
    {
        float elapsedTime = 0.0f;
        while(true){
            //To use Box Collider
            //List<GameObject> othersInAvoidanceArea = collisionAvoidance.GetOthersInAvoidanceArea();
            //To use FOV
            List<GameObject> othersInAvoidanceArea = collisionAvoidance.GetOthersInFOV();
            List<GameObject> othersOnPath          = collisionAvoidance.GetOthersInAvoidanceArea();
            Vector3 myPositionAtNearestApproach    = Vector3.zero;
            Vector3 otherPositionAtNearestApproach = Vector3.zero;

            //Update CurrentAvoidance Target
            if(othersInAvoidanceArea != null){
                currentAvoidanceTarget = DecideUrgentAvoidanceTarget(GetCurrentDirection(), GetCurrentPosition(), GetCurrentSpeed(), 
                                                                     othersInAvoidanceArea, 
                                                                     minTimeToCollision, collisionDangerThreshold, 
                                                                     out myPositionAtNearestApproach, out otherPositionAtNearestApproach);  
                //Check if the CurrentAvoidance Target is on the path way
                if(!othersOnPath.Contains(currentAvoidanceTarget)){
                    //if the CurrentAvoidance Target is not on the path way
                    currentAvoidanceTarget = null;
                }            
            }

            //Calculate Avoidance Force
            if (currentAvoidanceTarget != null)
            {
                Vector3 currentPosition          = GetCurrentPosition();
                Vector3 currentDirection         = GetCurrentDirection();
                Vector3 avoidanceTargetPosition  = currentAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentPosition();
                Vector3 avoidanceTargetAvoidanceVector = currentAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentAvoidanceVector();

                avoidanceVector = ComputeAvoidanceVector(currentAvoidanceTarget, currentDirection, currentPosition);

                //Check opponent dir
                if(avoidanceTargetAvoidanceVector != Vector3.zero && Vector3.Dot(currentDirection, avoidanceVector) < 0.5 && onAvoidanceCoordination){
                    bool isParallel = false;
                    avoidanceVector = CheckOppoentDir(avoidanceVector, currentPosition, avoidanceTargetAvoidanceVector ,avoidanceTargetPosition, 
                                                      out isParallel);
                    if(isParallel){
                        OnMutualGaze?.Invoke(currentAvoidanceTarget);
                    }
                }

                //gradually increase the avoidance force considering the distance 
                Vector3 colliderSize = collisionAvoidance.GetAvoidanceColliderSize();
                float   agentRadius  = collisionAvoidance.GetAgentCollider().radius;
                avoidanceVector = avoidanceVector*(1.0f-Vector3.Distance(avoidanceTargetPosition, 
                                                                         currentPosition)/(Mathf.Sqrt(colliderSize.x/2*colliderSize.x/2+colliderSize.z*colliderSize.z)+agentRadius*2));

                //Group or Individual
                avoidanceVector *= TagChecker(currentAvoidanceTarget);
                
                elapsedTime = 0.0f;
            }
            else
            {
                elapsedTime += Time.deltaTime;
                if(transitionTime > elapsedTime){
                    avoidanceVector = Vector3.Lerp(avoidanceVector, Vector3.zero, elapsedTime/transitionTime);
                    yield return new WaitForSeconds(Time.deltaTime);
                }else{
                    avoidanceVector = Vector3.zero;
                    elapsedTime = 0.0f;
                }
            }
            yield return new WaitForSeconds(updateTime);
        }
    }

    protected virtual Vector3 ComputeAvoidanceVector(GameObject avoidanceTarget, Vector3 _currentDirection, Vector3 _currentPosition)
    {
        Vector3 directionToAvoidanceTarget = (avoidanceTarget.transform.position - _currentPosition).normalized;
        Vector3 upVector;

        //0.9748 → approximately 13.4 degrees.
        if (Vector3.Dot(directionToAvoidanceTarget, _currentDirection) >= 0.9748f)
        {
            upVector = Vector3.up;
        }
        else
        {
            upVector = Vector3.Cross(directionToAvoidanceTarget, _currentDirection);
        }
        return Vector3.Cross(upVector, directionToAvoidanceTarget).normalized;
    }

    protected virtual float TagChecker(GameObject Target){
        if(Target.CompareTag("Group")){
            Target.GetComponent<CapsuleCollider>();
            float radius = Target.GetComponent<CapsuleCollider>().radius;
            return radius + 2f;
        }else if(Target.CompareTag("Agent")){
            return 1f;
        }
        return 1f;
    }

    protected virtual GameObject DecideUrgentAvoidanceTarget(Vector3 myDirection, Vector3 myPosition, float mySpeed, List<GameObject> others, float minTimeToCollision, float collisionDangerThreshold, out Vector3 myPositionAtNearestApproach, out Vector3 otherPositionAtNearestApproach){
        GameObject _currentAvoidanceTarget = null;
        myPositionAtNearestApproach = Vector3.zero;
        otherPositionAtNearestApproach = Vector3.zero;
        foreach(GameObject other in others){
            //Skip self
            if(other == collisionAvoidance.GetAgentGameObject()){
                continue;
            }
            IParameterManager otherParameterManager = other.GetComponent<IParameterManager>();
            Vector3 otherDirection = otherParameterManager.GetCurrentDirection();
            Vector3 otherPosition  = otherParameterManager.GetCurrentPosition();
            float   otherSpeed     = otherParameterManager.GetCurrentSpeed();

            // predicted time until nearest approach of "me" and "other"
            float time = PredictNearestApproachTime (myDirection, myPosition, mySpeed, 
                                                     otherDirection, otherPosition, otherSpeed);
            //Debug.Log("time:"+time);
            if ((time >= 0) && (time < minTimeToCollision)){
                //Debug.Log("Distance:"+computeNearestApproachPositions (time, CurrentPosition, CurrentDirection, CurrentSpeed, otherParameterManager.GetRawCurrentPosition(), otherParameterManager.GetCurrentDirection(), otherParameterManager.GetCurrentSpeed()));
                if (ComputeNearestApproachPositions (time, 
                                                     myPosition, myDirection, mySpeed, 
                                                     otherPosition, otherDirection, otherSpeed, 
                                                     out myPositionAtNearestApproach, out otherPositionAtNearestApproach) 
                                                     < collisionDangerThreshold)
                {
                    minTimeToCollision = time;
                    _currentAvoidanceTarget = other;
                }
            }
        }
        return _currentAvoidanceTarget;
    }

    #endregion

    /***********************************************************************************************************
    * Anticipated Collision Avoidance[Reynolds 1987]:
    * This section of the code handles scenarios where objects might collide in the future(prediction).
    ************************************************************************************************************/
    #region ANTICIPATED COLLISION AVOIDANCE
    protected virtual IEnumerator UpdateAvoidNeighborsVector(float updateTime, float transitionTime){
        while(true){
            if(currentAvoidanceTarget != null){
                avoidNeighborsVector = Vector3.zero;
            }else{

                //Get Agents//
                List<GameObject> Agents = new List<GameObject>();
                if(groupColliderManager!=null && groupColliderManager.GetOnGroupCollider()){
                    //if the agent is in a group and in certain distance, shared FOV happens
                    Agents = groupColliderManager.GetAgentsInSharedFOV();
                }else{
                    //if the agent is "not" in a group
                    Agents = collisionAvoidance.GetOthersInFOV();
                }
                if(Agents == null) yield return null;

                //Calculate Anticipated Collision Avoidance Force//
                Vector3 newAvoidNeighborsVector;
                if(groupColliderManager!=null && groupColliderManager.GetOnGroupCollider()){
                    //if the agent is in a group and in certain distance
                    Vector3 groupCurrentDirection = groupColliderManager.GetGroupParameterManager().GetCurrentDirection();
                    Vector3 groupCurrentPosition  = groupColliderManager.GetGroupParameterManager().GetCurrentPosition();
                    float   groupCurrentSpeed     = groupColliderManager.GetGroupParameterManager().GetCurrentSpeed();

                    newAvoidNeighborsVector       = SteerToAvoidNeighbors(groupCurrentDirection, groupCurrentPosition, groupCurrentSpeed,
                                                                          Agents, minTimeToCollision, collisionDangerThreshold);
                }else{
                    //If the agent is "not" in a group
                    newAvoidNeighborsVector       = SteerToAvoidNeighbors(GetCurrentDirection(), GetCurrentPosition(), GetCurrentSpeed(), Agents, 
                                                                          minTimeToCollision, collisionDangerThreshold);
                }

                //Check if the agent is a part of a group, then the agent will have more strong force to avoid collision//
                if(potentialAvoidanceTarget != null){
                    newAvoidNeighborsVector *= TagChecker(potentialAvoidanceTarget);
                }
                yield return StartCoroutine(AvoidNeighborsVectorGradualTransition(transitionTime, avoidNeighborsVector, newAvoidNeighborsVector));
            }
            yield return new WaitForSeconds(updateTime);
        }
    }

    protected virtual Vector3 SteerToAvoidNeighbors (Vector3 myDirection, Vector3 myPosition, float mySpeed, List<GameObject> others, float minTimeToCollision, float collisionDangerThreshold)
    {
        float steer = 0;
        // potentialAvoidanceTarget = null;
        Vector3 myPositionAtNearestApproach    = Vector3.zero;
        Vector3 otherPositionAtNearestApproach = Vector3.zero;
        potentialAvoidanceTarget = DecideUrgentAvoidanceTarget(myDirection, myPosition, mySpeed, 
                                                               others, 
                                                               minTimeToCollision, collisionDangerThreshold, 
                                                               out myPositionAtNearestApproach, out otherPositionAtNearestApproach);

        if(potentialAvoidanceTarget != null){
            // parallel: +1, perpendicular: 0, anti-parallel: -1
            float parallelness = Vector3.Dot(myDirection, potentialAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentDirection());
            float angle = 0.707f;

            if (parallelness < -angle)
            {
                // anti-parallel "head on" paths:
                // steer away from future threat position
                Vector3 offset = otherPositionAtNearestApproach - myPosition;
                Vector3 rightVector = Vector3.Cross(myDirection, Vector3.up);

                float sideDot = Vector3.Dot(offset, rightVector);
                //If there is the predicted potential collision agent on your right side:SideDot>0, steer should be -1(left side)
                steer = (sideDot > 0) ? -1.0f : 1.0f;
            }
            else
            {
                if (parallelness > angle)
                {
                    // parallel paths: steer away from threat
                    Vector3 offset = potentialAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentPosition() - myPosition;
                    Vector3 rightVector = Vector3.Cross(myDirection, Vector3.up);

                    float sideDot = Vector3.Dot(offset, rightVector);
                    steer = (sideDot > 0) ? -1.0f : 1.0f;
                }
                else
                {
                    // perpendicular paths: steer behind threat
                    // (only the slower of the two does this)
                    // if(potentialAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentSpeed() <= GetCurrentSpeed()){
                    //     Vector3 rightVector = Vector3.Cross(myDirection, Vector3.up);
                    //     float sideDot = Vector3.Dot(rightVector, potentialAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentDirection());
                    //     steer = (sideDot > 0) ? -1.0f : 1.0f;
                    // }

                    if(GetCurrentSpeed() <= potentialAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentSpeed()){
                        Vector3 rightVector = Vector3.Cross(myDirection, Vector3.up);
                        float sideDot = Vector3.Dot(rightVector, potentialAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentDirection());
                        steer = (sideDot > 0) ? -1.0f : 1.0f;
                    }
                }
            }
        }
        return Vector3.Cross(myDirection, Vector3.up) * steer;
    }

    protected virtual float PredictNearestApproachTime (Vector3 myDirection, Vector3 myPosition, float mySpeed, Vector3 otherDirection, Vector3 otherPosition, float otherSpeed)
    {
        Vector3 relVelocity = otherDirection*otherSpeed - myDirection*mySpeed;
        float      relSpeed = relVelocity.magnitude;
        Vector3  relTangent = relVelocity / relSpeed;
        Vector3 relPosition = myPosition - otherPosition;
        float    projection = Vector3.Dot(relTangent, relPosition); 

        if (relSpeed == 0) return 0;

        return projection / relSpeed;
    }

    protected virtual float ComputeNearestApproachPositions (float time, Vector3 myPosition, Vector3 myDirection, float mySpeed, Vector3 otherPosition, Vector3 otherDirection, float otherSpeed, out Vector3 myPositionAtNearestApproach, out Vector3 otherPositionAtNearestApproach)
    {
        Vector3    myTravel = myDirection * mySpeed * time;
        Vector3     myFinal =  myPosition +    myTravel;
        Vector3 otherTravel = otherDirection * otherSpeed * time;
        Vector3  otherFinal = otherPosition + otherTravel;

        // xxx for annotation
        myPositionAtNearestApproach = myFinal;
        otherPositionAtNearestApproach = otherFinal;

        return Vector3.Distance(myFinal, otherFinal);
    }

    protected virtual IEnumerator AvoidNeighborsVectorGradualTransition(float duration, Vector3 initialVector, Vector3 targetVector){
        float elapsedTime = 0.0f;
        Vector3 initialavoidNeighborsVector = initialVector;
        while(elapsedTime < duration){
            elapsedTime += Time.deltaTime;
            avoidNeighborsVector = Vector3.Slerp(initialavoidNeighborsVector, targetVector, elapsedTime/duration);
            yield return new WaitForSeconds(Time.deltaTime);
        }
        avoidNeighborsVector = targetVector;

        yield return null;
    }
    #endregion

    /***********************************************************************************************
    * Synthetic-Vision Based Steering for Crowds[Ondrej, J. et al. (2010)]:
    * This part of the code helps virtual agents "see" and avoid bumping into each other.
    * It detects when agents might crash and helps them change direction or speed to avoid it.
    ***********************************************************************************************/
    #region Synthetic-Vision Based Steering

    protected Vector3 syntheticVisionForce;
    [HideInInspector]
    public float syntheticVisionForceWeight = 1.0f;
    protected float minTimeToInteraction;
    protected virtual IEnumerator UpdateAngularVelocityControl(float updateTime){
        while(true){
            //List<GameObject> others = collisionAvoidance.GetOthersInAnticipatedAvoidanceArea();
            List<GameObject> others = avatarCreator.GetAgents();
            Vector3 myPosition      = GetCurrentPosition();
            Vector3 myDirection     = GetCurrentDirection();
            float   _currentSpeed   = GetCurrentSpeed();
            Vector3 myGoal          = GetCurrentGoal();
            float minTimeToInteraction = float.MaxValue;
            float   angularVelocity = CalculateAngularVelocities(others, myPosition, myDirection, myGoal, out minTimeToInteraction);
            //currentSpeed = UpdateSpeed(others, _currentSpeed, minTimeToInteraction);
            //rotation
            Vector3 rotationAxis = Vector3.up;
            Quaternion rotation = Quaternion.AngleAxis(angularVelocity * Mathf.Rad2Deg, rotationAxis);
            syntheticVisionForce = rotation * myDirection;

            yield return new WaitForSeconds(updateTime);
        }
    }

    protected virtual float UpdateSpeed(List<GameObject> others, float _currentSpeed, float minTimeToInteraction, float ttiThr = 1f){
        if(others == null){
            return _currentSpeed;
        }else{
            if(minTimeToInteraction < ttiThr){
                _currentSpeed = _currentSpeed * (1 - Mathf.Exp(-0.5f * minTimeToInteraction * minTimeToInteraction));
            }
        }
        return _currentSpeed;
    }

    protected virtual float CalculateAngularVelocities(List<GameObject> others, Vector3 myPosition, Vector3 myDirection,Vector3 myGoal, out float minTimeToInteraction){
        float rightTurn = float.MaxValue;
        float leftTurn = float.MinValue;
        minTimeToInteraction = float.MaxValue; 

        foreach(GameObject other in others){
            Vector3 otherPosition     = other.GetComponent<IParameterManager>().GetCurrentPosition();
            Vector3 otherDirection    = other.GetComponent<IParameterManager>().GetCurrentDirection();

            //Calculate Angular Velocity
            float distance = Vector3.Distance(myPosition, otherPosition);
            Vector3 pi_walker = otherPosition - myPosition;
            //Vector3 pi_walker = myPosition - otherPosition;
            Vector3 k = pi_walker.normalized;
            Vector3 V_pi_w = otherDirection - myDirection;//(10)
            Vector3 V_conv_pi_w = ProjectVector(V_pi_w, k);//(11)
            Vector3 V_orth_pi_w = V_pi_w - V_conv_pi_w;//(12)
            float angularVelocity_Other = CalculateAngularVelocity(distance, V_orth_pi_w, V_conv_pi_w, 1f);//(14)
            //Calculate Time-To-Interaction
            float timeToInteraction = CalculateTimeToIntersection(distance, V_conv_pi_w);//(13)

            //Update Minimum TimeToInteraction
            if(timeToInteraction < minTimeToInteraction){
                minTimeToInteraction = timeToInteraction;
            }

            //Calculate Angular Velocities Threshold
            float bearingAngleThreshold = CalculateBearingAngleThreshold(angularVelocity_Other, timeToInteraction);
            float currentBearingAngle_Other = CalculateBearingAngle(myPosition, myDirection, otherPosition);
            //Points a walker has to react to
            if(timeToInteraction > 0f && currentBearingAngle_Other < bearingAngleThreshold){
                float turn = angularVelocity_Other - bearingAngleThreshold;
                if(angularVelocity_Other > 0f){
                    //the two walkers will not collide and I will give way.
                    if(turn < rightTurn || rightTurn == 0f){
                        rightTurn = turn;
                    }
                }else if(angularVelocity_Other < 0f || leftTurn == 0f){
                    //the two walkers will not collide and I will pass first.
                    if(turn > leftTurn){
                        leftTurn = turn;
                    }
                }   
            }
        }

        //Calculate bearing-angle corresponding to my goal
        float distance_Goal = Vector3.Distance(myPosition, myGoal);
        Vector3 pg_walker = myPosition - myGoal;
        Vector3 k_Goal = pg_walker.normalized;
        Vector3 V_pg_w = Vector3.zero - myDirection;//(10)
        Vector3 V_conv_pg_w = ProjectVector(V_pg_w, k_Goal);//(11)
        Vector3 V_orth_pg_w = V_pg_w - V_conv_pg_w;//(12)
        float angularVelocity_Goal = CalculateAngularVelocity(distance_Goal, V_orth_pg_w, V_conv_pg_w, 1f);//(14)

        float myAngularVelocity = 0f;

        //Calculate angular velocities for me
        if(Mathf.Abs(angularVelocity_Goal) < 0.1f){
            //walkers are currently heading to their goal
            if(Mathf.Abs(rightTurn) < Mathf.Abs(leftTurn)){
                //why -rightturn????
                myAngularVelocity = -rightTurn;
            }else{
                myAngularVelocity = leftTurn;
            }
        }else if(leftTurn < angularVelocity_Goal && angularVelocity_Goal < rightTurn){
            if(Mathf.Abs(rightTurn-angularVelocity_Goal) < Mathf.Abs(leftTurn-angularVelocity_Goal)){
                myAngularVelocity = rightTurn;
            }else{
                myAngularVelocity = leftTurn;
            }
        }else if(angularVelocity_Goal < leftTurn && angularVelocity_Goal > rightTurn){
            myAngularVelocity = angularVelocity_Goal;
        }

        if(myAngularVelocity == float.MaxValue || myAngularVelocity == float.MinValue){
            myAngularVelocity = 0f;
        }
        return myAngularVelocity;
    }

    protected virtual float CalculateBearingAngle(Vector3 basePosition, Vector3 baseDirection, Vector3 otherPosition)
    {
        Vector3 directionToOther = otherPosition - basePosition;
        float angleInDegrees = Vector3.SignedAngle(baseDirection, directionToOther, Vector3.up);
        return -angleInDegrees * Mathf.Deg2Rad; 
    }


    protected virtual float CalculateBearingAngleThreshold(float angularVelocity, float timeToInteraction, float a = 1.0f, float b = 1.1f, float c = 0.7f){
        float bearingAngleThreshold = 0f;
        if(angularVelocity < 0){
            bearingAngleThreshold= a - b * Mathf.Pow(timeToInteraction, -c);
        }else{
            bearingAngleThreshold= a + b * Mathf.Pow(timeToInteraction, -c);
        }
        return bearingAngleThreshold;
    }

    protected virtual Vector3 ProjectVector(Vector3 vector, Vector3 direction)
    {
        float dotProduct = Vector3.Dot(vector, direction);
        return direction * dotProduct;
    }

    protected virtual float CalculateTimeToIntersection(float distance, Vector3 velocityProjection)
    {
        float speedTowardsWalker = velocityProjection.magnitude;

        if (speedTowardsWalker < Mathf.Epsilon)
        {
            return float.PositiveInfinity;
        }

        return distance / speedTowardsWalker;
    }

    protected virtual float CalculateAngularVelocity(float distance, Vector3 orthogonalComponent, Vector3 projectionComponent, float timeUnit)
    {
        float numerator = orthogonalComponent.magnitude;
        float denominator = distance - projectionComponent.magnitude;

        if (Mathf.Abs(denominator) < Mathf.Epsilon)
        {
            return 0f;
        }

        float angle = Mathf.Atan2(numerator , denominator) / timeUnit;
        float signedAngle = Vector3.SignedAngle(projectionComponent, orthogonalComponent, Vector3.up);

        if (signedAngle < 0)
        {
            angle = -angle;
        }

        return angle;
    }

    #endregion
    /******************************************************************************************************************************
    * Force from Group[Moussaid et al. (2010)]:
    * This section of the code calculates the collective force exerted by or on a group of objects.
    * It takes into account the interactions and influences of multiple objects within a group to determine the overall force or direction.
    ********************************************************************************************************************************/
    #region GROUP FORCE
    //protected float socialInteractionWeight = 1.0f;
    protected float cohesionWeight = 2.0f;
    protected float repulsionForceWeight = 1.5f;
    protected float alignmentForceWeight = 1.5f;

    protected virtual IEnumerator UpdateGroupForce(float updateTime, SocialRelations _socialRelations){
        List<GameObject> groupAgents = avatarCreator.GetAgentsInCategory(_socialRelations);

        CapsuleCollider  agentCollider = collisionAvoidance.GetAgentCollider();
        float              agentRadius = agentCollider.radius;
        GameObject     agentGameObject = collisionAvoidance.GetAgentGameObject();

        if(groupAgents.Count <= 1 || _socialRelations == SocialRelations.Individual){
            groupForce = Vector3.zero;
        }else{

            while(true){
                Vector3  _currentPosition = GetCurrentPosition();   
                Vector3 _currentDirection = GetCurrentDirection();  

                Vector3    cohesionForce = CalculateCohesionForce (groupAgents, cohesionWeight,       agentGameObject, _currentPosition);
                Vector3   repulsionForce = CalculateRepulsionForce(groupAgents, repulsionForceWeight, agentGameObject, _currentPosition, agentRadius);
                Vector3   alignmentForce = CalculateAlignment     (groupAgents, alignmentForceWeight, agentGameObject, _currentDirection, agentRadius);
                Vector3    newGroupForce = (cohesionForce + repulsionForce + alignmentForce).normalized;

                //Vector3 AdjustPosForce = Vector3.zero;
                //Vector3  headDirection = socialBehaviour.GetCurrentLookAt();
                // if(headDirection!=null){
                //     float GazeAngle = CalculateGazingAngle(groupAgents, _currentPosition, headDirection, fieldOfView);
                //     AdjustPosForce = CalculateAdjustPosForce(socialInteractionWeight, GazeAngle, headDirection);
                // }
                //Vector3 newGroupForce = (AdjustPosForce + cohesionForce + repulsionForce + alignmentForce).normalized;

                StartCoroutine(GroupForceGradualTransition(updateTime, groupForce, newGroupForce));

                yield return new WaitForSeconds(updateTime);
            }
        }
    }

    protected virtual float CalculateGazingAngle(List<GameObject> groupAgents, Vector3 currentPos, Vector3 currentDir, float angleLimit, GameObject myself)
    {
        Vector3            centerOfMass = CalculateCenterOfMass(groupAgents, myself);
        Vector3 directionToCenterOfMass = centerOfMass - currentPos;

        float             angle = Vector3.Angle(currentDir, directionToCenterOfMass);
        float neckRotationAngle = 0f;

        if (angle > angleLimit)
        {
            neckRotationAngle = angle - angleLimit;
        }

        return neckRotationAngle;
    }

    protected virtual Vector3 CalculateAdjustPosForce(float socialInteractionWeight, float headRot, Vector3 currentDir){
        float adjustment = 0.05f;
        return -socialInteractionWeight * headRot * currentDir *adjustment;
    }

    protected virtual Vector3 CalculateCohesionForce(List<GameObject> groupAgents, float cohesionWeight, GameObject myself, Vector3 currentPos){
        //float threshold = (groupAgents.Count-1)/2;
        //float threshold = (groupAgents.Count)/2;
        float safetyDistance = 0.05f;
        float threshold = groupAgents.Count * 0.3f + safetyDistance;
        Vector3 centerOfMass = CalculateCenterOfMass(groupAgents, myself);
        float dist = Vector3.Distance(currentPos, centerOfMass);
        float judgeWithinThreshold = 0;
        if(dist > threshold){
            judgeWithinThreshold = 1;
        }
        Vector3 toCenterOfMassDir = (centerOfMass - currentPos).normalized;

        return judgeWithinThreshold*cohesionWeight*toCenterOfMassDir;
    }

    protected virtual Vector3 CalculateRepulsionForce(List<GameObject> groupAgents, float repulsionForceWeight, GameObject myself, Vector3 currentPos, float agentRadius){
        Vector3 repulsionForceDir = Vector3.zero;
        foreach(GameObject agent in groupAgents){
            //skip myselfVector3.Cross
            if(agent == myself) continue;
            Vector3 toOtherDir = agent.transform.position - currentPos;
            float dist = Vector3.Distance(currentPos, agent.transform.position);
            float threshold = 0;
            float safetyDistance = 0.05f;
            if(dist < 2*agentRadius + safetyDistance){
                threshold = 1.0f / dist;
            }
            toOtherDir = toOtherDir.normalized;
            repulsionForceDir += threshold*repulsionForceWeight*toOtherDir;
        }
        return -repulsionForceDir;
    }

    protected virtual Vector3 CalculateAlignment(List<GameObject> groupAgents, float alignmentForceWeight, GameObject myself, Vector3 currentDirection, float agentRadius){
        Vector3 steering = Vector3.zero;
        int neighborsCount = 0;

        foreach (GameObject go in groupAgents)
        {
            if (go != myself)
            {
                Vector3 otherDirection = go.GetComponent<IParameterManager>().GetCurrentDirection();
                steering += otherDirection;
                neighborsCount++;
            }
            // float alignmentAngle  = 0.7f;
            // if (InBoidNeighborhood(go, myself, agentRadius * 3, agentRadius * 6, alignmentAngle, currentDirection))
            // {
            //     Vector3 otherDirection = go.GetComponent<IParameterManager>().GetCurrentDirection();
            //     steering += otherDirection;
            //     neighborsCount++;
            // }
        }

        if (neighborsCount > 0)
        {
            //steering =  ((steering / neighborsCount) - currentDirection).normalized;
            steering = steering * alignmentForceWeight;
        }

        return steering * alignmentForceWeight;
    }

    protected virtual bool InBoidNeighborhood(GameObject other, GameObject myself, float minDistance, float maxDistance, float cosMaxAngle, Vector3 currentDirection){
        if (other == myself)
        {
            return false;
        }
        else
        {
            float dist = Vector3.Distance(other.transform.position, myself.transform.position);
            Vector3 offset = other.transform.position - myself.transform.position;

            if (dist < minDistance)
            {
                return true;
            }
            else if (dist > maxDistance)
            {
                return false;
            }
            else
            {
                Vector3 unitOffset = offset.normalized;
                float forwardness = Vector3.Dot(currentDirection, unitOffset);
                return forwardness > cosMaxAngle;
            }
        }
    }

    protected virtual Vector3 CalculateCenterOfMass(List<GameObject> groupAgents, GameObject myself)
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

    protected virtual IEnumerator GroupForceGradualTransition(float duration, Vector3 initialVector, Vector3 targetVector){
        float elapsedTime = 0.0f;
        Vector3 initialGroupForce = initialVector;
        while(elapsedTime < duration){
            elapsedTime += Time.deltaTime;
            groupForce = Vector3.Slerp(initialGroupForce, targetVector, elapsedTime/duration);
            yield return new WaitForSeconds(Time.deltaTime);
        }
        groupForce = targetVector;

        yield return null;
    }
    #endregion

    /********************************************************************************************************************************
    * Wall force[Nuria HiDAC]:
    * This section of the code is dedicated to modifying the speed of an object based on certain conditions or criteria.
    * It ensures that the object maintains an appropriate speed, possibly in response to environmental factors, obstacles, or other objects.
    ********************************************************************************************************************************/
    #region WALL FORCE
    protected virtual IEnumerator UpdateWallForce(float updateTime, float transitionTime){
        while(true){
            GameObject currentWallTarget = collisionAvoidance.GetCurrentWallTarget();
            if(currentWallTarget != null){
                NormalVector normalVector = currentWallTarget.GetComponent<NormalVector>();
                wallRepForce = normalVector.CalculateNormalVectorFromWall(GetCurrentPosition());
            }else{
                yield return StartCoroutine(WallForceGradualTransition(transitionTime, wallRepForce, Vector3.zero));
            }
            yield return new WaitForSeconds(updateTime);
        }
    }

    protected virtual IEnumerator WallForceGradualTransition(float duration, Vector3 initialVector, Vector3 targetVector){
        float elapsedTime = 0.0f;
        Vector3 initialWallForce = initialVector;
        while(elapsedTime < duration){
            elapsedTime += Time.deltaTime;
            wallRepForce = Vector3.Slerp(initialWallForce, targetVector, elapsedTime/duration);
            yield return new WaitForSeconds(Time.deltaTime);
        }
        wallRepForce = targetVector;

        yield return null;
    }
    #endregion

    /********************************************************************************************************************************
    * Speed Adjustment Function:
    * This section of the code is dedicated to modifying the speed of an object based on certain conditions or criteria.
    * It ensures that the object maintains an appropriate speed, possibly in response to environmental factors, obstacles, or other objects.
    ********************************************************************************************************************************/
    #region SPEED ADJUSTMENT 
    protected virtual IEnumerator UpdateSpeed(List<GameObject> groupAgents, GameObject myself, float updateTime = 0.1f, float speedChangeRate = 0.05f){
        if(groupAgents.Count == 1 || GetSocialRelations() == SocialRelations.Individual){
            StartCoroutine(DecreaseSpeedBaseOnUpperBodyAnimation(updateTime));
            yield return null;
        }

        float averageSpeed = 0.0f;
        foreach(GameObject go in groupAgents){
            IParameterManager parameterManager = go.GetComponent<IParameterManager>();
            averageSpeed += parameterManager.GetCurrentSpeed();
        }
        averageSpeed /= groupAgents.Count;

        averageSpeed = averageSpeed - 0.1f*groupAgents.Count;
        if(averageSpeed<minSpeed){
            averageSpeed = minSpeed;
        }

        while(true){
            Vector3 centerOfMass = CalculateCenterOfMass(groupAgents, myself);
            Vector3 directionToCenterOfMass = (centerOfMass - (Vector3)GetCurrentPosition()).normalized;
            Vector3 myForward = GetCurrentDirection();
            float distFromMeToCenterOfMass = Vector3.Distance(GetCurrentPosition(), centerOfMass);

            //0.3f is the radius of the agent
            float safetyDistance = 0.05f;
            float speedChangeDist = groupAgents.Count * 0.3f + safetyDistance;

            if(distFromMeToCenterOfMass > speedChangeDist){
                float dotProduct = Vector3.Dot(myForward, directionToCenterOfMass);
                //lowest speed or average speed?
                if (dotProduct > 0)
                {
                    //accelerate when the center of mass is in front of me
                    if(GetCurrentSpeed() <= maxSpeed){
                        currentSpeed += speedChangeRate; 
                    }else{
                        currentSpeed = maxSpeed;
                    }
                }
                else
                {
                    //decelerate when the center of mass is behind
                    if(GetCurrentSpeed() >= minSpeed){
                        currentSpeed -= speedChangeRate;
                    }else{
                        currentSpeed = minSpeed;
                    }
                }
            }else{
                currentSpeed = averageSpeed;
            }
            yield return new WaitForSeconds(updateTime);
        }
    }

    protected virtual IEnumerator DecreaseSpeedBaseOnUpperBodyAnimation(float updateTime){
        float initialSpeed = GetCurrentSpeed();
        while(true){
            if(collisionAvoidance.GetUpperBodyAnimationState() == UpperBodyAnimationState.SmartPhone){
                currentSpeed = minSpeed;
            }else{
                currentSpeed = initialSpeed;
            }
            yield return new WaitForSeconds(updateTime);
        }
    }

    protected virtual IEnumerator UpdateSpeedBasedOnGoalDist(float updateTime){
        OnGoalReached += () =>
        {
            float duration = 2.0f;
            StartCoroutine(SpeedChanger(duration, currentSpeed, initialSpeed));
        };
        while(true){
            float distanceToGoal = Vector3.Distance(GetCurrentPosition(), GetCurrentGoal());
            if(distanceToGoal < slowingRadius) currentSpeed = Mathf.Lerp(minSpeed, currentSpeed, distanceToGoal / slowingRadius);
            
            yield return new WaitForSeconds(updateTime);
        }
    }

    protected virtual IEnumerator SpeedChanger(float duration, float _currentSpeed, float targetSpeed){
        float elapsedTime = 0.0f;
        while(elapsedTime < duration){
            elapsedTime += Time.deltaTime;
            currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, elapsedTime/duration);
            yield return new WaitForSeconds(Time.deltaTime);
        }
        currentSpeed = targetSpeed;

        yield return null;
    }
    #endregion
    
    /********************************************************************************************************************************
    * Update Attractions
    ********************************************************************************************************************************/
    #region OTHER
    protected virtual IEnumerator CheckForGoalReached(float updateTime){
        while(true){
            float distanceToGoal = Vector3.Distance(GetCurrentPosition(), GetCurrentGoal());
            if(distanceToGoal < slowingRadius){
                onInSlowingArea = true;
            }else{
                onInSlowingArea = false;
            }
            yield return new WaitForSeconds(updateTime);
        }
    }
    #endregion
    
    /********************************************************************************************************************************
    * Get and Set Methods:
    * This section of the code contains methods to retrieve (get) and update (set) the values of properties or attributes of an object.
    * These methods ensure controlled access and potential validation when changing the state of the object.
    ********************************************************************************************************************************/
    #region GET AND SET
    public override void GetTrajectoryFeature(TrajectoryFeature feature, int index, Transform character, NativeArray<float> output)
    {
        if (!feature.SimulationBone) Debug.Assert(false, "Trajectory should be computed using the SimulationBone");
        switch (feature.FeatureType)
        {
            case TrajectoryFeature.Type.Position:
                Vector3 world = GetWorldPredictedPos(index);
                Vector3 local = character.InverseTransformPoint(new Vector3(world.x, 0.0f, world.z));
                output[0] = local.x;
                output[1] = local.z;
                break;
            case TrajectoryFeature.Type.Direction:
                Vector3 worldDir = GetWorldPredictedDir(index);
                Vector3 localDir = character.InverseTransformDirection(new Vector3(worldDir.x, 0.0f, worldDir.z));
                output[0] = localDir.x;
                output[1] = localDir.z;
                break;
            default:
                Debug.Assert(false, "Unknown feature type: " + feature.FeatureType);
                break;
        }
    }

    protected virtual Vector3 GetWorldPredictedPos(int index)
    {
        return PredictedPositions[index] + transform.position;
    }
    protected virtual Vector3 GetWorldPredictedDir(int index)
    {
        return PredictedDirections[index];
    }

    public override float3 GetWorldInitPosition()
    {
        return Path[currentGoalIndex-1] + this.transform.position;
    }
    public override float3 GetWorldInitDirection()
    {
        // float2 dir = Path.Length > 0 ? Path[1].Position - Path[0].Position : new float2(0, 1);
        Vector3 dir = Path.Length > 0 ? Path[currentGoalIndex] - Path[currentGoalIndex-1] : new Vector3(0, 0, 1);
        return dir.normalized;
    }

    public float3 GetWorldPosition(Transform transform, Vector3 Position)
    {
        return transform.position + Position;
    }

    public float3 GetCurrentPosition()
    {
        return transform.position + CurrentPosition;
    }
    public quaternion GetCurrentRotation()
    {
        Quaternion rot = Quaternion.LookRotation(CurrentDirection);
        return rot * transform.rotation;
    }
    public Vector3 GetCurrentDirection(){
        return CurrentDirection;
    }
    public Vector3 GetRawCurrentPosition()
    {
        return CurrentPosition;
    }
    public float GetCurrentSpeed(){
        return currentSpeed;
    }
    public Vector3 GetCurrentGoal(){
        return currentGoal;
    }
    public SocialRelations GetSocialRelations(){
        return socialRelations;
    }
    public AvatarCreatorBase GetAvatarCreatorBase(){
        return avatarCreator;
    }
    public GameObject GetPotentialAvoidanceTarget()
    {
        return potentialAvoidanceTarget;
    }
    public Vector3 GetCurrentAvoidanceVector(){
        return avoidanceVector;
    }
    public CollisionAvoidanceController GetCollisionAvoidanceController(){
        return collisionAvoidance;
    }
    public bool GetOnInSlowingArea(){
        return onInSlowingArea;
    }
    #endregion

    /******************************************************************************************************************************
    * Gizmos and Drawing:
    * This section of the code is dedicated to visual debugging and representation in the Unity editor.
    * It contains methods and logic to draw gizmos, shapes, and other visual aids that help in understanding and debugging the scene or object behaviors.
    ******************************************************************************************************************************/
    #region GIZMOS AND DRAW
    protected virtual void DrawInfo(){
        Color gizmoColor;
        if(showAvoidanceForce){
            gizmoColor = Color.blue;
            Draw.ArrowheadArc((Vector3)GetCurrentPosition(), avoidanceVector, 0.55f, gizmoColor);
        }

        if(showCurrentDirection){
            gizmoColor = Color.red;
            Draw.ArrowheadArc((Vector3)GetCurrentPosition(), CurrentDirection, 0.55f, gizmoColor);
        }
        
        if(showGoalDirection){
            gizmoColor = Color.white;
            Draw.ArrowheadArc((Vector3)GetCurrentPosition(), toGoalVector, 0.55f, gizmoColor);
        }

        if(showAnticipatedCollisionAvoidance){
            gizmoColor = Color.green;
            Draw.ArrowheadArc((Vector3)GetCurrentPosition(), avoidNeighborsVector, 0.55f, gizmoColor);
        }

        if(showGroupForce){
            gizmoColor = Color.cyan;
            Draw.ArrowheadArc((Vector3)GetCurrentPosition(), groupForce, 0.55f, gizmoColor);
        }

        if(showWallForce){
            gizmoColor = Color.black;
            Draw.ArrowheadArc((Vector3)GetCurrentPosition(), wallRepForce, 0.55f, gizmoColor);
        }

        if(showSyntheticVisionForce){
            gizmoColor = Color.magenta;
            Draw.ArrowheadArc((Vector3)GetCurrentPosition(), syntheticVisionForce, 0.55f, gizmoColor);
        }
        // const float heightOffset = 0.01f;
        //Draw slowingRadius, goalRadius
        // for (int i = 0; i < Path.Length; i++)
        // {
        //     Vector3 pos = GetWorldPosition(transform, Path[i]);
        //     Draw.Circle(new Vector3(pos.x, heightOffset, pos.z), Vector3.up ,slowingRadius);
        //     Draw.Circle(new Vector3(pos.x, heightOffset, pos.z), Vector3.up, goalRadius);
        //     Draw.SolidCircle(new Vector3(pos.x, heightOffset, pos.z), Vector3.up, 0.1f);
        // }
    }

    #if UNITY_EDITOR
    protected virtual void OnDrawGizmos()
    {

        if (Path == null) return;

        // const float heightOffset = 0.01f;

        // Draw KeyPoints
        // Gizmos.color = Color.blue;
        // for (int i = 0; i < Path.Length; i++)
        // {
        //     Vector3 pos = GetWorldPosition(transform, Path[i]);
        //     Gizmos.DrawSphere(new Vector3(pos.x, heightOffset, pos.z), 0.1f);
        // }
        //Draw OnlyStartPos

        // Color gizmoColor;
        // if (GetSocialRelations() == SocialRelations.Couple){
        //     gizmoColor = new Color(1.0f, 0.0f, 0.0f); // red
        // }else if (GetSocialRelations() == SocialRelations.Friend){
        //     gizmoColor = new Color(0.0f, 1.0f, 0.0f); // green
        // }else if  (GetSocialRelations() == SocialRelations.Family){
        //     gizmoColor = new Color(0.0f, 0.0f, 1.0f); // blue
        // }else if  (GetSocialRelations() == SocialRelations.Coworker){
        //     gizmoColor = new Color(1.0f, 1.0f, 0.0f); // yellow
        // }else{
        //     gizmoColor = new Color(1.0f, 1.0f, 1.0f); // white
        // }
        // Gizmos.color = gizmoColor;
        // Vector3 pos = GetWorldPosition(transform, Path[0]);
        // Gizmos.DrawSphere(new Vector3(pos.x, heightOffset, pos.z), 0.1f);
        
        // Draw Path
        // Gizmos.color = new Color(0.5f, 0.0f, 0.0f, 1.0f);
        // for (int i = 0; i < Path.Length - 1; i++)
        // {
        //     Vector3 pos = GetWorldPosition(transform, Path[i]);
        //     Vector3 nextPos = GetWorldPosition(transform, Path[i+1]);
        //     GizmosExtensions.DrawLine(new Vector3(pos.x, heightOffset, pos.z), new Vector3(nextPos.x, heightOffset, nextPos.z), 6);
        // }
        // //Last Line
        // Vector3 lastPos = GetWorldPosition(transform, Path[Path.Length - 1]);
        // Vector3 firstPos = GetWorldPosition(transform, Path[0]);
        // GizmosExtensions.DrawLine(new Vector3(lastPos.x, heightOffset, lastPos.z), new Vector3(firstPos.x, heightOffset, firstPos.z), 6);
        
        // Draw Velocity
        // for (int i = 0; i < Path.Length - 1; i++)
        // {
        //     Vector3 pos = Path[i];
        //     Vector3 nextPos = Path[i + 1];
        //     Vector3 start = new Vector3(pos.x, heightOffset, pos.z);
        //     Vector3 end = new Vector3(nextPos.x, heightOffset, nextPos.z);
        //     GizmosExtensions.DrawArrow(start, start + (end - start).normalized * Vector3.Min(Path[i], Vector3.Distance(pos, nextPos)), thickness: 6);
        // }
        // Last Line
        // Vector3 lastPos2 = GetWorldPosition(transform, Path[Path.Length - 1]);
        // Vector3 firstPos2 = GetWorldPosition(transform,Path[0]);
        // Vector3 start2 = new Vector3(lastPos2.x, heightOffset, lastPos2.z);
        // Vector3 end2 = new Vector3(firstPos2.x, heightOffset, firstPos2.z);
        // GizmosExtensions.DrawArrow(start2, start2 + (end2 - start2).normalized * currentSpeed, thickness: 3);

        // Draw Current Position And Direction
        // if (!Application.isPlaying) return;
        // Gizmos.color = new Color(1.0f, 0.3f, 0.1f, 1.0f);
        // Vector3 currentPos = (Vector3)GetCurrentPosition() + Vector3.up * heightOffset * 2;
        // Gizmos.DrawSphere(currentPos, 0.1f);
        // GizmosExtensions.DrawLine(currentPos, currentPos + (Quaternion)GetCurrentRotation() * Vector3.forward, 12);
        // // Draw Prediction
        // if (PredictedPositions == null || PredictedPositions.Length != NumberPredictionPos ||
        //     PredictedDirections == null || PredictedDirections.Length != NumberPredictionRot) return;
        // Gizmos.color = new Color(0.6f, 0.3f, 0.8f, 1.0f);
        // for (int i = 0; i < NumberPredictionPos; i++)
        // {
        //     Vector3 predictedPosf2 = GetWorldPredictedPos(i);
        //     Vector3 predictedPos = new Vector3(predictedPosf2.x, heightOffset * 2, predictedPosf2.z);
        //     Gizmos.DrawSphere(predictedPos, 0.1f);
        //     Vector3 dirf2 = GetWorldPredictedDir(i);
        //     GizmosExtensions.DrawLine(predictedPos, predictedPos + new Vector3(dirf2.x, 0.0f, dirf2.z) * 0.5f, 12);
        // }
    }
    #endif
    #endregion

}
}
