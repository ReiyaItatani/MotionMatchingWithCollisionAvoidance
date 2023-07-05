using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateBoxCollider : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Quaternion targetRotation = Quaternion.LookRotation(this.transform.parent.forward);
        transform.rotation = targetRotation;
    }
}
