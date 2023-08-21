using UnityEngine;

#if UNITY_EDITOR
[ExecuteInEditMode] 
public class GizmoDrawer : MonoBehaviour
{
    public Color gizmoColor = Color.red;

    [Range(0.1f, 5.0f)] // 0.1から5.0までの範囲でスライダーバーを表示します。
    public float gizmoRadius = 0.5f; // デフォルトの半径を0.5に設定します。

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoRadius);
    }
}
#endif





