using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CollisionAvoidance{

#if UNITY_EDITOR
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
#endif
}
