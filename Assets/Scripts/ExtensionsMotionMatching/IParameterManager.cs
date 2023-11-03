using UnityEngine;

namespace CollisionAvoidance{

public interface IParameterManager
{
    Vector3 GetCurrentDirection();
    Vector3 GetCurrentPosition();
    Vector3 GetCurrentAvoidanceVector();
    float GetCurrentSpeed();
    SocialRelations GetSocialRelations();
}
}