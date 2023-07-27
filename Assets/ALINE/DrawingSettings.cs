#pragma warning disable CS0169, CS0414 // The field 'DrawingSettings.version' is never used
using UnityEditor;
using UnityEngine;

namespace Drawing {
	/// <summary>Stores ALINE project settings</summary>
	public class DrawingSettings : ScriptableObject {
		public const string SettingsPathCompatibility = "Assets/Settings/ALINE.asset";
		public const string SettingsPath = "Assets/Settings/Resources/ALINE.asset";

		/// <summary>Stores ALINE project settings</summary>
		[System.Serializable]
		public class Settings {
			/// <summary>Opacity of lines when in front of objects</summary>
			public float lineOpacity = 1.0f;

			/// <summary>Opacity of solid objects when in front of other objects</summary>

			public float solidOpacity = 0.55f;

			/// <summary>Opacity of text when in front of other objects</summary>

			public float textOpacity = 1.0f;

			/// <summary>Additional opacity multiplier of lines when behind or inside objects</summary>

			public float lineOpacityBehindObjects = 0.12f;

			/// <summary>Additional opacity multiplier of solid objects when behind or inside other objects</summary>

			public float solidOpacityBehindObjects = 0.45f;

			/// <summary>Additional opacity multiplier of text when behind or inside other objects</summary>

			public float textOpacityBehindObjects = 0.9f;
		}

		[SerializeField]
		private int version;
		public Settings settings;

		public static Settings DefaultSettings => new Settings();

		public static DrawingSettings GetSettingsAsset () {
#if UNITY_EDITOR
			System.IO.Directory.CreateDirectory(Application.dataPath + "/../" + System.IO.Path.GetDirectoryName(SettingsPath));
			var settings = AssetDatabase.LoadAssetAtPath<DrawingSettings>(SettingsPath);
			if (settings == null && AssetDatabase.LoadAssetAtPath<DrawingSettings>(SettingsPathCompatibility) != null) {
				AssetDatabase.MoveAsset(SettingsPathCompatibility, SettingsPath);
				settings = AssetDatabase.LoadAssetAtPath<DrawingSettings>(SettingsPath);
			}
			if (settings == null) {
				settings = ScriptableObject.CreateInstance<DrawingSettings>();
				settings.settings = DefaultSettings;
				settings.version = 0;
				AssetDatabase.CreateAsset(settings, SettingsPath);
				AssetDatabase.SaveAssets();
			}
#else
			var settings = Resources.Load<DrawingSettings>("ALINE");
#endif
			return settings;
		}
	}
}
