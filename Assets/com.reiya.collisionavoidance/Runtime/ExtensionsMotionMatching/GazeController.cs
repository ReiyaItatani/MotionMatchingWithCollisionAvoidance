using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CollisionAvoidance{
[RequireComponent(typeof(ParameterManager))]
[RequireComponent(typeof(SocialBehaviour))]
public class GazeController : MonoBehaviour
{
    public enum CurrentLookTarget{
        CollidedTarget,
        CurerntAvoidancetarget,
        MyDirection,
        CenterOfMass
    }
    private SkinnedMeshRenderer meshRenderer;
    private Animator animator;
    private Transform t_Neck;
    private Transform t_Head;

    private SocialBehaviour socialBehaviour;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        GetBodyTransforms(animator);

        socialBehaviour = GetComponent<SocialBehaviour>();

        ParameterManager parameterManager = GetComponent<ParameterManager>();
        List<GameObject> groupAgents = parameterManager.GetAvatarCreatorBase().GetAgentsInCategory(parameterManager.GetSocialRelations());
        SocialRelations mySocialRelations = parameterManager.GetSocialRelations();
        ifIndividual = groupAgents.Count <= 1 || mySocialRelations == SocialRelations.Individual;
    }

    void Start()
    {
        GameObject body = FindObjectWithSkinnedMeshRenderer(gameObject);
        meshRenderer = body.GetComponentInChildren<SkinnedMeshRenderer>();

        StartCoroutine(UpdateNeckState(2.0f));
    }

    GameObject FindObjectWithSkinnedMeshRenderer(GameObject parent)
    {
        if (parent.GetComponent<SkinnedMeshRenderer>() != null)
        {
            return parent;
        }

        foreach (Transform child in parent.transform)
        {
            GameObject found = FindObjectWithSkinnedMeshRenderer(child.gameObject);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    private void GetBodyTransforms(Animator _animator){
        t_Neck = _animator.GetBoneTransform(HumanBodyBones.Neck);
        t_Head = _animator.GetBoneTransform(HumanBodyBones.Head);
    }
    private void SetBodyTransforms(Animator _animator)
    {
        _animator.SetBoneLocalRotation(HumanBodyBones.Neck, t_Neck.localRotation);
        _animator.SetBoneLocalRotation(HumanBodyBones.Head, t_Head.localRotation);
    }

    public void UpdateGaze()
    {
        GetBodyTransforms(animator);
        ParameterUpdater();

        AdjustEyeLevelPass();
        //LookAt
        LookAtAttractionPointUpdater();
        UpdateCurrentLookAtSave();
        LookAtPass(currentLookAt, attractionPoint, Random.Range(0.3f, 0.5f));
        //LookAtAdjustmentPass
        LookAtAdjustmentPass(neckRotationLimit);
        //EyesMovement
        EyesMovementPass();
        //Set transforms
        SetBodyTransforms(animator);
    }
    #region LOOK AT PASS
    /* * *
    * 
    * LOOK AT PASS
    * 
    * * */
    [Header("Look At Params")]
    [ReadOnly]
    public CurrentLookTarget currentLookTarget;
    private GameObject collidedTarget;
    private Vector3 attractionPoint;
    private Quaternion saveLookAtRot = Quaternion.identity;
    private Vector3 currentLookAt = Vector3.zero;
    private bool ifIndividual = false;
    private float neckRotationLimit = 40.0f;
    private Vector3 currentCenterOfMass = Vector3.zero;
    private Vector3 currentAvoidanceTarget = Vector3.zero;
    private Vector3 currentAgentDirection = Vector3.zero;


    private void LookAtPass(Vector3 currentLookAtDir, Vector3 targetLookAtDir, float rotationSpeed){
        Vector3 crossResult = Vector3.Cross(currentLookAtDir, targetLookAtDir);
        if (crossResult.y > 0)
        {
            saveLookAtRot *= Quaternion.Euler(0, rotationSpeed, 0);
        }
        else if (crossResult.y < 0)
        {
            saveLookAtRot *= Quaternion.Euler(0, -rotationSpeed, 0);
        }
        t_Neck.localRotation *= saveLookAtRot;
    }

//Todo refine
    private void ParameterUpdater(){
        //Update Params
        currentCenterOfMass    = socialBehaviour.GetCurrentCenterOfMass();
        currentAvoidanceTarget = socialBehaviour.GetPotentialAvoidanceTarget();
        currentAgentDirection  = socialBehaviour.GetCurrentDirection();
        collidedTarget         = socialBehaviour.GetCollidedTarget();
    }

    private void LookAtAttractionPointUpdater(){
        if(collidedTarget != null){
            //when collide
            attractionPoint = (collidedTarget.transform.position - this.transform.position).normalized;
            currentLookTarget = CurrentLookTarget.CollidedTarget;
        }else if(collidedTarget == null && currentAvoidanceTarget != Vector3.zero){
            attractionPoint = (currentAvoidanceTarget - this.transform.position).normalized;
            currentLookTarget = CurrentLookTarget.CurerntAvoidancetarget;
        }else{
            //in normal situation
            if (ifIndividual) {
                // if the agent is individual
                attractionPoint = currentAgentDirection.normalized;
                currentLookTarget = CurrentLookTarget.MyDirection;
            } else{
                // if the agent is in a group
                attractionPoint = currentCenterOfMass.normalized;
                currentLookTarget = CurrentLookTarget.CenterOfMass;
            }
            //checklookForward
            if(lookForward){
                attractionPoint = currentAgentDirection.normalized;
                currentLookTarget = CurrentLookTarget.MyDirection;
            }
        }
    }
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (t_Head == null) return;

        Vector3 offset = new Vector3(0f, 0f, 0f);
        Vector3 eyePosition = t_Head.transform.position + offset;
        Gizmos.color = Color.magenta;
        Vector3 targetPosition = this.transform.position + attractionPoint;
        Vector3 lineEndPoint = new Vector3(targetPosition.x, eyePosition.y, targetPosition.z);
        Gizmos.DrawLine(eyePosition, lineEndPoint);  
        float sphereSize = 0.01f; 
        Gizmos.DrawSphere(lineEndPoint, sphereSize);
    }
    #endif

    //call this in fixed update
    private IEnumerator UpdateNeckState(float updateTime){

        while(true){
            CheckNeckRotation(GetCurrentLookAt(), GetCurrentAgentDirection(), neckRotationLimit);
            yield return new WaitForSeconds(updateTime);
        }
    }

    private void CheckNeckRotation(Vector3 _currentLookAt, Vector3 myDirection, float _neckRotationLimit, float lookAtForwardDuration = 2.0f, float probability = 0.5f){
        float currentNeckRotation = Vector3.Angle(_currentLookAt.normalized, myDirection.normalized);
        if(UnityEngine.Random.Range(0.0f, 1.0f) < probability){
            if(currentNeckRotation >= _neckRotationLimit && lookForward == false){
                StartCoroutine(TemporalLookAtForward(lookAtForwardDuration));
            }
        }
    }

    private bool lookForward = false;
    private IEnumerator TemporalLookAtForward(float duration){
        if(lookForward == false){
            lookForward = true;
            yield return new WaitForSeconds(duration);
            lookForward = false;
        }
        yield return null;
    }

    private void UpdateCurrentLookAtSave(float angleLimit = 40.0f){
        saveLookAtRot = LimitRotation(saveLookAtRot, angleLimit);
        currentLookAt = saveLookAtRot * t_Head.forward;
    }

    public Vector3 GetCurrentLookAt(){
        return currentLookAt;
    }

    public Vector3 GetCurrentAgentDirection(){
        return currentAgentDirection;
    }
    
    private void AdjustEyeLevelPass(){
        Vector3 horizontalForward = new Vector3(t_Head.forward.x, 0, t_Head.forward.z).normalized;
        Quaternion horizontalRotation = Quaternion.LookRotation(horizontalForward, Vector3.up);
        t_Head.localRotation *= Quaternion.Inverse(t_Head.rotation) * horizontalRotation;
        //t_Neck.localRotation *= Quaternion.Inverse(t_Neck.rotation) * horizontalRotation;
    }

    private void LookAtAdjustmentPass(float angleLimit = 40.0f){
        t_Neck.localRotation = LimitRotation(t_Neck.localRotation, angleLimit);
        t_Head.localRotation = LimitRotation(t_Head.localRotation, angleLimit);
    }
    public static Quaternion LimitRotation(Quaternion rotation, float angleLimit)
    {
        Vector3 eulerRotation = rotation.eulerAngles;

        //eulerRotation.x = ClampAngle(eulerRotation.x, angleLimit);
        eulerRotation.y = ClampAngle(eulerRotation.y, angleLimit);
        //eulerRotation.z = ClampAngle(eulerRotation.z, angleLimit);

        return Quaternion.Euler(eulerRotation);
    }

    private static float ClampAngle(float angle, float limit)
    {
        if (angle > 180f) angle -= 360f;

        return Mathf.Clamp(angle, -limit, limit);
    }
    #endregion

    #region EYES PASS
    /* * *
    * 
    * EYES MOVEMENT PASS
    * 
    * * */

    private int lookRight_Eyes = 112;
    private int lookLeft_Eyes = 111;
    private float blendValue;

    private void EyesMovementPass()
    {
        CalculateBlendValueBasedOnDirection(GetCurrentLookAt(), attractionPoint);
    }

    private void ResetEyesBlendShape()
    {
        if (meshRenderer.GetBlendShapeWeight(lookRight_Eyes) > 0)
        {
            SetEyesBlendShape(lookRight_Eyes, blendValue);
        }
        else if (meshRenderer.GetBlendShapeWeight(lookLeft_Eyes) > 0)
        {
            SetEyesBlendShape(lookLeft_Eyes, blendValue);
        }
    }

    private void CalculateBlendValueBasedOnDirection(Vector3 currentDirection, Vector3 targetDirection)
    {
        float angle = Vector3.Angle(currentDirection, targetDirection);
        float sign = Mathf.Sign(Vector3.Cross(currentDirection, targetDirection).y);

        blendValue = Mathf.Clamp(angle / 90.0f * 100.0f, 0.0f, 100.0f);

        // Sign indicates targetDirection (1 for right, -1 for left)
        if (sign >= 0)
        {
            SetEyesBlendShape(lookLeft_Eyes, 0);
            SetEyesBlendShape(lookRight_Eyes, blendValue);
        }
        else
        {
            SetEyesBlendShape(lookLeft_Eyes, blendValue);
            SetEyesBlendShape(lookRight_Eyes, 0);
        }
    }

    private void SetEyesBlendShape(int blendShapeIndex, float value)
    {
        meshRenderer.SetBlendShapeWeight(blendShapeIndex, value);
    }

    private IEnumerator EyesWeightChanger(float originalWeight, float targetWeight, float duration)
    {
        float elapsedTime = 0;
        float initialWeight = originalWeight;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            blendValue = Mathf.Lerp(initialWeight, targetWeight, elapsedTime / duration);
            yield return new WaitForSeconds(Time.deltaTime);
        }

        blendValue = targetWeight;
    }
    #endregion


}
}