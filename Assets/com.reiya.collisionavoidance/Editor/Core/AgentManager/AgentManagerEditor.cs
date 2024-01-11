using UnityEngine;
using UnityEditor;

namespace CollisionAvoidance{
[CustomEditor(typeof(AgentManager))]
public class AgentManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); 

        AgentManager script = (AgentManager)target;

        if (GUILayout.Button("Save Settings"))
        {
            script.SaveSettings();
        }

        if (GUILayout.Button("Load Settings"))
        {
            script.LoadSettings();
        }
    }
}
}
