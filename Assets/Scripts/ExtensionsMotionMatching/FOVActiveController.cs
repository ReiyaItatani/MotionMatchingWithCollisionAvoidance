using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance{

public enum FOVDegree {
    Normal = 180, //180
    PeripheralFOV = 120, //120
    FieldOf3dVision = 60, //60
    Focus = 30 //30
}

public class FOVActiveController : MonoBehaviour {
    public FOVDegree currentFOV = FOVDegree.PeripheralFOV;
    private CollisionAvoidanceController collisionAvoidance;

    private void Start() {
        UpdateFOV();
    }

    private void OnValidate() {
        SetFOV(currentFOV);
    }

    void Update(){
        if(collisionAvoidance == null) return;
        UpperBodyAnimationState upperBodyAnimationState = collisionAvoidance.GetUpperBodyAnimationState();
        if(upperBodyAnimationState == UpperBodyAnimationState.Walk){
            SetFOV(FOVDegree.PeripheralFOV);
        }else if(upperBodyAnimationState == UpperBodyAnimationState.Talk){
            SetFOV(FOVDegree.FieldOf3dVision);
        }else if(upperBodyAnimationState == UpperBodyAnimationState.SmartPhone){
            SetFOV(FOVDegree.Focus);
        }
    }

    private void UpdateFOV() {
        foreach (Transform child in transform) {
            int fovValue;
            if (int.TryParse(child.name, out fovValue)) {
                child.gameObject.SetActive(fovValue == (int)currentFOV);
            }
        }
    }

    public void SetFOV(FOVDegree newFOV) {
        currentFOV = newFOV;
        UpdateFOV();
    }

    public GameObject GetActiveChildObject()
    {
        foreach (Transform childTransform in gameObject.transform)
        {
            if (childTransform.gameObject.activeSelf)
            {
                return childTransform.gameObject;
            }
        }

        return null;
    }

    public void InitParameter(CollisionAvoidanceController _collisionAvoidance){
        collisionAvoidance = _collisionAvoidance;
    } 
}
}