using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Drawing;
using System;

public class CollisionAvoidanceController : MonoBehaviour
{
    public PathController pathController;
    public CapsuleCollider agentCollider;
    public CapsuleCollider groupCollider;


    [Header("Basic Collision Avoidance")]
    public Vector3 avoidanceColliderSize = new Vector3(1.5f, 1.5f, 2.0f); 
    private GameObject basicAvoidanceArea;
    private UpdateAvoidanceTarget updateAvoidanceTarget;
    private BoxCollider avoidanceCollider;


    [Header("Unaligned Collision Avoidance")]
    public Vector3 unalignedAvoidanceColliderSize = new Vector3(4.5f, 1.5f, 6.0f); 
    private GameObject unalignedAvoidanceArea;
    private UpdateUnalignedAvoidanceTarget updateUnalignedAvoidanceTarget;
    private BoxCollider unalignedAvoidanceCollider;

    [HideInInspector]
    public bool showAgentSphere = false;


    void Start(){
        //Create Box Collider for Collision Avoidance Force
        basicAvoidanceArea                  = new GameObject("BasicCollisionAvoidanceArea");
        basicAvoidanceArea.transform.parent = this.transform;
        updateAvoidanceTarget               = basicAvoidanceArea.AddComponent<UpdateAvoidanceTarget>();
        updateAvoidanceTarget.InitParameter(pathController, agentCollider, groupCollider);
        avoidanceCollider                   = basicAvoidanceArea.AddComponent<BoxCollider>();
        avoidanceCollider.size              = avoidanceColliderSize;
        avoidanceCollider.isTrigger         = true;

        //Create Box Collider for Unaligned Collision Avoidance Force
        unalignedAvoidanceArea                  = new GameObject("UnalignedCollisionAvoidanceArea");
        unalignedAvoidanceArea.transform.parent = this.transform;
        updateUnalignedAvoidanceTarget          = unalignedAvoidanceArea.AddComponent<UpdateUnalignedAvoidanceTarget>();
        updateUnalignedAvoidanceTarget.InitParameter(pathController, agentCollider, groupCollider);
        unalignedAvoidanceCollider              = unalignedAvoidanceArea.AddComponent<BoxCollider>();
        unalignedAvoidanceCollider.size         = unalignedAvoidanceColliderSize;
        unalignedAvoidanceCollider.isTrigger    = true;

        //Create Agent Collision Detection
        AgentCollisionDetection agentCollisionDetection = agentCollider.GetComponent<AgentCollisionDetection>();
        if (agentCollisionDetection == null)
        {
            agentCollisionDetection = agentCollider.gameObject.AddComponent<AgentCollisionDetection>();
            Debug.Log("AgentCollisionDetection script added");
        }
        agentCollisionDetection.InitParameter(pathController, agentCollider);

        //Update AvoidanceArea
        StartCoroutine(UpdateBasicAvoidanceAreaPos(agentCollider.height/2));
        StartCoroutine(UpdateUnalignedAvoidanceAreaPos(agentCollider.height/2));
    }

    void Update(){
        DrawInfo();
    }

    private IEnumerator UpdateBasicAvoidanceAreaPos(float AgentHeight){
        while(true){
            if(pathController.GetCurrentDirection() == Vector3.zero) yield return null;
            Vector3 Center = (Vector3)pathController.GetCurrentPosition() + pathController.GetCurrentDirection().normalized * avoidanceCollider.size.z/2;
            basicAvoidanceArea.transform.position = new Vector3(Center.x, AgentHeight, Center.z);
            Quaternion targetRotation = Quaternion.LookRotation(pathController.GetCurrentDirection());
            basicAvoidanceArea.transform.rotation = targetRotation;
            yield return null;
        }
    }

    private IEnumerator UpdateUnalignedAvoidanceAreaPos(float AgentHeight){
        while(true){
            if(pathController.GetCurrentDirection() == Vector3.zero) yield return null;
            Vector3 Center = (Vector3)pathController.GetCurrentPosition() + pathController.GetCurrentDirection().normalized*unalignedAvoidanceCollider.size.z/2;
            unalignedAvoidanceArea.transform.position = new Vector3(Center.x, AgentHeight, Center.z);
            Quaternion targetRotation = Quaternion.LookRotation(pathController.GetCurrentDirection());
            unalignedAvoidanceArea.transform.rotation = targetRotation;
            yield return null;
        }
    }

    public List<GameObject> GetOthersInUnalignedAvoidanceArea(){
        return updateUnalignedAvoidanceTarget.GetOthersInUnalignedAvoidanceArea();
    }

    public GameObject GetCurrentAvoidanceTarget(){
        return updateAvoidanceTarget.GetCurrentAvoidanceTarget();
    }

    public CapsuleCollider GetAgentCollider(){
        return agentCollider;
    }

    public GameObject GetAgentGameObject(){
        return agentCollider.gameObject;
    }

    public Vector3 GetAvoidanceColliderSize(){
        return avoidanceColliderSize;
    }

    private void DrawInfo(){
        Color gizmoColor;
        if(showAgentSphere){
            if (pathController.socialRelations == SocialRelations.Couple){
                gizmoColor = new Color(1.0f, 0.0f, 0.0f); // red
            }else if (pathController.socialRelations == SocialRelations.Friend){
                gizmoColor = new Color(0.0f, 1.0f, 0.0f); // green
            }else if  (pathController.socialRelations == SocialRelations.Family){
                gizmoColor = new Color(0.0f, 0.0f, 1.0f); // blue
            }else if  (pathController.socialRelations == SocialRelations.Coworker){
                gizmoColor = new Color(1.0f, 1.0f, 0.0f); // yellow
            }else{
                gizmoColor = new Color(1.0f, 1.0f, 1.0f); // white
            }
            Draw.WireCylinder((Vector3)pathController.GetCurrentPosition(), Vector3.up, agentCollider.height, agentCollider.radius, gizmoColor);
        }
    }
}
