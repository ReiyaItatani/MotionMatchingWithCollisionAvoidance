using System.Collections;
using UnityEngine;
using MotionMatching;

namespace CollisionAvoidance{

/// <summary>
/// Handles collision detection and responses for an agent in a virtual environment.
/// This class is responsible for detecting collisions with other agents and walls, 
/// and managing the agent's behavior in response to these collisions. 
/// It utilizes a CapsuleCollider for collision detection and interacts with 
/// both the PathController and SocialBehaviour components to adjust the agent's 
/// movement and social interactions based on collisions.
/// </summary>
[RequireComponent(typeof(CapsuleCollider))]
public class AgentCollisionDetection : MonoBehaviour
{
    private const float minReactionTime = 2f;
    private const float maxReactionTime = 4f;

    [Header("Collision Handling Parameters")]
    private ParameterManager parameterManager;
    private CapsuleCollider capsuleCollider;
    private bool isColliding = false;
    public SocialBehaviour socialBehaviour;

    [Header("Repulsion Force Parameters")]
    private GameObject currentWallTarget;

    //For subscribe trigger event
    public delegate void TriggerEvent(Collider other);
    public event TriggerEvent OnEnterTrigger;

    void Awake()
    {
        socialBehaviour  = GetComponent<SocialBehaviour>();
        parameterManager = GetComponent<ParameterManager>();
        capsuleCollider  = GetComponent<CapsuleCollider>();


        if (socialBehaviour == null)
        {
            Debug.LogError("SocialBehaviour component not found on this GameObject.");
        }
    }

    void Update()
    {
        if (parameterManager != null)
        {
            Vector3 currentPosition = parameterManager.GetCurrentPosition();
            Vector3 offset = new Vector3(currentPosition.x - transform.position.x, capsuleCollider.center.y, currentPosition.z - transform.position.z);
            capsuleCollider.center = offset;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agent") && isColliding == false)
        {
            OnEnterTrigger?.Invoke(other);
            StartCoroutine(ReactionToCollision(Random.Range(minReactionTime, maxReactionTime), other.gameObject));
        }
        else if (other.CompareTag("Wall"))
        {
            currentWallTarget = other.gameObject;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            currentWallTarget = null;
        }
    }

    /// <summary>
    /// Coroutine that handles the agent's reaction when a collision occurs.
    /// </summary>
    /// <param name="time">Time duration of the reaction.</param>
    /// <param name="collidedAgent">The agent that was collided with.</param>
    /// <returns></returns>
    public IEnumerator ReactionToCollision(float time, GameObject collidedAgent)
    {
        isColliding = true;
        yield return new WaitForSeconds(time);
        isColliding = false;
    }

    /// <summary>
    /// Returns the current wall target the agent is interacting with.
    /// </summary>
    /// <returns>Current wall target GameObject.</returns>
    public GameObject GetCurrentWallTarget()
    {
        return currentWallTarget;
    }

    /// <summary>
    /// Returns the current look-at position of the social behaviour.
    /// </summary>
    /// <returns>Current look-at position as a Vector3.</returns>
    public Vector3 GetCurrentLookAt()
    {
        return socialBehaviour.GetCurrentLookAt();
    }
}
}