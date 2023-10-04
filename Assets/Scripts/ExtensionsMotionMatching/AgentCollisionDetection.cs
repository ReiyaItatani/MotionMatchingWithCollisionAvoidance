using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;
using Unity.VisualScripting;

[RequireComponent(typeof(UnityEngine.CapsuleCollider))]
public class AgentCollisionDetection : MonoBehaviour
{
    private PathController pathController;
    private CapsuleCollider capsuleCollider;
    private MotionMatchingSkinnedMeshRendererWithOCEAN motionMatchingSkinnedMeshRendererWithOCEAN;
    private bool onCollide = false;
    private bool onMoving = false;
    public SocialBehaviour socialBehaviour;

    //For checking what happend when collide
    [HideInInspector]
    public Camera collisionDetectionCam;

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
            pathController.SetOnCollide(onCollide);
            pathController.SetOnMoving(onMoving);
            pathController.SetCollidedAgent(collider.gameObject);
            if(socialBehaviour != null && 
            motionMatchingSkinnedMeshRendererWithOCEAN != null) StartCoroutine(WaitTime(Random.Range(3f, 7f), collider.gameObject));
            
            //CamPos For Debugging(Delete here)
            if(collisionDetectionCam != null){
                ChangeCamPosChecker changeCamPosChecker = collisionDetectionCam.GetComponent<ChangeCamPosChecker>();
                if(changeCamPosChecker.ChangeCamPos == false){
                    changeCamPosChecker.ChangeCamPos = true;
                    collisionDetectionCam.transform.position = collider.transform.position;
                    Vector3 offset = new Vector3(-0.4f,1.7f,-2.5f);
                    collisionDetectionCam.transform.position += offset;
                    StartCoroutine(DurationAfterCamPosChange(changeCamPosChecker, 5.0f));
                }
            }
        }
    }

    private IEnumerator DurationAfterCamPosChange(ChangeCamPosChecker changeCamPosChecker, float duration){
        yield return new WaitForSeconds(duration);
        changeCamPosChecker.ChangeCamPos = false;
        yield return null;
    }

    public void InitParameter(PathController _pathController, CapsuleCollider _capsuleCollider){
        pathController = _pathController;
        capsuleCollider = _capsuleCollider;
    }

    public IEnumerator WaitTime(float time, GameObject _collidedAgent)
    {
        //Start wait
        onCollide = true;
        pathController.SetOnCollide(onCollide);
        //Look at
        socialBehaviour.LookAtTarget(_collidedAgent);
        //Start talk
        socialBehaviour.TryPlayAudio();
        //Start Animation
        socialBehaviour.TriggerUnityAnimation();
        yield return new WaitForSeconds(time/2.0f);

        //Look at forward
        socialBehaviour.DeleteLookObject();
        //Stop Animaiton
        socialBehaviour.FollowMotionMacthing();
        //StartMove
        onMoving = true;
        pathController.SetOnMoving(onMoving);
        yield return new WaitForSeconds(time - time/2.0f);

        //Back to normal
        onCollide = false;
        onMoving = false;
        pathController.SetOnCollide(onCollide);
        pathController.SetOnMoving(onMoving);
        yield return null;
    }
}
