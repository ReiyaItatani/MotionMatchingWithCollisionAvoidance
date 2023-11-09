using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CollisionAvoidance{
public class WallToWallDistChanger : MonoBehaviour
{
    [Range(0,10)]
    public float WallToWallDist;
    public GameObject leftWall;
    public GameObject rightWall;

    void OnValidate()
    {
        if(leftWall == null || rightWall == null) return;
        leftWall.transform.position = new Vector3(leftWall.transform.position.x, leftWall.transform.position.y, -WallToWallDist);
        rightWall.transform.position = new Vector3(rightWall.transform.position.x, rightWall.transform.position.y, WallToWallDist);
    }
}
}