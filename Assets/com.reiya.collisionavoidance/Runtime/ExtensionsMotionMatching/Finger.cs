using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace CollisionAvoidance{

public class Finger : MonoBehaviour
{
    private RightHandRotModifier handController;
    private int handIndex;

    public void Init(RightHandRotModifier _handController, int _handIndex){
        handController = _handController;
        //0: Thumb, 1:Index, 2:Middle, 3:Ring, 4:Little
        handIndex = _handIndex;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(handController && other.gameObject.CompareTag("Object")){
            handController.StopBendingFinger(handIndex);
        }
    }
}
}