using UnityEngine;

#if UNITY_EDITOR
namespace CollisionAvoidance{
[ExecuteInEditMode] 
public class GizmoDrawer : MonoBehaviour
{
    public Color gizmoColor = Color.red;

    [Range(0.1f, 5.0f)] 
    public float gizmoRadius = 0.2f; 

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoRadius);
    }
}
}
#endif




