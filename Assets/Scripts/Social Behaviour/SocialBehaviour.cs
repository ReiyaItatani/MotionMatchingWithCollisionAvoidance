using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

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


    void Awake()
    {
        if(motionMatchingSkinnedMeshRendererWithOCEAN!=null){
            initialAvatarMask = motionMatchingSkinnedMeshRendererWithOCEAN.AvatarMask;
        }
        FollowMotionMacthing();
    }

    public void LookAtTarget(GameObject LookAtTarget){
        motionMatchingSkinnedMeshRendererWithOCEAN.LookObject = LookAtTarget;
    }

    public void DeleteLookObject(){
        motionMatchingSkinnedMeshRendererWithOCEAN.LookObject = null;
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

}
