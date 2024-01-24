using UnityEngine;
using System.Linq;
using MotionMatching;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace CollisionAvoidance{
public class PlayerCreationWindow : EditorWindow
{
    private MotionMatchingData MMData;
    private GameObject humanoidAvatar;

    // MenuItem to open this custom editor window.
    [MenuItem("CollisionAvoidance/Create Player")]
    private static void ShowWindow()
    {
        var window = GetWindow<PlayerCreationWindow>("Create Player");
        window.Show();
    }

    private void OnGUI()
    {
        DrawMotionMatchingField();
        DrawHumanoidDropArea();
        DrawCreatePlayerButton();
    }

    // Draws the field for selecting Motion Matching Data.
    private void DrawMotionMatchingField()
    {
        GUILayout.Label("Motion Matching Data", EditorStyles.boldLabel);
        MMData = (MotionMatchingData)EditorGUILayout.ObjectField(MMData, typeof(MotionMatchingData), false);
        EditorGUILayout.Space();
    }

    // Draws the drop area for humanoid avatars and handles the drag and drop logic.
    private void DrawHumanoidDropArea()
    {
        GUILayout.Label("Drag and Drop Humanoid to Attach Components", EditorStyles.boldLabel);
        humanoidAvatar = (GameObject)EditorGUILayout.ObjectField("Humanoid Avatar", humanoidAvatar, typeof(GameObject), true);
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drop Humanoid Here");
        HandleDragAndDrop(dropArea);
    }

    // Creates the button that triggers the player creation process.
    private void DrawCreatePlayerButton()
    {
        if (humanoidAvatar && GUILayout.Button("Create Player"))
        {
            CreatePlayer(humanoidAvatar);
        }
    }

    // Checks if the provided GameObject is a humanoid.
    private bool IsHumanoid(GameObject go)
    {
        var animator = go.GetComponent<Animator>();
        return animator && animator.isHuman;
    }

    // Main function to handle player creation.
    private void CreatePlayer(GameObject avatar)
    {
        if (!IsHumanoid(avatar))
        {
            Debug.LogError("GameObject is not a Humanoid.");
            return;
        }

        GameObject playerParent = CreateParentPlayer();
        GameObject instance = InstantiateAvatar(avatar, playerParent);
        ConfigureMotionMatchingComponents(instance, playerParent);
        FinalizePlayerCreation(playerParent);
    }

    // Handles drag and drop operations within the specified area.
    private void HandleDragAndDrop(Rect dropArea)
    {
        if (!dropArea.Contains(Event.current.mousePosition))
        {
            return;
        }

        switch (Event.current.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (Event.current.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    humanoidAvatar = DragAndDrop.objectReferences.OfType<GameObject>().FirstOrDefault();
                    if (IsHumanoid(humanoidAvatar))
                    {
                        CreatePlayer(humanoidAvatar);
                    }
                    Event.current.Use();
                }
                break;
        }
    }

    // Creates the "Player" parent GameObject.
    private GameObject CreateParentPlayer()
    {
        GameObject playerParent = new GameObject("Player");
        playerParent.tag = "Agent";
        return playerParent;
    }

    // Instantiates the avatar and returns the instance.
    private GameObject InstantiateAvatar(GameObject avatar, GameObject parent)
    {
        GameObject instance = Instantiate(avatar, Vector3.zero, Quaternion.identity, parent.transform);
        instance.name = avatar.name + "_PlayerInstance";
        instance.tag = "Agent";
        return instance;
    }

    // Configures Motion Matching and Character Controller components.
    private void ConfigureMotionMatchingComponents(GameObject instance, GameObject parent)
    {
        //        
        Rigidbody rigidBody = instance.AddComponent<Rigidbody>();
        rigidBody.mass = 60f;
        rigidBody.useGravity = false;
        //
        CapsuleCollider capsuleCollider = instance.AddComponent<CapsuleCollider>();
        capsuleCollider.isTrigger = true;
        capsuleCollider.center = new Vector3(0,0.9f,0);
        capsuleCollider.radius = 0.25f;
        capsuleCollider.height = 1.8f;
        //
        var motionMatchingRenderer = instance.AddComponent<MotionMatching.MotionMatchingSkinnedMeshRenderer>();
        motionMatchingRenderer.AvoidToesFloorPenetration = true;
        motionMatchingRenderer.ToesSoleOffset = new Vector3(0, 0, -0.02f);      
        //
        SpringParameterManager springParameterManager = instance.AddComponent<SpringParameterManager>();
        //
        Transform headTransform = instance.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
        GameObject cameraGameObject = new GameObject("HeadCamera");
        Camera headCamera = cameraGameObject.AddComponent<Camera>();
        cameraGameObject.transform.parent = headTransform;
        cameraGameObject.transform.localPosition = Vector3.zero;
        cameraGameObject.transform.localRotation = Quaternion.identity;
        

        var motionMatchingObject = CreateChildGameObject(parent, "MotionMatching");
        var motionMatchingController = motionMatchingObject.AddComponent<MotionMatchingController>();
        motionMatchingController.MMData = MMData;
        motionMatchingController.FootLock = false;
        motionMatchingRenderer.MotionMatching = motionMatchingController;

        var characterControllerObject = CreateChildGameObject(parent, "CharacterController");
        characterControllerObject.AddComponent<InputManager>();
        characterControllerObject.AddComponent<InputCharacterController>();
        var springCharacterController = characterControllerObject.AddComponent<CollisionAvoidance.SpringCharacterController>();
        springCharacterController.MotionMatching = motionMatchingController;
        motionMatchingController.CharacterController = springCharacterController;
        springParameterManager.springCharacterController = springCharacterController;

    }

    // Creates a child GameObject with the given name under the specified parent.
    private GameObject CreateChildGameObject(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    // Finalizes player creation by selecting the new player and marking it as dirty.
    private void FinalizePlayerCreation(GameObject playerParent)
    {
        Selection.activeGameObject = playerParent;
        EditorUtility.SetDirty(playerParent);
    }
}
}
#endif