using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentManager : MonoBehaviour
{
    public List<GameObject> Agents;

    public List<GameObject> GetAgents(){
        return Agents;
    }
}
