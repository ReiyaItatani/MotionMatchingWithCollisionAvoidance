using UnityEngine;

namespace CollisionAvoidance
{
    public class AnimationModifier : MonoBehaviour
    {
        private SocialBehaviour socialBehaviour;
        private Animator animator;
        private GameObject rightHandTarget;
        private GameObject leftHandTarget;
        private Transform rightHand;
        private Transform leftHand;

        // Serialized fields allow you to adjust IK weights from the Unity Editor
        [SerializeField, Range(0, 1), ReadOnly]
        private float rightHandIKWeight;

        [SerializeField, Range(0, 1), ReadOnly]
        private float leftHandIKWeight;

        // Threshold distance for full IK activation
        private const float proximityThreshold = 0.8f;

        void Start()
        {
            animator        = GetComponent<Animator>();
            socialBehaviour = GetComponent<SocialBehaviour>();
            // Initialize the IK setup by finding bones and setting up targets
            InitializeBonesAndTargets();
        }

        private void InitializeBonesAndTargets()
        {
            // Get the necessary bone transforms from the animator
            Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);

            // Create IK targets based on estimated positions near the head
            SetupIKTargets(headBone);
        }

        private void SetupIKTargets(Transform headBone)
        {
            // Calculate positions for right and left hand targets
            Vector3 rightEarPosition = headBone.position + headBone.right * 0.15f + headBone.forward * 0.2f - headBone.up * 0.1f;
            Vector3 leftEarPosition = headBone.position - headBone.right * 0.15f + headBone.forward * 0.2f- headBone.up * 0.1f;

            // Create and initialize the IK target GameObjects
            rightHandTarget = CreateTarget("RightHandTarget", rightEarPosition, headBone);
            leftHandTarget = CreateTarget("LeftHandTarget", leftEarPosition, headBone);
        }

        private GameObject CreateTarget(string name, Vector3 position, Transform parent)
        {
            // Create a new GameObject to serve as an IK target
            GameObject target = new GameObject(name);
            target.transform.position = position;
            target.transform.SetParent(parent);
            return target;
        }

        void OnAnimatorIK(int layerIndex)
        {
            if(animator && socialBehaviour){
                UpperBodyAnimationState upperBodyAnimationState = socialBehaviour.GetUpperBodyAnimationState();
                // Update the IK weights based on the current positions of the hands
                UpdateIKWeight(AvatarIKGoal.RightHand, rightHand, rightHandTarget.transform, upperBodyAnimationState, ref rightHandIKWeight);
                UpdateIKWeight(AvatarIKGoal.LeftHand, leftHand, leftHandTarget.transform, upperBodyAnimationState, ref leftHandIKWeight);
            }
        }

        private void UpdateIKWeight(AvatarIKGoal hand, Transform handTransform, Transform target,UpperBodyAnimationState upperBodyAnimationState, ref float weight)
        {
            if(upperBodyAnimationState == UpperBodyAnimationState.SmartPhone){
                // Calculate the distance between the hand and its target
                float distance = Vector3.Distance(handTransform.position, target.position);
                
                // Adjust the IK weight based on the distance, reaching 1 when close to the target
                weight = Mathf.Clamp01(1 - (distance / proximityThreshold));

                // Apply the calculated weight to the IK system
                animator.SetIKPositionWeight(hand, weight);
                animator.SetIKPosition(hand, target.position);
            }
        }
    }
}
