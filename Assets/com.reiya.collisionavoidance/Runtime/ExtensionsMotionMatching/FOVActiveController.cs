using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance {

    // Enum to define different states of Field of View (FOV)
    public enum FOVDegree {
        Normal = 180,
        PeripheralFOV = 120,
        FieldOf3dVision = 60,
        Focus = 30
    }

    public class FOVActiveController : MonoBehaviour {
        // Current state of FOV
        public FOVDegree currentFOV = FOVDegree.PeripheralFOV;
        // Reference to CollisionAvoidanceController
        private CollisionAvoidanceController collisionAvoidance;

        // Called on the frame when the script is enabled
        private void Start() {
            ActivateFOV();
        }

        // Called when the script is loaded or a value is changed in the Inspector
        private void OnValidate() {
            ApplyFOV(currentFOV);
        }

        // Called once per frame
        void Update() {
            if (collisionAvoidance == null) return;

            // Get the current state of the upper body animation
            UpperBodyAnimationState upperBodyAnimationState = collisionAvoidance.GetUpperBodyAnimationState();

            // Set FOV based on the current upper body animation state
            switch (upperBodyAnimationState) {
                case UpperBodyAnimationState.Walk:
                    ApplyFOV(FOVDegree.PeripheralFOV);
                    break;
                case UpperBodyAnimationState.Talk:
                    ApplyFOV(FOVDegree.FieldOf3dVision);
                    break;
                case UpperBodyAnimationState.SmartPhone:
                    ApplyFOV(FOVDegree.Focus);
                    break;
            }
        }

        // Activate the child GameObject that matches the current FOV
        private void ActivateFOV() {
            foreach (Transform child in transform) {
                // Try to parse the child's name as an integer to get the FOV value.
                if (int.TryParse(child.name, out int fovValue)) {
                    // Determine if this child should be active based on the currentFOV.
                    bool isActive = fovValue == (int)currentFOV;

                    // If the child is not going to be active, clear its othersInAvoidanceArea list.
                    if (!isActive) {
                        UpdateAvoidanceTarget avoidanceTarget = child.gameObject.GetComponent<UpdateAvoidanceTarget>();
                        if (avoidanceTarget != null) {
                            avoidanceTarget.othersInAvoidanceArea.Clear();
                        }
                    }

                    // Set the active state of the child GameObject.
                    child.gameObject.SetActive(isActive);
                }
            }
        }

        // Set the new FOV and update the active child GameObject
        public void ApplyFOV(FOVDegree newFOV) {
            currentFOV = newFOV;
            ActivateFOV();
        }

        // Get the currently active child GameObject
        public GameObject GetActiveChildObject() {
            foreach (Transform childTransform in transform) {
                if (childTransform.gameObject.activeSelf) {
                    return childTransform.gameObject;
                }
            }
            return null;
        }

        // Initialize the CollisionAvoidanceController parameter
        public void InitParameter(CollisionAvoidanceController _collisionAvoidance) {
            collisionAvoidance = _collisionAvoidance;
        }
    }
}
