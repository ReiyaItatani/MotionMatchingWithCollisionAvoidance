using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

namespace CollisionAvoidance{
    public class ParameterManager : MonoBehaviour, IParameterManager
    {
    public PathController pathController;

    public Vector3 GetCurrentDirection(){
        return pathController.GetCurrentDirection();           
    }

    public Vector3 GetCurrentPosition(){
        return pathController.GetCurrentPosition();           
    }

    public float GetCurrentSpeed(){
        return pathController.GetCurrentSpeed();           
    }

    public SocialRelations GetSocialRelations(){
        return pathController.GetSocialRelations();
    }

    public AvatarCreatorBase GetAvatarCreatorBase(){
        return pathController.GetAvatarCreatorBase();
    }

    public Vector3 GetCurrentAvoidanceVector(){
        return pathController.GetCurrentAvoidanceVector();
    }

    public GameObject GetPotentialAvoidanceTarget(){
        GameObject potentialAvoidanceTarget = pathController.GetPotentialAvoidanceTarget();
        if(potentialAvoidanceTarget != null){
            return potentialAvoidanceTarget;
        }
        return null;
    }

    public PathController GetPathController(){
        return pathController;
    }   

    public CollisionAvoidanceController GetCollisionAvoidanceController(){
        return pathController.GetCollisionAvoidanceController();
    }

    public bool GetOnInSlowingArea(){
        return pathController.GetOnInSlowingArea();
    }
    }
}
