using System.Collections;
using System.Collections.Generic;
using MotionMatching;
using UnityEngine;

namespace CollisionAvoidance{

public class SpringParameterManager : MonoBehaviour, IParameterManager
{
    public CollisionAvoidance.SpringCharacterController springCharacterController;
    
    public Vector3 GetCurrentDirection(){
        return springCharacterController.GetCurrentDirection();
    }

    public Vector3 GetCurrentPosition(){
        return springCharacterController.GetCurrentPosition();
    }

    public float GetCurrentSpeed(){
        return springCharacterController.MaxSpeed;
    }

    public Vector3 GetCurrentAvoidanceVector(){
        return Vector3.zero;
    }

    public SocialRelations GetSocialRelations(){
        return SocialRelations.Individual;
    }
}
}
