using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroupColliderManager : MonoBehaviour
{
    public SocialRelations socialRelations;
    public AvatarCreator avatarCreator;
    public GameObject groupColliderGameObject;

    private List<GameObject> agentsInCategory = new List<GameObject>();

    void Start()
    {
        agentsInCategory = avatarCreator.GetAgentsInCategory(socialRelations);
    }

    void Update()
    {
        UpdateCenterOfMass();
        DistanceChecker();
    }

    void UpdateCenterOfMass()
    {
        Vector3 combinedPosition = Vector3.zero;
        foreach (GameObject agent in agentsInCategory)
        {
            combinedPosition += agent.transform.position;
        }
        this.transform.position = combinedPosition / agentsInCategory.Count;
    }

    private void DistanceChecker(){
        float maxDistance = 0f;
        foreach (GameObject agent in agentsInCategory)
        {
            float distance = Vector3.Distance(this.transform.position, agent.transform.position);
            if (distance > maxDistance)
            {
                maxDistance = distance;
            }
        }
        if(maxDistance <= (agentsInCategory.Count)/2){
            groupColliderGameObject.SetActive(true);
        }else{
            groupColliderGameObject.SetActive(false);
        }
    }
}
