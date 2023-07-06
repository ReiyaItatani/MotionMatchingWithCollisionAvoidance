using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

public class ParameterManager : MonoBehaviour
{
    public PathController pathController;
    public Vector3 GetCurrentDirection(){
        return pathController.GetCurrentDirection();
    }
    public Vector3 GetRawCurrentPosition(){
        return pathController.GetRawCurrentPosition();
    }
    public Vector3 GetCurrentPosition(){
        return pathController.GetCurrentPosition();
    }
    public float GetCurrentSpeed(){
        return pathController.GetCurrentSpeed();
    }
}
