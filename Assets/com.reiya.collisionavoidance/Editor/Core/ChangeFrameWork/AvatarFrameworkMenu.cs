using System;
using System.Linq;
using UnityEditor;

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
        private static TargetFramework TargetFramework
        {
            get => EditorUtil.StringToTargetFramework(EditorPrefs.GetString(SettingPrefKey));
            set => EditorPrefs.SetString(SettingPrefKey, EditorUtil.TargetFrameworkToString(value));
        }

            // MENU FUNCTIONS -------------------------------------------------------------
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
        
        // OTHER FUNCTIONS -------------------------------------------------------------
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
    }

        public enum TargetFramework
        {
            MicrosoftRocketBox,
            AvatarSDK,
        }
}
