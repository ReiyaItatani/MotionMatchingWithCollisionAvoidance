using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace CollisionAvoidance{
public class CollisionAvoidanceMenuItems
{
    [MenuItem("CollisionAvoidance/Create Field/For Video")]
    private static void CreateFieldForVideo()
    {
        AddTag("Agent");
        AddTag("Group");
        AddTag("Wall");
        AddTag("Object");
        GameObject avatarCreator = CreateAvatarCreator("AvatarCreatorForVideo");
        SetPathEndpoints(avatarCreator, "AvatarCreatorForVideo", new Vector3(15,0,0), new Vector3(-15,0,0));
        Debug.Log("AvatarCreator with Path, StartPos, and EndPos created");
    }

    [MenuItem("CollisionAvoidance/Create Field/With Wall")]
    private static void CreateFieldWithWall()
    {
        AddTag("Agent");
        AddTag("Group");
        AddTag("Wall");
        AddTag("Object");
        GameObject avatarCreator = CreateAvatarCreator("AvatarCreatorCorridor");
        SetPathEndpoints(avatarCreator, "AvatarCreatorCorridor", new Vector3(15,0,0), new Vector3(-15,0,0));
        Debug.Log("Field with wall and AvatarCreator with Path, StartPos, and EndPos created");
    }

    [MenuItem("CollisionAvoidance/Create Field/Without Wall")]
    private static void CreateFieldWithoutWall()
    {
        AddTag("Agent");
        AddTag("Group");
        AddTag("Wall");
        AddTag("Object");
        GameObject avatarCreator = CreateAvatarCreator("AvatarCreatorBasic");
        SetPathEndpoints(avatarCreator, "AvatarCreatorBasic", new Vector3(15,0,0), new Vector3(-15,0,0));
        Debug.Log("Field without wall and AvatarCreator with Path, StartPos, and EndPos created");
    }

    [MenuItem("CollisionAvoidance/Create Field/Cross Road")]
    private static void CreateFieldCrossRoad()
    {
        AddTag("Agent");
        AddTag("Group");
        AddTag("Wall");
        AddTag("Object");
        GameObject avatarCreator1 = CreateAvatarCreator("AvatarCreatorCrossRoad");
        SetPathEndpoints(avatarCreator1, "AvatarCreatorCrossRoad", new Vector3(15,0,0), new Vector3(-15,0,0));
        GameObject avatarCreator2 = CreateAvatarCreator("AvatarCreatorCrossRoad");
        SetPathEndpoints(avatarCreator2, "AvatarCreatorCrossRoad", new Vector3(0,0,15), new Vector3(0,0,-15));
        Debug.Log("Two AvatarCreator with Path, StartPos, and EndPos created");
    }

    [MenuItem("CollisionAvoidance/Create Field/Path")]
    private static void CreateFieldPath()
    {
        AddTag("Agent");
        AddTag("Group");
        AddTag("Wall");
        AddTag("Object");
        GameObject avatarCreator = CreateAvatarCreator("AvatarCreatorPath");
        Debug.Log("AvatarCreator created");
    }

    // Creates the AvatarCreator game object and checks for the presence of the OVRLipSync script in the scene.
    private static GameObject CreateAvatarCreator(string scriptName)
    {
        // Check if OVRLipSync is already present in the scene
        if (GameObject.FindObjectOfType<OVRLipSync>() == null)
        {
            // If not present, create a new game object and attach the OVRLipSync script
            GameObject ovrLipSyncObject = new GameObject("OVRLipSyncObject");
            ovrLipSyncObject.AddComponent<OVRLipSync>();
            Debug.Log("OVRLipSync game object created and script attached.");
        }

        // Create the AvatarCreator game object
        GameObject avatarCreator = new GameObject("AvatarCreator");

        // Add the script component based on the scriptName
        if (scriptName == "AvatarCreatorCorridor")
        {
            avatarCreator.AddComponent<AvatarCreatorCorridor>(); 
        }
        else if (scriptName == "AvatarCreatorBasic")
        {
            avatarCreator.AddComponent<AvatarCreatorBasic>(); 
        }
        else if (scriptName == "AvatarCreatorCrossRoad")
        {
            avatarCreator.AddComponent<AvatarCreatorCrossRoad>(); 
        }
        else if (scriptName == "AvatarCreatorForVideo"){
            avatarCreator.AddComponent<AvatarCreatorForVideo>(); 
        }
        else if (scriptName == "AvatarCreatorPath")
        {
            avatarCreator.AddComponent<AvatarCreatorPath>(); 
        }
        else
        {
            Debug.LogError($"Script '{scriptName}' not found. Make sure it exists and is compiled.");
        }
        return avatarCreator;
    }

    private static void SetPathEndpoints(GameObject avatarCreator, string scriptName, Vector3 startPosition, Vector3 endPosition)
    {
        GameObject path = CreateChildGameObject(avatarCreator, "Path");
        GameObject startPos = CreateChildGameObject(path, "StartPos", startPosition);
        GameObject endPos = CreateChildGameObject(path, "EndPos", endPosition);
        
        AttachGizmoDrawer(startPos);
        AttachGizmoDrawer(endPos);

        // Set the startPoint and endPoint in the AvatarCreator script
        var script = avatarCreator.GetComponent(scriptName) as MonoBehaviour;
        if (script != null)
        {
            var startProp = script.GetType().GetField("startPoint");
            var endProp = script.GetType().GetField("endPoint");
            if (startProp != null && endProp != null)
            {
                startProp.SetValue(script, startPos.transform);
                endProp.SetValue(script, endPos.transform);
            }
            else
            {
                Debug.LogError($"One or both of the fields 'startPoint' or 'endPoint' were not found on the script '{scriptName}'.");
            }
        }
        else
        {
            Debug.LogError($"Script '{scriptName}' not found on the AvatarCreator game object.");
        }
    }

    private static GameObject CreateChildGameObject(GameObject parent, string name, Vector3? position = null)
    {
        GameObject child = new GameObject(name);
        child.transform.parent = parent.transform;
        if (position.HasValue)
        {
            child.transform.position = position.Value;
        }
        return child;
    }

    private static void AttachGizmoDrawer(GameObject gameObject)
    {
        var gizmoDrawer = gameObject.AddComponent<GizmoDrawer>();
        if (gizmoDrawer == null)
        {
            Debug.LogError("GizmoDrawer script not found. Make sure it exists and is compiled.");
        }
    }
    private static void AddTag(string tag)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

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
            tagManager.ApplyModifiedProperties();
        }
    }
}
}
#endif