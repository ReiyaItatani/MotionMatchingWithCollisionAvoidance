using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System;
using UnityEngine.Animations;
using UnityEngine.Playables;
using System.Linq;
using MotionMatching;

namespace CollisionAvoidance
{
[RequireComponent(typeof(Animator))]
public class MotionMatchingSkinnedMeshRenderer : MonoBehaviour
{
    [Header("General")]
    public MotionMatchingController MotionMatching;

    [Header("Animator Integration")]
    [Tooltip("Joints animated by Motion Matching. If none, all Joints are animated.")]
    public AvatarMaskData AvatarMask;
    [Tooltip("Whether to animate the root position by Motion Matching or not.")]
    public bool RootPositionsMask = true;
    [Tooltip("Whether to animate the root rotations by Motion Matching or not.")]
    public bool RootRotationsMask = true;
    [Tooltip("Whether poses should be blended when a joint changes its animation source (Motion Matching or Unity's Animator)")]
    public bool BlendPoses = true;
    [Tooltip("Decrease this value to accelerate blending. Time needed to move half of the distance between the source to the target pose.")]
    [Range(0.0f, 1.0f)] public float BlendHalfLife = 0.05f;

    [Header("Retargeting")]
    [Tooltip("Local vector (axis) pointing in the forward direction of the character")] 
    public Vector3 ForwardLocalVector = new Vector3(0, 0, 1);
    [Tooltip("Local vector (axis) pointing in the up direction of the character")] 
    public Vector3 UpLocalVector = new Vector3(0, 1, 0);

    [Header("Toes Floor Penetration")]
    [Tooltip("Enable to avoid the toes joint (+ ToesSoleOffset) to penetrate the floor (assuming floor at y=0). The root joint will be adjusted to compensate the height difference.")] 
    public bool AvoidToesFloorPenetration;
    [Tooltip("Offset added to the toes joint to determine the sole position to avoid toes-floor penetration.")] 
    public Vector3 ToesSoleOffset;

    // References
    private Animator Animator;

    // Retargeting
    // Initial orientations of the bones The code assumes the initial orientations are in T-Pose
    private Quaternion[] SourceTPose;
    private Quaternion[] TargetTPose;
    // Mapping from BodyJoints to the actual transforms
    private Transform[] SourceBones;
    private Transform[] TargetBones;
    // Mapping Hips Orientation
    private Quaternion HipsCorrection;
    // Toes-Floor Penetration
    private float ToesPenetrationMovingCorrection;

    // Inertialization
    private bool[] PreviousJointMask;
    private bool PreviousHipsPositionMask;
    private quaternion[] PreviousJointRotations;
    private quaternion[] OffsetJointRotations;
    private float3 PreviousHipsPosition;
    private float3 OffsetHipsPosition;

    //ConversationalAgentFrameWork
    public ConversationalAgentFramework conversationalAgentFramework;
    
    private void Awake()
    {
        Animator = GetComponent<Animator>();
        PreviousJointMask = new bool[BodyJoints.Length];
        PreviousJointRotations = new quaternion[BodyJoints.Length];
        OffsetJointRotations = new quaternion[BodyJoints.Length];
    }

    private void OnEnable()
    {
        MotionMatching.OnSkeletonTransformUpdated += OnSkeletonTransformUpdated;

        UpdatePreviousInertialization();
    }

    private void OnDisable()
    {
        MotionMatching.OnSkeletonTransformUpdated -= OnSkeletonTransformUpdated;
    }

    private void Start()
    {
        InitRetargeting();
    }

