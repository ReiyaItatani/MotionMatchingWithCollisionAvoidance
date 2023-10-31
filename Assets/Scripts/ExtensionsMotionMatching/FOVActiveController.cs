using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FOVDegree {
    Normal = 180, //180
    PeripheralFOV = 120, //120
    FieldOf3dVision = 60, //60
    Focus = 30 //30
}

public class FOVActiveController : MonoBehaviour {
    [ReadOnly]
    public FOVDegree currentFOV = FOVDegree.Normal;

    private void Start() {
        UpdateFOV();
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
}
