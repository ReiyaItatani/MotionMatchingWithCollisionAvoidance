#pragma warning disable 649 // Field `Drawing.GizmoContext.activeTransform' is never assigned to, and will always have its default value `null'. Not used outside of the unity editor.
using UnityEngine;
using System.Collections;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Rendering;
#if MODULE_RENDER_PIPELINES_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif
#if MODULE_RENDER_PIPELINES_HIGH_DEFINITION
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Drawing {
	/// <summary>Info about the current selection in the editor</summary>
	public static class GizmoContext {
#if UNITY_EDITOR
		static Transform activeTransform;
#endif

		static HashSet<Transform> selectedTransforms = new HashSet<Transform>();

		static internal bool drawingGizmos;

		/// <summary>Number of top-level transforms that are selected</summary>
		public static int selectionSize { get; private set; }

		internal static void Refresh () {
#if UNITY_EDITOR
			activeTransform = Selection.activeTransform;
			selectedTransforms.Clear();
			var topLevel = Selection.transforms;
			for (int i = 0; i < topLevel.Length; i++) selectedTransforms.Add(topLevel[i]);
			selectionSize = topLevel.Length;
#endif
		}

		/// <summary>
		/// True if the component is selected.
		/// This is a deep selection: even children of selected transforms are considered to be selected.
		/// </summary>
		public static bool InSelection (Component c) {
			if (!drawingGizmos) throw new System.Exception("Can only be used inside the ALINE library's gizmo drawing functions.");
			return InSelection(c.transform);
		}

		/// <summary>
		/// True if the transform is selected.
		/// This is a deep selection: even children of selected transforms are considered to be selected.
		/// </summary>
		public static bool InSelection (Transform tr) {
			if (!drawingGizmos) throw new System.Exception("Can only be used inside the ALINE library's gizmo drawing functions.");
			var leaf = tr;
			while (tr != null) {
				if (selectedTransforms.Contains(tr)) {
					selectedTransforms.Add(leaf);
					return true;
				}
				tr = tr.parent;
			}
			return false;
		}

		/// <summary>
		/// True if the component is shown in the inspector.
		/// The active selection is the GameObject that is currently visible in the inspector.
		/// </summary>
		public static bool InActiveSelection (Component c) {
			if (!drawingGizmos) throw new System.Exception("Can only be used inside the ALINE library's gizmo drawing functions.");
			return InActiveSelection(c.transform);
		}

		/// <summary>
		/// True if the transform is shown in the inspector.
		/// The active selection is the GameObject that is currently visible in the inspector.
		/// </summary>
		public static bool InActiveSelection (Transform tr) {
#if UNITY_EDITOR
			if (!drawingGizmos) throw new System.Exception("Can only be used inside the ALINE library's gizmo drawing functions.");
			return tr.transform == activeTransform;
#else
			return false;
#endif
		}
	}

	/// <summary>
	/// Every object that wants to draw gizmos should implement this interface.
	/// See: <see cref="Drawing.MonoBehaviourGizmos"/>
	/// </summary>
	public interface IDrawGizmos {
		void DrawGizmos();
	}

	public enum DetectedRenderPipeline {
		BuiltInOrCustom,
		HDRP,
		URP
	}

	/// <summary>
	/// Global script which draws debug items and gizmos.
	/// If a Draw.* method has been used or if any script inheriting from the <see cref="Drawing.MonoBehaviourGizmos"/> class is in the scene then an instance of this script
	/// will be created and put on a hidden GameObject.
	///
	/// It will inject drawing logic into any cameras that are rendered.
	///
	/// Usually you never have to interact with this class.
	/// </summary>
	[ExecuteAlways]
	[AddComponentMenu("")]
	public class DrawingManager : MonoBehaviour {
		public DrawingData gizmos;
		static List<IDrawGizmos> gizmoDrawers = new List<IDrawGizmos>();
		static DrawingManager _instance;
		bool framePassed;
		int lastFrameCount = int.MinValue;
#if UNITY_EDITOR
		bool builtGizmos;
#endif

		/// <summary>True if OnEnable has been called on this instance and OnDisable has not</summary>
		[SerializeField]
		bool actuallyEnabled;

		RedrawScope previousFrameRedrawScope;

		/// <summary>
		/// Allow rendering to cameras that render to RenderTextures.
		/// By default cameras which render to render textures are never rendered to.
		/// You may enable this if you wish.
		///
		/// See: <see cref="Drawing.CommandBuilder.cameraTargets"/>
		/// See: advanced (view in online documentation for working links)
		/// </summary>
		public static bool allowRenderToRenderTextures = false;
		public static bool drawToAllCameras = false;
		CommandBuffer commandBuffer;

		[System.NonSerialized]
		DetectedRenderPipeline detectedRenderPipeline = DetectedRenderPipeline.BuiltInOrCustom;

#if MODULE_RENDER_PIPELINES_UNIVERSAL
		HashSet<ScriptableRenderer> scriptableRenderersWithPass = new HashSet<ScriptableRenderer>();
		AlineURPRenderPassFeature renderPassFeature;
#endif

		public static DrawingManager instance {
			get {
				if (_instance == null) Init();
				return _instance;
			}
		}

#if UNITY_EDITOR
		[InitializeOnLoadMethod]
#endif
		public static void Init () {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			if (Unity.Jobs.LowLevel.Unsafe.JobsUtility.IsExecutingJob) throw new System.Exception("Draw.* methods cannot be called from inside a job. See the documentation for info about how to use drawing functions from the Unity Job System.");
#endif
			if (_instance != null) return;

			// Here one might try to look for existing instances of the class that haven't yet been enabled.
			// However, this turns out to be tricky.
			// Resources.FindObjectsOfTypeAll<T>() is the only call that includes HideInInspector GameObjects.
			// But it is hard to distinguish between objects that are internal ones which will never be enabled and objects that will be enabled.
			// Checking .gameObject.scene.isLoaded doesn't work reliably (object may be enabled and working even if isLoaded is false)
			// Checking .gameObject.scene.isValid doesn't work reliably (object may be enabled and working even if isValid is false)

			// So instead we just always create a new instance. This is not a particularly heavy operation and it only happens once per game, so why not.
			// The OnEnable call will clean up duplicate managers if there are any.

			var go = new GameObject("RetainedGizmos") {
				hideFlags = HideFlags.DontSave | HideFlags.NotEditable | HideFlags.HideInInspector | HideFlags.HideInHierarchy
			};
			_instance = go.AddComponent<DrawingManager>();
			if (Application.isPlaying) DontDestroyOnLoad(go);
		}

		/// <summary>Detects which render pipeline is being used and configures them for rendering</summary>
		void RefreshRenderPipelineMode () {
			var pipelineType = RenderPipelineManager.currentPipeline != null? RenderPipelineManager.currentPipeline.GetType() : null;

#if MODULE_RENDER_PIPELINES_HIGH_DEFINITION
			if (pipelineType == typeof(HDRenderPipeline)) {
				if (detectedRenderPipeline != DetectedRenderPipeline.HDRP) {
					detectedRenderPipeline = DetectedRenderPipeline.HDRP;
					if (!_instance.gameObject.TryGetComponent<CustomPassVolume>(out CustomPassVolume volume)) {
						volume = _instance.gameObject.AddComponent<CustomPassVolume>();
						volume.isGlobal = true;
						volume.injectionPoint = CustomPassInjectionPoint.AfterPostProcess;
						volume.customPasses.Add(new AlineHDRPCustomPass());
					}

					var asset = GraphicsSettings.defaultRenderPipeline as HDRenderPipelineAsset;
					if (asset != null) {
						if (!asset.currentPlatformRenderPipelineSettings.supportCustomPass) {
							Debug.LogWarning("ALINE: The current render pipeline has custom pass support disabled. ALINE will not be able to render anything. Please enable custom pass support on your HDRenderPipelineAsset.", asset);
						}
					}
				}
				return;
			}
#endif
#if MODULE_RENDER_PIPELINES_UNIVERSAL
			if (pipelineType == typeof(UniversalRenderPipeline)) {
				detectedRenderPipeline = DetectedRenderPipeline.URP;
				return;
			}
#endif
			detectedRenderPipeline = DetectedRenderPipeline.BuiltInOrCustom;
		}

#if UNITY_EDITOR
		void DelayedDestroy () {
			EditorApplication.update -= DelayedDestroy;
			// Check if the object still exists (it might have been destroyed in some other way already).
			if (gameObject) GameObject.DestroyImmediate(gameObject);
		}

		void OnPlayModeStateChanged (PlayModeStateChange change) {
			if (change == PlayModeStateChange.ExitingEditMode || change == PlayModeStateChange.ExitingPlayMode) {
				gizmos.sceneModeVersion++;
			}
		}
#endif

		void OnEnable () {
			if (_instance == null) _instance = this;

			// Ensure we don't have duplicate managers
			if (_instance != this) {
				// We cannot destroy the object while it is being enabled, so we need to delay it a bit
#if UNITY_EDITOR
				// This is only important in the editor to avoid a build-up of old managers.
				// In an actual game at most 1 (though in practice zero) old managers will be laying around.
				// It would be nice to use a coroutine for this instead, but unfortunately they do not work for objects marked with HideAndDontSave.
				EditorApplication.update += DelayedDestroy;
#endif
				return;
			}

			actuallyEnabled = true;
			if (gizmos == null) gizmos = new DrawingData();
			gizmos.frameRedrawScope = new RedrawScope(gizmos);
			Draw.builder = gizmos.GetBuiltInBuilder(false);
			Draw.ingame_builder = gizmos.GetBuiltInBuilder(true);
			commandBuffer = new CommandBuffer();
			commandBuffer.name = "ALINE Gizmos";

			// Callback when rendering with the built-in render pipeline
			Camera.onPostRender += PostRender;
			// Callback when rendering with a scriptable render pipeline
			UnityEngine.Rendering.RenderPipelineManager.beginFrameRendering += BeginFrameRendering;
			UnityEngine.Rendering.RenderPipelineManager.endCameraRendering += EndCameraRendering;
#if UNITY_EDITOR
			EditorApplication.update += OnUpdate;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
		}

		void BeginFrameRendering (ScriptableRenderContext context, Camera[] cameras) {
			RefreshRenderPipelineMode();

#if MODULE_RENDER_PIPELINES_UNIVERSAL
			if (detectedRenderPipeline == DetectedRenderPipeline.URP) {
				for (int i = 0; i < cameras.Length; i++) {
					var cam = cameras[i];
					var data = cam.GetUniversalAdditionalCameraData();
					if (data != null) {
						var renderer = data.scriptableRenderer;
						if (renderPassFeature == null) {
							renderPassFeature = ScriptableObject.CreateInstance<AlineURPRenderPassFeature>();
						}
						renderPassFeature.AddRenderPasses(renderer);
					}
				}
			}
#endif
		}

		void OnDisable () {
			if (!actuallyEnabled) return;
			actuallyEnabled = false;
			Camera.onPostRender -= PostRender;
			UnityEngine.Rendering.RenderPipelineManager.beginFrameRendering -= BeginFrameRendering;
			UnityEngine.Rendering.RenderPipelineManager.endCameraRendering -= EndCameraRendering;
#if UNITY_EDITOR
			EditorApplication.update -= OnUpdate;
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
			// Gizmos can be null here if this GameObject was duplicated by a user in the hierarchy.
			if (gizmos != null) {
				Draw.builder.DiscardAndDisposeInternal();
				Draw.ingame_builder.DiscardAndDisposeInternal();
				gizmos.ClearData();
			}
#if MODULE_RENDER_PIPELINES_UNIVERSAL
			if (renderPassFeature != null) {
				ScriptableObject.DestroyImmediate(renderPassFeature);
				renderPassFeature = null;
			}
#endif
		}

		// When enter play mode = reload scene & reload domain
		//	editor => play mode: OnDisable -> OnEnable (same object)
		//  play mode => editor: OnApplicationQuit (note: no OnDisable/OnEnable)
		// When enter play mode = reload scene & !reload domain
		//	editor => play mode: Nothing
		//  play mode => editor: OnApplicationQuit
		// When enter play mode = !reload scene & !reload domain
		//	editor => play mode: Nothing
		//  play mode => editor: OnApplicationQuit
		// OnDestroy is never really called for this object (unless Unity or the game quits I quess)

		// TODO: Should run in OnDestroy. OnApplicationQuit runs BEFORE OnDestroy (which we do not want)
		// private void OnApplicationQuit () {
		// Debug.Log("OnApplicationQuit");
		// Draw.builder.DiscardAndDisposeInternal();
		// Draw.ingame_builder.DiscardAndDisposeInternal();
		// gizmos.ClearData();
		// Draw.builder = gizmos.GetBuiltInBuilder(false);
		// Draw.ingame_builder = gizmos.GetBuiltInBuilder(true);
		// }

		void OnUpdate () {
			framePassed = true;
			if (Time.frameCount > lastFrameCount + 1) {
				// More than one frame old
				// It is possible no camera is being rendered at all.
				// Ensure we don't get any memory leaks from drawing items being queued every frame.
				CheckFrameTicking();

				// Note: We do not always want to call the above method here
				// because it is nicer to call it right after the cameras have been rendered.
				// Otherwise drawing items queued before OnUpdate or after OnUpdate may end up
				// in different frames (for the purposes of rendering gizmos)
			}
		}

		internal void ExecuteCustomRenderPass (ScriptableRenderContext context, Camera camera) {
			UnityEngine.Profiling.Profiler.BeginSample("ALINE");
			commandBuffer.Clear();
			SubmitFrame(camera, commandBuffer, true);
			context.ExecuteCommandBuffer(commandBuffer);
			UnityEngine.Profiling.Profiler.EndSample();
		}

		private void EndCameraRendering (ScriptableRenderContext context, Camera camera) {
			if (detectedRenderPipeline == DetectedRenderPipeline.BuiltInOrCustom) {
				// Execute the custom render pass after the camera has finished rendering.
				// For the HDRP and URP the render pass will already have been executed.
				// However for a custom render pipline we execute the rendering code here.
				// This is only best effort. It's impossible to be compatible with all custom render pipelines.
				// However it should work for most simple ones.
				// For Unity's built-in render pipeline the EndCameraRendering method will never be called.
				ExecuteCustomRenderPass(context, camera);
			}
		}

		void PostRender (Camera camera) {
			// This method is only called when using Unity's built-in render pipeline
			commandBuffer.Clear();
			SubmitFrame(camera, commandBuffer, false);
			UnityEngine.Profiling.Profiler.BeginSample("Executing command buffer");
			Graphics.ExecuteCommandBuffer(commandBuffer);
			UnityEngine.Profiling.Profiler.EndSample();
		}

		void CheckFrameTicking () {
			if (Time.frameCount != lastFrameCount) {
				framePassed = true;
				lastFrameCount = Time.frameCount;
				previousFrameRedrawScope = gizmos.frameRedrawScope;
				gizmos.frameRedrawScope = new RedrawScope(gizmos);
				Draw.builder.DisposeInternal();
				Draw.ingame_builder.DisposeInternal();
				Draw.builder = gizmos.GetBuiltInBuilder(false);
				Draw.ingame_builder = gizmos.GetBuiltInBuilder(true);
			} else if (framePassed && Application.isPlaying) {
				// Rendered frame passed without a game frame passing!
				// This might mean the game is paused.
				// Redraw gizmos while the game is paused.
				// It might also just mean that we are rendering with multiple cameras.
				previousFrameRedrawScope.Draw();
			}

			if (framePassed) {
				gizmos.TickFrame();
#if UNITY_EDITOR
				builtGizmos = false;
#endif
				framePassed = false;
			}
		}

		internal void SubmitFrame (Camera camera, CommandBuffer cmd, bool usingRenderPipeline) {
#if UNITY_EDITOR
			bool isSceneViewCamera = SceneView.currentDrawingSceneView != null && SceneView.currentDrawingSceneView.camera == camera;
#else
			bool isSceneViewCamera = false;
#endif
			// Do not include when rendering to a texture unless this is a scene view camera
			bool allowCameraDefault = allowRenderToRenderTextures || drawToAllCameras || camera.targetTexture == null || isSceneViewCamera;

			CheckFrameTicking();

			Submit(camera, cmd, usingRenderPipeline, allowCameraDefault);
		}

#if UNITY_EDITOR
		static System.Reflection.MethodInfo IsGizmosAllowedForObject = typeof(UnityEditor.EditorGUIUtility).GetMethod("IsGizmosAllowedForObject", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
		static System.Type AnnotationUtility = typeof(UnityEditor.PlayModeStateChange).Assembly?.GetType("UnityEditor.AnnotationUtility");
		System.Object[] cachedObjectParameterArray = new System.Object[1];
#endif

		bool use3dGizmos {
			get {
#if UNITY_EDITOR
				var use3dGizmosProperty = AnnotationUtility.GetProperty("use3dGizmos", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
				return (bool)use3dGizmosProperty.GetValue(null);
#else
				return true;
#endif
			}
		}

		Dictionary<System.Type, bool> typeToGizmosEnabled = new Dictionary<Type, bool>();

		bool ShouldDrawGizmos (UnityEngine.Object obj) {
#if UNITY_EDITOR
			// Use reflection to call EditorGUIUtility.IsGizmosAllowedForObject which is an internal method.
			// It is exactly the information we want though.
			// In case Unity has changed its API or something so that the method can no longer be found then just return true
			cachedObjectParameterArray[0] = obj;
			return IsGizmosAllowedForObject == null || (bool)IsGizmosAllowedForObject.Invoke(null, cachedObjectParameterArray);
#else
			return true;
#endif
		}

		void RemoveDestroyedGizmoDrawers () {
			UnityEngine.Profiling.Profiler.BeginSample("Filter destroyed objects");
			int j = 0;
			for (int i = 0; i < gizmoDrawers.Count; i++) {
				var mono = gizmoDrawers[i] as MonoBehaviour;
				if (mono) {
					gizmoDrawers[j] = gizmoDrawers[i];
					j++;
				}
			}
			gizmoDrawers.RemoveRange(j, gizmoDrawers.Count - j);
			UnityEngine.Profiling.Profiler.EndSample();
		}

#if UNITY_EDITOR
		void DrawGizmos (bool usingRenderPipeline) {
			UnityEngine.Profiling.Profiler.BeginSample("Refresh Selection Cache");
			GizmoContext.Refresh();
			UnityEngine.Profiling.Profiler.EndSample();
			UnityEngine.Profiling.Profiler.BeginSample("GizmosAllowed");
			typeToGizmosEnabled.Clear();
#if !UNITY_2022_1_OR_NEWER
			if (!usingRenderPipeline) {
#endif
			// Fill the typeToGizmosEnabled dict with info about which classes should be drawn
			// We take advantage of the fact that IsGizmosAllowedForObject only depends on the type of the object and if it is active and enabled
			// and not the specific object instance.
			// When using a render pipeline the ShouldDrawGizmos method cannot be used because it seems to occasionally crash Unity :(
			// So we need these two separate cases.
			// In Unity 2022.1 we can use a new utility class which is more robust.
			for (int i = gizmoDrawers.Count - 1; i >= 0; i--) {
				var tp = gizmoDrawers[i].GetType();
				if (!typeToGizmosEnabled.ContainsKey(tp)) {
#if UNITY_2022_1_OR_NEWER
					if (GizmoUtility.TryGetGizmoInfo(tp, out var gizmoInfo)) {
						typeToGizmosEnabled[tp] = gizmoInfo.gizmoEnabled;
					} else {
						typeToGizmosEnabled[tp] = true;
					}
#else
					if ((gizmoDrawers[i] as MonoBehaviour).isActiveAndEnabled) {
						typeToGizmosEnabled[tp] = ShouldDrawGizmos((UnityEngine.Object)gizmoDrawers[i]);
					}
#endif
				}
			}
#if !UNITY_2022_1_OR_NEWER
		}
#endif

			UnityEngine.Profiling.Profiler.EndSample();

			// Set the current frame's redraw scope to an empty scope.
			// This is because gizmos are rendered every frame anyway so we never want to redraw them.
			// The frame redraw scope is otherwise used when the game has been paused.
			var frameRedrawScope = gizmos.frameRedrawScope;
			gizmos.frameRedrawScope = default(RedrawScope);

#if UNITY_EDITOR && UNITY_2020_1_OR_NEWER
			var currentStage = StageUtility.GetCurrentStage();
			var isInNonMainStage = currentStage != StageUtility.GetMainStage();
#endif

			// This would look nicer as a 'using' block, but built-in command builders
			// cannot be disposed normally to prevent user error.
			// The try-finally is equivalent to a 'using' block.
			var gizmoBuilder = gizmos.GetBuiltInBuilder();
			try {
				// Replace Draw.builder with a custom one just for gizmos
				var debugBuilder = Draw.builder;
				Draw.builder = gizmoBuilder;

				UnityEngine.Profiling.Profiler.BeginSample("DrawGizmos");
				GizmoContext.drawingGizmos = true;
				if (usingRenderPipeline) {
					for (int i = gizmoDrawers.Count - 1; i >= 0; i--) {
						var mono = gizmoDrawers[i] as MonoBehaviour;
#if UNITY_EDITOR && UNITY_2020_1_OR_NEWER
						// True if the scene is in isolation mode (e.g. focusing on a single prefab) and this object is not part of that sub-stage
						var disabledDueToIsolationMode = isInNonMainStage && StageUtility.GetStage(mono.gameObject) != currentStage;
#else
						var disabledDueToIsolationMode = false;
#endif
#if UNITY_2022_1_OR_NEWER
						var gizmosEnabled = mono.isActiveAndEnabled && typeToGizmosEnabled[gizmoDrawers[i].GetType()];
#else
						var gizmosEnabled = mono.isActiveAndEnabled;
#endif
						if (gizmosEnabled && (mono.hideFlags & HideFlags.HideInHierarchy) == 0 && !disabledDueToIsolationMode) {
							try {
								gizmoDrawers[i].DrawGizmos();
							} catch (System.Exception e) {
								Debug.LogException(e, mono);
							}
						}
					}
				} else {
					for (int i = gizmoDrawers.Count - 1; i >= 0; i--) {
						var mono = gizmoDrawers[i] as MonoBehaviour;
#if UNITY_EDITOR && UNITY_2020_1_OR_NEWER
						// True if the scene is in isolation mode (e.g. focusing on a single prefab) and this object is not part of that sub-stage
						var disabledDueToIsolationMode = isInNonMainStage && StageUtility.GetStage(mono.gameObject) != currentStage;
#else
						var disabledDueToIsolationMode = false;
#endif
						if (mono.isActiveAndEnabled && (mono.hideFlags & HideFlags.HideInHierarchy) == 0 && typeToGizmosEnabled[gizmoDrawers[i].GetType()] && !disabledDueToIsolationMode) {
							try {
								gizmoDrawers[i].DrawGizmos();
							} catch (System.Exception e) {
								Debug.LogException(e, mono);
							}
						}
					}
				}
				GizmoContext.drawingGizmos = false;
				UnityEngine.Profiling.Profiler.EndSample();

				// Revert to the original builder
				Draw.builder = debugBuilder;
			} finally {
				gizmoBuilder.DisposeInternal();
			}

			gizmos.frameRedrawScope = frameRedrawScope;

			// Schedule jobs that may have been scheduled while drawing gizmos
			JobHandle.ScheduleBatchedJobs();
		}
#endif

		/// <summary>Submit a camera for rendering.</summary>
		/// <param name="allowCameraDefault">Indicates if built-in command builders and custom ones without a custom CommandBuilder.cameraTargets should render to this camera.</param>
		void Submit (Camera camera, CommandBuffer cmd, bool usingRenderPipeline, bool allowCameraDefault) {
			// This must always be done to avoid a potential memory leak if gizmos are never drawn
			RemoveDestroyedGizmoDrawers();
#if UNITY_EDITOR
			bool drawGizmos = Handles.ShouldRenderGizmos() || drawToAllCameras;
			// Only build gizmos if a camera actually needs them.
			// This is only done for the first camera that needs them each frame.
			if (drawGizmos && !builtGizmos && allowCameraDefault) {
				builtGizmos = true;
				DrawGizmos(usingRenderPipeline);
			}
#else
			bool drawGizmos = false;
#endif

			UnityEngine.Profiling.Profiler.BeginSample("Submit Gizmos");
			Draw.builder.DisposeInternal();
			Draw.ingame_builder.DisposeInternal();
			gizmos.Render(camera, drawGizmos, cmd, allowCameraDefault);
			Draw.builder = gizmos.GetBuiltInBuilder(false);
			Draw.ingame_builder = gizmos.GetBuiltInBuilder(true);
			UnityEngine.Profiling.Profiler.EndSample();
		}

		/// <summary>
		/// Registers an object for gizmo drawing.
		/// The DrawGizmos method on the object will be called every frame until it is destroyed (assuming there are cameras with gizmos enabled).
		/// </summary>
		public static void Register (IDrawGizmos item) {
			gizmoDrawers.Add(item);
		}

		/// <summary>
		/// Get an empty builder for queuing drawing commands.
		///
		/// <code>
		/// // Create a new CommandBuilder
		/// using (var draw = DrawingManager.GetBuilder()) {
		///     // Use the exact same API as the global Draw class
		///     draw.WireBox(Vector3.zero, Vector3.one);
		/// }
		/// </code>
		/// See: <see cref="Drawing.CommandBuilder"/>
		/// </summary>
		/// <param name="renderInGame">If true, this builder will be rendered in standalone games and in the editor even if gizmos are disabled.
		/// If false, it will only be rendered in the editor when gizmos are enabled.</param>
		public static CommandBuilder GetBuilder (bool renderInGame = false) {
			return instance.gizmos.GetBuilder(renderInGame);
		}

		/// <summary>
		/// Get an empty builder for queuing drawing commands.
		///
		/// See: <see cref="Drawing.CommandBuilder"/>
		/// </summary>
		/// <param name="redrawScope">Scope for this command builder. See #GetRedrawScope.</param>
		/// <param name="renderInGame">If true, this builder will be rendered in standalone games and in the editor even if gizmos are disabled.
		/// If false, it will only be rendered in the editor when gizmos are enabled.</param>
		public static CommandBuilder GetBuilder (RedrawScope redrawScope, bool renderInGame = false) {
			return instance.gizmos.GetBuilder(redrawScope, renderInGame);
		}

		/// <summary>
		/// Get an empty builder for queuing drawing commands.
		/// TODO: Example usage.
		///
		/// See: <see cref="Drawing.CommandBuilder"/>
		/// </summary>
		/// <param name="hasher">Hash of whatever inputs you used to generate the drawing data.</param>
		/// <param name="redrawScope">Scope for this command builder. See #GetRedrawScope.</param>
		/// <param name="renderInGame">If true, this builder will be rendered in standalone games and in the editor even if gizmos are disabled.</param>
		public static CommandBuilder GetBuilder (DrawingData.Hasher hasher, RedrawScope redrawScope = default, bool renderInGame = false) {
			return instance.gizmos.GetBuilder(hasher, redrawScope, renderInGame);
		}

		/// <summary>
		/// A scope which can be used to draw things over multiple frames.
		/// You can use <see cref="GetBuilder(RedrawScope,bool)"/> to get a builder with a given redraw scope.
		/// After you have disposed the builder you may call <see cref="Drawing.RedrawScope.Draw"/> in any number of future frames to render the command builder again.
		///
		/// <code>
		/// private RedrawScope redrawScope;
		///
		/// void Start () {
		///     redrawScope = DrawingManager.GetRedrawScope();
		///     using (var builder = DrawingManager.GetBuilder(redrawScope)) {
		///         builder.WireSphere(Vector3.zero, 1.0f, Color.red);
		///     }
		/// }
		///
		/// void Update () {
		///     redrawScope.Draw();
		/// }
		/// </code>
		///
		/// Note: The data will only be kept if <see cref="Drawing.RedrawScope.Draw"/> is called every frame.
		/// The command builder's data will be cleared if you do not call <see cref="Drawing.RedrawScope.Draw"/> in a future frame.
		/// After that point calling <see cref="Drawing.RedrawScope.Draw"/> will not do anything.
		/// </summary>
		public static RedrawScope GetRedrawScope () {
			return new RedrawScope(instance.gizmos);
		}
	}
}
