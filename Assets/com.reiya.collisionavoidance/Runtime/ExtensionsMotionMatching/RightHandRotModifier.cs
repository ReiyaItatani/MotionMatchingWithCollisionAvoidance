using System.Collections;
using System.Collections.Generic;
using CollisionAvoidance;
using UnityEngine;

namespace CollisionAvoidance{

public class RightHandRotModifier : MonoBehaviour
{
    private Animator animator; // Animator component attached to the humanoid
    public Transform[][] fingerColliders; // Assign the fingertip transforms here
    public float[] bendAngles; // Max bend angles for each joint

    public Transform[][] fingerJoints; // Stores the transforms for each finger joint of the right hand
    public Vector3[][] initialRotations; // Stores the initial local rotations for each finger joint
    public Vector3[][] endRotations; // Stores the end local rotations for each finger joint

    public bool[] stopBending;
    
    [SerializeField, Range(0f,1f)]
    float interpolation=0f;

    private SocialBehaviour socialBehaviour;
    private void Start()
    {
        animator = GetComponent<Animator>();
        socialBehaviour = GetComponent<SocialBehaviour>();

        // Initialize finger joints array for the right hand
        // Assuming 3 joints per finger (excluding the thumb, which has 2)
        fingerJoints = new Transform[5][];

        // Assign finger joint transforms for right hand
        fingerJoints[0] = new Transform[3] { // Right Thumb
            animator.GetBoneTransform(HumanBodyBones.RightThumbProximal),
            animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate),
            animator.GetBoneTransform(HumanBodyBones.RightThumbDistal)
        };
        fingerJoints[1] = new Transform[3] { // Right Index
            animator.GetBoneTransform(HumanBodyBones.RightIndexProximal),
            animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate),
            animator.GetBoneTransform(HumanBodyBones.RightIndexDistal)
        };
        fingerJoints[2] = new Transform[3] { // Right Middle
            animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal),
            animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate),
            animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal)
        };
        fingerJoints[3] = new Transform[3] { // Right Ring
            animator.GetBoneTransform(HumanBodyBones.RightRingProximal),
            animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate),
            animator.GetBoneTransform(HumanBodyBones.RightRingDistal)
        };
        fingerJoints[4] = new Transform[3] { // Right Little
            animator.GetBoneTransform(HumanBodyBones.RightLittleProximal),
            animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate),
            animator.GetBoneTransform(HumanBodyBones.RightLittleDistal)
        };

        // Initialize the fingerColliders array and add Sphere Collider to the fingerColliders
        fingerColliders = new Transform[fingerJoints.Length][];
        stopBending = new bool[fingerJoints.Length];
        for (int i = 0; i < fingerJoints.Length; i++)
        {
            Transform fingertip = fingerJoints[i][fingerJoints[i].Length - 1].GetChild(0);
            Transform fingerdistal = fingerJoints[i][fingerJoints[i].Length - 1];

            // Add capsule collider between fingertip and distal
            AddCapsuleCollider(fingerdistal, fingertip, i);

            if (i != 0) // Handle non-thumb fingers
            {
                Transform fingerintermediate = fingerJoints[i][fingerJoints[i].Length - 2];

                // Add capsule collider between distal and intermediate
                AddCapsuleCollider(fingerintermediate, fingerdistal, i);
            }

            stopBending[i] = false;
        }

        // Initialize the bendAngles
        int totalJoints = 0;
        foreach (Transform[] finger in fingerJoints)
        {
            totalJoints += finger.Length;
        }

        bendAngles = new float[totalJoints];
        for (int i = 0; i < bendAngles.Length; i++)
        {
            if(i == 0){
                bendAngles[i] = 10f;
            }else{
                bendAngles[i] = 60f;
            }
        }

        // Initialize the initialRotations array with the same structure as fingerJoints
        initialRotations = new Vector3[fingerJoints.Length][];
        endRotations = new Vector3[fingerJoints.Length][];

        for (int i = 0; i < fingerJoints.Length; i++)
        {
            initialRotations[i] = new Vector3[fingerJoints[i].Length];
            for (int j = 0; j < fingerJoints[i].Length; j++)
            {
                // Save the initial local rotation for each joint
                initialRotations[i][j] = fingerJoints[i][j].localEulerAngles;
            }
        }
    }

    // private void AddCollider(Transform joint, int fingerIndex, bool isSphere = true)
    // {
    //     if (isSphere)
    //     {
    //         SphereCollider collider = joint.gameObject.AddComponent<SphereCollider>();
    //         collider.radius = 0.005f; // Set an appropriate radius for the sphere collider
    //         collider.isTrigger = true;
    //     }
    //     else
    //     {
    //         // Capsule collider creation logic will go here
    //     }

    //     Finger finger = joint.gameObject.AddComponent<Finger>();
    //     finger.Init(GetComponent<RightHandRotModifier>(), fingerIndex);
    // }

    private void AddCapsuleCollider(Transform startJoint, Transform endJoint, int fingerIndex)
    {
        CapsuleCollider collider = startJoint.gameObject.AddComponent<CapsuleCollider>();
        collider.radius = 0.005f; // Set an appropriate radius for the capsule collider
        collider.height = Vector3.Distance(startJoint.position, endJoint.position);
        collider.direction = 2; // Assuming Z-axis alignment, change if needed
        collider.isTrigger = true;

        // Position the collider in the middle of the two joints
        Vector3 midPoint = (startJoint.position + endJoint.position) / 2;
        collider.center = startJoint.InverseTransformPoint(midPoint);

        //rotate colldier
        collider.direction = 1;

        // Additional initialization for Finger component
        Finger finger = startJoint.gameObject.AddComponent<Finger>();
        finger.Init(GetComponent<RightHandRotModifier>(), fingerIndex);
    }

    public void LateUpdate()
    {
        if(socialBehaviour.GetOnSmartPhone() == false) return;
        if(interpolation < 1){
            interpolation += 0.01f;
        }
        BendFingers(interpolation);
    }

    // Call this method to bend fingers
    public void BendFingers(float interpolate)
    {
        int bendIndex = 0;
        for (int i = 0; i < fingerJoints.Length; i++)
        {
            for (int j = 0; j < fingerJoints[i].Length; j++)
            {
                if(stopBending[i] == true){
                    fingerJoints[i][j].localEulerAngles = endRotations[i][j];
                    continue;
                }

                Quaternion additionalRotation = Quaternion.identity;

                if (i == 0) // Check if it's the thumb
                {
                    // For the thumb, we might rotate around a different axis, e.g., z-axis
                    //For Rocket Box
                #if MicrosoftRocketBox
                    additionalRotation = Quaternion.Euler(new Vector3(initialRotations[i][j].x, initialRotations[i][j].y, initialRotations[i][j].z - bendAngles[bendIndex] * interpolate));
                #elif AvatarSDK
                    additionalRotation = Quaternion.Euler(new Vector3(initialRotations[i][j].x, initialRotations[i][j].y + bendAngles[bendIndex] * interpolate, initialRotations[i][j].z));
                #endif
                }
                else
                {
                    // For other fingers, rotate around x-axis
                    //For Rocket Box
                #if MicrosoftRocketBox
                    additionalRotation = Quaternion.Euler(new Vector3(initialRotations[i][j].x + bendAngles[bendIndex] * interpolate, initialRotations[i][j].y, initialRotations[i][j].z));
                #elif AvatarSDK
                    additionalRotation = Quaternion.Euler(new Vector3(initialRotations[i][j].x, initialRotations[i][j].y, initialRotations[i][j].z - bendAngles[bendIndex] * interpolate));
                #endif
                }

                // Combine the original rotation with the new rotation
                fingerJoints[i][j].localRotation = additionalRotation;
                bendIndex ++;
            }
        }
    }

    // Stops bending the specified finger
    public void StopBendingFinger(int fingerIndex)
    {
        endRotations[fingerIndex] = new Vector3[fingerJoints[fingerIndex].Length];
        // Save the end local rotation for each joint
        for (int j = 0; j < fingerJoints[fingerIndex].Length; j++)
        {
            endRotations[fingerIndex][j] = fingerJoints[fingerIndex][j].localEulerAngles;
        }
        stopBending[fingerIndex] = true;
    }
}
}