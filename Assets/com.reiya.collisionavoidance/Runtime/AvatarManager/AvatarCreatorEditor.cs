using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
namespace CollisionAvoidance{
[CustomEditor(typeof(AvatarCreatorBase), true)]
public class AvatarCreatorEditor : Editor
{
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

        // GUILayout.Space(10);

        // GUILayout.Label("Avatar Create Buttons In Corridor", EditorStyles.boldLabel);

        // GUI.backgroundColor = Color.yellow;
        // if (GUILayout.Button(new GUIContent("Instantiate Avatars In Corridor", "Use this for debugging purposes")))
        // {
        //     script.InstantiateAvatarsCorridor();
        // }
        // if (GUILayout.Button("Delete Avatars In Corridor"))
        // {
        //     script.DeleteAvatarsCorridor();
        // }
        // GUI.backgroundColor = Color.white;

        GUILayout.EndVertical();
    }
}
}
#endif