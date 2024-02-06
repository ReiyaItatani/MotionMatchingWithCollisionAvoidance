using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

namespace CollisionAvoidance{

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

    // Checks each GameObject in othersInAvoidanceArea to determine if it should be removed.
    private void AvoidanceTargetActiveChecker(){
        // Remove GameObject from the list if it's not active in the hierarchy,
        // or if its CapsuleCollider is not enabled.
        othersInAvoidanceArea.RemoveAll(gameObject =>
            !gameObject.activeInHierarchy || !IsCapsuleColliderActive(gameObject)
        );
    }

    // Determines if the CapsuleCollider component of the given GameObject is active and enabled.
    private bool IsCapsuleColliderActive(GameObject obj) {
        // Retrieve the CapsuleCollider component from the GameObject.
        CapsuleCollider capsuleCollider = obj.GetComponent<CapsuleCollider>();

        // Return true if MeshCollider is not null and is enabled, otherwise return false.
        return capsuleCollider != null && capsuleCollider.enabled;
    }

    public void InitParameter(CapsuleCollider _myAgentCollider, CapsuleCollider _myGroupCollider){
        myAgentCollider = _myAgentCollider;
        myGroupCollider = _myGroupCollider;
    }
}
}