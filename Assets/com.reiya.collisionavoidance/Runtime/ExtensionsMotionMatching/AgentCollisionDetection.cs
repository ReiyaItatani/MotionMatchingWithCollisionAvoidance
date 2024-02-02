using System.Collections;
using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// Handles collision detection and response for an agent in a virtual environment.
    /// Utilizes a CapsuleCollider for collision detection and interacts with
    /// the ParameterManager to adjust the agent's movement based on collisions.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public class AgentCollisionDetection : MonoBehaviour
    {
        private const float MinReactionTime = 2f;
        private const float MaxReactionTime = 4f;
        private const string AgentTag = "Agent";
        private const string WallTag = "Wall";

        [Header("Collision Handling Parameters")]
        private ParameterManager parameterManager;
        private CapsuleCollider capsuleCollider;
        // private bool isColliding;

        [Header("Repulsion Force Parameters")]
        private GameObject currentWallTarget;

        public delegate void TriggerEvent(Collider other);
        public event TriggerEvent OnEnterTrigger;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Update()
        {
            UpdateColliderCenterToMatchParameterManager();
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleCollisionEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            HandleCollisionExit(other);
        }

        public GameObject GetCurrentWallTarget()
        {
            return currentWallTarget;
        }

        private void InitializeComponents()
        {
            parameterManager = GetComponent<ParameterManager>();
            capsuleCollider = GetComponent<CapsuleCollider>();
        }

        private void UpdateColliderCenterToMatchParameterManager()
        {
            if (parameterManager == null) return;

            Vector3 currentPosition = parameterManager.GetCurrentPosition();
            Vector3 offset = new Vector3(currentPosition.x - transform.position.x, capsuleCollider.center.y, currentPosition.z - transform.position.z);
            capsuleCollider.center = offset;
        }

        private void HandleCollisionEnter(Collider other)
        {
            if (other.CompareTag(AgentTag))
            {
                OnEnterTrigger?.Invoke(other);
                //StartCoroutine(ReactToCollision(Random.Range(MinReactionTime, MaxReactionTime), other.gameObject));
            }
            else if (other.CompareTag(WallTag))
            {
                currentWallTarget = other.gameObject;
            }
        }

        private void HandleCollisionExit(Collider other)
        {
            if (other.CompareTag(WallTag))
            {
                currentWallTarget = null;
            }
        }

        // private IEnumerator ReactToCollision(float time, GameObject collidedAgent)
        // {
        //     isColliding = true;
        //     yield return new WaitForSeconds(time);
        //     isColliding = false;
        // }
    }
}
