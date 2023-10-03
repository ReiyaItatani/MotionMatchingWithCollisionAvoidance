using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

public class UpdateAvoidanceTarget : MonoBehaviour
{
    private PathController pathCharacterController;
    private CapsuleCollider colliderMySelf;
    private CapsuleCollider colliderMyGroup;
    
    void Start(){
        pathCharacterController = this.transform.parent.GetComponent<PathController>();
        colliderMySelf = pathCharacterController.agentCollider;
        colliderMyGroup = pathCharacterController.groupCollider;
    }
    void OnTriggerStay(Collider other)
    {
        if(pathCharacterController == null) return;
        if(!other.Equals(colliderMySelf) && other.gameObject.CompareTag("Agent") || 
           !other.Equals(colliderMyGroup) && other.gameObject.CompareTag("Group")) 
        {
            if (pathCharacterController.CurrentAvoidanceTarget == null || Vector3.Distance(pathCharacterController.GetCurrentPosition(), pathCharacterController.CurrentAvoidanceTarget.transform.position) > Vector3.Distance(pathCharacterController.GetCurrentPosition(), other.transform.position))
            {
                pathCharacterController.CurrentAvoidanceTarget = other.gameObject;
            }
        }   
    }

    void OnTriggerExit(Collider other)
    {
        if(pathCharacterController == null) return;
        if (pathCharacterController.CurrentAvoidanceTarget!=null && pathCharacterController.CurrentAvoidanceTarget.Equals(other.gameObject))
        {
            pathCharacterController.CurrentAvoidanceTarget = null;
        }
    }
}
