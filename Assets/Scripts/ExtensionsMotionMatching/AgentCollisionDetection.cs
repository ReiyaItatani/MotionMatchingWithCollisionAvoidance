using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

public class AgentCollisionDetection : MonoBehaviour
{
    private PathController pathController;
    private CapsuleCollider capsuleCollider;
    private float height;
    private float currentSpeed;
    private bool onWaiting = false;

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
                if(onWaiting == false){
                    StartCoroutine(WaitTime(3.0f, collider.gameObject));
                }
            }
        }
    }

    public void InitParameter(PathController _pathController, CapsuleCollider _capsuleCollider){
        pathController = _pathController;
        capsuleCollider = _capsuleCollider;
    }

    public IEnumerator WaitTime(float time, GameObject collidedAgent)
    {
        onWaiting = true;
        pathController.SetOnWaiting(onWaiting, collidedAgent);
        yield return new WaitForSeconds(time);
        onWaiting = false;
        pathController.SetOnWaiting(onWaiting, collidedAgent);
        yield return null;
    }

    public bool GetWaiting(){
        return onWaiting;
    }

}
