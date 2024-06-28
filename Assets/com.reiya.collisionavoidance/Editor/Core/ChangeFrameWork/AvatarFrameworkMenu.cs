using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CollisionAvoidance
{
    public class AvatarFrameworkMenu
    {
        // CONSTS -------------------------------------------------------------
        private const string RocketBoxSettingPath = "CollisionAvoidance/Target Framework/RocketBox Avatar";
        private const string AvatarSDKSettingPath = "CollisionAvoidance/Target Framework/AvatarSDK Avatar";
        
        public const string SettingPrefKey = "CollisionAvoidance_TargetFramework";

        // DEFINE SYMBOLS
        private const string DefineRocketBox = "MicrosoftRocketBox";
        private const string DefineAvatarSDK = "AvatarSDK";

        // EDITOR PREFS -------------------------------------------------------------
#if UNITY_EDITOR
        private static TargetFramework TargetFramework
        {
            get => EditorUtil.StringToTargetFramework(EditorPrefs.GetString(SettingPrefKey));
            set => EditorPrefs.SetString(SettingPrefKey, EditorUtil.TargetFrameworkToString(value));
        }
#endif

        // MENU FUNCTIONS -------------------------------------------------------------
#if UNITY_EDITOR
        [MenuItem(RocketBoxSettingPath)]
        private static void SetOculus()
        {
            TargetFramework = TargetFramework.MicrosoftRocketBox;
            SetupDefines();
        }
        
        [MenuItem(AvatarSDKSettingPath)]
        private static void SetXRITK()
        {
            TargetFramework = TargetFramework.AvatarSDK;
            SetupDefines();
        }

        // VALIDATION FUNCTIONS -------------------------------------------------------------
        [MenuItem(RocketBoxSettingPath, true)]
        private static bool ValidateOculus()
        {
            Menu.SetChecked(RocketBoxSettingPath, TargetFramework == TargetFramework.MicrosoftRocketBox);
            return true;
        }
        
        [MenuItem(AvatarSDKSettingPath, true)]
        private static bool ValidateXRITK()
        {
            Menu.SetChecked(AvatarSDKSettingPath, TargetFramework == TargetFramework.AvatarSDK);
            return true;
        }
#endif
        // OTHER FUNCTIONS -------------------------------------------------------------
#if UNITY_EDITOR
        private static void SetupDefines()
        {
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (TargetFramework == TargetFramework.MicrosoftRocketBox)
            {
                PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup, out var defines);
                if (defines.Contains(DefineRocketBox)) return;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, new[] { DefineRocketBox });
            }
            else if (TargetFramework == TargetFramework.AvatarSDK)
            {
                PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup, out var defines);
                if (defines.Contains(DefineAvatarSDK)) return;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, new[] { DefineAvatarSDK });
            }
        }
#endif
    }

        public enum TargetFramework
        {
            MicrosoftRocketBox,
            AvatarSDK,
        }
}
