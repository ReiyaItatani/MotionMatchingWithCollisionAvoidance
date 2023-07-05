using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Collections;

namespace MotionMatching{
    using TrajectoryFeature = MotionMatchingData.TrajectoryFeature;

    public class PathController : MotionMatchingCharacterController
    {
        public string TrajectoryPositionFeatureName = "FuturePosition";
        public string TrajectoryDirectionFeatureName = "FutureDirection";
        public Vector3[] Path;
        private Vector3 CurrentPosition;
        private Vector3 CurrentDirection;
        private Vector3[] PredictedPositions;
        private Vector3[] PredictedDirections;
        [Range (1f, 3f)]
        public float CurrentSpeed = 1.0f;
        private float MinSpeed = 1.0f;
        // Features -----------------------------------------------------------------
        private int TrajectoryPosFeatureIndex;
        private int TrajectoryRotFeatureIndex;
        private int[] TrajectoryPosPredictionFrames;
        private int[] TrajectoryRotPredictionFrames;
        private int NumberPredictionPos { get { return TrajectoryPosPredictionFrames.Length; } }
        private int NumberPredictionRot { get { return TrajectoryRotPredictionFrames.Length; } }
        // --------------------------------------------------------------------------
        // Collision Avoidance ------------------------------------------------------
        public CapsuleCollider agentCollider;
        public BoxCollider avoidanceCollider;
        private float agentRadius;
        private Vector3 avoidanceArea;
        private float avoidanceWeight = 1.5f;
        private Vector3 avoidanceVector = Vector3.zero;
        // --------------------------------------------------------------------------
        // To Goal Direction --------------------------------------------------------
        private Vector3 toGoalVector = Vector3.zero;
        private float toGoalWeight = 1.7f;
        public Vector3 CurrentGoal;
        private int CurrentGoalIndex = 0;
        [SerializeField]
        private float goalRadius = 0.5f;
        [SerializeField]
        private float slowingRadius = 2.0f;
        // --------------------------------------------------------------------------
        // Unalligned Collision Avoidance -------------------------------------------
        private Vector3 avoidNeighborsVector = Vector3.zero;
        private float avoidNeighborWeight = 1.0f;
        // --------------------------------------------------------------------------

        private void Start()
        {
            //init
            agentRadius = agentCollider.radius;
            avoidanceArea = avoidanceCollider.size;
            CurrentGoal = Path[0];

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
            // TODO: generalize this, allow for different number of prediction frames
            Debug.Assert(TrajectoryPosPredictionFrames.Length == TrajectoryRotPredictionFrames.Length, "Trajectory Position and Trajectory Direction Prediction Frames must be the same for PathCharacterController");
            for (int i = 0; i < TrajectoryPosPredictionFrames.Length; ++i)
            {
                Debug.Assert(TrajectoryPosPredictionFrames[i] == TrajectoryRotPredictionFrames[i], "Trajectory Position and Trajectory Direction Prediction Frames must be the same for PathCharacterController");
            }


            PredictedPositions = new Vector3[NumberPredictionPos];
            PredictedDirections = new Vector3[NumberPredictionRot];
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
        }

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
        }

        private void SimulatePath(float time, Vector3 currentPosition, out Vector3 nextPosition, out Vector3 direction)
        {
            //Update To Goal Vector
            toGoalVector = (CurrentGoal - currentPosition).normalized;
            direction = (toGoalWeight*toGoalVector).normalized;
            //gradually decrease speed of the agent if it is close to the goal:
            float distanceToGoal = Vector3.Distance(currentPosition, CurrentGoal);
            CurrentSpeed = distanceToGoal < slowingRadius ? Mathf.Lerp(MinSpeed, CurrentSpeed, distanceToGoal / slowingRadius) : CurrentSpeed;
            nextPosition = currentPosition + direction * CurrentSpeed * time;
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
                    Vector3 local = character.InverseTransformPoint(new Vector3(world.x, 0.0f, world.y));
                    output[0] = local.x;
                    output[1] = local.z;
                    break;
                case TrajectoryFeature.Type.Direction:
                    Vector3 worldDir = GetWorldPredictedDir(index);
                    Vector3 localDir = character.InverseTransformDirection(new Vector3(worldDir.x, 0.0f, worldDir.y));
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
            return transform.position;
        }
        public override float3 GetWorldInitDirection()
        {
            // float2 dir = Path.Length > 0 ? Path[1].Position - Path[0].Position : new float2(0, 1);
            Vector3 dir = Path.Length > 0 ? Path[1] - Path[0] : new Vector3(0, 0, 1);
            return dir.normalized;
        }


        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            GizmosExtensions.DrawArrow(CurrentPosition, CurrentPosition + CurrentDirection* CurrentSpeed);      

            Gizmos.DrawSphere(CurrentPosition, 0.5f);

            for (int i = 0; i < Path.Length; i++)
            {
                Gizmos.color = Color.red;
                Vector3 gizmoPosition = Path[i] - new Vector3(0, 1, 0);
                Gizmos.DrawSphere(gizmoPosition, 0.5f);

                // Draw slowing radius around the goal in 2D
                Gizmos.color = Color.yellow;
                DrawCircle(gizmoPosition, slowingRadius);

                // Draw goal radius around the goal in 2D
                Gizmos.color = Color.cyan;
                DrawCircle(gizmoPosition, goalRadius);
            }
        }

        private void DrawCircle(Vector3 center, float radius)
        {
            float theta = 0;
            float x = radius * Mathf.Cos(theta);
            float y = radius * Mathf.Sin(theta);
            Vector3 pos = center + new Vector3(x, 0, y);
            Vector3 newPos = pos;
            Vector3 lastPos = pos;
            for (theta = 0.1f; theta < Mathf.PI * 2; theta += 0.1f)
            {
                x = radius * Mathf.Cos(theta);
                y = radius * Mathf.Sin(theta);
                newPos = center + new Vector3(x, 0, y);
                Gizmos.DrawLine(pos, newPos);
                pos = newPos;
            }
            // Draw the final line segment
            Gizmos.DrawLine(pos, lastPos);
        }
    }
}
