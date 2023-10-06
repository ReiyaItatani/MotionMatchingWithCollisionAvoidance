using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallCollisionDetection : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Agent")){
            Debug.Log("collisiionDetection");
        }
    }
}
