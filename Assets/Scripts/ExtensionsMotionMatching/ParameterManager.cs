using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

public class ParameterManager : MonoBehaviour
{
    public PathController pathController;
    private bool onCollide = false;
    private bool onMoving = false;
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

    public void SetOnCollide(bool _onCollide){
        onCollide = _onCollide;
    }

    public void SetOnMoving(bool _onMoving){
        onMoving = _onMoving;
    }

    public bool GetoOnCollide(){
        return onCollide;
    }

    public bool GetOnMoving(){
        return onMoving;
    }
}
