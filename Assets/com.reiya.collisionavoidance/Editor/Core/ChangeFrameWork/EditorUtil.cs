#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace CollisionAvoidance{

public static class EditorUtil
{
    // CONSTANTS
    private const string MicrosoftRocketBox = "MicrosoftRocketBox";
    private const string AvatarSDK = "AvatarSDK";

#if UNITY_EDITOR
    public static TargetFramework GetTargetFramework()
    {
        return StringToTargetFramework(EditorPrefs.GetString(AvatarFrameworkMenu.SettingPrefKey));
    }
#endif    

    public static TargetFramework StringToTargetFramework(string targetFramework)
    {
        switch (targetFramework)
        {
            case MicrosoftRocketBox:
                return TargetFramework.MicrosoftRocketBox;
            case AvatarSDK:
                return TargetFramework.AvatarSDK;
            default:
                return TargetFramework.MicrosoftRocketBox;
        }
    }
    
    public static string TargetFrameworkToString(TargetFramework targetFramework)
    {
        switch (targetFramework)
        {
            case TargetFramework.MicrosoftRocketBox:
                return MicrosoftRocketBox;
            case TargetFramework.AvatarSDK:
                return AvatarSDK;
            default:
                return MicrosoftRocketBox;
        }
    }
}

}

