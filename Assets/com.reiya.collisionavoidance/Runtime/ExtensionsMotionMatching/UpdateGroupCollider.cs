using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance{
public class UpdateGroupCollider : MonoBehaviour
{
    public AvatarCreatorBase avatarCreator; 
    private List<GameObject> agentsInCategory = new List<GameObject>();
    private CapsuleCollider groupCollider;
    public float agentRadius = 0.3f;

    void Start()
    {
        agentsInCategory = avatarCreator.GetAgentsInCategory(avatarCreator.StringToSocialRelations(this.transform.parent.name));
        groupCollider = GetComponent<CapsuleCollider>();
    }

    void Update()
    {
        UpdateCenterOfMass();
        UpdateCircleColliderRadius();
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

    void UpdateCircleColliderRadius()
    {
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
            groupCollider.radius = maxDistance + agentRadius;    
        }
    }
}
}