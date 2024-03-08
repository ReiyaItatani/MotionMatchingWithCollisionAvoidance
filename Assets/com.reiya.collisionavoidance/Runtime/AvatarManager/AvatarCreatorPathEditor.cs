using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
using CollisionAvoidance;
using System.Linq;

[CustomEditor(typeof(AvatarCreatorPath))]
public class AvatarCreatorPathEditor : Editor
{
    void OnSceneGUI()
    {
        AvatarCreatorPath path = (AvatarCreatorPath)target;

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

        CollisionAvoidance.AvatarCreatorPath script = (CollisionAvoidance.AvatarCreatorPath)target;

        // Button to add "Individual" as the first item if the list is empty, then add other relations respecting the limit of 5 and avoiding duplicates
        if (GUILayout.Button("Add Social Relation"))
        {
            // Ensure the list has less than 5 items
            if (script.socialRelations.Count < 5)
            {
                // If the list is empty, add "Individual" first
                if (script.socialRelations.Count == 0)
                {
                    script.socialRelations.Add(CollisionAvoidance.SocialRelations.Individual);
                    EditorUtility.SetDirty(target); // Mark the object as dirty to ensure changes are saved
                }
                else
                {
                    // Get all enum values except "Individual" because it should be added first and only once
                    var values = System.Enum.GetValues(typeof(CollisionAvoidance.SocialRelations))
                        .Cast<CollisionAvoidance.SocialRelations>()
                        .Where(val => val != CollisionAvoidance.SocialRelations.Individual);
                    
                    // Find the first value not already in the list
                    var notAdded = values.Except(script.socialRelations).FirstOrDefault();

                    // If there is a value not already added, add it to the list
                    if (!script.socialRelations.Contains(notAdded))
                    {
                        script.socialRelations.Add(notAdded);
                        EditorUtility.SetDirty(target); // Mark the object as dirty to ensure changes are saved
                    }
                }
            }
            else
            {
                Debug.LogWarning("Cannot add more than 5 social relations.");
            }
        }

        // Button to clear the social relations list
        if (GUILayout.Button("Clear Social Relations"))
        {
            script.socialRelations.Clear();
            EditorUtility.SetDirty(target); // Mark the object as dirty to ensure changes are saved
        }

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
#endif