using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

public class UpdateAvoidanceTarget : MonoBehaviour
{
    private PathController pathController;
    private CapsuleCollider myAgentCollider;
    private CapsuleCollider myGroupCollider;
    private GameObject currentAvoidanceTarget;

    private void Update(){
        AvoidanceTargetActiveChecker();
    }

    void OnTriggerStay(Collider other)
    {
        if(pathController == null) return;
        if(!other.Equals(myAgentCollider) && other.gameObject.CompareTag("Agent") || 
           !other.Equals(myGroupCollider) && other.gameObject.CompareTag("Group")) 
        {
            if (pathController.CurrentAvoidanceTarget == null || Vector3.Distance(pathController.GetCurrentPosition(), pathController.CurrentAvoidanceTarget.transform.position) > Vector3.Distance(pathController.GetCurrentPosition(), other.transform.position))
            {
                pathController.CurrentAvoidanceTarget = other.gameObject;
                currentAvoidanceTarget = other.gameObject;
            }
        }   
    }

    void OnTriggerExit(Collider other)
    {
        if(pathController == null) return;
        if (pathController.CurrentAvoidanceTarget!=null && pathController.CurrentAvoidanceTarget.Equals(other.gameObject))
        {
            currentAvoidanceTarget = null;
            pathController.CurrentAvoidanceTarget = null;
        }
    }

    //Group Colldier wil be inactive so in that case this will help 
    private void AvoidanceTargetActiveChecker(){
        if(currentAvoidanceTarget != null){
            if(!currentAvoidanceTarget.activeInHierarchy){
                currentAvoidanceTarget = null;
                pathController.CurrentAvoidanceTarget = null;
            }
        }
    }

    public void InitParameter(PathController _pathController, CapsuleCollider _myAgentCollider, CapsuleCollider _myGroupCollider){
        pathController = _pathController;
        myAgentCollider = _myAgentCollider;
        myGroupCollider = _myGroupCollider;
    }
}
