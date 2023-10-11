using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

public class UpdateUnalignedAvoidanceTarget : MonoBehaviour
{
    private PathController pathController;
    private CapsuleCollider myAgentCollider;
    private CapsuleCollider myGroupCollider;
    [ReadOnly]
    public List<GameObject> othersInUnalignedAvoidanceArea = new List<GameObject>();

    void Update(){
        UnalignedAvoidanceTargetActiveChecker();
    }

    void OnTriggerStay(Collider other)
    {
        if(pathController == null) return;
        if(!other.Equals(myAgentCollider) && other.gameObject.CompareTag("Agent") || 
           !other.Equals(myGroupCollider) && other.gameObject.CompareTag("Group"))
        {
            if (!othersInUnalignedAvoidanceArea.Contains(other.gameObject))
            {
                othersInUnalignedAvoidanceArea.Add(other.gameObject);
            }
        }   
    }

    void OnTriggerExit(Collider other)
    {
        if(pathController == null) return;
        if(!other.Equals(myAgentCollider) && other.gameObject.CompareTag("Agent") || 
           !other.Equals(myGroupCollider) && other.gameObject.CompareTag("Group")){
            if (othersInUnalignedAvoidanceArea.Contains(other.gameObject))
            {
                othersInUnalignedAvoidanceArea.Remove(other.gameObject);
            }
        }
    }

    public List<GameObject> GetOthersInUnalignedAvoidanceArea(){
        return othersInUnalignedAvoidanceArea;
    }

    private void UnalignedAvoidanceTargetActiveChecker(){
        othersInUnalignedAvoidanceArea.RemoveAll(gameObject => !gameObject.activeInHierarchy);
    }
    
    public void InitParameter(PathController _pathController, CapsuleCollider _myAgentCollider, CapsuleCollider _myGroupCollider){
        pathController = _pathController;
        myAgentCollider = _myAgentCollider;
        myGroupCollider = _myGroupCollider;
    }
}
