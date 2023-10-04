using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

public class UpdateUnalignedAvoidanceTarget : MonoBehaviour
{
    private PathController pathCharacterController;
    private CapsuleCollider myCapsuleCollider;
    public List<GameObject> othersInUnalignedAvoidanceArea;
    private CapsuleCollider colliderMyGroup;

    
    void Awake(){
        pathCharacterController = this.transform.parent.GetComponent<PathController>();
        myCapsuleCollider = pathCharacterController.agentCollider;
        colliderMyGroup = pathCharacterController.groupCollider;
        othersInUnalignedAvoidanceArea = new List<GameObject>();
    }

    void Update(){
        CheckUnalignedAvoidanceTarget();
    }

    void OnTriggerStay(Collider other)
    {
        if(pathCharacterController == null) return;
        if(!other.Equals(myCapsuleCollider) && other.gameObject.CompareTag("Agent") || 
           !other.Equals(colliderMyGroup) && other.gameObject.CompareTag("Group"))
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
        if(!other.Equals(myCapsuleCollider) && other.gameObject.CompareTag("Agent") || 
           !other.Equals(colliderMyGroup) && other.gameObject.CompareTag("Group")){
            if (othersInUnalignedAvoidanceArea.Contains(other.gameObject))
            {
                othersInUnalignedAvoidanceArea.Remove(other.gameObject);
            }
        }
    }

    public List<GameObject> GetOthersInUnalignedAvoidanceArea(){
        return othersInUnalignedAvoidanceArea;
    }

    private void CheckUnalignedAvoidanceTarget(){
        othersInUnalignedAvoidanceArea.RemoveAll(gameObject => !gameObject.activeInHierarchy);
    }
}
