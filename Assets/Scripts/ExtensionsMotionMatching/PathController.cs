using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Drawing;

namespace MotionMatching{
    using TrajectoryFeature = MotionMatchingData.TrajectoryFeature;

    public class PathController : MotionMatchingCharacterController
    {
        public string TrajectoryPositionFeatureName = "FuturePosition";
        public string TrajectoryDirectionFeatureName = "FutureDirection";
        public Vector3[] Path;
        //Warning:current position is not the current position of the agent itself when the parent transform is not (0.0f, 0.0f, 0.0f);
        //To get current position of the agent you have to use GetCurrentPosition()
        private Vector3 CurrentPosition;
        private Vector3 CurrentDirection;
        private Vector3[] PredictedPositions;
        private Vector3[] PredictedDirections;
        [Range(0.0f, 2.0f)] public float MaxDistanceSimulationBoneAndObject = 0.1f; // Max distance between SimulationBone and SimulationObject
        [Range(0.0f, 2.0f)] public float PositionAdjustmentHalflife = 0.1f; // Time needed to move half of the distance between SimulationBone and SimulationObject
        [Range(0.0f, 2.0f)] public float PosMaximumAdjustmentRatio = 0.1f; // Ratio between the adjustment and the character's velocity to clamp the adjustment
        // Speed Of Agents -----------------------------------------------------------------
        [Header("Speed")]
        private float CurrentSpeed = 1.0f; //Current speed of the agent
        [Range (0.0f, 1.5f)]
        public float initialSpeed = 1.0f; //Initial speed of the agent
        private float MinSpeed = 0.5f; //Minimum speed of the agent
        // --------------------------------------------------------------------------
        // To Mange Agents -----------------------------------------------------------------
        [Tooltip("Agent Manager is a script to manage agents")] public AgentManager agentManager; //Manager for all of the agents
        // --------------------------------------------------------------------------
        // Features -----------------------------------------------------------------
        [Header("Features For Motion Matching")]
        private int TrajectoryPosFeatureIndex;
        private int TrajectoryRotFeatureIndex;
        private int[] TrajectoryPosPredictionFrames;
        private int[] TrajectoryRotPredictionFrames;
        private int NumberPredictionPos { get { return TrajectoryPosPredictionFrames.Length; } }
        private int NumberPredictionRot { get { return TrajectoryRotPredictionFrames.Length; } }
        // --------------------------------------------------------------------------
        // Collision Avoidance ------------------------------------------------------
        [Header("Parameters For Basic Collision Avoidance")]
        public Vector3 avoidanceColliderSize = new Vector3(1.5f, 1.5f, 2.0f); 
        private Vector3 avoidanceVector = Vector3.zero;//Direction of basic collision avoidance
        private float avoidanceWeight = 1.5f;//Weight for basic collision avoidance
        private GameObject currentAvoidanceTarget;
        public GameObject CurrentAvoidanceTarget{
            get => currentAvoidanceTarget;
            set => currentAvoidanceTarget = value;
        }
        public CapsuleCollider agentCollider; //Collider of the agent
        private BoxCollider avoidanceCollider; //The area to trigger basic collision avoidance
        private GameObject avoidanceColliderArea;
        private float agentRadius;
        // --------------------------------------------------------------------------
        // To Goal Direction --------------------------------------------------------
        [Header("Parameters For Goal Direction")]
        private Vector3 CurrentGoal;
        private Vector3 toGoalVector = Vector3.zero;//Direction to goal
        private float toGoalWeight = 1.7f;//Weight for goal direction
        private int CurrentGoalIndex = 1;
        public float goalRadius = 0.5f;
        public float slowingRadius = 2.0f;
        // --------------------------------------------------------------------------
        // Unaligned Collision Avoidance -------------------------------------------
        [Header("Parameters For Unaligned Collision Avoidance")]
        private Vector3 otherPositionAtNearestApproach;
        private Vector3 myPositionAtNearestApproach;
        private Vector3 avoidNeighborsVector = Vector3.zero;//Direction for unaligned collision avoidance
        private GameObject potentialAvoidanceTarget;
        private float avoidNeighborWeight = 1.0f;//Weight for unaligned collision avoidance
        private float minTimeToCollision =5.0f;
        private float collisionDangerThreshold = 4.0f;
        // --------------------------------------------------------------------------
        // When Collide each other -----------------------------------------------------
        [Header("Social Behaviour, Non-verbal Communication")]
        private bool onCollide = false;
        private bool onMoving = false;
        private GameObject collidedAgent;
        // --------------------------------------------------------------------------
        // Gizmo Parameters -------------------------------------------------------------
        [Header("Controll Gizmos")]
        public bool ShowAvoidanceForce = false;
        public bool ShowUnalignedCollisionAvoidance = false;
        public bool ShowGoalDirection = false;
        public bool ShowCurrentDirection = false;
        // --------------------------------------------------------------------------
        

