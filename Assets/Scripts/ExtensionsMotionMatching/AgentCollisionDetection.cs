using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

public class AgentCollisionDetection : MonoBehaviour
{
    private PathController pathController;
    private CapsuleCollider capsuleCollider;
    private MotionMatchingSkinnedMeshRendererWithOCEAN motionMatchingSkinnedMeshRendererWithOCEAN;
    private bool onCollide = false;
    private bool onMoving = false;

    public RandomAudioPlay randomAudioPlay;

    void Start(){
        if(this.gameObject.GetComponent<MotionMatchingSkinnedMeshRendererWithOCEAN>()!=null){
            motionMatchingSkinnedMeshRendererWithOCEAN = this.gameObject.GetComponent<MotionMatchingSkinnedMeshRendererWithOCEAN>();
        }
        randomAudioPlay = this.gameObject.GetComponentInChildren<RandomAudioPlay>();
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
        if (collider.gameObject.tag == "Agent")
        {
            if(pathController!=null){
                onCollide = false;
                onMoving = false;
                pathController.SetOnCollide(onCollide, collider.gameObject);
                pathController.SetOnMoving(onMoving, collider.gameObject);
                pathController.SetCollidedAgent(collider.gameObject);
                StartCoroutine(WaitTime(Random.Range(3f, 7f), collider.gameObject));
            }
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
        if(motionMatchingSkinnedMeshRendererWithOCEAN != null){
            motionMatchingSkinnedMeshRendererWithOCEAN.lookObject = _collidedAgent;
        }
        //Start talk
        if(randomAudioPlay!=null){
            randomAudioPlay.TryPlayAudio();
        }

        yield return new WaitForSeconds(time/4f);

        if(motionMatchingSkinnedMeshRendererWithOCEAN != null){
            motionMatchingSkinnedMeshRendererWithOCEAN.lookObject = null;
        }
        onMoving = true;
        pathController.SetOnMoving(onMoving, _collidedAgent);
        yield return new WaitForSeconds(time - time/4f);
        //Back to normal
        onCollide = false;
        onMoving = false;
        pathController.SetOnCollide(onCollide, _collidedAgent);
        pathController.SetOnMoving(onMoving, _collidedAgent);
        yield return null;
    }

    public bool GetCollide(){
        return onCollide;
    }

}
