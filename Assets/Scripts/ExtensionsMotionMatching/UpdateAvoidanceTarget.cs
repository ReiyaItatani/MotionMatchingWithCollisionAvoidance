using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

public class UpdateAvoidanceTarget : MonoBehaviour
{

    private CapsuleCollider myAgentCollider;
    private CapsuleCollider myGroupCollider;
    [ReadOnly]
    public List<GameObject> othersInAvoidanceArea = new List<GameObject>();

    private void Update(){
        AvoidanceTargetActiveChecker();
    }

    void OnTriggerStay(Collider other)
    {
        if(!other.Equals(myAgentCollider) && other.gameObject.CompareTag("Agent") || 
           !other.Equals(myGroupCollider) && other.gameObject.CompareTag("Group")) 
        {
            if (!othersInAvoidanceArea.Contains(other.gameObject))
            {
                othersInAvoidanceArea.Add(other.gameObject);
            }
        }   
    }

    void OnTriggerExit(Collider other)
    {
        if(!other.Equals(myAgentCollider) && other.gameObject.CompareTag("Agent") || 
           !other.Equals(myGroupCollider) && other.gameObject.CompareTag("Group")){
            if (othersInAvoidanceArea.Contains(other.gameObject))
            {
                othersInAvoidanceArea.Remove(other.gameObject);
            }
        }
    }

    public List<GameObject> GetOthersInAvoidanceArea(){
        return othersInAvoidanceArea;
    }

    //Group Colldier wil be inactive so in that case this will help 
    private void AvoidanceTargetActiveChecker(){
        othersInAvoidanceArea.RemoveAll(gameObject => !gameObject.activeInHierarchy);
    }

    public void InitParameter(CapsuleCollider _myAgentCollider, CapsuleCollider _myGroupCollider){
        myAgentCollider = _myAgentCollider;
        myGroupCollider = _myGroupCollider;
    }
}