using UnityEditor;
using UnityEngine;

public class AddTagsEditorScript : AssetPostprocessor
{
    private static void AddTag(string tag)
    {
        var asset = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset");
        if (asset != null)
        {
            SerializedObject serializedObject = new SerializedObject(asset);
            SerializedProperty tagsProp = serializedObject.FindProperty("tags");

            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(tag)) { found = true; break; }
            }

            if (!found)
            {
                tagsProp.arraySize++;
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }
    }

    static void OnPostprocessAllAssets()
    {
        AddTag("Agent");
        AddTag("Group");
        AddTag("Wall");
    }
}
