using System.Collections;
using UnityEngine;
using Unity.Mathematics;
using System;
using UnityEditor;
using CollisionAvoidance;

public class ConversationalAgentFramework : MonoBehaviour
{
    private Animator Animator;
    private MotionMatchingSkinnedMeshRenderer motionMatchingSkinnedMeshRenderer;

     private void Awake()
    {
        Animator = GetComponent<Animator>();
        /*Ocean*/
        GetBodyTransforms();
    }
    private void Start()
    {
        /*Ocean*/
        // get the animator
        Animator.logWarnings = false;
        // face script part
        GameObject body = FindObjectWithSkinnedMeshRenderer(gameObject);
        // GameObject body = GetChildGameObject(gameObject, "Body");
        faceController = body.AddComponent<FaceScript>();
        //faceController = body.AddComponent<FaceScriptForAvatarSDK>();
        faceController.meshRenderer = body.GetComponentInChildren<SkinnedMeshRenderer>();
        faceController.InitShapeKeys();

        // SinkPassInit();
        FluctuatePassInit();
    }

    private void OnEnable()
    {
        //Subscribe the event
        motionMatchingSkinnedMeshRenderer = GetComponent<MotionMatchingSkinnedMeshRenderer>();
        motionMatchingSkinnedMeshRenderer.OnUpdateOcean += UpdateOCEAN;
    }

    private void OnDisable()
    {
        //Un-Subscribe the event
        motionMatchingSkinnedMeshRenderer.OnUpdateOcean -= UpdateOCEAN;
    }
    
    // Recursive method to find a GameObject with a SkinnedMeshRenderer component
    GameObject FindObjectWithSkinnedMeshRenderer(GameObject parent)
    {
        // Check if the current GameObject has a SkinnedMeshRenderer component
        if (parent.GetComponent<SkinnedMeshRenderer>() != null)
        {
            // Return this GameObject if it has a SkinnedMeshRenderer
            return parent;
        }

        // Iterate through all child GameObjects
        foreach (Transform child in parent.transform)
        {
            // Recursively search in each child
            GameObject found = FindObjectWithSkinnedMeshRenderer(child.gameObject);
            if (found != null)
            {
                // Return the first found GameObject with a SkinnedMeshRenderer
                return found;
            }
        }

        // Return null if no GameObject with SkinnedMeshRenderer is found in the hierarchy
        return null;
    }

    public void UpdateOCEAN(){
        // if (Map_OCEAN_to_LabanShape) OCEAN_to_LabanShape();
        if (Map_OCEAN_to_LabanEffort) OCEAN_to_LabanEffort();
        if (Map_OCEAN_to_Additional) OCEAN_to_Additional();

        //Posture
        GetBodyTransforms();
        t_Head.localRotation *= Quaternion.Euler(nrp_head.x, 0f, 0f);
        t_Neck.localRotation *= Quaternion.Euler(nrp_neck.x, 0f, 0f);
        //Ocean parameters to rotation of each bones
        LabanEffort_to_Rotations();
        AdditionalPass();
        NewRotatePass();

        //Emotion
        EmotionPass();

        //Fluctuate
        //FluctuatePass();

        //Noise for Look
        circularNoise.SetScalingFactor(21, -ls_ver, ls_ver);
        circularNoise.SetScalingFactor(22, -ls_hor, ls_hor);
        circularNoise.SetDeltaAngle(21, ls_ver_speed);
        circularNoise.SetDeltaAngle(22, ls_hor_speed);
        t_Neck.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(circularNoise.values[21], circularNoise.values[22], 0), multiplyRotationFactor);
    
        //Set transforms
        SetBodyTransforms();

