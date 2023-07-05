using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

public class UpdateAvoidanceTarget : MonoBehaviour
{
    public PathCharacterController pathCharacterController;
    void OnTriggerStay(Collider other)
    {
        if(other is CapsuleCollider && other.gameObject.CompareTag("Agent")) 
        {
            if (pathCharacterController.CurrentAvoidanceTarget == null || Vector3.Distance(transform.position, pathCharacterController.CurrentAvoidanceTarget.transform.position) > Vector3.Distance(transform.position, other.transform.position))
            {
                pathCharacterController.CurrentAvoidanceTarget = other.gameObject;
            }
        }   
    }

    void OnTriggerExit(Collider other)
    {
        if (pathCharacterController.CurrentAvoidanceTarget!=null && pathCharacterController.CurrentAvoidanceTarget.Equals(other.gameObject))
        {
            pathCharacterController.CurrentAvoidanceTarget = null;
        }
    }
}
