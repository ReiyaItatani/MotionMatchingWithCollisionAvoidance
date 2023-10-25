using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;
using Unity.VisualScripting;

[RequireComponent(typeof(ParameterManager))]
public class SocialBehaviour : MonoBehaviour
{
    [Header("Conversation")]
    public bool onTalk = false;
    public AudioSource audioSource;
    public AudioClip[] audioClips;
    [Range(0, 1)] 
    public float playProbability = 0.1f; 

    [Header("Animation")]
    public bool onAnimation = true;
    public MotionMatchingSkinnedMeshRendererWithOCEAN motionMatchingSkinnedMeshRendererWithOCEAN;
    private AvatarMaskData initialAvatarMask;

    [Header("LookAt")]
    private ParameterManager parameterManager;
    private float fieldOfView = 45f;

    void Awake()
    {
        parameterManager = this.GetComponent<ParameterManager>();
        if(motionMatchingSkinnedMeshRendererWithOCEAN!=null){
            initialAvatarMask = motionMatchingSkinnedMeshRendererWithOCEAN.AvatarMask;
        }
        FollowMotionMacthing();
    }

    void Start(){
        StartCoroutine(UpdateCurrentLookAt(0.2f, parameterManager.GetSocialRelations(), this.gameObject));  
    }
    #region SET
    public void SetCollidedTarget(GameObject collidedTarget){
        motionMatchingSkinnedMeshRendererWithOCEAN.SetCollidedTarget(collidedTarget);
    }
    public void SetCurrentDirection(Vector3 currentDirection){
        motionMatchingSkinnedMeshRendererWithOCEAN.SetCurrentAgentDirection(currentDirection);
    }

    public void SetCurrentCenterOfMass(Vector3 lookAtCenterOfMass){
        motionMatchingSkinnedMeshRendererWithOCEAN.SetCurrentCenterOfMass(lookAtCenterOfMass);
    }

    public void SetCurrentAvoidanceTarget(Vector3 currentAvoidanceTarget){
        motionMatchingSkinnedMeshRendererWithOCEAN.SetCurrentAvoidanceTarget(currentAvoidanceTarget);
    }
    #endregion


    public void DeleteCollidedTarget(){
        motionMatchingSkinnedMeshRendererWithOCEAN.SetCollidedTarget(null);
    }

    public void FollowMotionMacthing(){
        motionMatchingSkinnedMeshRendererWithOCEAN.AvatarMask = null;
    }

    public void TriggerUnityAnimation(){
        if(onAnimation) motionMatchingSkinnedMeshRendererWithOCEAN.AvatarMask = initialAvatarMask;
    }

    public void TryPlayAudio()
    {
        if (onTalk && audioSource != null && Random.value < playProbability && audioClips.Length >= 1)
        {
            int randomIndex = Random.Range(0, audioClips.Length);
            audioSource.clip = audioClips[randomIndex];
            audioSource.Play();
        }
    }

    public void IfIndividual(bool ifIndividual){
        motionMatchingSkinnedMeshRendererWithOCEAN.IfIndividual(ifIndividual);
    }

    public Vector3 GetCurrentLookAt(){
        return motionMatchingSkinnedMeshRendererWithOCEAN.GetCurrentLookAt();
    }

    private IEnumerator UpdateCurrentLookAt(float updateTime, SocialRelations _socialRelations, GameObject agentGameObject){

        List<GameObject> groupAgents = parameterManager.GetAvatarCreatorBase().GetAgentsInCategory(_socialRelations);
        SocialRelations mySocialRelations = parameterManager.GetSocialRelations();

        if(groupAgents.Count <= 1 || _socialRelations == SocialRelations.Individual){
            //For Individual Agent
            IfIndividual(true);
            while(true){
                Vector3 currentDirection = parameterManager.GetCurrentDirection();
                SetCurrentDirection(currentDirection);
            
                GameObject currentAvoidanceTarget = parameterManager.GetCurrentAvoidanceTarget();
                if(currentAvoidanceTarget != null){
                    SocialRelations avoidanceTargetSocialRelations = currentAvoidanceTarget.GetComponent<IParameterManager>().GetSocialRelations();
                    if(avoidanceTargetSocialRelations != mySocialRelations){
                        SetCurrentAvoidanceTarget(currentAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentPosition());
                    }else{
                        SetCurrentAvoidanceTarget(Vector3.zero);
                    }
                }else{
                    SetCurrentAvoidanceTarget(Vector3.zero);
                }

                yield return new WaitForSeconds(updateTime);
            }
        }else{
            //For Group Agents
            IfIndividual(false);
            while(true){
                Vector3  currentDirection = parameterManager.GetCurrentDirection();     
                SetCurrentDirection(currentDirection);

                GameObject currentAvoidanceTarget = parameterManager.GetCurrentAvoidanceTarget();
                if(currentAvoidanceTarget != null){
                    SocialRelations avoidanceTargetSocialRelations = currentAvoidanceTarget.GetComponent<IParameterManager>().GetSocialRelations();
                    if(avoidanceTargetSocialRelations != mySocialRelations){
                        SetCurrentAvoidanceTarget(currentAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentPosition());
                    }else{
                        SetCurrentAvoidanceTarget(Vector3.zero);
                    }
                }else{
                    SetCurrentAvoidanceTarget(Vector3.zero);
                }

                Vector3     headDirection = GetCurrentLookAt();
                if(headDirection!=null){
                    Vector3    currentPosition = parameterManager.GetCurrentPosition();
                    Vector3 GazeAngleDirection = CalculateGazingDirectionToCOM(groupAgents, currentPosition, headDirection, agentGameObject, fieldOfView);
                    SetCurrentCenterOfMass(GazeAngleDirection);
                }

                yield return new WaitForSeconds(updateTime);
            }
        }
    }

    private Vector3 CalculateGazingDirectionToCOM(List<GameObject> groupAgents, Vector3 currentPos, Vector3 currentLookDir, GameObject myself, float angleLimit)
    {
        Vector3            centerOfMass = CalculateCenterOfMass(groupAgents, myself);
        Vector3 directionToCenterOfMass = (centerOfMass - currentPos).normalized;    

        float             angle = Vector3.Angle(currentLookDir, directionToCenterOfMass);
        float neckRotationAngle = 0f;

        if (angle > angleLimit)
        {
            neckRotationAngle = angle - angleLimit;
        }

        Vector3 crossProduct = Vector3.Cross(currentLookDir, directionToCenterOfMass);
        Quaternion  rotation = Quaternion.identity;
        if (crossProduct.y > 0)
        {
            // directionToCenterOfMass is on your right side
            rotation = Quaternion.Euler(0, neckRotationAngle, 0);
        }
        else if (crossProduct.y <= 0)
        {
            // directionToCenterOfMass is on your left side
            rotation = Quaternion.Euler(0, -neckRotationAngle, 0);
        }

        Vector3 rotatedVector = rotation * currentLookDir;

        return rotatedVector.normalized;
    }

    private Vector3 CalculateCenterOfMass(List<GameObject> groupAgents, GameObject myself)
    {
        if (groupAgents == null || groupAgents.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 sumOfPositions = Vector3.zero;
        int count = 0;

        foreach (GameObject go in groupAgents)
        {
            if (go != null && go != myself) 
            {
                sumOfPositions += go.transform.position;
                count++; 
            }
        }

        if (count == 0) 
        {
            return Vector3.zero;
        }

        return sumOfPositions / count;
    }
}
