using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class HandController : MonoBehaviour
{
    private Animator animator; // Animator component attached to the humanoid
    public Transform[] fingerTips; // Assign the fingertip transforms here
    public float[] bendAngles; // Max bend angles for each joint

    public Transform[][] fingerJoints; // Stores the transforms for each finger joint of the right hand
    public Vector3[][] initialRotations; // Stores the initial local rotations for each finger joint

    public bool[] stopBending;

    
    [SerializeField, Range(0f,1f)]
    float interpolation=0f;
    private void Start()
    {
        animator = GetComponent<Animator>();

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

        // Initialize the fingerTips array and add Sphere Collider to the fingertips
        fingerTips = new Transform[fingerJoints.Length];
        stopBending = new bool[fingerJoints.Length];
        for (int i = 0; i < fingerJoints.Length; i++)
        {
            // Assign the last joint of each finger as the fingertip
            Transform fingertip = fingerJoints[i][fingerJoints[i].Length - 1].GetChild(0);
            fingerTips[i] = fingertip;

            stopBending[i] = false;

            // Add Sphere Collider to the fingertip
            AddFingertipCollider(fingertip, i);
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

    private void AddFingertipCollider(Transform fingertip, int fingerIndex)
    {
        SphereCollider collider = fingertip.gameObject.AddComponent<SphereCollider>();
        collider.radius = 0.01f; // Set an appropriate radius for the collider
        collider.isTrigger = true;

        Finger finger = fingertip.gameObject.AddComponent<Finger>();
        finger.Init(GetComponent<HandController>(), fingerIndex);
    }

    void LateUpdate()
    {
        BendFingers(interpolation);
    }

    // Call this method to bend fingers
    public void BendFingers(float interpolate)
    {
        int bendIndex = 0;
        for (int i = 0; i < fingerJoints.Length; i++)
        {
            if(stopBending[i] == true) continue;
            for (int j = 0; j < fingerJoints[i].Length; j++)
            {
                Quaternion additionalRotation;

                if (i == 0) // Check if it's the thumb
                {
                    // For the thumb, we might rotate around a different axis, e.g., z-axis
                    additionalRotation = Quaternion.Euler(new Vector3(initialRotations[i][j].x, initialRotations[i][j].y, initialRotations[i][j].z - bendAngles[bendIndex] * interpolate));
                }
                else
                {
                    // For other fingers, rotate around x-axis
                    additionalRotation = Quaternion.Euler(new Vector3(initialRotations[i][j].x + bendAngles[bendIndex] * interpolate, initialRotations[i][j].y, initialRotations[i][j].z));
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
        stopBending[fingerIndex] = true;
    }
}
