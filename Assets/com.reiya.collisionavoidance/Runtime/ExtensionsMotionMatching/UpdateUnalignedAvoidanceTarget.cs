using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

namespace CollisionAvoidance{
public class UpdateAnticipatedAvoidanceTarget : MonoBehaviour
{
    private CapsuleCollider myAgentCollider;
    private CapsuleCollider myGroupCollider;
    [ReadOnly]
    public List<GameObject> othersInAnticipatedAvoidanceArea = new List<GameObject>();

    void Update(){
        AnticipatedAvoidanceTargetActiveChecker();
    }

    void OnTriggerStay(Collider other)
    {
        if(!other.Equals(myAgentCollider) && other.gameObject.CompareTag("Agent") || 
           !other.Equals(myGroupCollider) && other.gameObject.CompareTag("Group"))
        {
            if (!othersInAnticipatedAvoidanceArea.Contains(other.gameObject))
            {
                othersInAnticipatedAvoidanceArea.Add(other.gameObject);
            }
        }   
    }

    void OnTriggerExit(Collider other)
    {
        if(!other.Equals(myAgentCollider) && other.gameObject.CompareTag("Agent") || 
           !other.Equals(myGroupCollider) && other.gameObject.CompareTag("Group")){
            if (othersInAnticipatedAvoidanceArea.Contains(other.gameObject))
            {
                othersInAnticipatedAvoidanceArea.Remove(other.gameObject);
            }
        }
    }

    public List<GameObject> GetOthersInAnticipatedAvoidanceArea(){
        return othersInAnticipatedAvoidanceArea;
    }

    private void AnticipatedAvoidanceTargetActiveChecker(){
        othersInAnticipatedAvoidanceArea.RemoveAll(gameObject => !gameObject.activeInHierarchy);
    }
    
    public void InitParameter(CapsuleCollider _myAgentCollider, CapsuleCollider _myGroupCollider){
        myAgentCollider = _myAgentCollider;
        myGroupCollider = _myGroupCollider;
    }
}
}
