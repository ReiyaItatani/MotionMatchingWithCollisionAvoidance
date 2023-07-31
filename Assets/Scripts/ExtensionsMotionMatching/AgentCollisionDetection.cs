using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

public class AgentCollisionDetection : MonoBehaviour
{
    private PathController pathController;
    private CapsuleCollider capsuleCollider;
    private bool onCollide = false;
    private bool onMoving = false;

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
                if(onCollide == false){
                    pathController.SetCollidedAgent(collider.gameObject);
                    StartCoroutine(WaitTime(Random.Range(1f, 5f), collider.gameObject));
                }
            }
        }
    }

    public void InitParameter(PathController _pathController, CapsuleCollider _capsuleCollider){
        pathController = _pathController;
        capsuleCollider = _capsuleCollider;
    }

    public IEnumerator WaitTime(float time, GameObject _collidedAgent)
    {
        //waitstart
        onCollide = true;
        pathController.SetOnCollide(onCollide, _collidedAgent);
        yield return new WaitForSeconds(time/4f);
        //talkstart
        onMoving = true;
        pathController.SetOnMoving(onMoving, _collidedAgent);
        yield return new WaitForSeconds(time - time/4f);
        //backtonormal
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