        private void Start()
        {
            //Create Box Collider
            avoidanceColliderArea = new GameObject("AvoidanceColliderArea");
            avoidanceColliderArea.transform.parent = this.transform;
            avoidanceCollider = avoidanceColliderArea.AddComponent<BoxCollider>();
            avoidanceCollider.size = avoidanceColliderSize;
            avoidanceCollider.isTrigger = true;
            avoidanceColliderArea.AddComponent<UpdateAvoidanceTarget>();


            //init
            CurrentSpeed = initialSpeed;
            if (initialSpeed < MinSpeed)
            {
                MinSpeed = initialSpeed;
            }
            agentRadius = agentCollider.radius;
            CurrentPosition = Path[0];
            CurrentGoal = Path[CurrentGoalIndex];            
            

            //Create Agent Collision Detection:This is a script to detect the collision between agents by using capsule collider.
            AgentCollisionDetection agentCollisionDetection = agentCollider.GetComponent<AgentCollisionDetection>();
            if (agentCollisionDetection == null)
            {
                agentCollisionDetection = agentCollider.gameObject.AddComponent<AgentCollisionDetection>();
                Debug.Log("AgentCollisionDetection script added");
            }
            agentCollisionDetection.InitParameter(this.gameObject.GetComponent<PathController>(), agentCollider);
 

            // Get the feature indices
            TrajectoryPosFeatureIndex = -1;
            TrajectoryRotFeatureIndex = -1;
            for (int i = 0; i < SimulationBone.MMData.TrajectoryFeatures.Count; ++i)
            {
                if (SimulationBone.MMData.TrajectoryFeatures[i].Name == TrajectoryPositionFeatureName) TrajectoryPosFeatureIndex = i;
                if (SimulationBone.MMData.TrajectoryFeatures[i].Name == TrajectoryDirectionFeatureName) TrajectoryRotFeatureIndex = i;
            }
            Debug.Assert(TrajectoryPosFeatureIndex != -1, "Trajectory Position Feature not found");
            Debug.Assert(TrajectoryRotFeatureIndex != -1, "Trajectory Direction Feature not found");


            TrajectoryPosPredictionFrames = SimulationBone.MMData.TrajectoryFeatures[TrajectoryPosFeatureIndex].FramesPrediction;
            TrajectoryRotPredictionFrames = SimulationBone.MMData.TrajectoryFeatures[TrajectoryRotFeatureIndex].FramesPrediction;
            Debug.Assert(TrajectoryPosPredictionFrames.Length == TrajectoryRotPredictionFrames.Length, "Trajectory Position and Trajectory Direction Prediction Frames must be the same for PathCharacterController");
            for (int i = 0; i < TrajectoryPosPredictionFrames.Length; ++i)
            {
                Debug.Assert(TrajectoryPosPredictionFrames[i] == TrajectoryRotPredictionFrames[i], "Trajectory Position and Trajectory Direction Prediction Frames must be the same for PathCharacterController");
            }


            PredictedPositions = new Vector3[NumberPredictionPos];
            PredictedDirections = new Vector3[NumberPredictionRot];
            

            StartCoroutine(UpdateAvoidanceColliderPos(0.9f));
            StartCoroutine(UpdateAvoidanceVector(0.1f, 0.5f));
            //StartCoroutine(UpdateAvoidNeighborsVector(agentManager.GetAgents(), 0.1f, 0.3f));
        }

