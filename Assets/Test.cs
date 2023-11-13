using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Vector3 a = new Vector3(this.transform.forward.x, 0 , this.transform.forward.z);
        Quaternion b = quaternion.LookRotation(a, Vector3.up);
        this.transform.localRotation *= b;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
