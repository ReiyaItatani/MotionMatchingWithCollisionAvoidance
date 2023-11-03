using UnityEngine;
using UnityEditor;
using System.IO;
using MotionMatching;
using CollisionAvoidance;

// Custom editor window to create prefabs of Humanoid characters.
public class PrefabCreatorWindow : EditorWindow
{
    // Adds a menu item named "Prefab Creator" to a "Window/Custom" menu in the menu bar.
    [MenuItem("CollisionAvoidance/Prefab Creator")]
    public static void ShowWindow()
    {
        // Opens the window, otherwise focuses it if it’s already open.
        GetWindow<PrefabCreatorWindow>("Prefab Creator");
    }

    // Implement your own editor GUI here.
    void OnGUI()
    {
        // Display a label and a box where users can drag and drop GameObjects.
        GUILayout.Label("Drag and Drop Humanoid here to create a Prefab");

        // Create a draggable area for objects.
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drop Humanoid Here");

        // Handle drag and drop events.
        Event evt = Event.current;
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    // Accept the drag-and-drop operation.
                    DragAndDrop.AcceptDrag();

                    // Process each dragged object.
                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        // Check if the object is a GameObject with a Humanoid rig.
                        GameObject go = draggedObject as GameObject;
                        if (go != null && IsHumanoid(go))
                        {
                            // Create a prefab for the humanoid.
                            CreatePrefab(go);
                        }
                    }
                }
                break;
        }
    }

    // Check if the GameObject has a Humanoid Animator.
    private bool IsHumanoid(GameObject go)
    {
        Animator animator = go.GetComponent<Animator>();
        return animator != null && animator.isHuman;
    }

    // Create a prefab from the GameObject and attach the specified script.
private void CreatePrefab(GameObject humanoid)
{
    // Ensure we have an instance of the humanoid to work with.
    if (humanoid == null)
    {
        Debug.LogError("No humanoid GameObject provided to create a prefab.");
        return;
    }

    // Clone the humanoid instance to avoid modifying the original in the scene.
    GameObject humanoidInstance = Instantiate(humanoid);

    // Reset the position of the clone to the origin.
    humanoidInstance.transform.position = Vector3.zero;

    // Create a new parent GameObject for the humanoid clone named "Agent".
    GameObject agent = new GameObject("Agent");

    // Make the humanoid clone a child of the new parent.
    humanoidInstance.transform.SetParent(agent.transform);

    // Create and attach the "PathController" GameObject and script.
    GameObject pathController = new GameObject("PathController");
    pathController.transform.SetParent(agent.transform);
    pathController.AddComponent<PathController>(); // Make sure you have a PathController script.

    // Create and attach the "MotionMatching" GameObject and script.
    GameObject motionMatching = new GameObject("MotionMatching");
    motionMatching.transform.SetParent(agent.transform);
    motionMatching.AddComponent<MotionMatchingController>(); // Make sure you have a MotionMatchingController script.

    // Create and attach the "CollisionAvoidance" GameObject and script.
    GameObject collisionAvoidance = new GameObject("CollisionAvoidance");
    collisionAvoidance.transform.SetParent(agent.transform);
    collisionAvoidance.AddComponent<CollisionAvoidanceController>(); // Make sure you have a CollisionAvoidanceController script.

    // Define the path within the Resources folder.
    string resourcesPath = "Assets/Resources";
    string agentPath = Path.Combine(resourcesPath, humanoidInstance.name);
    if (!Directory.Exists(agentPath))
    {
        Directory.CreateDirectory(agentPath);
    }

    // Save the Agent GameObject as a prefab.
    string prefabPath = Path.Combine(agentPath, agent.name + ".prefab");
    PrefabUtility.SaveAsPrefabAsset(agent, prefabPath);

    // Destroy the temporary Agent game object from the scene.
    DestroyImmediate(agent);

    Debug.Log($"Prefab created at: {prefabPath}");
}

}