        protected override void OnUpdate(){
            // Predict the future positions and directions
            for (int i = 0; i < NumberPredictionPos; i++)
            {
                SimulatePath(DatabaseDeltaTime * TrajectoryPosPredictionFrames[i], CurrentPosition, out PredictedPositions[i], out PredictedDirections[i]);
            }
            
            // Update Current Position and Direction
            SimulatePath(Time.deltaTime, CurrentPosition, out CurrentPosition, out CurrentDirection);
            CheckForGoalProximity();

            //Prevent agents from intersection
            AdjustSimulationBone();
            ClampSimulationBone();

            DrawInfo();
        }

        private void ClampSimulationBone()
        {
            // Clamp Position
            float3 simulationObject = GetCurrentPosition();
            float3 simulationBone = SimulationBone.transform.position;
            if (math.distance(simulationObject, simulationBone) > MaxDistanceSimulationBoneAndObject)
            {
                float3 newSimulationBonePos = MaxDistanceSimulationBoneAndObject * math.normalize(simulationBone - simulationObject) + simulationObject;
                SimulationBone.SetPosAdjustment(newSimulationBonePos - simulationBone);
            }
        }
        private void AdjustSimulationBone()
        {
            AdjustCharacterPosition();
        }

        private void AdjustCharacterPosition()
        {
            float3 simulationObject = GetCurrentPosition();
            float3 simulationBone = SimulationBone.transform.position;
            float3 differencePosition = simulationObject - simulationBone;
            // Damp the difference using the adjustment halflife and dt
            float3 adjustmentPosition = Spring.DampAdjustmentImplicit(differencePosition, PositionAdjustmentHalflife, Time.deltaTime);
            // Clamp adjustment if the length is greater than the character velocity
            // multiplied by the ratio
            float maxLength = PosMaximumAdjustmentRatio * math.length(SimulationBone.Velocity) * Time.deltaTime;
            if (math.length(adjustmentPosition) > maxLength)
            {
                adjustmentPosition = maxLength * math.normalize(adjustmentPosition);
            }
            // Move the simulation bone towards the simulation object
            SimulationBone.SetPosAdjustment(adjustmentPosition);
        }


        private void SimulatePath(float time, Vector3 currentPosition, out Vector3 nextPosition, out Vector3 direction)
        {
            //Update ToGoalVector
            toGoalVector = (CurrentGoal - currentPosition).normalized;

            //gradually decrease speed of the agent if it is close to the goal:
            float distanceToGoal = Vector3.Distance(currentPosition, CurrentGoal);
            CurrentSpeed = distanceToGoal < slowingRadius ? Mathf.Lerp(MinSpeed, CurrentSpeed, distanceToGoal / slowingRadius) : CurrentSpeed;

            //Move Agent
            direction = (toGoalWeight*toGoalVector + avoidanceWeight*avoidanceVector + avoidNeighborWeight*avoidNeighborsVector).normalized;
        
            if(onCollide){
                if(onMoving){
                    Vector3 offset = collidedAgent.GetComponent<ParameterManager>().GetCurrentPosition() - (Vector3)GetCurrentPosition();
                    Vector3 myRightAgainstOther = Vector3.Cross(offset.normalized, Vector3.up);
                    direction = (direction + myRightAgainstOther.normalized*1.0f).normalized;
                    nextPosition = currentPosition + direction * CurrentSpeed * time;
                    // if(collidedAgent.GetComponent<ParameterManager>().GetOnMoving()){
                    //     nextPosition = currentPosition + ((Vector3)GetCurrentPosition()-collidedAgent.transform.position).normalized * 0.3f * time;
                    // }else{
                    //     Vector3 offset = collidedAgent.GetComponent<ParameterManager>().GetCurrentPosition() - (Vector3)GetCurrentPosition();
                    //     Vector3 myRightAgainstOther = Vector3.Cross(offset.normalized, Vector3.up);
                    //     direction = (direction + myRightAgainstOther.normalized*1.0f).normalized;
                    //     nextPosition = currentPosition + direction * CurrentSpeed * time;
                    // }   
                }else{
                    nextPosition = currentPosition + ((Vector3)GetCurrentPosition()-collidedAgent.transform.position).normalized * 0.3f * time;
                }
            }else{
                nextPosition = currentPosition + direction * CurrentSpeed * time;
            }
        }

