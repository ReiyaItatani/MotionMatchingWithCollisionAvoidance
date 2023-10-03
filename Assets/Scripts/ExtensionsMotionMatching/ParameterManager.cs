using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

public class ParameterManager : MonoBehaviour
{
    public List<PathController> pathControllers = new List<PathController>();

    public Vector3 GetCurrentDirection(){
        if(pathControllers.Count <= 1){
            return pathControllers[0].GetCurrentDirection();           
        }else{
            Vector3 currentDirectionAverage = Vector3.zero;  
            foreach(PathController pathController in pathControllers){
                currentDirectionAverage += pathController.GetCurrentDirection();
            }
            return currentDirectionAverage.normalized;
        }
    }

    public Vector3 GetCurrentPosition(){
        if(pathControllers.Count <= 1){
            return pathControllers[0].GetCurrentPosition();           
        }else{
            Vector3 currentPositionAverage = Vector3.zero;  
            foreach(PathController pathController in pathControllers){
                currentPositionAverage += (Vector3)pathController.GetCurrentPosition();
            }
            return currentPositionAverage/pathControllers.Count;
        }
    }

    public float GetCurrentSpeed(){
        if(pathControllers.Count <= 1){
            return pathControllers[0].GetCurrentSpeed();           
        }else{
            float currentSpeedAverage = 0f;  
            foreach(PathController pathController in pathControllers){
                currentSpeedAverage += pathController.GetCurrentSpeed();
            }
            return currentSpeedAverage/pathControllers.Count;
        }
    }
}
