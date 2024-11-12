using UnityEngine;
using System.Collections.Generic;
using System;

namespace CollisionAvoidance
{
    // Enum to define social relations, categorizing relationships between characters.
    public enum SocialRelations
    {
        Individual,
        Couple,
        Friend,
        Family,
        Coworker
    }

    // Base class for creating and managing avatars.
    [RequireComponent(typeof(AgentManager))]
    public class AvatarCreator : MonoBehaviour
    {
        // avatarPrefabs: A list of available avatar prefabs.
        public List<GameObject> avatarPrefabs = new List<GameObject>();
        // categoryGameObjects: A list of game objects per category (might not be used).
        [ReadOnly]public List<GameObject> categoryGameObjects = new List<GameObject>();  
        // instantiatedAvatars: A list of instantiated avatars.
        [ReadOnly]public List<GameObject> instantiatedAvatars = new List<GameObject>();

        // pathVertices: A list to store vertices of the path that avatars will move along.
        [HideInInspector]
        public List<Vector3> pathVertices= new List<Vector3>();

        // Social Relations Section: Categorizes the possible social relations avatars might have.
        [Header("Social Relations")]
        // categoryCounts: A dictionary to hold the number of avatars belonging to each category.
        protected Dictionary<SocialRelations, int> categoryCounts = new Dictionary<SocialRelations, int>
        {
            { SocialRelations.Couple, 0 },
            { SocialRelations.Family, 0 },
            { SocialRelations.Friend, 0 },
            { SocialRelations.Coworker, 0 },
            { SocialRelations.Individual, 0 }
        };

        [Header("Agent Info")]
        // spawnCount: The number of avatars to spawn.
        public int spawnCount = 1;  
        public float maxSpeed = 0.8f;
        public float minSpeed = 0.1f;
        public float agentHeight = 1.8f;
        public float agentRadius = 0.3f;


        // InstantiateAvatars: Virtual method to instantiate avatars, can be overridden by derived classes.
        public virtual void InstantiateAvatars()
        {
            // Default implementation (can be left empty or provide some basic functionality)
            Debug.Log("InstantiateAvatars called in base AvatarCreator.");
        }

        // DeleteAvatars: Virtual method to delete avatars, can be overridden by derived classes.
        public virtual void DeleteAvatars()
        {
            // Default implementation (can be left empty or provide some basic functionality)
            Debug.Log("DeleteAvatars called in base AvatarCreator.");
        }
            // GetAgents: A method to retrieve agent game objects from instantiated avatars.
        public virtual List<GameObject> GetAgents(){
            List<GameObject> agentsList = new List<GameObject>();
            for (int i = 0; i < instantiatedAvatars.Count; i++)
            {
                ConversationalAgentFramework component = instantiatedAvatars[i].GetComponentInChildren<ConversationalAgentFramework>();
                if (component != null)
                {
                    agentsList.Add(component.gameObject);
                }
            }
            return agentsList;
        }

        // GetAgentsInCategory: A method to retrieve agents belonging to a specific social relation category.
        public virtual List<GameObject> GetAgentsInCategory(SocialRelations socialRelations){
            List<GameObject> agentsList = new List<GameObject>();
            for (int i = 0; i < instantiatedAvatars.Count; i++)
            {
                PathController pathController = instantiatedAvatars[i].GetComponentInChildren<PathController>();
                if(pathController.GetSocialRelations() == socialRelations){
                    GameObject agent = instantiatedAvatars[i].GetComponentInChildren<ConversationalAgentFramework>().gameObject;
                    agentsList.Add(agent);
                }
            }
            return agentsList;
        }

        // StringToSocialRelations: A method to convert a string to a SocialRelations enum.
        public virtual SocialRelations StringToSocialRelations(string relationName)
        {
            if (Enum.IsDefined(typeof(SocialRelations), relationName))
            {
                return (SocialRelations)Enum.Parse(typeof(SocialRelations), relationName);
            }
            else
            {
                throw new ArgumentException($"'{relationName}' is not a valid SocialRelations name.");
            }
        }

        // InitializeDictionaries: A method to initialize dictionaries, resetting the counts of each category.
        protected virtual void InitializeDictionaries()
        {
            categoryCounts = new Dictionary<SocialRelations, int>
            {
                { SocialRelations.Couple, 0 },
                { SocialRelations.Family, 0 },
                { SocialRelations.Friend, 0 },
                { SocialRelations.Coworker, 0 },
                { SocialRelations.Individual, 0 }
            };
        }
    }
}
