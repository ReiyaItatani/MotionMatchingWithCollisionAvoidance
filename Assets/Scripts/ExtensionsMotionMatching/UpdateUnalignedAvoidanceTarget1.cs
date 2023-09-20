using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

public class UpdateUnalignedAvoidanceTarget : MonoBehaviour
{
    private PathController pathCharacterController;
    private CapsuleCollider myCapsuleCollider;
    public List<GameObject> othersInUnalignedAvoidanceArea;

    
    void Awake(){
        pathCharacterController = this.transform.parent.GetComponent<PathController>();
        myCapsuleCollider = pathCharacterController.agentCollider;
        othersInUnalignedAvoidanceArea = new List<GameObject>();
    }

    void OnTriggerStay(Collider other)
    {
        if(pathCharacterController == null) return;
        if(!other.Equals(myCapsuleCollider) && other.gameObject.CompareTag("Agent")) 
        {
            if (!othersInUnalignedAvoidanceArea.Contains(other.gameObject))
            {
                othersInUnalignedAvoidanceArea.Add(other.gameObject);
            }
        }   
    }

    void OnTriggerExit(Collider other)
    {
        if(pathCharacterController == null) return;
        if(!other.Equals(myCapsuleCollider) && other.gameObject.CompareTag("Agent")){
            if (othersInUnalignedAvoidanceArea.Contains(other.gameObject))
            {
                othersInUnalignedAvoidanceArea.Remove(other.gameObject);
            }
        }
    }

    public List<GameObject> GetOthersInUnalignedAvoidanceArea(){
        return othersInUnalignedAvoidanceArea;
    }
}
