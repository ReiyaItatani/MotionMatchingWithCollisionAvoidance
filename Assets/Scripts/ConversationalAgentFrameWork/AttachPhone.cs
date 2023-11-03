using UnityEngine;

[ExecuteInEditMode]
public class AttachPhone : MonoBehaviour
{
    public GameObject phonePrefab;
    public Animator animator;
    public HumanBodyBones handBone = HumanBodyBones.RightHand;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    private GameObject phoneInstance;

    void OnEnable()
    {
        // Ensure the animator and phonePrefab are assigned
        if (animator == null || phonePrefab == null) return;

        // Get the transform of the specified hand bone
        Transform handTransform = animator.GetBoneTransform(handBone);
        if (handTransform == null) return;

        // Instantiate the phone if it hasn't been instantiated already
        if (phoneInstance == null)
        {
            phoneInstance = Instantiate(phonePrefab, handTransform.position + positionOffset, handTransform.rotation);
            phoneInstance.transform.SetParent(handTransform);
        }
    }

    void Update()
    {
        // Ensure the phone instance is available
        if (phoneInstance == null) return;

        // Apply position and rotation offsets in real-time
        phoneInstance.transform.localPosition = positionOffset;
        phoneInstance.transform.localEulerAngles = rotationOffset;
    }
}
