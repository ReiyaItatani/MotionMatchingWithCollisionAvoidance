using UnityEngine;
using System.IO;
using MotionMatching;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace CollisionAvoidance{
// Custom editor window to create prefabs of Humanoid characters.
public class PrefabCreatorWindow : EditorWindow
{
    [SerializeField]private MotionMatchingData MMData;
    [SerializeField]private GameObject FOVMeshPrefab;
    [SerializeField]private RuntimeAnimatorController  animator;
    [SerializeField]private GameObject phonePrefab;
    [SerializeField]private Vector3 positionOffset = new Vector3(0.1f, -0.03f, 0.03f);
    [SerializeField]private Vector3 rotationOffset = new Vector3(0,0,-20);
    [SerializeField]private MotionMatching.AvatarMaskData avatarMask;
    [SerializeField]private AudioClip[] audioClips;

    // Adds a menu item named "Prefab Creator" to a "Window/Custom" menu in the menu bar.
    [MenuItem("CollisionAvoidance/Prefab Creator")]
    public static void ShowWindow()
    {
        // Opens the window, otherwise focuses it if it’s already open.
        GetWindow<PrefabCreatorWindow>("Prefab Creator");
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical("box");
        DrawSettings();
        DrawAudioClips();
        DrawDragAndDropArea();
        EditorGUILayout.EndVertical();
    }

    private void DrawSettings()
    {
        // Display the title for the settings section
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(); // Adds a little space after the label for better readability

        // Field for Motion Matching Data
        MMData = (MotionMatchingData)EditorGUILayout.ObjectField("Motion Matching Data", MMData, typeof(MotionMatchingData), false);
        EditorGUILayout.Space(); // Adds spacing between fields for clarity

        // Field for FOV Mesh Prefab
        FOVMeshPrefab = (GameObject)EditorGUILayout.ObjectField("FOV Mesh", FOVMeshPrefab, typeof(GameObject), false);
        EditorGUILayout.Space(); // Adds spacing between fields for clarity

        // Field for Animator Controller
        animator = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Animator Controller", animator, typeof(RuntimeAnimatorController), false);
        EditorGUILayout.Space(); // Adds spacing between fields for clarity

        // Field for Avatar Mask
        avatarMask = (MotionMatching.AvatarMaskData)EditorGUILayout.ObjectField("Avatar Mask", avatarMask, typeof(MotionMatching.AvatarMaskData), false);
        EditorGUILayout.Space(); // Adds a separator space before the next category

        // SmartPhone Prefab Offset Settings
        GUILayout.Label("SmartPhone Prefab Settings", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Set the position and rotation offsets for where the smartphone prefab should appear relative to the parent object.", MessageType.Info);
        EditorGUILayout.Space(); // Adds space after the help box for better separation

        // Field for SmartPhone Mesh Prefab
        phonePrefab = (GameObject)EditorGUILayout.ObjectField("SmartPhone Mesh", phonePrefab, typeof(GameObject), false);
        EditorGUILayout.Space(); // Adds spacing between fields for clarity

        // Fields for Position Offset and Rotation Offset
        positionOffset = EditorGUILayout.Vector3Field("Position Offset", positionOffset);
        EditorGUILayout.Space(); // Adds spacing between fields for clarity
        rotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", rotationOffset);

        // Add a little extra space at the bottom for neatness
        EditorGUILayout.Space();
    }

    private void DrawAudioClips()
    {
        // Display the title for the Audio Clips section
        GUILayout.Label("Audio Clips", EditorStyles.boldLabel);

        // Description for Audio Clips
        EditorGUILayout.HelpBox("Add audio clips that agents will use to vocalize during collisions or interactions.", MessageType.Info);

        // List all the current audio clips with the option to change them
        if (audioClips != null)
        {
            for (int i = 0; i < audioClips.Length; i++)
            {
                audioClips[i] = (AudioClip)EditorGUILayout.ObjectField($"Clip {i + 1}", audioClips[i], typeof(AudioClip), false);
            }
        }

        // Buttons to add and remove audio clips
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add AudioClip"))
        {
            AddAudioClip();
        }

        if (audioClips != null && audioClips.Length > 0)
        {
            if (GUILayout.Button("Remove AudioClip"))
            {
                RemoveAudioClip();
            }
        }
        GUILayout.EndHorizontal();
    }

    private void DrawDragAndDropArea()
    {
        // Section title for drag and drop functionality
        GUILayout.Label("Create Avatar for Collision Avoidance!", EditorStyles.boldLabel);
        EditorGUILayout.Space(); // Adds a little space for better readability

        // Instructions for users
        EditorGUILayout.HelpBox("Drag and drop a humanoid GameObject here to automatically create a prefab.", MessageType.Info);
        EditorGUILayout.Space(); // Adds space after the help box for better separation

        // Drop area for dragging objects
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drop Humanoid Here", EditorStyles.helpBox);

        // Add a little extra space at the bottom for neatness
        EditorGUILayout.Space();

        // Handling drag and drop operations
        HandleDragAndDrop(dropArea);
    }

    private void HandleDragAndDrop(Rect dropArea)
    {
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
                    DragAndDrop.AcceptDrag();
                    foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                    {
                        GameObject go = draggedObject as GameObject;
                        if (go != null && IsHumanoid(go))
                        {
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

    void AddAudioClip()
    {
        List<AudioClip> clipsList = new List<AudioClip>(audioClips ?? new AudioClip[0]);
        clipsList.Add(null);
        audioClips = clipsList.ToArray();
    }

    void RemoveAudioClip()
    {
        if (audioClips.Length == 0) return;
        List<AudioClip> clipsList = new List<AudioClip>(audioClips);
        clipsList.RemoveAt(clipsList.Count - 1);
        audioClips = clipsList.ToArray();
    }

    SkinnedMeshRenderer FindSkinnedMeshRendererInSameHierarchy(GameObject go) {
    Transform parent = go.transform.parent;
    if (parent != null) {
        SkinnedMeshRenderer[] renderers = parent.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer renderer in renderers) {
            if (renderer.transform.parent == parent && renderer.gameObject != go) {
                return renderer;
            }
        }
    }
    return null;
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
    agent.tag = "Agent";

    // Make the humanoid clone a child of the new parent.
    humanoidInstance.transform.SetParent(agent.transform);
    humanoidInstance.tag = "Agent";

    // Create and attach the "PathController" GameObject and script.
    GameObject pathController = new GameObject("PathController");
    pathController.transform.SetParent(agent.transform);
    PathController pathControllerScript = pathController.AddComponent<PathController>(); // Make sure you have a PathController script.

    // Create and attach the "MotionMatching" GameObject and script.
    GameObject motionMatching = new GameObject("MotionMatching");
    motionMatching.transform.SetParent(agent.transform);
    MotionMatchingController motionMatchingController = motionMatching.AddComponent<MotionMatchingController>(); // Make sure you have a MotionMatchingController script.

    // Create and attach the "CollisionAvoidance" GameObject and script.
    GameObject collisionAvoidance = new GameObject("CollisionAvoidance");
    collisionAvoidance.transform.SetParent(agent.transform);
    CollisionAvoidanceController collisionAvoidanceController = collisionAvoidance.AddComponent<CollisionAvoidanceController>(); // Make sure you have a CollisionAvoidanceController script.

    //set params
    pathControllerScript.MotionMatching     = motionMatchingController;
    pathControllerScript.collisionAvoidance = collisionAvoidanceController;
    pathControllerScript.Path               = new Vector3[2];
    pathControllerScript.Path[0]            = new Vector3(-15, 0, 0);
    pathControllerScript.Path[1]            = new Vector3(15, 0, 0);
    //
    motionMatchingController.CharacterController = pathControllerScript;
    motionMatchingController.MMData              = MMData;
    motionMatchingController.SearchTime          = 0.01f;
    //
    Transform handTransform = humanoidInstance.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand);
    GameObject phoneInstance = Instantiate(phonePrefab, handTransform.position + positionOffset, handTransform.rotation);
    phoneInstance.transform.localEulerAngles = rotationOffset;
    phoneInstance.transform.SetParent(handTransform);
    //
    humanoidInstance.GetComponent<Animator>().runtimeAnimatorController = animator;
    humanoidInstance.GetComponent<Animator>().applyRootMotion = false;
    //
    Rigidbody rigidBody            = humanoidInstance.AddComponent<Rigidbody>();
              rigidBody.mass       = 60f;
              rigidBody.useGravity = false;
    //
    CapsuleCollider capsuleCollider           = humanoidInstance.AddComponent<CapsuleCollider>();
                    capsuleCollider.isTrigger = true;
                    capsuleCollider.center    = new Vector3(0,0.9f,0);
                    capsuleCollider.radius    = 0.3f;
                    capsuleCollider.height    = 1.8f;
    //
    ParameterManager parameterManager                = humanoidInstance.AddComponent<ParameterManager>();
                     parameterManager.pathController = pathControllerScript;
    //
    GameObject soundObject                         = new GameObject("Sound");
               soundObject.transform.localPosition = Vector3.zero;
               soundObject.transform.SetParent(humanoidInstance.transform);
    AudioSource audioSource             = soundObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
    OVRLipSyncContext lipSyncContext               = soundObject.AddComponent<OVRLipSyncContext>();
                      lipSyncContext.audioSource   = audioSource;
                      lipSyncContext.audioLoopback = true;
    OVRLipSyncContextMorphTarget lipSyncMorphTarget                     = soundObject.AddComponent<OVRLipSyncContextMorphTarget>();
                                 lipSyncMorphTarget.skinnedMeshRenderer = FindSkinnedMeshRendererInSameHierarchy(lipSyncMorphTarget.gameObject);
    //
    SocialBehaviour socialBehaviour             = humanoidInstance.AddComponent<SocialBehaviour>();
                    socialBehaviour.smartPhone  = phoneInstance;
                    socialBehaviour.audioSource = audioSource;
                    socialBehaviour.audioClips  = audioClips;
    //
    AgentCollisionDetection agentCollisionDetection  = humanoidInstance.AddComponent<AgentCollisionDetection>();
    //
    ConversationalAgentFramework conversationalAgentFramework = humanoidInstance.AddComponent<ConversationalAgentFramework>();
    //
    GazeController gazeController = humanoidInstance.AddComponent<GazeController>();
    //
    CollisionAvoidance.MotionMatchingSkinnedMeshRenderer motionMatchingSkinnedMeshRenderer                              = humanoidInstance.AddComponent<CollisionAvoidance.MotionMatchingSkinnedMeshRenderer>();
                                                         motionMatchingSkinnedMeshRenderer.MotionMatching               = motionMatchingController;
                                                         motionMatchingSkinnedMeshRenderer.AvatarMask                   = avatarMask;
                                                         motionMatchingSkinnedMeshRenderer.AvoidToesFloorPenetration    = true;
                                                         motionMatchingSkinnedMeshRenderer.ToesSoleOffset               = new Vector3(0, 0, -0.02f);
    //
    AnimationModifier animationModifier = humanoidInstance.AddComponent<AnimationModifier>();
    //
    RightHandRotModifier rightHandRotModifier = humanoidInstance.AddComponent<RightHandRotModifier>();
    //
    collisionAvoidanceController.pathController = pathControllerScript;
    collisionAvoidanceController.FOVMeshPrefab = FOVMeshPrefab;
    collisionAvoidanceController.socialBehaviour = socialBehaviour;
    collisionAvoidanceController.agentCollisionDetection = agentCollisionDetection;
    collisionAvoidanceController.agentCollider = capsuleCollider;
    
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
}
#endif