        // GetBodyTransforms();
    }
    #region OCEAN PARAMS

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
    
    //OCEAN
    public bool FLUC_ADD = false;
    [Header("Map_OCEAN")]
    // public bool Map_OCEAN_to_LabanShape = true;
    public bool Map_OCEAN_to_LabanEffort = true;
    public bool Map_OCEAN_to_Additional = true;

    [Header("OCEAN Parameters")]
    [Range(-1f, 1f), HideInInspector] public float openness = 0f;
    [Range(-1f, 1f), HideInInspector] public float conscientiousness = 0f;
    [Range(-1f, 1f)] public float extraversion = 0f;
    [Range(-1f, 1f), HideInInspector] public float agreeableness = 0f;
    [Range(-1f, 1f), HideInInspector] public float neuroticism = 0f;

    [Header("Laban Effort Parameters")]
    [Range(-1f, 1f), HideInInspector] public float space = 0f;
    [Range(-1f, 1f), HideInInspector] public float weight = 0f;
    [Range(-1f, 1f), HideInInspector] public float time = 0f;
    [Range(-1f, 1f), HideInInspector] public float flow = 0f;

    [Header("Emotion Parameters")]
    [Range(0f, 1f)] public float e_happy = 0f;
    [Range(0f, 1f)] public float e_sad = 0f;
    [Range(0f, 1f)] public float e_angry = 0f;
    [Range(0f, 1f)] public float e_disgust = 0f;
    [Range(0f, 1f)] public float e_fear = 0f;
    [Range(0f, 1f)] public float e_shock = 0f;

    [Header("Base Expression Parameters")]
    [Range(-1f, 1f), HideInInspector] public float base_happy = 0f;
    [Range(-1f, 1f), HideInInspector] public float base_sad = 0f;
    [Range(-1f, 1f), HideInInspector] public float base_angry = 0f;
    [Range(-1f, 1f), HideInInspector] public float base_shock = 0f;
    [Range(-1f, 1f), HideInInspector] public float base_disgust = 0f;
    [Range(-1f, 1f), HideInInspector] public float base_fear = 0f;

    [Header("Look Shift Parameters")]
    [Range(0f, 100f), HideInInspector] public float ls_hor;
    [Range(0f, 100f), HideInInspector] public float ls_ver;
    [Range(0f, 5f), HideInInspector] public float ls_hor_speed;
    [Range(0f, 5f), HideInInspector] public float ls_ver_speed;

    [Header("Additional Body Parameters")]
    [Range(-1f, 1f), HideInInspector] public float spine_bend;
    //private readonly float spine_max = 12;
    private readonly float spine_max = 16;
    //private readonly float spine_min = -10;
    private readonly float spine_min = -14;
    [Range(-1f, 1f), HideInInspector] public float sink_bend;
    //private readonly float sink_max = 13;
    [Range(-1f, 1f), HideInInspector] public float head_bend;
    //private readonly float head_max = 2f;
    private readonly float head_max = 5f;
    //private readonly float head_min = -2f;
    private readonly float head_min = -5f;

    private readonly float multiplyRotationFactor = 1f;

    // distances of body parts
    // float d_upperArm, d_lowerArm, d_hand;

    [HideInInspector] public FaceScript faceController;
    //For avatar that is using blendshape of avatarsdk
    //[HideInInspector] public FaceScriptForAvatarSDK faceController;

    static public GameObject GetChildGameObject(GameObject fromGameObject, string withName)
    {
        Transform[] ts = fromGameObject.transform.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in ts) if (t.gameObject.name == withName) return t.gameObject;
        return null;
    }
    #endregion

    #region TRANSFORMS GET SET
    // arms
    private Transform t_LeftShoulder;
    private Transform t_RightShoulder;
    private Transform t_LeftUpperArm;
    private Transform t_RightUpperArm;
    private Transform t_LeftLowerArm;
    private Transform t_RightLowerArm;
    private Transform t_LeftHand;
    private Transform t_RightHand;

    // legs
    private Transform t_LeftUpperLeg;
    private Transform t_RightUpperLeg;
    private Transform t_LeftLowerLeg;
    private Transform t_RightLowerLeg;
    private Transform t_LeftFoot;
    private Transform t_RightFoot;
    private Transform t_LeftToes;
    private Transform t_RightToes;

    // body
    private Transform t_Spine;
    private Transform t_Chest;
    private Transform t_UpperChest;
    private Transform t_Neck;
    private Transform t_Head;
    private Transform t_Hips;

    // fingers
    private Transform t_LeftIndexDistal;
    private Transform t_LeftIndexIntermediate;
    private Transform t_LeftIndexProximal;
    private Transform t_LeftMiddleDistal;
    private Transform t_LeftMiddleIntermediate;
    private Transform t_LeftMiddleProximal;
    private Transform t_LeftRingDistal;
    private Transform t_LeftRingIntermediate;
    private Transform t_LeftRingProximal;
    private Transform t_LeftThumbDistal;
    private Transform t_LeftThumbIntermediate;
    private Transform t_LeftThumbProximal;
    private Transform t_LeftLittleDistal;
    private Transform t_LeftLittleIntermediate;
    private Transform t_LeftLittleProximal;

    private Transform t_RightIndexDistal;
    private Transform t_RightIndexIntermediate;
    private Transform t_RightIndexProximal;
    private Transform t_RightMiddleDistal;
    private Transform t_RightMiddleIntermediate;
    private Transform t_RightMiddleProximal;
    private Transform t_RightRingDistal;
    private Transform t_RightRingIntermediate;
    private Transform t_RightRingProximal;
    private Transform t_RightThumbDistal;
    private Transform t_RightThumbIntermediate;
    private Transform t_RightThumbProximal;
    private Transform t_RightLittleDistal;
    private Transform t_RightLittleIntermediate;
    private Transform t_RightLittleProximal;

    private void GetBodyTransforms()
    {
        t_LeftShoulder = Animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
        t_RightShoulder = Animator.GetBoneTransform(HumanBodyBones.RightShoulder);
        t_LeftUpperArm = Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        t_RightUpperArm = Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        t_LeftLowerArm = Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        t_RightLowerArm = Animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        t_LeftHand = Animator.GetBoneTransform(HumanBodyBones.LeftHand);
        t_RightHand = Animator.GetBoneTransform(HumanBodyBones.RightHand);

        t_LeftUpperLeg = Animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        t_RightUpperLeg = Animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        t_LeftLowerLeg = Animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        t_RightLowerLeg = Animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        t_LeftFoot = Animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        t_RightFoot = Animator.GetBoneTransform(HumanBodyBones.RightFoot);
        t_LeftToes = Animator.GetBoneTransform(HumanBodyBones.LeftToes);
        t_RightToes = Animator.GetBoneTransform(HumanBodyBones.RightToes);

        t_RightFoot = Animator.GetBoneTransform(HumanBodyBones.RightFoot);
        t_RightFoot = Animator.GetBoneTransform(HumanBodyBones.RightFoot);

        t_Spine = Animator.GetBoneTransform(HumanBodyBones.Spine);
        t_Chest = Animator.GetBoneTransform(HumanBodyBones.Chest);
        t_UpperChest = Animator.GetBoneTransform(HumanBodyBones.UpperChest);
        t_Neck = Animator.GetBoneTransform(HumanBodyBones.Neck);
        t_Head = Animator.GetBoneTransform(HumanBodyBones.Head);

        t_Hips = Animator.GetBoneTransform(HumanBodyBones.Hips);

        t_LeftIndexDistal = Animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal);
        t_LeftIndexIntermediate = Animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate);
        t_LeftIndexProximal = Animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal);
        t_LeftMiddleDistal = Animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal);
        t_LeftMiddleIntermediate = Animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate);
        t_LeftMiddleProximal = Animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
        t_LeftRingDistal = Animator.GetBoneTransform(HumanBodyBones.LeftRingDistal);
        t_LeftRingIntermediate = Animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate);
        t_LeftRingProximal = Animator.GetBoneTransform(HumanBodyBones.LeftRingProximal);
        t_LeftThumbDistal = Animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal);
        t_LeftThumbIntermediate = Animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate);
        t_LeftThumbProximal = Animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal);
        t_LeftLittleDistal = Animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal);
        t_LeftLittleIntermediate = Animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate);
        t_LeftLittleProximal = Animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal);

        t_RightIndexDistal = Animator.GetBoneTransform(HumanBodyBones.RightIndexDistal);
        t_RightIndexIntermediate = Animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate);
        t_RightIndexProximal = Animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
        t_RightMiddleDistal = Animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal);
        t_RightMiddleIntermediate = Animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal);
        t_RightMiddleProximal = Animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
        t_RightRingDistal = Animator.GetBoneTransform(HumanBodyBones.RightRingDistal);
        t_RightRingIntermediate = Animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate);
        t_RightRingProximal = Animator.GetBoneTransform(HumanBodyBones.RightRingProximal);
        t_RightThumbDistal = Animator.GetBoneTransform(HumanBodyBones.RightThumbDistal);
        t_RightThumbIntermediate = Animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate);
        t_RightThumbProximal = Animator.GetBoneTransform(HumanBodyBones.RightThumbProximal);
        t_RightLittleDistal = Animator.GetBoneTransform(HumanBodyBones.RightLittleDistal);
        t_RightLittleIntermediate = Animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate);
        t_RightLittleProximal = Animator.GetBoneTransform(HumanBodyBones.RightLittleProximal);
    }

    private void SetBodyTransforms()
    {
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftShoulder, t_LeftShoulder.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightShoulder, t_RightShoulder.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftUpperArm, t_LeftUpperArm.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightUpperArm, t_RightUpperArm.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftLowerArm, t_LeftLowerArm.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightLowerArm, t_RightLowerArm.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftHand, t_LeftHand.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightHand, t_RightHand.localRotation);

        Animator.SetBoneLocalRotation(HumanBodyBones.LeftUpperLeg, t_LeftUpperLeg.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightUpperLeg, t_RightUpperLeg.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftLowerLeg, t_LeftLowerLeg.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightLowerLeg, t_RightLowerLeg.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftFoot, t_LeftFoot.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightFoot, t_RightFoot.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftToes, t_LeftToes.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightToes, t_RightToes.localRotation);

        Animator.SetBoneLocalRotation(HumanBodyBones.Spine, t_Spine.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.Chest, t_Chest.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.UpperChest, t_UpperChest.localRotation);

        Animator.SetBoneLocalRotation(HumanBodyBones.Neck, t_Neck.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.Head, t_Head.localRotation);
        // Animator.SetBoneLocalRotation(HumanBodyBones.Hips, t_Hips.localRotation);

        Animator.SetBoneLocalRotation(HumanBodyBones.LeftIndexDistal, t_LeftIndexDistal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftIndexIntermediate, t_LeftIndexIntermediate.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftIndexProximal, t_LeftIndexProximal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftMiddleDistal, t_LeftMiddleDistal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftMiddleIntermediate, t_LeftMiddleIntermediate.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftMiddleProximal, t_LeftMiddleProximal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftRingDistal, t_LeftRingDistal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftRingIntermediate, t_LeftRingIntermediate.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftRingProximal, t_LeftRingProximal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftThumbDistal, t_LeftThumbDistal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftThumbIntermediate, t_LeftThumbIntermediate.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftThumbProximal, t_LeftThumbProximal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftLittleDistal, t_LeftLittleDistal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftLittleIntermediate, t_LeftLittleIntermediate.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.LeftLittleProximal, t_LeftLittleProximal.localRotation);

        Animator.SetBoneLocalRotation(HumanBodyBones.RightIndexDistal, t_RightIndexDistal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightIndexIntermediate, t_RightIndexIntermediate.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightIndexProximal, t_RightIndexProximal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightMiddleDistal, t_RightMiddleDistal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightMiddleIntermediate, t_RightMiddleIntermediate.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightMiddleProximal, t_RightMiddleProximal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightRingDistal, t_RightRingDistal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightRingIntermediate, t_RightRingIntermediate.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightRingProximal, t_RightRingProximal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightThumbDistal, t_RightThumbDistal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightThumbIntermediate, t_RightThumbIntermediate.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightThumbProximal, t_RightThumbProximal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightLittleDistal, t_RightLittleDistal.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightLittleIntermediate, t_RightLittleIntermediate.localRotation);
        Animator.SetBoneLocalRotation(HumanBodyBones.RightLittleProximal, t_RightLittleProximal.localRotation);
    }
    #endregion

    #region ADDITIONAL PASS
    // PASSES

    private void AdditionalPass()
    {
        // spine bend
        //nrp_spine.x = ScaleBetween(spine_bend*0.35f, spine_min, spine_max, - 1f, 1f);
        // nrp_chest.x = ScaleBetween(spine_bend, spine_min, spine_max, -1f, 1f);
        nrp_upperChest.x = ScaleBetween(spine_bend, spine_min, spine_max, -1f, 1f);

        // sink bend
        // sinkAngle = ScaleBetween(sink_bend, sink_min, sink_max, -1f, 1f);

        // head bend
        nrp_neck.x = ScaleBetween(head_bend, head_min, head_max, -1f, 1f);
        nrp_head.x = ScaleBetween(-head_bend*0.6f, head_min, head_max, -1f, 1f);

    }
    #endregion

    #region FLUCTUATE
    private CircularNoise circularNoise;

    private float fluctuateAngle;
    private float fluctuateAngle_pre;
    private readonly int fluctuate_numOfNRandom = 23;

    private float fluctuateSpeed;
    private float fluctuateSpeed_pre;

    private void FluctuatePassInit()
    {
        circularNoise = new CircularNoise(fluctuate_numOfNRandom, 0.02f);
        circularNoise.SetScalingFactorRange(0, 18, -fluctuateAngle, fluctuateAngle);
        tempF = fluctuateAngle * 0.25f;
        circularNoise.SetScalingFactorRange(18, 21, -tempF, tempF);
    }

    float tempF;

    private void FluctuatePass()
    {
        if (fluctuateAngle != fluctuateAngle_pre)
        {
            circularNoise.SetScalingFactorRange(0, 18, -fluctuateAngle, fluctuateAngle);
            tempF = fluctuateAngle * 0.25f;
            circularNoise.SetScalingFactorRange(18, 21, -tempF, tempF);
            fluctuateAngle_pre = fluctuateAngle;
        }

        if(fluctuateSpeed != fluctuateSpeed_pre)
        {
            circularNoise.SetDeltaAngleRange(0, 21, fluctuateSpeed);
            fluctuateSpeed_pre = fluctuateSpeed;
        }

        if (FLUC_ADD)
        {
            circularNoise.TickDouble();
        }
        else
        {
            circularNoise.Tick();
        }
        // quaternion math
        t_LeftUpperArm.localRotation *= Quaternion.Euler(circularNoise.values[0], circularNoise.values[1], circularNoise.values[2]);
        t_RightUpperArm.localRotation *= Quaternion.Euler(circularNoise.values[3], circularNoise.values[4], circularNoise.values[5]);
        t_LeftLowerArm.localRotation *= Quaternion.Euler(circularNoise.values[6], circularNoise.values[7], circularNoise.values[8]);
        t_RightLowerArm.localRotation *= Quaternion.Euler(circularNoise.values[9], circularNoise.values[10], circularNoise.values[11]);
        t_LeftHand.localRotation *= Quaternion.Euler(circularNoise.values[12], circularNoise.values[13], circularNoise.values[14]);
        t_RightHand.localRotation *= Quaternion.Euler(circularNoise.values[15], circularNoise.values[16], circularNoise.values[17]);
        t_Spine.localRotation *= Quaternion.Euler(circularNoise.values[18], circularNoise.values[19], circularNoise.values[20]);
    }
    #endregion

    #region NEW ROTATE PASS
    private Vector3 nrp_spine;
    private Vector3 nrp_chest;
    private Vector3 nrp_upperChest;
    private Vector3 nrp_neck;
    private Vector3 nrp_head;

    private Vector3 nrp_shoulder;
    private Vector3 nrp_upperArm;
    private Vector3 nrp_lowerArm;
    private Vector3 nrp_hand;

    private Vector3 nrp_upperLeg;
    private Vector3 nrp_lowerLeg;
    private Vector3 nrp_foot;

    private Vector3 nrp_shoulder_x;
    private Vector3 nrp_upperArm_x;
    private Vector3 nrp_lowerArm_x;
    private Vector3 nrp_hand_x;

    private Vector3 nrp_upperLeg_x;
    private Vector3 nrp_lowerLeg_x;
    private Vector3 nrp_foot_x;

    private void NewRotatePass()
    {
        t_Spine.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_spine), multiplyRotationFactor);
        t_Chest.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_chest), multiplyRotationFactor);
        t_UpperChest.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_upperChest), multiplyRotationFactor);
        t_Neck.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_neck), multiplyRotationFactor);
        t_Head.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_head), multiplyRotationFactor);

        nrp_shoulder_x = nrp_shoulder;
        nrp_shoulder_x.y = -nrp_shoulder_x.y;
        nrp_shoulder_x.z = -nrp_shoulder_x.z;

        nrp_upperArm_x = nrp_upperArm;
        nrp_upperArm_x.y = -nrp_upperArm_x.y;
        nrp_upperArm_x.z = -nrp_upperArm_x.z;

        nrp_lowerArm_x = nrp_lowerArm;
        nrp_lowerArm_x.y = -nrp_lowerArm_x.y;
        nrp_lowerArm_x.z = -nrp_lowerArm_x.z;

        nrp_hand_x = nrp_hand;
        nrp_hand_x.y = -nrp_hand_x.y;
        nrp_hand_x.z = -nrp_hand_x.z;

        t_LeftShoulder.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_shoulder), multiplyRotationFactor);
        t_RightShoulder.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_shoulder_x), multiplyRotationFactor);
        t_LeftUpperArm.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_upperArm), multiplyRotationFactor);
        t_RightUpperArm.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_upperArm_x), multiplyRotationFactor);
        t_LeftLowerArm.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_lowerArm), multiplyRotationFactor);
        t_RightLowerArm.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_lowerArm_x), multiplyRotationFactor);
        t_LeftHand.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_hand), multiplyRotationFactor);
        t_RightHand.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_hand_x), multiplyRotationFactor);

        nrp_upperLeg_x = nrp_upperLeg;
        nrp_upperLeg_x.y = -nrp_upperLeg_x.y;
        nrp_upperLeg_x.z = -nrp_upperLeg_x.z;

        nrp_lowerLeg_x = nrp_lowerLeg;
        nrp_lowerLeg_x.y = -nrp_lowerLeg_x.y;
        nrp_lowerLeg_x.z = -nrp_lowerLeg_x.z;

        nrp_foot_x = nrp_foot;
        nrp_foot_x.y = -nrp_foot_x.y;
        nrp_foot_x.z = -nrp_foot_x.z;

        t_LeftUpperLeg.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_upperLeg), multiplyRotationFactor);
        t_RightUpperLeg.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_upperLeg_x), multiplyRotationFactor);
        t_LeftLowerLeg.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_lowerLeg), multiplyRotationFactor);
        t_RightLowerLeg.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_lowerLeg_x), multiplyRotationFactor);
        t_LeftFoot.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_foot), multiplyRotationFactor);
        t_RightFoot.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(nrp_foot_x), multiplyRotationFactor);
    }
    #endregion

    #region EMOTION PASS
    /* * *
    * 
    * EMOTION PASS
    * 
    * * */

    public void AddBaseToExp()
    {
        e_angry += base_angry;
        e_disgust += base_disgust;
        e_fear += base_fear;
        e_happy += base_happy;
        e_sad += base_sad;
        e_shock += base_shock;
    }

    public void ClampEmotions()
    {
        e_angry = Mathf.Clamp(e_angry, 0, 1);
        e_disgust = Mathf.Clamp(e_disgust, 0, 1);
        e_fear = Mathf.Clamp(e_fear, 0, 1);
        e_happy = Mathf.Clamp(e_happy, 0, 1);
        e_sad = Mathf.Clamp(e_sad, 0, 1);
        e_shock = Mathf.Clamp(e_shock, 0, 1);
    }

    private float emotionDecayFactor = 0.06f; // not scaled to OCEAN for now
    private float tmpDecayValue;

    private void EmotionPass()
    {
        if(faceController == null) { Debug.Log("Face Control was not created in " + gameObject.name); return; }

        // base is not added constantly, use AddBaseToExp to add it to current exp
        faceController.exp_angry = ScaleBetween(Mathf.Clamp(e_angry,0,100), 0, 100, 0, 1);
        faceController.exp_disgust = ScaleBetween(Mathf.Clamp(e_disgust, 0, 100), 0, 100, 0, 1);
        faceController.exp_fear = ScaleBetween(Mathf.Clamp(e_fear, 0, 100), 0, 100, 0, 1);
        faceController.exp_happy = ScaleBetween(Mathf.Clamp(e_happy, 0, 100), 0, 100, 0, 1);
        faceController.exp_sad = ScaleBetween(Mathf.Clamp(e_sad, 0, 100), 0, 100, 0, 1);
        faceController.exp_shock = ScaleBetween(Mathf.Clamp(e_shock, 0, 100), 0, 100, 0, 1);

        //tmpDecayValue = emotionDecayFactor * Time.deltaTime;
        // decay emotion
        // if (e_angry > 0) e_angry -= tmpDecayValue; else e_angry = 0;
        // if (e_disgust > 0) e_disgust -= tmpDecayValue; else e_disgust = 0;
        // if (e_fear > 0) e_fear -= tmpDecayValue; else e_fear = 0;
        // if (e_happy > 0) e_happy -= tmpDecayValue; else e_happy = 0;
        // if (e_sad > 0) e_sad -= tmpDecayValue; else e_sad = 0;
        // if (e_shock > 0) e_shock -= tmpDecayValue; else e_shock = 0;

    }
    #endregion

    #region MAP FUNCTIONS
    /* * *
    * 
    * MAP FUNCTIONS
    * 
    * * */

    private readonly bool map_new_weights = false;
    private readonly bool map_emotion_decay = false;
    private readonly bool map_express_factor = false;

    public void OCEAN_to_LabanEffort()
    {
        if(map_new_weights)
        {
            space = ScaleBetween(extraversion + openness, -1, 1, -2, 2);
            weight = ScaleBetween(openness + extraversion + agreeableness, -1, 1, -3, 3);
            time = ScaleBetween(extraversion + neuroticism - (conscientiousness * 1.5f), -1, 1, -3.5f, 3.5f);
            flow = ScaleBetween((neuroticism * 2) - conscientiousness + openness, -1, 1, -4, 4);
        }
        else
        {
            space = ScaleBetween(extraversion + openness, -1, 1, -1, 1);
            weight = ScaleBetween(openness + extraversion + agreeableness, -1, 1, -1, 1);
            time = ScaleBetween(extraversion + neuroticism - (conscientiousness * 1.5f), -1, 1, -1.5f, 1.5f);
            //flow = ScaleBetween((neuroticism) - conscientiousness + openness, -1, 1, -1, 1);
            flow = ScaleBetween((neuroticism * 2) - conscientiousness + openness, -1, 1, -2, 2);
        }
    }

    // public void OCEAN_to_LabanShape()
    // {
    //     if(map_new_weights)
    //     {
    //         IKFAC_up = ScaleBetween(extraversion + openness + agreeableness + conscientiousness, -1, 1, -4, 4);
    //         IKFAC_side = ScaleBetween(extraversion * 1.5f + openness, -1, 1, -2.5f, 2.5f);
    //         IKFAC_forward = ScaleBetween(neuroticism + extraversion, -1, 1, -4f, 4f);

    //     }
    //     else
    //     {
    //         IKFAC_up = ScaleBetween(extraversion + openness + agreeableness + conscientiousness, -1, 1, -1, 1);
    //         IKFAC_side = ScaleBetween(extraversion * 1.5f + openness, -1, 1, -1.5f, 1.5f);
    //         IKFAC_forward = ScaleBetween(neuroticism + extraversion, -1, 1, -1, 1);
    //     }
    // }

    public void OCEAN_to_Additional()
    {
        if(map_new_weights)
        {
            spine_bend = ScaleBetween(-0.5f * agreeableness - extraversion * .8f, -1, 1, -1.5f, 1.5f) * le_lsq_fac;
            head_bend = ScaleBetween(-0.5f * openness - 0.5f * agreeableness - 0.5f * conscientiousness - extraversion * .8f, -1, 1, -2.5f, 2.5f) * le_lsq_fac;
            sink_bend = ScaleBetween(-0.5f * conscientiousness - 0.5f * extraversion * .8f - openness, -1, 1, -2f, 2f) * le_lsq_fac;
            // finger_bend_open = ScaleBetween(-0.5f * openness - agreeableness, -1, 1, -1.5f, 1.5f) * le_lsq_fac;
            // finger_bend_close = ScaleBetween(-openness - agreeableness + neuroticism, -1, 1, -3f, 3f) * le_lsq_fac;

            faceController.blink_min = ScaleBetween(conscientiousness - neuroticism, 0.6f, 5f, -2f, 2f);
            faceController.blink_max = ScaleBetween(conscientiousness - neuroticism, 2f, 8f, -2f, 2f);
            faceController.blinkOpenSpeed = ScaleBetween(conscientiousness - neuroticism, 16f, 6f, -2f, 2f);
            faceController.blinkCloseSpeed = ScaleBetween(conscientiousness - neuroticism, 22f, 12f, -2f, 2f);

            faceController.expressFactor = (map_express_factor) ? ScaleBetween(extraversion, .5f, 2f, -1, 1) : 1;
            if(map_emotion_decay) emotionDecayFactor = ScaleBetween(neuroticism - extraversion, 0.02f, 0.05f, -2, 2);

            ls_hor = ScaleBetween(extraversion - conscientiousness, 0f, 20f, -2, 2) * le_lsq_fac;
            ls_ver = ScaleBetween(extraversion - conscientiousness, 0f, 5f, -2, 2) * le_lsq_fac;
            ls_hor_speed = ScaleBetween(neuroticism, 0.2f, 4f, -1, 1) * le_lsq_fac;
            ls_ver_speed = ScaleBetween(neuroticism, 0.2f, 2f, -1, 1) * le_lsq_fac;

            fluctuateSpeed = ScaleBetween(neuroticism, 0f, 10f, -1, 1) * 4;
        }
        else
        {
            spine_bend = ScaleBetween(-agreeableness * 0.5f - extraversion * .6f, -1, 1, -1f, 1f);
            head_bend = ScaleBetween(openness - agreeableness * 0.5f - conscientiousness - extraversion * .5f, -1, 1, -1, 1);
            sink_bend = ScaleBetween(conscientiousness - extraversion * .7f - openness, -1, 1, -1f, 1f);
            // finger_bend_open = ScaleBetween(openness - agreeableness, -1, 1, -1, 1);
            // finger_bend_close = ScaleBetween(-openness - agreeableness + neuroticism, -1, 1, -1, 1);

            faceController.blink_min = ScaleBetween(conscientiousness - neuroticism, 0.6f, 5f, -1, 1);
            faceController.blink_max = ScaleBetween(conscientiousness - neuroticism, 2f, 8f, -1, 1);
            faceController.blinkOpenSpeed = ScaleBetween(conscientiousness - neuroticism, 16f, 6f, -1, 1);
            faceController.blinkCloseSpeed = ScaleBetween(conscientiousness - neuroticism, 22f, 12f, -1, 1);

            faceController.expressFactor = (map_express_factor) ? ScaleBetween(extraversion, .5f, 2f, -1, 1) : 1;
            if (map_emotion_decay) emotionDecayFactor = ScaleBetween(neuroticism - extraversion, 0.02f, 0.05f, -2, 2);

            ls_hor = ScaleBetween(extraversion - conscientiousness, 0f, 20f, -1, 1);
            ls_ver = ScaleBetween(extraversion - conscientiousness, 0f, 5f, -1, 1);
            ls_hor_speed = ScaleBetween(neuroticism, 0.2f, 4f, -1, 1);
            ls_ver_speed = ScaleBetween(neuroticism, 0.2f, 2f, -1, 1);

            fluctuateSpeed = ScaleBetween(neuroticism, 0f, 10f, -1, 1);
        }
    }

    private readonly bool map_effort_instead_direct_OCEAN = true;
    private readonly float le_lsq_fac = 1.2f;

    public void LabanEffort_to_Rotations()
    {
        // fluctuate ocean
        // fluctuateAngle = ScaleBetween(flow, 0, 18, -1, 1);
        fluctuateAngle = ScaleBetween(flow, 0, 8, -1, 1);

        if (map_effort_instead_direct_OCEAN)
        {
            // body legs sink & rotate ocean
            // nrp_upperLeg.y = ScaleBetween(space, -8, 6, -1f, 1f) * le_lsq_fac;
            // nrp_upperLeg.z = ScaleBetween(space, 4, -2, -1f, 1f) * le_lsq_fac;
            // nrp_lowerLeg.y = ScaleBetween(space, -8, 4, -1f, 1f) * le_lsq_fac;
            // nrp_lowerLeg.z = ScaleBetween(space, 4, -1, -1f, 1f) * le_lsq_fac;
            // nrp_foot.y = ScaleBetween(space, 0, 2, -1f, 1f) * le_lsq_fac;

            // rotate ocean
            nrp_shoulder.x = ScaleBetween(space, 1, -3, -1f, 1f) * le_lsq_fac;
            nrp_shoulder.y = ScaleBetween(-weight, 5, 0, -1f, 1f) * le_lsq_fac;
            nrp_shoulder.z = ScaleBetween(-weight, 0, -3, -1f, 1f) * le_lsq_fac;
            nrp_upperArm.x = ScaleBetween(-weight, 1, -2, -1f, 1f) * le_lsq_fac;
            nrp_lowerArm.x = ScaleBetween(-weight, 1, 0, -1f, 1f) * le_lsq_fac;

            nrp_lowerArm.y = ScaleBetween(space, -10, 10, -1f, 1f) * le_lsq_fac;
            nrp_lowerArm.z = ScaleBetween(space, 0, -4, -1f, 1f) * le_lsq_fac;
            nrp_hand.x = ScaleBetween(space, 14, -10, -1f, 1f) * le_lsq_fac;
            nrp_hand.y = ScaleBetween(space, -10, 28, -1f, 1f) * le_lsq_fac;
            nrp_hand.z = ScaleBetween(space, 0, -6, -1f, 1f) * le_lsq_fac;
        }
        else
        {
            // body legs sink & rotate ocean
            // nrp_upperLeg.y = ScaleBetween(openness, -8f, 6f, -1, 1);
            // nrp_upperLeg.z = ScaleBetween(openness, 4f, -2f, -1, 1);
            // nrp_lowerLeg.y = ScaleBetween(openness, -8f, 4f, -1, 1);
            // nrp_lowerLeg.z = ScaleBetween(openness, 4f, -1f, -1, 1);
            // nrp_foot.y = ScaleBetween(openness, 0f, 2f, -1, 1);

            // rotate ocean
            nrp_shoulder.x = ScaleBetween(extraversion, 1, -3, -1, 1);
            nrp_shoulder.y = ScaleBetween(extraversion, 5, 0, -1, 1);
            nrp_shoulder.z = ScaleBetween(extraversion, 0, -3, -1, 1);
            nrp_upperArm.x = ScaleBetween(extraversion, 1, -2, -1, 1);
            nrp_lowerArm.x = ScaleBetween(extraversion, 1, 0, -1, 1);

            nrp_lowerArm.y = ScaleBetween(openness, -10, 10, -1, 1);
            nrp_lowerArm.z = ScaleBetween(openness, 0, -4, -1, 1);
            nrp_hand.x = ScaleBetween(openness, 10, -8, -1, 1) + ScaleBetween(agreeableness, 4, -2, -1, 1);
            nrp_hand.y = ScaleBetween(openness, -10, 20, -1, 1) + ScaleBetween(agreeableness, 0, 8, -1, 1);
            nrp_hand.z = ScaleBetween(openness, 0, -6, -1, 1);   
        }
    }
    #endregion

    #region TEXT OCEAN PROBS
    /* * *
    * 
    * TEXT OCEAN PROBS
    * 
    * * */

    // public float[] probs;
    // private TextOCEAN[] oceans;
    // [HideInInspector] public String text_O;
    // [HideInInspector] public String text_C;
    // [HideInInspector] public String text_E;
    // [HideInInspector] public String text_A;
    // [HideInInspector] public String text_N;

    // public void InitTextOCEANProbs()
    // {
    //     oceans = new TextOCEAN[10];

    //     oceans[0] = TextOCEAN.O_pos;
    //     oceans[1] = TextOCEAN.O_neg;
    //     oceans[2] = TextOCEAN.C_pos;
    //     oceans[3] = TextOCEAN.C_neg;
    //     oceans[4] = TextOCEAN.E_pos;
    //     oceans[5] = TextOCEAN.E_neg;
    //     oceans[6] = TextOCEAN.A_pos;
    //     oceans[7] = TextOCEAN.A_neg;
    //     oceans[8] = TextOCEAN.N_pos;
    //     oceans[9] = TextOCEAN.N_neg;

    //     probs = new float[10];
    // }

    // public void CalculateTextOCEANProbs()
    // {
    //     probs[0] = Mathf.Clamp(openness, 0, 1);
    //     probs[1] = Mathf.Clamp(-openness, 0, 1);
    //     probs[2] = Mathf.Clamp(conscientiousness, 0, 1);
    //     probs[3] = Mathf.Clamp(-conscientiousness, 0, 1);
    //     probs[4] = Mathf.Clamp(extraversion, 0, 1);
    //     probs[5] = Mathf.Clamp(-extraversion, 0, 1);
    //     probs[6] = Mathf.Clamp(agreeableness, 0, 1);
    //     probs[7] = Mathf.Clamp(-agreeableness, 0, 1);
    //     probs[8] = Mathf.Clamp(neuroticism, 0, 1);
    //     probs[9] = Mathf.Clamp(-neuroticism, 0, 1);

    //     float total = 0f;

    //     for (int i = 0; i < 10; i++)
    //     {
    //         total += probs[i];
    //     }

    //     if (total == 0f)
    //     {
    //         for (int i = 0; i < 10; i++)
    //         {
    //             probs[i] = 1f / 10f;
    //         }
    //     }
    //     else
    //     {
    //         for (int i = 0; i < 10; i++)
    //         {
    //             probs[i] = probs[i] / total;
    //         }
    //     }

    //     if(probs[0] > probs[1])
    //     {
    //         text_O = "Openness: (+) " + probs[0].ToString("F2") + "½";
    //     }
    //     else if (probs[0] < probs[1])
    //     {
    //         text_O = "Openness: (-) " + probs[1].ToString("F2") + "½";
    //     }
    //     else
    //     {
    //         text_O = "Openness: (+) " + probs[0].ToString("F2") + "½, (-) " + probs[1].ToString("F2") + "½";
    //     }

    //     if (probs[2] > probs[3])
    //     {
    //         text_C = "Conscientiousness: (+) " + probs[2].ToString("F2") + "½";
    //     }
    //     else if (probs[2] < probs[3])
    //     {
    //         text_C = "Conscientiousness: (-) " + probs[3].ToString("F2") + "½";
    //     }
    //     else
    //     {
    //         text_C = "Conscientiousness: (+) " + probs[2].ToString("F2") + "½, (-) " + probs[3].ToString("F2") + "½";
    //     }

    //     if (probs[4] > probs[5])
    //     {
    //         text_E = "Extroversion: (+) " + probs[4].ToString("F2") + "½";
    //     }
    //     else if (probs[4] < probs[5])
    //     {
    //         text_E = "Extroversion: (-) " + probs[5].ToString("F2") + "½";
    //     }
    //     else
    //     {
    //         text_E = "Extroversion: (+) " + probs[4].ToString("F2") + "½, (-) " + probs[5].ToString("F2") + "½";
    //     }

    //     if (probs[6] > probs[7])
    //     {
    //         text_A = "Agreeableness: (+) " + probs[6].ToString("F2") + "½";
    //     }
    //     else if (probs[6] < probs[7])
    //     {
    //         text_A = "Agreeableness: (-) " + probs[7].ToString("F2") + "½";
    //     }
    //     else
    //     {
    //         text_A = "Agreeableness: (+) " + probs[6].ToString("F2") + "½, (-) " + probs[7].ToString("F2") + "½";
    //     }

    //     if (probs[8] > probs[9])
    //     {
    //         text_N = "Neuroticism: (+) " + probs[8].ToString("F2") + "½";
    //     }
    //     else if (probs[8] < probs[9])
    //     {
    //         text_N = "Neuroticism: (-) " + probs[9].ToString("F2") + "½";
    //     }
    //     else
    //     {
    //         text_N = "Neuroticism: (+) " + probs[8].ToString("F2") + "½, (-) " + probs[9].ToString("F2") + "½";
    //     }
    // }

    // public TextOCEAN DetermineTextOCEAN()
    // {
    //     if(probs == null || probs.Length != 10)
    //     {
    //         InitTextOCEANProbs();
    //     }

    //     CalculateTextOCEANProbs();

    //     float r = UnityEngine.Random.value;

    //     for (int i = 0; i < 10; i++)
    //     {
    //         r -= probs[i];

    //         if(r <= 0)
    //         {
    //             return oceans[i];
    //         }
    //     }

    //     return oceans[9];
    // }
    
    private static float ScaleBetween(float oldvalue, float newmin, float newmax, float oldmin, float oldmax)
    {
        float d = oldmax - oldmin;
        if (d == 0) return 0;
        else return (newmax - newmin) * (oldvalue - oldmin) / d + newmin;
    }
    #endregion
}
