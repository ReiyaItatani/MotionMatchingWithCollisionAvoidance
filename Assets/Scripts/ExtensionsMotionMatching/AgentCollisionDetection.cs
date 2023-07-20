using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

public class AgentCollisionDetection : MonoBehaviour
{
    private PathController pathController;
    private float currentSpeed;
    private bool onWaiting = false;

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

    public void SetPathController(PathController _pathController){
        pathController = _pathController;
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
