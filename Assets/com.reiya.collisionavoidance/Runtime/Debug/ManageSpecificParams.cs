using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance{
    public class ManageSpecificParams : MonoBehaviour
    {
        [SerializeField]
        private bool onNeckRotation = true;
        [SerializeField]
        private bool onAnimationShift = true;
        [SerializeField]
        private bool onAvoidanceCoordination = true;
        [SerializeField]
        private bool onGroupCollider = true;


        [SerializeField, ReadOnly]
        private AvatarCreatorBase avatarCreatorBase;
        private List<GameObject> avatarPrefabs = new List<GameObject>();
        private List<PathController> pathControllers = new List<PathController>();
        private List<GazeController> gazeControllers = new List<GazeController>();
        private List<SocialBehaviour> socialBehaviours = new List<SocialBehaviour>();
        [SerializeField]
        private List<GroupColliderManager> groupColliderManagers = new List<GroupColliderManager>();
        // Start is called before the first frame update
        void Awake()
        {
            avatarCreatorBase = GetComponent<AvatarCreatorBase>();
            avatarPrefabs = avatarCreatorBase.GetAgents();
            foreach(GameObject avatarprefab in avatarPrefabs){
                gazeControllers.Add(avatarprefab.GetComponent<GazeController>());
                socialBehaviours.Add(avatarprefab.GetComponent<SocialBehaviour>());
            }
            pathControllers = avatarCreatorBase.GetPathControllers();

            SetAvatarParams();

        }

        private void OnValidate()
        {
            SetAvatarParams();
        }

        private void SetAvatarParams(){
            foreach(PathController pathController in pathControllers){
                pathController.onAvoidanceCoordination = onAvoidanceCoordination;
            }
            foreach(GazeController gazeController in gazeControllers){
                gazeController.onNeckRotation = onNeckRotation;
            }
            foreach(SocialBehaviour socialBehaviour in socialBehaviours){
                socialBehaviour.onAnimationShift = onAnimationShift;
            }
            foreach(GroupColliderManager groupColliderManager in groupColliderManagers){
                groupColliderManager.OnGroupCollider = onGroupCollider;
            }
        }
    }
}
