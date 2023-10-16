using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

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
}