    private void InitRetargeting()
    {
        MotionMatchingData mmData = MotionMatching.MMData;
        SourceTPose = new Quaternion[BodyJoints.Length];
        TargetTPose = new Quaternion[BodyJoints.Length];
        SourceBones = new Transform[BodyJoints.Length];
        TargetBones = new Transform[BodyJoints.Length];
        // Animation containing in the first frame a T-Pose
        BVHAnimation tposeAnimation = mmData.AnimationDataTPose.GetAnimation();
        // Store Rotations
        // Source
        Skeleton skeleton = tposeAnimation.Skeleton;
        for (int i = 0; i < BodyJoints.Length; i++)
        {
            if (mmData.GetJointName(BodyJoints[i], out string jointName) &&
                skeleton.Find(jointName, out Skeleton.Joint joint))
            {
                // Get the rotation for the first frame of the animation
                SourceTPose[i] = tposeAnimation.GetWorldRotation(joint, 0);
            }
        }
        // Target
        Quaternion rot = Animator.transform.rotation;
        Animator.transform.rotation = Quaternion.identity;
        for (int i = 0; i < BodyJoints.Length; i++)
        {
            TargetTPose[i] = Animator.GetBoneTransform(BodyJoints[i]).rotation;
        }
        Animator.transform.rotation = rot;
        // Correct body orientation so they are both facing the same direction
        float3 targetWorldForward = math.mul(TargetTPose[0], ForwardLocalVector);
        float3 targetWorldUp = math.mul(TargetTPose[0], UpLocalVector);
        float3 sourceWorldForward = math.mul(SourceTPose[0], mmData.HipsForwardLocalVector);
        float3 sourceWorldUp = math.mul(SourceTPose[0], mmData.HipsUpLocalVector);
        quaternion targetLookAt = quaternion.LookRotation(targetWorldForward, targetWorldUp);
        quaternion sourceLookAt = quaternion.LookRotation(sourceWorldForward, sourceWorldUp);
        HipsCorrection = math.mul(sourceLookAt, math.inverse(targetLookAt));
        // Store Transforms
        Transform[] mmBones = MotionMatching.GetSkeletonTransforms();
        Dictionary<string, Transform> boneDict = new Dictionary<string, Transform>();
        foreach (Transform bone in mmBones)
        {
            boneDict.Add(bone.name, bone);
        }
        // Source
        for (int i = 0; i < BodyJoints.Length; i++)
        {
            if (mmData.GetJointName(BodyJoints[i], out string jointName) &&
                boneDict.TryGetValue(jointName, out Transform bone))
            {
                SourceBones[i] = bone;
            }
        }
        // Target
        for (int i = 0; i < BodyJoints.Length; i++)
        {
            TargetBones[i] = Animator.GetBoneTransform(BodyJoints[i]);
        }
    }

    private void OnSkeletonTransformUpdated()
    {
        // Motion
        if (RootPositionsMask)
        {
            transform.position = MotionMatching.transform.position;
        }
        else
        {
            MotionMatching.SetPosAdjustment(transform.position - MotionMatching.transform.position);
        }
        // Retargeting
        for (int i = 0; i < BodyJoints.Length; i++)
        {
            bool currentJointMask = false;
            // Unity's Animator Target Rotation
            Quaternion targetRotation = TargetBones[i].rotation;
            // Check Avatar Mask
            if (AvatarMask == null ||
                (BodyJoints[i] == HumanBodyBones.Hips && RootRotationsMask) ||
                (BodyJoints[i] != HumanBodyBones.Hips && AvatarMask != null && AvatarMask.IsEnabled(BodyJoints[i])))
            {
                currentJointMask = true;
                // Motion Matching Target Rotation
                Quaternion sourceTPoseRotation = SourceTPose[i];
                Quaternion targetTPoseRotation = TargetTPose[i];
                Quaternion sourceRotation = SourceBones[i].rotation;
                /*
                    R_t = Rotation transforming from target local space to world space
                    R_s = Rotation transforming from source local space to world space
                    R_t = R_s * R_st (R_st is a matrix transforming from target local to source local space)
                    // It makes sense because R_st will be mapping from target to source, and R_s from source to world.
                    // The result is transforming from T to world, which is what R_t does.
                    RTPose_t = RTPose_s * R_st
                    R_st = (RTPose_s)^-1 * RTPose_t
                    R_t = R_s * (R_st)^-1 * RTPose_t
                */
                // targetTPoseRotation -> Local Target -> World (Target TPose)
                // HipsCorrection -> World (Target TPose) -> World (Source TPose)
                // sourceTPoseRotation^-1 -> World (SourceTPose) -> Local Source
                // sourceRotation -> Local Source -> World (Source)
                targetRotation = sourceRotation * Quaternion.Inverse(sourceTPoseRotation) * HipsCorrection * targetTPoseRotation;
            }

            if (BlendPoses && PreviousJointMask[i] != currentJointMask)
            {
                // Pose Transition
                float3 offsetAngVel = float3.zero;
                Inertialization.InertializeJointTransition(PreviousJointRotations[i], float3.zero,
                                                        targetRotation, float3.zero,
                                                        ref OffsetJointRotations[i], ref offsetAngVel);
            }

            if (BlendPoses)
            {
                float3 offsetAngVel = float3.zero;
                Inertialization.InertializeJointUpdate(targetRotation, float3.zero,
                                                    BlendHalfLife, Time.deltaTime,
                                                    ref OffsetJointRotations[i], ref offsetAngVel,
                                                    out quaternion inertializedRotation, out _);
                TargetBones[i].rotation = inertializedRotation;
            }
            else
            {
                TargetBones[i].rotation = targetRotation;
            }
        }
        // Hips
        float3 targetHipsPosition = TargetBones[0].position;
        if (RootPositionsMask)
        {
            targetHipsPosition = MotionMatching.GetSkeletonTransforms()[1].position;
        }
        if (BlendPoses && PreviousHipsPositionMask != RootPositionsMask)
        {
            // Position Transition
            float3 offsetAngVel = float3.zero;
            Inertialization.InertializeJointTransition(PreviousHipsPosition, float3.zero,
                                                    targetHipsPosition, float3.zero,
                                                    ref OffsetHipsPosition, ref offsetAngVel);
        }
        if (BlendPoses)
        {
            float3 offsetAngVel = float3.zero;
            Inertialization.InertializeJointUpdate(targetHipsPosition, float3.zero,
                                                BlendHalfLife, Time.deltaTime,
                                                ref OffsetHipsPosition, ref offsetAngVel,
                                                out float3 inertializedHipsPosition, out _);
            TargetBones[0].position = inertializedHipsPosition;
        }
        else
        {
            TargetBones[0].position = targetHipsPosition;
        }

        // Toes-Floor Penetration
        if (AvoidToesFloorPenetration)
        {
            const int leftToesIndex = 17;
            const int rightToesIndex = 21;
            float height = Mathf.Min(TargetBones[leftToesIndex].TransformPoint(ToesSoleOffset).y,
                                    TargetBones[rightToesIndex].TransformPoint(ToesSoleOffset).y);
            height = height < 0.0f ? -height : 0.0f;

            const float movingAverangeFactor = 0.99f;
            ToesPenetrationMovingCorrection = ToesPenetrationMovingCorrection * movingAverangeFactor + height * (1.0f - movingAverangeFactor);

            Vector3 hipsPos = TargetBones[0].position;
            hipsPos.y += ToesPenetrationMovingCorrection;
            TargetBones[0].position = hipsPos;
        }

        // Update State
        UpdatePreviousInertialization();
        if(conversationalAgentFramework != null){
            conversationalAgentFramework.UpdateOCEAN();
        }
    }

