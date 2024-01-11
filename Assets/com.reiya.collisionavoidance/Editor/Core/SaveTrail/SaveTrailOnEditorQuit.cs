#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CollisionAvoidance{


[InitializeOnLoad]
public class SaveTrailOnPlayModeExit
{
    static SaveTrailOnPlayModeExit()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            SaveTrailData();
        }
    }

    static void SaveTrailData()
    {
        foreach (TrailRendererGizmo trailRenderer in Object.FindObjectsOfType<TrailRendererGizmo>())
        {
            trailRenderer.SavePointsToCSV();
        }
    }
}
#endif

}