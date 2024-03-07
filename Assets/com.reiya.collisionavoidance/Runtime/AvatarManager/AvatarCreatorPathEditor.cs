using UnityEditor;
using UnityEngine;
using CollisionAvoidance;

[CustomEditor(typeof(AgentCreatorPath))]
public class AvatarCreatorPathEditor : Editor
{
    void OnSceneGUI()
    {
        AgentCreatorPath path = (AgentCreatorPath)target;

        for (int i = 0; i < path.agentPath.Count; i++)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newPoint = Handles.PositionHandle(path.transform.position + path.agentPath[i], Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(path, "Move Point");
                path.agentPath[i] = path.transform.InverseTransformPoint(newPoint);
            }
        }
    }
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); 

        AvatarCreatorBase script = (AvatarCreatorBase)target;

        GUILayout.BeginVertical("box");

        GUILayout.Label("Avatar Create Buttons", EditorStyles.boldLabel);

        if (GUILayout.Button("Instantiate Avatars"))
        {
            script.InstantiateAvatars();
        }

        if (GUILayout.Button("Delete Avatars"))
        {
            script.DeleteAvatars();
        }

        GUILayout.EndVertical();
    }
}