        // private void CheckCollision(Vector3 otherPosition,Vector3 myPosition, Vector3 otherDirection, Vector3 myDirection)
        // {
        //     // parallel: +1, perpendicular: 0, anti-parallel: -1
        //     float dotProduct = Vector3.Dot(myDirection.normalized, otherDirection.normalized);

        //     float angle = 0.707f;

        //     if (dotProduct < -angle)
        //     {
        //         Vector3 offset = otherPosition - myPosition;
        //         Vector3 myRightAgainstOther = Vector3.Cross(offset.normalized, Vector3.up);
        //         float dotToMyRight = Vector3.Dot(myRightAgainstOther, myDirection);

        //         Vector3 myRightAgainstMe = Vector3.Cross(-offset.normalized, Vector3.up);
        //         float dotToOtherRight = Vector3.Dot(myRightAgainstMe, otherDirection);

        //         if ((dotToMyRight > 0 && dotToOtherRight < 0) || (dotToMyRight < 0 && dotToOtherRight > 0))
        //         {
        //             Debug.Log("Potential to Collide");
        //         }
        //     }
        //     else
        //     {
        //         if (dotProduct > angle)
        //         {
                    
        //         }
        //         else
        //         {
                    
        //         }
        //     }
        // }
        
        //To Update Goal Direction
        private void CheckForGoalProximity()
        {
            float distanceToGoal = Vector3.Distance(CurrentPosition, CurrentGoal);
            if (distanceToGoal < goalRadius)
            {
                SelectRandomGoal();
            }
        }
        private void SelectRandomGoal(){
            CurrentGoalIndex++;
            CurrentGoal = Path[(CurrentGoalIndex + 1) % Path.Length];
            StartCoroutine(GradualSpeedUp(1.0f, CurrentSpeed, initialSpeed));
        }

        private IEnumerator GradualSpeedUp(float duration, float currentSpeed, float targetSpeed){
            float elapsedTime = 0.0f;
            float initialSpeed = currentSpeed;
            while(elapsedTime < duration){
                elapsedTime += Time.deltaTime;
                CurrentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, elapsedTime/duration);
                yield return new WaitForSeconds(Time.deltaTime);
            }
            CurrentSpeed = targetSpeed;

            yield return null;
        }

