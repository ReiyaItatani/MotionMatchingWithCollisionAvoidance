using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

[RequireComponent(typeof(UnityEngine.CapsuleCollider))]
public class AgentCollisionDetection : MonoBehaviour
{
    private PathController pathController;
    private CapsuleCollider capsuleCollider;
    private MotionMatchingSkinnedMeshRendererWithOCEAN motionMatchingSkinnedMeshRendererWithOCEAN;
    private bool onCollide = false;
    private bool onMoving = false;

    public SocialBehaviour socialBehaviour;

    void Awake(){
        motionMatchingSkinnedMeshRendererWithOCEAN = this.gameObject.GetComponent<MotionMatchingSkinnedMeshRendererWithOCEAN>();
        socialBehaviour = this.gameObject.GetComponent<SocialBehaviour>();
    }

    void Update(){
        if(pathController!=null){
            Vector3 currentPosition = pathController.GetCurrentPosition();
            Vector3 difference = new Vector3(currentPosition.x - this.transform.position.x, capsuleCollider.center.y, currentPosition.z - this.transform.position.z);
            capsuleCollider.center = difference;
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        // Check if the object we collided with has the "Agent" tag
        if (collider.gameObject.tag == "Agent" && pathController!=null)
        {
            onCollide = false;
            onMoving = false;
            pathController.SetOnCollide(onCollide, collider.gameObject);
            pathController.SetOnMoving(onMoving, collider.gameObject);
            pathController.SetCollidedAgent(collider.gameObject);
            if(socialBehaviour != null && 
            motionMatchingSkinnedMeshRendererWithOCEAN != null) StartCoroutine(WaitTime(Random.Range(3f, 7f), collider.gameObject));
        }
    }

    public void InitParameter(PathController _pathController, CapsuleCollider _capsuleCollider){
        pathController = _pathController;
        capsuleCollider = _capsuleCollider;
    }

    public IEnumerator WaitTime(float time, GameObject _collidedAgent)
    {
        //Start wait
        onCollide = true;
        pathController.SetOnCollide(onCollide, _collidedAgent);
        //Look at
        socialBehaviour.LookAtTarget(_collidedAgent);
        //Start talk
        socialBehaviour.TryPlayAudio();
        //Start Animation
        socialBehaviour.TriggerUnityAnimation();
        yield return new WaitForSeconds(time/2.0f);

        //Look at forward
        socialBehaviour.LookForward();
        //Stop Animaiton
        socialBehaviour.FollowMotionMacthing();
        //StartMove
        onMoving = true;
        pathController.SetOnMoving(onMoving, _collidedAgent);
        yield return new WaitForSeconds(time - time/2.0f);

        //Back to normal
        onCollide = false;
        onMoving = false;
        pathController.SetOnCollide(onCollide, _collidedAgent);
        pathController.SetOnMoving(onMoving, _collidedAgent);
        yield return null;
    }
}
