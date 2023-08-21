using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AvatarCreator))]
public class AvatarCreatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); 

        AvatarCreator script = (AvatarCreator)target;
        if (GUILayout.Button("Instantiate Avatars"))
        {
            script.InstantiateAvatars();
        }

        if (GUILayout.Button("Delete Avatars"))
        {
            script.DeleteAvatars();
        }
    }
}