        //Collision Avoidance
        //※current avoidance target is the agent that has capsule collider
        private IEnumerator UpdateAvoidanceVector(float updateTime, float transitionTime)
        {
            float elapsedTime = 0.0f;
            while(true){
                //temporalily
                if (currentAvoidanceTarget != null)
                {
                    avoidanceVector = ComputeAvoidanceVector(currentAvoidanceTarget, CurrentDirection, GetCurrentPosition());
                    //gradually increase the avoidance force considering the distance 
                    //Debug.Log(1.0f-Vector3.Distance(currentAvoidanceTarget.transform.position, transform.position)/(Mathf.Sqrt(avoidanceArea.x/2*avoidanceArea.x/2+avoidanceArea.z*avoidanceArea.z)+agentRadius*2));
                    avoidanceVector = avoidanceVector*(1.0f-Vector3.Distance(currentAvoidanceTarget.transform.position, GetCurrentPosition())/(Mathf.Sqrt(avoidanceColliderSize.x/2*avoidanceColliderSize.x/2+avoidanceColliderSize.z*avoidanceColliderSize.z)+agentRadius*2));
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

        private Vector3 ComputeAvoidanceVector(GameObject avoidanceTarget, Vector3 currentDirection, Vector3 currentPosition)
        {
            Vector3 directionToAvoidanceTarget = (avoidanceTarget.transform.position - currentPosition).normalized;
            Vector3 upVector;
            if (Vector3.Dot(directionToAvoidanceTarget, currentDirection) >= 0.9748f)
            {
                upVector = new Vector3(0.0f, 1.0f, 0.0f);
            }
            else
            {
                upVector = Vector3.Cross(directionToAvoidanceTarget, currentDirection);
            }
            return Vector3.Cross(upVector, directionToAvoidanceTarget).normalized;
        }

        //For Update AvoidanceTarget
        private IEnumerator UpdateAvoidanceColliderPos(float AgentHeight){
            while(true){
                Vector3 Center = (Vector3)GetCurrentPosition()+CurrentDirection;
                avoidanceColliderArea.transform.position = new Vector3(Center.x, AgentHeight, Center.z);
                Quaternion targetRotation = Quaternion.LookRotation(CurrentDirection);
                avoidanceColliderArea.transform.rotation = targetRotation;
                yield return null;
            }
        }

        //Unnaligned Collision Avoidance
        private float predictNearestApproachTime (Vector3 myDirection, Vector3 myPosition, float mySpeed, Vector3 otherDirection, Vector3 otherPosition, float otherSpeed)
        {
            Vector3 relVelocity = otherDirection*otherSpeed - myDirection*mySpeed;
            float relSpeed = relVelocity.magnitude;
            Vector3 relTangent = relVelocity / relSpeed;
            Vector3 relPosition = myPosition - otherPosition;
            float projection = Vector3.Dot(relTangent, relPosition); 
            if (relSpeed == 0) return 0;

            return projection / relSpeed;
        }
        private float computeNearestApproachPositions (float time, Vector3 myPosition, Vector3 myDirection, float mySpeed, Vector3 otherPosition, Vector3 otherDirection, float otherSpeed)
        {
            Vector3    myTravel = myDirection * mySpeed * time;
            Vector3 otherTravel = otherDirection * otherSpeed * time;

            Vector3    myFinal =  myPosition +    myTravel;
            Vector3 otherFinal = new Vector3(0f,0f,0f)+otherPosition + otherTravel;

            // xxx for annotation
            myPositionAtNearestApproach = myFinal;
            otherPositionAtNearestApproach = otherFinal;

            return Vector3.Distance(myFinal, otherFinal);
        }
        public IEnumerator UpdateAvoidNeighborsVector(List<GameObject> Agents , float updateTime, float transitionTime){
            while(true){
                if(currentAvoidanceTarget != null){
                    avoidNeighborsVector = Vector3.zero;
                }else{
                    Vector3 newavoidNeighborsVector = steerToAvoidNeighbors(Agents, minTimeToCollision, collisionDangerThreshold);
                    yield return StartCoroutine(AvoidNeighborsVectorGradualVectorTransition(transitionTime, avoidNeighborsVector, newavoidNeighborsVector));
                }
                yield return new WaitForSeconds(updateTime);
            }
        }
        private IEnumerator AvoidNeighborsVectorGradualVectorTransition(float duration, Vector3 initialVector, Vector3 targetVector){
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
        public Vector3 steerToAvoidNeighbors (List<GameObject> others, float minTimeToCollision, float collisionDangerThreshold)
        {
            float steer = 0;
            potentialAvoidanceTarget = null;
            // if(currentAvoidanceTarget != null) return Vector3.zero;
            foreach(GameObject other in others){
                //Skip self
                if(other == agentCollider.gameObject){
                    continue;
                }
                ParameterManager otherParameterManager = other.GetComponent<ParameterManager>();

                // predicted time until nearest approach of "this" and "other"
                float time = predictNearestApproachTime (CurrentDirection, GetCurrentPosition(), CurrentSpeed, otherParameterManager.GetCurrentDirection(), otherParameterManager.GetCurrentPosition(), otherParameterManager.GetCurrentSpeed());
                //Debug.Log("time:"+time);
                if ((time >= 0) && (time < minTimeToCollision)){
                    //Debug.Log("Distance:"+computeNearestApproachPositions (time, CurrentPosition, CurrentDirection, CurrentSpeed, otherParameterManager.GetRawCurrentPosition(), otherParameterManager.GetCurrentDirection(), otherParameterManager.GetCurrentSpeed()));
                    if (computeNearestApproachPositions (time, GetCurrentPosition(), CurrentDirection, CurrentSpeed, otherParameterManager.GetCurrentPosition(), otherParameterManager.GetCurrentDirection(), otherParameterManager.GetCurrentSpeed()) < collisionDangerThreshold)
                    {
                        minTimeToCollision = time;
                        potentialAvoidanceTarget = other;
                    }
                }
            }

            if(potentialAvoidanceTarget != null){
                // parallel: +1, perpendicular: 0, anti-parallel: -1
                float parallelness = Vector3.Dot(CurrentDirection, potentialAvoidanceTarget.GetComponent<ParameterManager>().GetCurrentDirection());
                float angle = 0.707f;

                if (parallelness < -angle)
                {
                    // anti-parallel "head on" paths:
                    // steer away from future threat position
                    Vector3 offset = otherPositionAtNearestApproach - (Vector3)GetCurrentPosition();
                    Vector3 rightVector = Vector3.Cross(CurrentDirection, Vector3.up);

                    float sideDot = Vector3.Dot(offset, rightVector);
                    //If there is the predicted potential collision agent on your right side:SideDot>0, steer should be -1(left side)
                    steer = (sideDot > 0) ? -1.0f : 1.0f;
                }
                else
                {
                    if (parallelness > angle)
                    {
                        // parallel paths: steer away from threat
                        Vector3 offset = potentialAvoidanceTarget.GetComponent<ParameterManager>().GetCurrentPosition() - (Vector3)GetCurrentPosition();
                        Vector3 rightVector = Vector3.Cross(CurrentDirection, Vector3.up);

                        float sideDot = Vector3.Dot(offset, rightVector);
                        steer = (sideDot > 0) ? -1.0f : 1.0f;
                    }
                    else
                    {
                        // perpendicular paths: steer behind threat
                        // (only the slower of the two does this)
                        if(potentialAvoidanceTarget.GetComponent<ParameterManager>().GetCurrentSpeed() <= CurrentSpeed){
                            Vector3 rightVector = Vector3.Cross(CurrentDirection, Vector3.up);
                            float sideDot = Vector3.Dot(rightVector, potentialAvoidanceTarget.GetComponent<ParameterManager>().GetCurrentDirection());
                            steer = (sideDot > 0) ? -1.0f : 1.0f;
                        }
                    }
                }
            }
            return Vector3.Cross(CurrentDirection, Vector3.up) * steer;
        }

        private Vector3 GetWorldPredictedPos(int index)
        {
            return PredictedPositions[index] + transform.position;
        }
        private Vector3 GetWorldPredictedDir(int index)
        {
            return PredictedDirections[index];
        }
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

        public override float3 GetWorldInitPosition()
        {
            return Path[0] + this.transform.position;
        }
        public override float3 GetWorldInitDirection()
        {
            // float2 dir = Path.Length > 0 ? Path[1].Position - Path[0].Position : new float2(0, 1);
            Vector3 dir = Path.Length > 0 ? Path[1] - Path[0] : new Vector3(0, 0, 1);
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
            return CurrentSpeed;
        }

        public void SetCollidedAgent(GameObject _collidedAgent){
            collidedAgent = _collidedAgent;
        }
        
        public void SetOnCollide(bool _onCollide, GameObject _collidedAgent){
            onCollide = _onCollide;
            _collidedAgent.GetComponent<ParameterManager>().SetOnCollide(onCollide);
        }

        public void SetOnMoving(bool _onMoving, GameObject _collidedAgent){
            onMoving = _onMoving;
            _collidedAgent.GetComponent<ParameterManager>().SetOnMoving(onMoving);
        }

        private void DrawInfo(){
            
            Color gizmoColor = new Color(1.0f, 88/255f, 85/255f);
            Draw.WireCylinder((Vector3)GetCurrentPosition(), Vector3.up, agentCollider.height, agentRadius, gizmoColor);

            if(ShowAvoidanceForce){
                gizmoColor = Color.blue;
                Draw.ArrowheadArc((Vector3)GetCurrentPosition(), avoidanceVector, 0.55f, gizmoColor);
            }

            if(ShowCurrentDirection){
                gizmoColor = Color.yellow;
                Draw.ArrowheadArc((Vector3)GetCurrentPosition(), CurrentDirection, 0.55f, gizmoColor);
            }
            
            if(ShowGoalDirection){
                gizmoColor = Color.white;
                Draw.ArrowheadArc((Vector3)GetCurrentPosition(), toGoalVector, 0.55f, gizmoColor);
            }

            if(ShowUnalignedCollisionAvoidance){
                gizmoColor = Color.green;
                Draw.ArrowheadArc((Vector3)GetCurrentPosition(), avoidNeighborsVector, 0.55f, gizmoColor);
            }
        }


    #if UNITY_EDITOR
        private void OnDrawGizmos()
        {

            if (Path == null) return;

            const float heightOffset = 0.01f;

            // Draw KeyPoints
            Gizmos.color = Color.red;
            for (int i = 0; i < Path.Length; i++)
            {
                Vector3 pos = GetWorldPosition(transform, Path[i]);
                Gizmos.DrawSphere(new Vector3(pos.x, heightOffset, pos.z), 0.1f);
            }
            // Draw Path
            Gizmos.color = new Color(0.5f, 0.0f, 0.0f, 1.0f);
            for (int i = 0; i < Path.Length - 1; i++)
            {
                Vector3 pos = GetWorldPosition(transform, Path[i]);
                Vector3 nextPos = GetWorldPosition(transform, Path[i+1]);
                GizmosExtensions.DrawLine(new Vector3(pos.x, heightOffset, pos.z), new Vector3(nextPos.x, heightOffset, nextPos.z), 6);
            }
            // Last Line
            Vector3 lastPos = GetWorldPosition(transform, Path[Path.Length - 1]);
            Vector3 firstPos = GetWorldPosition(transform, Path[0]);
            GizmosExtensions.DrawLine(new Vector3(lastPos.x, heightOffset, lastPos.z), new Vector3(firstPos.x, heightOffset, firstPos.z), 6);
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
            Vector3 lastPos2 = GetWorldPosition(transform, Path[Path.Length - 1]);
            Vector3 firstPos2 = GetWorldPosition(transform,Path[0]);
            Vector3 start2 = new Vector3(lastPos2.x, heightOffset, lastPos2.z);
            Vector3 end2 = new Vector3(firstPos2.x, heightOffset, firstPos2.z);
            GizmosExtensions.DrawArrow(start2, start2 + (end2 - start2).normalized * CurrentSpeed, thickness: 3);

            // Draw Current Position And Direction
            if (!Application.isPlaying) return;
            Gizmos.color = new Color(1.0f, 0.3f, 0.1f, 1.0f);
            Vector3 currentPos = (Vector3)GetCurrentPosition() + Vector3.up * heightOffset * 2;
            Gizmos.DrawSphere(currentPos, 0.1f);
            GizmosExtensions.DrawLine(currentPos, currentPos + (Quaternion)GetCurrentRotation() * Vector3.forward, 12);
            // Draw Prediction
            if (PredictedPositions == null || PredictedPositions.Length != NumberPredictionPos ||
                PredictedDirections == null || PredictedDirections.Length != NumberPredictionRot) return;
            Gizmos.color = new Color(0.6f, 0.3f, 0.8f, 1.0f);
            for (int i = 0; i < NumberPredictionPos; i++)
            {
                Vector3 predictedPosf2 = GetWorldPredictedPos(i);
                Vector3 predictedPos = new Vector3(predictedPosf2.x, heightOffset * 2, predictedPosf2.z);
                Gizmos.DrawSphere(predictedPos, 0.1f);
                Vector3 dirf2 = GetWorldPredictedDir(i);
                GizmosExtensions.DrawLine(predictedPos, predictedPos + new Vector3(dirf2.x, 0.0f, dirf2.z) * 0.5f, 12);
            }
        }
    #endif
    }
}