    private void UpdatePreviousInertialization()
    {
        // Previous Joint Mask
        PreviousJointMask[0] = RootRotationsMask;
        for (int i = 1; i < BodyJoints.Length; i++)
        {
            PreviousJointMask[i] = AvatarMask != null ? AvatarMask.IsEnabled(BodyJoints[i]) : true;
        }
        PreviousHipsPositionMask = RootPositionsMask;
        // Previous Joint Rotations
        for (int i = 0; i < PreviousJointRotations.Length; ++i) PreviousJointRotations[i] = TargetBones != null ? TargetBones[i].rotation : quaternion.identity;
        PreviousHipsPosition = TargetBones != null ? TargetBones[0].position : float3.zero;
    }

    // Used for retargeting. First parent, then children
    private HumanBodyBones[] BodyJoints =
    {
        HumanBodyBones.Hips, // 0

        HumanBodyBones.Spine, // 1
        HumanBodyBones.Chest, // 2
        HumanBodyBones.UpperChest, // 3

        HumanBodyBones.Neck, // 4
        HumanBodyBones.Head, // 5

        HumanBodyBones.LeftShoulder, // 6
        HumanBodyBones.LeftUpperArm, // 7
        HumanBodyBones.LeftLowerArm, // 8
        HumanBodyBones.LeftHand, // 9

        HumanBodyBones.RightShoulder, // 10
        HumanBodyBones.RightUpperArm, // 11
        HumanBodyBones.RightLowerArm, // 12
        HumanBodyBones.RightHand, // 13

        HumanBodyBones.LeftUpperLeg, // 14
        HumanBodyBones.LeftLowerLeg, // 15
        HumanBodyBones.LeftFoot, // 16
        HumanBodyBones.LeftToes, // 17

        HumanBodyBones.RightUpperLeg, // 18
        HumanBodyBones.RightLowerLeg, // 19
        HumanBodyBones.RightFoot, // 20
        HumanBodyBones.RightToes // 21
    };

    private void OnValidate()
    {
        if (math.abs(math.length(ForwardLocalVector)) < 1E-3f)
        {
            Debug.LogWarning("ForwardLocalVector is too close to zero. Object: " + name);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Animator animator = GetComponent<Animator>();

        if (animator == null) return;

        Vector3 leftSole = animator.GetBoneTransform(HumanBodyBones.LeftToes).TransformPoint(ToesSoleOffset);
        Vector3 rightSole = animator.GetBoneTransform(HumanBodyBones.RightToes).TransformPoint(ToesSoleOffset);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(leftSole, 0.005f);
        Gizmos.DrawSphere(rightSole, 0.005f);
    }
#endif
}
}