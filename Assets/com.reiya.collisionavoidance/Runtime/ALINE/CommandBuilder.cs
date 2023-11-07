using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace Drawing {
	using static DrawingData;
	using BitPackedMeta = DrawingData.BuilderData.BitPackedMeta;
	using Drawing.Text;

	/// <summary>
	/// Specifies text alignment relative to an anchor point.
	///
	/// <code>
	/// Draw.Label2D(transform.position, "Hello World", 14, LabelAlignment.TopCenter);
	/// </code>
	/// <code>
	/// // Draw the label 20 pixels below the object
	/// Draw.Label2D(transform.position, "Hello World", 14, LabelAlignment.TopCenter.withPixelOffset(0, -20));
	/// </code>
	///
	/// See: \reflink{Draw.Label2D}
	/// See: \reflink{Draw.Label3D}
	/// </summary>
	public struct LabelAlignment {
		/// <summary>
		/// Where on the text's bounding box to anchor the text.
		///
		/// The pivot is specified in relative coordinates, where (0,0) is the bottom left corner and (1,1) is the top right corner.
		/// </summary>
		public float2 relativePivot;
		/// <summary>How much to move the text in screen-space</summary>
		public float2 pixelOffset;

		public static readonly LabelAlignment TopLeft = new LabelAlignment { relativePivot = new float2(0.0f, 1.0f), pixelOffset = new float2(0, 0) };
		public static readonly LabelAlignment MiddleLeft = new LabelAlignment { relativePivot = new float2(0.0f, 0.5f), pixelOffset = new float2(0, 0) };
		public static readonly LabelAlignment BottomLeft = new LabelAlignment { relativePivot = new float2(0.0f, 0.0f), pixelOffset = new float2(0, 0) };
		public static readonly LabelAlignment BottomCenter = new LabelAlignment { relativePivot = new float2(0.5f, 0.0f), pixelOffset = new float2(0, 0) };
		public static readonly LabelAlignment BottomRight = new LabelAlignment { relativePivot = new float2(1.0f, 0.0f), pixelOffset = new float2(0, 0) };
		public static readonly LabelAlignment MiddleRight = new LabelAlignment { relativePivot = new float2(1.0f, 0.5f), pixelOffset = new float2(0, 0) };
		public static readonly LabelAlignment TopRight = new LabelAlignment { relativePivot = new float2(1.0f, 1.0f), pixelOffset = new float2(0, 0) };
		public static readonly LabelAlignment TopCenter = new LabelAlignment { relativePivot = new float2(0.5f, 1.0f), pixelOffset = new float2(0, 0) };
		public static readonly LabelAlignment Center = new LabelAlignment { relativePivot = new float2(0.5f, 0.5f), pixelOffset = new float2(0, 0) };

		/// <summary>
		/// Moves the text by the specified amount of pixels in screen-space.
		///
		/// <code>
		/// // Draw the label 20 pixels below the object
		/// Draw.Label2D(transform.position, "Hello World", 14, LabelAlignment.TopCenter.withPixelOffset(0, -20));
		/// </code>
		/// </summary>
		public LabelAlignment withPixelOffset (float x, float y) {
			return new LabelAlignment {
					   relativePivot = this.relativePivot,
					   pixelOffset = new float2(x, y),
			};
		}
	}

	/// <summary>Maximum allowed delay for a job that is drawing to a command buffer</summary>
	public enum AllowedDelay {
		/// <summary>
		/// If the job is not complete at the end of the frame, drawing will block until it is completed.
		/// This is recommended for most jobs that are expected to complete within a single frame.
		/// </summary>
		EndOfFrame,
		/// <summary>
		/// Wait indefinitely for the job to complete, and only submit the results for rendering once it is done.
		/// This is recommended for long running jobs that may take many frames to complete.
		/// </summary>
		Infinite,
	}

	/// <summary>Some static fields that need to be in a separate class because Burst doesn't support them</summary>
	static class MeshLayouts {
		internal static readonly VertexAttributeDescriptor[] MeshLayout = {
			new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
			new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
			new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
			new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
		};

		internal static readonly VertexAttributeDescriptor[] MeshLayoutText = {
			new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
			new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
			new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
		};
	}

	/// <summary>Some static fields that need to be in a separate class because Burst doesn't support them</summary>
	static class CommandBuilderSamplers {
		internal static readonly CustomSampler samplerConvert = CustomSampler.Create("Convert");
		internal static readonly CustomSampler samplerLayout = CustomSampler.Create("SetLayout");
		internal static readonly CustomSampler samplerUpdateVert = CustomSampler.Create("UpdateVertices");
		internal static readonly CustomSampler samplerUpdateInd = CustomSampler.Create("UpdateIndices");
		internal static readonly CustomSampler samplerSubmesh = CustomSampler.Create("Submesh");
		internal static readonly CustomSampler samplerUpdateBuffer = CustomSampler.Create("UpdateComputeBuffer");
	}

	/// <summary>
	/// Builder for drawing commands.
	/// You can use this to queue many drawing commands. The commands will be queued for rendering when you call the Dispose method.
	/// It is recommended that you use the <a href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/using-statement">using statement</a> which automatically calls the Dispose method.
	///
	/// <code>
	/// // Create a new CommandBuilder
	/// using (var draw = DrawingManager.GetBuilder()) {
	///     // Use the exact same API as the global Draw class
	///     draw.WireBox(Vector3.zero, Vector3.one);
	/// }
	/// </code>
	///
	/// Warning: You must call either <see cref="Dispose"/> or <see cref="DiscardAndDispose"/> when you are done with this object to avoid memory leaks.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	[BurstCompile]
	public partial struct CommandBuilder : IDisposable {
		// Note: Many fields/methods are explicitly marked as private. This is because doxygen otherwise thinks they are public by default (like struct members are in c++)

		[NativeDisableUnsafePtrRestriction]
		private unsafe UnsafeAppendBuffer* buffer;

		private GCHandle gizmos;

		[NativeSetThreadIndex]
		private int threadIndex;

		private DrawingData.BuilderData.BitPackedMeta uniqueID;

		internal CommandBuilder(DrawingData gizmos, Hasher hasher, RedrawScope frameRedrawScope, RedrawScope customRedrawScope, bool isGizmos, bool isBuiltInCommandBuilder, int sceneModeVersion) {
			// We need to use a GCHandle instead of a normal reference to be able to pass this object to burst compiled function pointers.
			// The NativeSetClassTypeToNullOnSchedule unfortunately only works together with the job system, not with raw functions.
			this.gizmos = GCHandle.Alloc(gizmos, GCHandleType.Normal);

			threadIndex = 0;
			uniqueID = gizmos.data.Reserve(isBuiltInCommandBuilder);
			gizmos.data.Get(uniqueID).Init(hasher, frameRedrawScope, customRedrawScope, isGizmos, gizmos.GetNextDrawOrderIndex(), sceneModeVersion);
			unsafe {
				buffer = gizmos.data.Get(uniqueID).bufferPtr;
			}
		}

		private unsafe int BufferSize {
			get {
				return buffer->GetLength();
			}
			set {
				buffer->SetLength(value);
			}
		}

		static int GetSize (UnsafeAppendBuffer buffer) {
#if MODULE_COLLECTIONS_0_6_0_OR_NEWER
			return buffer.Length;
#else
			return buffer.Size;
#endif
		}

		/// <summary>
		/// Can be set to render specifically to these cameras.
		/// If you set this property to an array of cameras then this command builder will only be rendered
		/// to the specified cameras. Setting this property bypasses <see cref="Drawing.DrawingManager.allowRenderToRenderTextures"/>.
		/// The camera will be rendered to even if it renders to a render texture.
		///
		/// A null value indicates that all valid cameras should be rendered to. This is the default value.
		///
		/// <code>
		/// var draw = DrawingManager.GetBuilder(true);
		///
		/// draw.cameraTargets = new Camera[] { myCamera };
		/// // This sphere will only be rendered to myCamera
		/// draw.WireSphere(Vector3.zero, 0.5f, Color.black);
		/// draw.Dispose();
		/// </code>
		///
		/// See: advanced (view in online documentation for working links)
		/// </summary>
		public Camera[] cameraTargets {
			get {
				if (gizmos.IsAllocated && gizmos.Target != null) {
					var target = gizmos.Target as DrawingData;
					if (target.data.StillExists(uniqueID)) {
						return target.data.Get(uniqueID).meta.cameraTargets;
					}
				}
				throw new System.Exception("Cannot get cameraTargets because the command builder has already been disposed or does not exist.");
			}
			set {
				if (uniqueID.isBuiltInCommandBuilder) throw new System.Exception("You cannot set the camera targets for a built-in command builder. Create a custom command builder instead.");
				if (gizmos.IsAllocated && gizmos.Target != null) {
					var target = gizmos.Target as DrawingData;
					if (!target.data.StillExists(uniqueID)) {
						throw new System.Exception("Cannot set cameraTargets because the command builder has already been disposed or does not exist.");
					}
					target.data.Get(uniqueID).meta.cameraTargets = value;
				}
			}
		}

		/// <summary>Submits this command builder for rendering</summary>
		public void Dispose () {
			if (uniqueID.isBuiltInCommandBuilder) throw new System.Exception("You cannot dispose a built-in command builder");
			DisposeInternal();
		}

		/// <summary>
		/// Disposes this command builder after the given job has completed.
		///
		/// This is convenient if you are using the entity-component-system/burst in Unity and don't know exactly when the job will complete.
		///
		/// You will not be able to use this command builder on the main thread anymore.
		///
		/// See: job-system (view in online documentation for working links)
		/// </summary>
		/// <param name="dependency">The job that must complete before this command builder is disposed.</param>
		/// <param name="allowedDelay">Whether to block on this dependency before rendering the current frame or not.
		///    If the job is expected to complete during a single frame, leave at the default of \reflink{AllowedDelay.EndOfFrame}.
		///    But if the job is expected to take multiple frames to complete, you can set this to \reflink{AllowedDelay.Infinite}.</param>
		public void DisposeAfter (JobHandle dependency, AllowedDelay allowedDelay = AllowedDelay.EndOfFrame) {
			if (!gizmos.IsAllocated) throw new System.Exception("You cannot dispose an invalid command builder. Are you trying to dispose it twice?");
			try {
				if (gizmos.IsAllocated && gizmos.Target != null) {
					var target = gizmos.Target as DrawingData;
					if (!target.data.StillExists(uniqueID)) {
						throw new System.Exception("Cannot dispose the command builder because the drawing manager has been destroyed");
					}
					target.data.Get(uniqueID).SubmitWithDependency(gizmos, dependency, allowedDelay);
				}
			} finally {
				this = default;
			}
		}

		internal void DisposeInternal () {
			if (!gizmos.IsAllocated) throw new System.Exception("You cannot dispose an invalid command builder. Are you trying to dispose it twice?");
			try {
				if (gizmos.IsAllocated && gizmos.Target != null) {
					var target = gizmos.Target as DrawingData;
					if (!target.data.StillExists(uniqueID)) {
						throw new System.Exception("Cannot dispose the command builder because the drawing manager has been destroyed");
					}
					target.data.Get(uniqueID).Submit(gizmos.Target as DrawingData);
				}
			} finally {
				gizmos.Free();
				this = default;
			}
		}

		/// <summary>
		/// Discards the contents of this command builder without rendering anything.
		/// If you are not going to draw anything (i.e. you do not call the <see cref="Dispose"/> method) then you must call this method to avoid
		/// memory leaks.
		/// </summary>
		public void DiscardAndDispose () {
			if (uniqueID.isBuiltInCommandBuilder) throw new System.Exception("You cannot dispose a built-in command builder");
			DiscardAndDisposeInternal();
		}

		internal void DiscardAndDisposeInternal () {
			try {
				if (gizmos.IsAllocated && gizmos.Target != null) {
					var target = gizmos.Target as DrawingData;
					if (!target.data.StillExists(uniqueID)) {
						throw new System.Exception("Cannot dispose the command builder because the drawing manager has been destroyed");
					}
					target.data.Release(uniqueID);
				}
			} finally {
				gizmos.Free();
				this = default;
			}
		}

		/// <summary>
		/// Pre-allocates the internal buffer to an additional size bytes.
		/// This can give you a minor performance boost if you are drawing a lot of things.
		///
		/// Note: Only resizes the buffer for the current thread.
		/// </summary>
		public void Preallocate (int size) {
			Reserve(size);
		}

		/// <summary>Internal rendering command</summary>
		private enum Command {
			PushColorInline = 1 << 8,
			PushColor = 0,
			PopColor,
			PushMatrix,
			PushSetMatrix,
			PopMatrix,
			Line,
			Circle,
			CircleXZ,
			Disc,
			DiscXZ,
			SphereOutline,
			Box,
			WirePlane,
			PushPersist,
			PopPersist,
			Text,
			Text3D,
			PushLineWidth,
			PopLineWidth,
			CaptureState,
		}

		/// <summary>Holds rendering data for a line</summary>
		private struct LineData {
			public float3 a, b;
		}

		private struct LineDataV3 {
			public Vector3 a, b;
		}

		/// <summary>Holds rendering data for a circle</summary>
		private struct CircleXZData {
			public float3 center;
			public float radius, startAngle, endAngle;
		}

		/// <summary>Holds rendering data for a circle</summary>
		private struct CircleData {
			public float3 center;
			public float3 normal;
			public float radius;
		}

		/// <summary>Holds rendering data for a sphere</summary>
		private struct SphereData {
			public float3 center;
			public float radius;
		}

		/// <summary>Holds rendering data for a box</summary>
		private struct BoxData {
			public float3 center;
			public float3 size;
		}

		private struct PlaneData {
			public float3 center;
			public quaternion rotation;
			public float2 size;
		}

		private struct PersistData {
			public float endTime;
		}

		internal struct LineWidthData {
			public float pixels;
			public bool automaticJoins;
		}



		private struct TextData {
			public float3 center;
			public LabelAlignment alignment;
			public float sizeInPixels;
			public int numCharacters;
		}

		private struct TextData3D {
			public float3 center;
			public quaternion rotation;
			public LabelAlignment alignment;
			public float size;
			public int numCharacters;
		}

		/// <summary>Ensures the buffer has room for at least N more bytes</summary>
		private void Reserve (int additionalSpace) {
			unsafe {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if (buffer == null) throw new System.Exception("CommandBuilder does not have a valid buffer. Is it properly initialized?");
#endif
				if (threadIndex != 0) {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
					if (threadIndex < 0 || threadIndex >= JobsUtility.MaxJobThreadCount) throw new System.Exception("Thread index outside the expected range");
					if (uniqueID.isBuiltInCommandBuilder) throw new System.Exception("You should use a custom command builder when using the Unity Job System. Take a look at the documentation for more info.");
#endif

					//if (BufferSize + additionalSpace > buffer->Capacity) throw new System.Exception("Buffer is too small. Preallocate a larger buffer using the CommandBuffer.Preallocate method.");
					buffer += threadIndex;
					threadIndex = 0;
				}

#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if (BufferSize == 0) {
					// Exploit the fact that right after this package has drawn gizmos the buffers will be empty
					// and the next task is that Unity will render its own internal gizmos.
					// We can therefore easily (and without a high performance cost)
					// trap accidental Draw.* calls from OnDrawGizmos functions
					// by doing this check when the buffer is empty.
					AssertNotRendering();
				}
#endif

				var newLength = BufferSize + additionalSpace;
				if (newLength > buffer->Capacity) {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
					// This really should run every time we access the buffer... but that would be a bit slow
					// This code will catch the error eventually.
					AssertBufferExists();
#endif
					buffer->SetCapacity(math.max(newLength, BufferSize * 2));
				}
			}
		}

		[BurstDiscard]
		private void AssertBufferExists () {
			if (!gizmos.IsAllocated || gizmos.Target == null || !(gizmos.Target as DrawingData).data.StillExists(uniqueID)) {
				// This command builder is invalid, clear all data on it to prevent it being used again
				this = default;
				throw new System.Exception("This command builder no longer exists. Are you trying to draw to a command builder which has already been disposed?");
			}
		}

		[BurstDiscard]
		static void AssertNotRendering () {
			// Some checking to see if drawing is being done from inside OnDrawGizmos
			if (!GizmoContext.drawingGizmos && !JobsUtility.IsExecutingJob) {
				// Inspect the stack-trace to be able to provide more helpful error messages
				var st = StackTraceUtility.ExtractStackTrace();
				if (st.Contains("OnDrawGizmos")) {
					throw new System.Exception("You are trying to use Draw.* functions from within Unity's OnDrawGizmos function. Use this package's gizmo callbacks instead (see the documentation).");
				}
			}
		}

		private void Reserve<A>() where A : struct {
			Reserve(UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<A>());
		}

		private void Reserve<A, B>() where A : struct where B : struct {
			Reserve(UnsafeUtility.SizeOf<Command>() * 2 + UnsafeUtility.SizeOf<A>() + UnsafeUtility.SizeOf<B>());
		}

		private void Reserve<A, B, C>() where A : struct where B : struct where C : struct {
			Reserve(UnsafeUtility.SizeOf<Command>() * 3 + UnsafeUtility.SizeOf<A>() + UnsafeUtility.SizeOf<B>() + UnsafeUtility.SizeOf<C>());
		}

		private unsafe void Add<T>(T value) where T : struct {
			int num = UnsafeUtility.SizeOf<T>();

			unsafe {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				UnityEngine.Assertions.Assert.IsTrue(BufferSize + UnsafeUtility.SizeOf<T>() <= buffer->Capacity);
#endif
				UnsafeUtility.CopyStructureToPtr(ref value, (void*)((byte*)buffer->Ptr + BufferSize));
				BufferSize += num;
			}
		}

		public struct ScopeMatrix : IDisposable {
			internal unsafe UnsafeAppendBuffer* buffer;
			internal GCHandle gizmos;
			internal BitPackedMeta uniqueID;
			public void Dispose () {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if (gizmos.IsAllocated && gizmos.Target != null && !(gizmos.Target as DrawingData).data.StillExists(uniqueID)) throw new System.InvalidOperationException("The drawing instance this matrix scope belongs to no longer exists. Matrix scopes cannot survive for longer than a frame unless you have a custom drawing instance. Are you using a matrix scope inside a coroutine?");
#endif
				unsafe {
					new CommandBuilder { gizmos = gizmos, buffer = buffer, threadIndex = 0, uniqueID = uniqueID }.PopMatrix();
					buffer = null;
				}
			}
		}

		public struct ScopeColor : IDisposable {
			internal unsafe UnsafeAppendBuffer* buffer;
			internal GCHandle gizmos;
			internal BitPackedMeta uniqueID;
			public void Dispose () {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if (gizmos.IsAllocated && gizmos.Target != null && !(gizmos.Target as DrawingData).data.StillExists(uniqueID)) throw new System.InvalidOperationException("The drawing instance this color scope belongs to no longer exists. Color scopes cannot survive for longer than a frame unless you have a custom drawing instance. Are you using a color scope inside a coroutine?");
#endif
				unsafe {
					// TODO: Just save the whole CommandBuilder? An extra 4 bytes for the thread index, but possibly faster due to less copying
					new CommandBuilder { gizmos = gizmos, buffer = buffer, threadIndex = 0, uniqueID = uniqueID }.PopColor();
					buffer = null;
				}
			}
		}

		public struct ScopePersist : IDisposable {
			internal unsafe UnsafeAppendBuffer* buffer;
			internal GCHandle gizmos;
			internal BitPackedMeta uniqueID;
			public void Dispose () {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if (gizmos.IsAllocated && gizmos.Target != null && !(gizmos.Target as DrawingData).data.StillExists(uniqueID)) throw new System.InvalidOperationException("The drawing instance this persist scope belongs to no longer exists. Persist scopes cannot survive for longer than a frame unless you have a custom drawing instance. Are you using a persist scope inside a coroutine?");
#endif
				unsafe {
					new CommandBuilder { gizmos = gizmos, buffer = buffer, threadIndex = 0, uniqueID = uniqueID }.PopDuration();
					buffer = null;
				}
			}
		}

		/// <summary>
		/// Scope that does nothing.
		/// Used for optimization in standalone builds.
		/// </summary>
		public struct ScopeEmpty : IDisposable {
			public void Dispose () {
			}
		}

		public struct ScopeLineWidth : IDisposable {
			internal unsafe UnsafeAppendBuffer* buffer;
			internal GCHandle gizmos;
			internal BitPackedMeta uniqueID;
			public void Dispose () {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if (gizmos.IsAllocated && gizmos.Target != null && !(gizmos.Target as DrawingData).data.StillExists(uniqueID)) throw new System.InvalidOperationException("The drawing instance this line width scope belongs to no longer exists. Line width scopes cannot survive for longer than a frame unless you have a custom drawing instance. Are you using a line width scope inside a coroutine?");
#endif
				unsafe {
					new CommandBuilder { gizmos = gizmos, buffer = buffer, threadIndex = 0, uniqueID = uniqueID }.PopLineWidth();
					buffer = null;
				}
			}
		}

		/// <summary>
		/// Scope to draw multiple things with an implicit matrix transformation.
		/// All coordinates for items drawn inside the scope will be multiplied by the matrix.
		/// If WithMatrix scopes are nested then coordinates are multiplied by all nested matrices in order.
		///
		/// <code>
		/// using (Draw.InLocalSpace(transform)) {
		///     // Draw a box at (0,0,0) relative to the current object
		///     // This means it will show up at the object's position
		///     Draw.WireBox(Vector3.zero, Vector3.one);
		/// }
		///
		/// // Equivalent code using the lower level WithMatrix scope
		/// using (Draw.WithMatrix(transform.localToWorldMatrix)) {
		///     Draw.WireBox(Vector3.zero, Vector3.one);
		/// }
		/// </code>
		///
		/// See: <see cref="InLocalSpace"/>
		/// </summary>
		[BurstDiscard]
		public ScopeMatrix WithMatrix (Matrix4x4 matrix) {
			PushMatrix(matrix);
			// TODO: Keep track of alive scopes and prevent dispose unless all scopes have been disposed
			unsafe {
				return new ScopeMatrix { buffer = buffer, gizmos = gizmos, uniqueID = uniqueID };
			}
		}

		/// <summary>
		/// Scope to draw multiple things with the same color.
		///
		/// <code>
		/// void Update () {
		///     using (Draw.WithColor(Color.red)) {
		///         Draw.Line(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
		///         Draw.Line(new Vector3(0, 0, 0), new Vector3(0, 1, 2));
		///     }
		/// }
		/// </code>
		///
		/// Any command that is passed an explicit color parameter will override this color.
		/// If another color scope is nested inside this one then that scope will override this color.
		/// </summary>
		[BurstDiscard]
		public ScopeColor WithColor (Color color) {
			PushColor(color);
			unsafe {
				return new ScopeColor { buffer = buffer, gizmos = gizmos, uniqueID = uniqueID };
			}
		}

		/// <summary>
		/// Scope to draw multiple things for a longer period of time.
		///
		/// Normally drawn items will only be rendered for a single frame.
		/// Using a persist scope you can make the items be drawn for any amount of time.
		///
		/// <code>
		/// void Update () {
		///     using (Draw.WithDuration(1.0f)) {
		///         var offset = Time.time;
		///         Draw.Line(new Vector3(offset, 0, 0), new Vector3(offset, 0, 1));
		///     }
		/// }
		/// </code>
		///
		/// Note: Outside of play mode the duration is measured against Unity's Time.realtimeSinceStartup.
		///
		/// Warning: It is recommended not to use this inside a DrawGizmos callback since DrawGizmos is called every frame anyway.
		/// </summary>
		/// <param name="duration">How long the drawn items should persist in seconds.</param>

		[BurstDiscard]
		public ScopePersist WithDuration (float duration) {
			PushDuration(duration);
			unsafe {
				return new ScopePersist { buffer = buffer, gizmos = gizmos, uniqueID = uniqueID };
			}
		}

		/// <summary>
		/// Scope to draw multiple things with a given line width.
		///
		/// Note that the line join algorithm is a quite simple one optimized for speed. It normally looks good on a 2D plane, but if the polylines curve a lot in 3D space then
		/// it can look odd from some angles.
		///
		/// [Open online documentation to see images]
		///
		/// In the picture the top row has automaticJoins enabled and in the bottom row it is disabled.
		/// </summary>
		/// <param name="pixels">Line width in pixels</param>
		/// <param name="automaticJoins">If true then sequences of lines that are adjacent will be automatically joined at their vertices. This typically produces nicer polylines without weird gaps.</param>
		[BurstDiscard]
		public ScopeLineWidth WithLineWidth (float pixels, bool automaticJoins = true) {
			PushLineWidth(pixels, automaticJoins);
			unsafe {
				return new ScopeLineWidth { buffer = buffer, gizmos = gizmos, uniqueID = uniqueID };
			}
		}

		/// <summary>
		/// Scope to draw multiple things relative to a transform object.
		/// All coordinates for items drawn inside the scope will be multiplied by the transform's localToWorldMatrix.
		///
		/// <code>
		/// void Update () {
		///     using (Draw.InLocalSpace(transform)) {
		///         // Draw a box at (0,0,0) relative to the current object
		///         // This means it will show up at the object's position
		///         // The box is also rotated and scaled with the transform
		///         Draw.WireBox(Vector3.zero, Vector3.one);
		///     }
		/// }
		/// </code>
		///
		/// [Open online documentation to see videos]
		/// </summary>
		[BurstDiscard]
		public ScopeMatrix InLocalSpace (Transform transform) {
			return WithMatrix(transform.localToWorldMatrix);
		}

		/// <summary>
		/// Scope to draw multiple things in screen space of a camera.
		/// If you draw 2D coordinates (i.e. (x,y,0)) they will be projected onto a plane approximately [2*near clip plane of the camera] world units in front of the camera (but guaranteed to be between the near and far planes).
		///
		/// The lower left corner of the camera is (0,0,0) and the upper right is (camera.pixelWidth, camera.pixelHeight, 0)
		///
		/// See: <see cref="InLocalSpace"/>
		/// See: <see cref="WithMatrix"/>
		/// </summary>
		[BurstDiscard]
		public ScopeMatrix InScreenSpace (Camera camera) {
			return WithMatrix(camera.cameraToWorldMatrix * camera.nonJitteredProjectionMatrix.inverse * Matrix4x4.TRS(new Vector3(-1.0f, -1.0f, 0), Quaternion.identity, new Vector3(2.0f/camera.pixelWidth, 2.0f/camera.pixelHeight, 1)));
		}

		/// <summary>
		/// Multiply all coordinates until the next PopMatrix with the given matrix.
		/// This differs from <see cref="PushSetMatrix"/> in that this stacks with all previously pushed matrices while <see cref="PushSetMatrix"/> does not.
		/// </summary>
		public void PushMatrix (Matrix4x4 matrix) {
			Reserve<float4x4>();
			Add(Command.PushMatrix);
			Add((float4x4)matrix);
		}

		/// <summary>
		/// Multiply all coordinates until the next PopMatrix with the given matrix.
		/// This differs from <see cref="PushSetMatrix"/> in that this stacks with all previously pushed matrices while <see cref="PushSetMatrix"/> does not.
		/// </summary>
		public void PushMatrix (float4x4 matrix) {
			Reserve<float4x4>();
			Add(Command.PushMatrix);
			Add(matrix);
		}

		/// <summary>
		/// Multiply all coordinates until the next PopMatrix with the given matrix.
		/// This differs from <see cref="PushMatrix"/> in that this sets the current matrix directly while <see cref="PushMatrix"/> stacks with all previously pushed matrices.
		/// </summary>
		public void PushSetMatrix (Matrix4x4 matrix) {
			Reserve<float4x4>();
			Add(Command.PushSetMatrix);
			Add((float4x4)matrix);
		}

		/// <summary>
		/// Multiply all coordinates until the next PopMatrix with the given matrix.
		/// This differs from <see cref="PushMatrix"/> in that this sets the current matrix directly while <see cref="PushMatrix"/> stacks with all previously pushed matrices.
		/// </summary>
		public void PushSetMatrix (float4x4 matrix) {
			Reserve<float4x4>();
			Add(Command.PushSetMatrix);
			Add(matrix);
		}

		/// <summary>Pops a matrix from the stack</summary>
		public void PopMatrix () {
			Reserve(4);
			Add(Command.PopMatrix);
		}

		/// <summary>
		/// Draws everything until the next PopColor with the given color.
		/// Any command that is passed an explicit color parameter will override this color.
		/// If another color scope is nested inside this one then that scope will override this color.
		/// </summary>
		public void PushColor (Color color) {
			Reserve<Color32>();
			Add(Command.PushColor);
			Add((Color32)color);
		}

		/// <summary>Pops a color from the stack</summary>
		public void PopColor () {
			Reserve(4);
			Add(Command.PopColor);
		}

		/// <summary>
		/// Draws everything until the next PopDuration for a number of seconds.
		/// Warning: This is not recommended inside a DrawGizmos callback since DrawGizmos is called every frame anyway.
		/// </summary>
		public void PushDuration (float duration) {
			Reserve<PersistData>();
			Add(Command.PushPersist);
			// We must use the BurstTime variable which is updated more rarely than Time.time.
			// This is necessary because this code may be called from a burst job or from a different thread.
			// Time.time can only be accessed in the main thread.
			Add(new PersistData { endTime = SharedDrawingData.BurstTime.Data + duration });
		}

		/// <summary>Pops a duration scope from the stack</summary>
		public void PopDuration () {
			Reserve(4);
			Add(Command.PopPersist);
		}

		/// <summary>
		/// Draws everything until the next PopPersist for a number of seconds.
		/// Warning: This is not recommended inside a DrawGizmos callback since DrawGizmos is called every frame anyway.
		///
		/// Deprecated: Renamed to <see cref="PushDuration"/>
		/// </summary>
		[System.Obsolete("Renamed to PushDuration for consistency")]
		public void PushPersist (float duration) {
			PushDuration(duration);
		}

		/// <summary>
		/// Pops a persist scope from the stack.
		/// Deprecated: Renamed to <see cref="PopDuration"/>
		/// </summary>
		[System.Obsolete("Renamed to PopDuration for consistency")]
		public void PopPersist () {
			PopDuration();
		}

		/// <summary>
		/// Draws all lines until the next PopLineWidth with a given line width in pixels.
		///
		/// Note that the line join algorithm is a quite simple one optimized for speed. It normally looks good on a 2D plane, but if the polylines curve a lot in 3D space then
		/// it can look odd from some angles.
		///
		/// [Open online documentation to see images]
		///
		/// In the picture the top row has automaticJoins enabled and in the bottom row it is disabled.
		/// </summary>
		/// <param name="pixels">Line width in pixels</param>
		/// <param name="automaticJoins">If true then sequences of lines that are adjacent will be automatically joined at their vertices. This typically produces nicer polylines without weird gaps.</param>
		public void PushLineWidth (float pixels, bool automaticJoins = true) {
			if (pixels < 0) throw new System.ArgumentOutOfRangeException("pixels", "Line width must be positive");

			Reserve<LineWidthData>();
			Add(Command.PushLineWidth);
			Add(new LineWidthData { pixels = pixels, automaticJoins = automaticJoins });
		}

		/// <summary>Pops a line width scope from the stack</summary>
		public void PopLineWidth () {
			Reserve(4);
			Add(Command.PopLineWidth);
		}

		/// <summary>
		/// Draws a line between two points.
		///
		/// [Open online documentation to see images]
		///
		/// <code>
		/// void Update () {
		///     Draw.Line(Vector3.zero, Vector3.up);
		/// }
		/// </code>
		/// </summary>
		public void Line (float3 a, float3 b) {
			Reserve<LineData>();
			Add(Command.Line);
			Add(new LineData { a = a, b = b });
		}

		/// <summary>
		/// Draws a line between two points.
		///
		/// [Open online documentation to see images]
		///
		/// <code>
		/// void Update () {
		///     Draw.Line(Vector3.zero, Vector3.up);
		/// }
		/// </code>
		/// </summary>
		public void Line (Vector3 a, Vector3 b) {
			Reserve<LineData>();
			// Add(Command.Line);
			// Add(new LineDataV3 { a = a, b = b });

			// The code below is equivalent to the commented out code above.
			// But drawing lines is the most common operation so it needs to be really fast.
			// Having this hardcoded improves line rendering performance by about 8%.
			var bufferSize = BufferSize;

			unsafe {
				var newLen = bufferSize + 4 + 24;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				UnityEngine.Assertions.Assert.IsTrue(newLen <= buffer->Capacity);
#endif
				var ptr = (byte*)buffer->Ptr + bufferSize;
				*(Command*)ptr = Command.Line;
				var lineData = (LineDataV3*)(ptr + 4);
				lineData->a = a;
				lineData->b = b;
				buffer->SetLength(newLen);
			}
		}

		/// <summary>
		/// Draws a line between two points.
		///
		/// [Open online documentation to see images]
		///
		/// <code>
		/// void Update () {
		///     Draw.Line(Vector3.zero, Vector3.up);
		/// }
		/// </code>
		/// </summary>
		public void Line (Vector3 a, Vector3 b, Color color) {
			Reserve<Color32, LineData>();
			// Add(Command.Line | Command.PushColorInline);
			// Add((Color32)color);
			// Add(new LineDataV3 { a = a, b = b });

			// The code below is equivalent to the code which is commented out above.
			// But drawing lines is the most common operation so it needs to be really fast
			// Having this hardcoded improves line rendering performance by about 8%.
			var bufferSize = BufferSize;

			unsafe {
				var newLen = bufferSize + 4 + 24 + 4;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				UnityEngine.Assertions.Assert.IsTrue(newLen <= buffer->Capacity);
#endif
				var ptr = (byte*)buffer->Ptr + bufferSize;
				*(Command*)ptr = Command.Line | Command.PushColorInline;
				*(Color32*)(ptr + 4) = (Color32)color;
				var lineData = (LineDataV3*)(ptr + 8);
				lineData->a = a;
				lineData->b = b;
				buffer->SetLength(newLen);
			}
		}

		/// <summary>
		/// Draws a ray starting at a point and going in the given direction.
		/// The ray will end at origin + direction.
		///
		/// [Open online documentation to see images]
		///
		/// <code>
		/// Draw.Ray(Vector3.zero, Vector3.up);
		/// </code>
		/// </summary>
		public void Ray (float3 origin, float3 direction) {
			Line(origin, origin + direction);
		}

		/// <summary>
		/// Draws a ray with a given length.
		///
		/// [Open online documentation to see images]
		///
		/// <code>
		/// Draw.Ray(Camera.main.ScreenPointToRay(Vector3.zero), 10);
		/// </code>
		/// </summary>
		public void Ray (Ray ray, float length) {
			Line(ray.origin, ray.origin + ray.direction * length);
		}

		/// <summary>
		/// Draws an arc between two points.
		///
		/// The rendered arc is the shortest arc between the two points.
		/// The radius of the arc will be equal to the distance between center and start.
		///
		/// [Open online documentation to see images]
		/// <code>
		/// float a1 = Mathf.PI*0.9f;
		/// float a2 = Mathf.PI*0.1f;
		/// var arcStart = new float3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
		/// var arcEnd = new float3(Mathf.Cos(a2), 0, Mathf.Sin(a2));
		/// Draw.Arc(new float3(0, 0, 0), arcStart, arcEnd, color);
		/// </code>
		///
		/// See: \reflink{CircleXZ(float3,float,float,float)}
		/// See: \reflink{CircleXY(float3,float,float,float)}
		/// </summary>
		/// <param name="center">Center of the imaginary circle that the arc is part of.</param>
		/// <param name="start">Starting point of the arc.</param>
		/// <param name="end">End point of the arc.</param>
		public void Arc (float3 center, float3 start, float3 end) {
			var d1 = start - center;
			var d2 = end - center;
			var normal = math.cross(d2, d1);

			if (math.any(normal)) {
				var m = Matrix4x4.TRS(center, Quaternion.LookRotation(d1, normal), Vector3.one);
				var angle = Vector3.SignedAngle(d1, d2, normal) * Mathf.Deg2Rad;
				PushMatrix(m);
				CircleXZ(float3.zero, math.length(d1), 90 * Mathf.Deg2Rad, 90 * Mathf.Deg2Rad - angle);
				PopMatrix();
			}
		}

		/// <summary>
		/// Draws a circle in the XZ plane.
		///
		/// You can draw an arc by supplying the startAngle and endAngle parameters.
		///
		/// [Open online documentation to see images]
		///
		/// See: \reflink{Circle(float3,float3,float)}
		/// See: \reflink{CircleXY(float3,float,float,float)}
		/// See: \reflink{Arc(float3,float3,float3)}
		/// </summary>
		/// <param name="center">Center of the circle or arc.</param>
		/// <param name="radius">Radius of the circle or arc.</param>
		/// <param name="startAngle">Starting angle in radians. 0 corrsponds to the positive X axis.</param>
		/// <param name="endAngle">End angle in radians.</param>
		public void CircleXZ (float3 center, float radius, float startAngle = 0f, float endAngle = 2 * Mathf.PI) {
			Reserve<CircleXZData>();
			Add(Command.CircleXZ);
			Add(new CircleXZData { center = center, radius = radius, startAngle = startAngle, endAngle = endAngle });
		}

		static readonly float4x4 XZtoXYPlaneMatrix = float4x4.RotateX(-math.PI*0.5f);
		static readonly float4x4 XZtoYZPlaneMatrix = float4x4.RotateZ(math.PI*0.5f);

		/// <summary>
		/// Draws a circle in the XY plane.
		///
		/// You can draw an arc by supplying the startAngle and endAngle parameters.
		///
		/// [Open online documentation to see images]
		///
		/// See: \reflink{Circle(float3,float3,float)}
		/// See: \reflink{CircleXZ(float3,float,float,float)}
		/// See: \reflink{Arc(float3,float3,float3)}
		/// </summary>
		/// <param name="center">Center of the circle or arc.</param>
		/// <param name="radius">Radius of the circle or arc.</param>
		/// <param name="startAngle">Starting angle in radians. 0 corrsponds to the positive X axis.</param>
		/// <param name="endAngle">End angle in radians.</param>
		public void CircleXY (float3 center, float radius, float startAngle = 0f, float endAngle = 2 * Mathf.PI) {
			PushMatrix(XZtoXYPlaneMatrix);
			CircleXZ(new float3(center.x, -center.z, center.y), radius, startAngle, endAngle);
			PopMatrix();
		}

		/// <summary>
		/// Draws a circle.
		///
		/// [Open online documentation to see images]
		///
		/// Note: This overload does not allow you to draw an arc. For that purpose use <see cref="Arc"/>, <see cref="CircleXY"/> or <see cref="CircleXZ"/> instead.
		/// </summary>
		public void Circle (float3 center, float3 normal, float radius) {
			Reserve<CircleData>();
			Add(Command.Circle);
			Add(new CircleData { center = center, normal = normal, radius = radius });
		}

		/// <summary>
		/// Draws a solid arc between two points.
		///
		/// The rendered arc is the shortest arc between the two points.
		/// The radius of the arc will be equal to the distance between center and start.
		///
		/// [Open online documentation to see images]
		/// <code>
		/// float a1 = Mathf.PI*0.9f;
		/// float a2 = Mathf.PI*0.1f;
		/// var arcStart = new float3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
		/// var arcEnd = new float3(Mathf.Cos(a2), 0, Mathf.Sin(a2));
		/// Draw.SolidArc(new float3(0, 0, 0), arcStart, arcEnd, color);
		/// </code>
		///
		/// See: \reflink{SolidCircleXZ(float3,float,float,float)}
		/// See: \reflink{SolidCircleXY(float3,float,float,float)}
		/// </summary>
		/// <param name="center">Center of the imaginary circle that the arc is part of.</param>
		/// <param name="start">Starting point of the arc.</param>
		/// <param name="end">End point of the arc.</param>
		public void SolidArc (float3 center, float3 start, float3 end) {
			var d1 = start - center;
			var d2 = end - center;
			var normal = math.cross(d2, d1);

			if (math.any(normal)) {
				var m = Matrix4x4.TRS(center, Quaternion.LookRotation(d1, normal), Vector3.one);
				var angle = Vector3.SignedAngle(d1, d2, normal) * Mathf.Deg2Rad;
				PushMatrix(m);
				SolidCircleXZ(float3.zero, math.length(d1), 90 * Mathf.Deg2Rad, 90 * Mathf.Deg2Rad - angle);
				PopMatrix();
			}
		}

		/// <summary>
		/// Draws a disc in the XZ plane.
		///
		/// You can draw an arc by supplying the startAngle and endAngle parameters.
		///
		/// [Open online documentation to see images]
		///
		/// See: \reflink{SolidCircle(float3,float3,float)}
		/// See: \reflink{SolidCircleXY(float3,float,float,float)}
		/// See: \reflink{SolidArc(float3,float3,float3)}
		/// </summary>
		/// <param name="center">Center of the disc or solid arc.</param>
		/// <param name="radius">Radius of the disc or solid arc.</param>
		/// <param name="startAngle">Starting angle in radians. 0 corrsponds to the positive X axis.</param>
		/// <param name="endAngle">End angle in radians.</param>
		public void SolidCircleXZ (float3 center, float radius, float startAngle = 0f, float endAngle = 2 * Mathf.PI) {
			Reserve<CircleXZData>();
			Add(Command.DiscXZ);
			Add(new CircleXZData { center = center, radius = radius, startAngle = startAngle, endAngle = endAngle });
		}

		/// <summary>
		/// Draws a disc in the XY plane.
		///
		/// You can draw an arc by supplying the startAngle and endAngle parameters.
		///
		/// [Open online documentation to see images]
		///
		/// See: \reflink{SolidCircle(float3,float3,float)}
		/// See: \reflink{SolidCircleXZ(float3,float,float,float)}
		/// See: \reflink{SolidArc(float3,float3,float3)}
		/// </summary>
		/// <param name="center">Center of the disc or solid arc.</param>
		/// <param name="radius">Radius of the disc or solid arc.</param>
		/// <param name="startAngle">Starting angle in radians. 0 corrsponds to the positive X axis.</param>
		/// <param name="endAngle">End angle in radians.</param>
		public void SolidCircleXY (float3 center, float radius, float startAngle = 0f, float endAngle = 2 * Mathf.PI) {
			PushMatrix(XZtoXYPlaneMatrix);
			SolidCircleXZ(new float3(center.x, -center.z, center.y), radius, startAngle, endAngle);
			PopMatrix();
		}

		/// <summary>
		/// Draws a disc.
		///
		/// [Open online documentation to see images]
		///
		/// Note: This overload does not allow you to draw an arc. For that purpose use <see cref="SolidArc"/>, <see cref="SolidCircleXY"/> or <see cref="SolidCircleXZ"/> instead.
		/// </summary>
		public void SolidCircle (float3 center, float3 normal, float radius) {
			Reserve<CircleData>();
			Add(Command.Disc);
			Add(new CircleData { center = center, normal = normal, radius = radius });
		}

		/// <summary>
		/// Draws a circle outline around a sphere.
		///
		/// Visually, this is a circle that always faces the camera, and is resized automatically to fit the sphere.
		///
		/// [Open online documentation to see images]
		/// </summary>
		public void SphereOutline (float3 center, float radius) {
			Reserve<SphereData>();
			Add(Command.SphereOutline);
			Add(new SphereData { center = center, radius = radius });
		}

		/// <summary>
		/// Draws a cylinder.
		/// The cylinder's bottom circle will be centered at the bottom parameter and similarly for the top circle.
		///
		/// <code>
		/// // Draw a tilted cylinder between the points (0,0,0) and (1,1,1) with a radius of 0.5
		/// Draw.WireCylinder(Vector3.zero, Vector3.one, 0.5f, Color.black);
		/// </code>
		///
		/// [Open online documentation to see images]
		/// </summary>
		public void WireCylinder (float3 bottom, float3 top, float radius) {
			WireCylinder(bottom, top - bottom, math.length(top - bottom), radius);
		}

		/// <summary>
		/// Draws a cylinder.
		///
		/// <code>
		/// // Draw a two meter tall cylinder at the world origin with a radius of 0.5
		/// Draw.WireCylinder(Vector3.zero, Vector3.up, 2, 0.5f, Color.black);
		/// </code>
		///
		/// [Open online documentation to see images]
		/// </summary>
		/// <param name="position">The center of the cylinder's "bottom" circle.</param>
		/// <param name="up">The cylinder's main axis. Does not have to be normalized. If zero, nothing will be drawn.</param>
		/// <param name="height">The length of the cylinder, as measured along it's main axis.</param>
		/// <param name="radius">The radius of the cylinder.</param>
		public void WireCylinder (float3 position, float3 up, float height, float radius) {
			var tangent = math.cross(up, new float3(1, 1, 1));

			if (math.all(tangent == float3.zero)) tangent = math.cross(up, new float3(-1, 1, 1));

			tangent = math.normalizesafe(tangent);
			up = math.normalizesafe(up);
			var rotation = math.quaternion(math.float3x3(math.cross(up, tangent), up, tangent));

			// If we get a NaN here then either
			// * one of the input parameters contained nans (bad)
			// * up is zero, or very close to zero
			//
			// In any case, we cannot draw anything.
			if (!math.any(math.isnan(rotation.value))) {
				PushMatrix(float4x4.TRS(position, rotation, new float3(radius, height, radius)));
				CircleXZ(float3.zero, 1);
				if (height > 0) {
					CircleXZ(new float3(0, 1, 0), 1);
					Line(new float3(1, 0, 0), new float3(1, 1, 0));
					Line(new float3(-1, 0, 0), new float3(-1, 1, 0));
					Line(new float3(0, 0, 1), new float3(0, 1, 1));
					Line(new float3(0, 0, -1), new float3(0, 1, -1));
				}
				PopMatrix();
			}
		}

		/// <summary>
		/// Draws a capsule with a (start,end) parameterization.
		///
		/// The behavior of this method matches common Unity APIs such as Physics.CheckCapsule.
		///
		/// <code>
		/// // Draw a tilted capsule between the points (0,0,0) and (1,1,1) with a radius of 0.5
		/// Draw.WireCapsule(Vector3.zero, Vector3.one, 0.5f, Color.black);
		/// </code>
		///
		/// [Open online documentation to see images]
		/// </summary>
		/// <param name="start">Center of the start hemisphere of the capsule.</param>
		/// <param name="end">Center of the end hemisphere of the capsule.</param>
		/// <param name="radius">Radius of the capsule.</param>
		public void WireCapsule (float3 start, float3 end, float radius) {
			var dir = end - start;
			var length = math.length(dir);

			if (length < 0.0001) {
				// The endpoints are the same, we can't draw a capsule from this because we don't know its orientation.
				// Draw a sphere as a fallback
				WireSphere(start, radius);
			} else {
				var normalized_dir = dir / length;

				WireCapsule(start - normalized_dir*radius, normalized_dir, length + 2*radius, radius);
			}
		}

		// TODO: Change to center, up, height parameterization
		/// <summary>
		/// Draws a capsule with a (position,direction/length) parameterization.
		///
		/// <code>
		/// // Draw a capsule that touches the y=0 plane, is 2 meters tall and has a radius of 0.5
		/// Draw.WireCapsule(Vector3.zero, Vector3.up, 2.0f, 0.5f, Color.black);
		/// </code>
		///
		/// [Open online documentation to see images]
		/// </summary>
		/// <param name="position">One endpoint of the capsule. This is at the edge of the capsule, not at the center of one of the hemispheres.</param>
		/// <param name="direction">The main axis of the capsule. Does not have to be normalized. If zero, nothing will be drawn.</param>
		/// <param name="length">Distance between the two endpoints of the capsule. The length will be clamped to be at least 2*radius.</param>
		/// <param name="radius">The radius of the capsule.</param>
		public void WireCapsule (float3 position, float3 direction, float length, float radius) {
			direction = math.normalizesafe(direction);

			if (radius <= 0) {
				Line(position, position + direction * length);
			} else {
				var tangent = math.cross(direction, new float3(1, 1, 1));

				if (math.all(tangent == float3.zero)) tangent = math.cross(direction, new float3(-1, 1, 1));

				length = math.max(length, radius*2);

				tangent = math.normalizesafe(tangent);
				var rotation = math.quaternion(math.float3x3(tangent, direction, math.cross(tangent, direction)));

				// If we get a NaN here then either
				// * one of the input parameters contained nans (bad)
				// * direction is zero, or very close to zero
				//
				// In any case, we cannot draw anything.
				if (!math.any(math.isnan(rotation.value))) {
					PushMatrix(float4x4.TRS(position, rotation, 1));
					CircleXZ(new float3(0, radius, 0), radius);
					CircleXY(new float3(0, radius, 0), radius, Mathf.PI, 2 * Mathf.PI);
					PushMatrix(XZtoYZPlaneMatrix);
					CircleXZ(new float3(radius, 0, 0), radius, Mathf.PI*0.5f, Mathf.PI*1.5f);
					PopMatrix();
					if (length > 0) {
						var upperY = length - radius;
						var lowerY = radius;
						CircleXZ(new float3(0, upperY, 0), radius);
						CircleXY(new float3(0, upperY, 0), radius, 0, Mathf.PI);
						PushMatrix(XZtoYZPlaneMatrix);
						CircleXZ(new float3(upperY, 0, 0), radius, -Mathf.PI*0.5f, Mathf.PI*0.5f);
						PopMatrix();
						Line(new float3(radius, lowerY, 0), new float3(radius, upperY, 0));
						Line(new float3(-radius, lowerY, 0), new float3(-radius, upperY, 0));
						Line(new float3(0, lowerY, radius), new float3(0, upperY, radius));
						Line(new float3(0, lowerY, -radius), new float3(0, upperY, -radius));
					}
					PopMatrix();
				}
			}
		}

		/// <summary>
		/// Draws a wire sphere.
		///
		/// [Open online documentation to see images]
		///
		/// <code>
		/// // Draw a wire sphere at the origin with a radius of 0.5
		/// Draw.WireSphere(Vector3.zero, 0.5f, Color.black);
		/// </code>
		///
		/// See: <see cref="Circle"/>
		/// </summary>
		public void WireSphere (float3 position, float radius) {
			SphereOutline(position, radius);
			Circle(position, new float3(1, 0, 0), radius);
			Circle(position, new float3(0, 1, 0), radius);
			Circle(position, new float3(0, 0, 1), radius);
		}

		/// <summary>
		/// Draws lines through a sequence of points.
		///
		/// [Open online documentation to see images]
		/// <code>
		/// // Draw a square
		/// Draw.Polyline(new [] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) }, true);
		/// </code>
		/// </summary>
		/// <param name="points">Sequence of points to draw lines through</param>
		/// <param name="cycle">If true a line will be drawn from the last point in the sequence back to the first point.</param>
		[BurstDiscard]
		public void Polyline (List<Vector3> points, bool cycle = false) {
			for (int i = 0; i < points.Count - 1; i++) {
				Line(points[i], points[i+1]);
			}
			if (cycle && points.Count > 1) Line(points[points.Count - 1], points[0]);
		}

		/// <summary>
		/// Draws lines through a sequence of points.
		///
		/// [Open online documentation to see images]
		/// <code>
		/// // Draw a square
		/// Draw.Polyline(new [] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) }, true);
		/// </code>
		/// </summary>
		/// <param name="points">Sequence of points to draw lines through</param>
		/// <param name="cycle">If true a line will be drawn from the last point in the sequence back to the first point.</param>
		[BurstDiscard]
		public void Polyline (Vector3[] points, bool cycle = false) {
			for (int i = 0; i < points.Length - 1; i++) {
				Line(points[i], points[i+1]);
			}
			if (cycle && points.Length > 1) Line(points[points.Length - 1], points[0]);
		}

		/// <summary>
		/// Draws lines through a sequence of points.
		///
		/// [Open online documentation to see images]
		/// <code>
		/// // Draw a square
		/// Draw.Polyline(new [] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) }, true);
		/// </code>
		/// </summary>
		/// <param name="points">Sequence of points to draw lines through</param>
		/// <param name="cycle">If true a line will be drawn from the last point in the sequence back to the first point.</param>
		[BurstDiscard]
		public void Polyline (float3[] points, bool cycle = false) {
			for (int i = 0; i < points.Length - 1; i++) {
				Line(points[i], points[i+1]);
			}
			if (cycle && points.Length > 1) Line(points[points.Length - 1], points[0]);
		}

		/// <summary>
		/// Draws lines through a sequence of points.
		///
		/// [Open online documentation to see images]
		/// <code>
		/// // Draw a square
		/// Draw.Polyline(new [] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) }, true);
		/// </code>
		/// </summary>
		/// <param name="points">Sequence of points to draw lines through</param>
		/// <param name="cycle">If true a line will be drawn from the last point in the sequence back to the first point.</param>
		public void Polyline (NativeArray<float3> points, bool cycle = false) {
			for (int i = 0; i < points.Length - 1; i++) {
				Line(points[i], points[i+1]);
			}
			if (cycle && points.Length > 1) Line(points[points.Length - 1], points[0]);
		}

		/// <summary>
		/// Draws the outline of a box which is axis-aligned.
		///
		/// [Open online documentation to see images]
		/// </summary>
		/// <param name="center">Center of the box</param>
		/// <param name="size">Width of the box along all dimensions</param>
		public void WireBox (float3 center, float3 size) {
			WireBox(new Bounds(center, size));
		}

		/// <summary>
		/// Draws the outline of a box.
		///
		/// [Open online documentation to see images]
		/// </summary>
		/// <param name="center">Center of the box</param>
		/// <param name="rotation">Rotation of the box</param>
		/// <param name="size">Width of the box along all dimensions</param>
		public void WireBox (float3 center, quaternion rotation, float3 size) {
			PushMatrix(float4x4.TRS(center, rotation, size));
			WireBox(new Bounds(Vector3.zero, Vector3.one));
			PopMatrix();
		}

		/// <summary>
		/// Draws the outline of a box.
		///
		/// [Open online documentation to see images]
		/// </summary>
		public void WireBox (Bounds bounds) {
			var min = bounds.min;
			var max = bounds.max;

			Line(new float3(min.x, min.y, min.z), new float3(max.x, min.y, min.z));
			Line(new float3(max.x, min.y, min.z), new float3(max.x, min.y, max.z));
			Line(new float3(max.x, min.y, max.z), new float3(min.x, min.y, max.z));
			Line(new float3(min.x, min.y, max.z), new float3(min.x, min.y, min.z));

			Line(new float3(min.x, max.y, min.z), new float3(max.x, max.y, min.z));
			Line(new float3(max.x, max.y, min.z), new float3(max.x, max.y, max.z));
			Line(new float3(max.x, max.y, max.z), new float3(min.x, max.y, max.z));
			Line(new float3(min.x, max.y, max.z), new float3(min.x, max.y, min.z));

			Line(new float3(min.x, min.y, min.z), new float3(min.x, max.y, min.z));
			Line(new float3(max.x, min.y, min.z), new float3(max.x, max.y, min.z));
			Line(new float3(max.x, min.y, max.z), new float3(max.x, max.y, max.z));
			Line(new float3(min.x, min.y, max.z), new float3(min.x, max.y, max.z));
		}

		/// <summary>
		/// Draws a wire mesh.
		/// Every single edge of the mesh will be drawn using a <see cref="Line"/> command.
		///
		/// <code>
		/// var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		/// go.transform.position = new Vector3(0, 0, 0);
		/// using (Draw.InLocalSpace(go.transform)) {
		///     Draw.WireMesh(go.GetComponent<MeshFilter>().sharedMesh, color);
		/// }
		/// </code>
		/// [Open online documentation to see images]
		///
		/// See: <see cref="SolidMesh(Mesh)"/>
		///
		/// Version: Supported in Unity 2020.1 or later.
		/// </summary>
		public void WireMesh (Mesh mesh) {
#if UNITY_2020_1_OR_NEWER
			if (mesh == null) throw new System.ArgumentNullException();

			// Use a burst compiled function to draw the lines
			// This is significantly faster than pure C# (about 5x).
			var meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
			var meshData = meshDataArray[0];

			JobWireMesh.JobWireMeshFunctionPointer(ref meshData, ref this);
			meshDataArray.Dispose();
#else
			Debug.LogError("The WireMesh method is only suppored in Unity 2020.1 or later");
#endif
		}

#if UNITY_2020_1_OR_NEWER
		/// <summary>Helper job for <see cref="WireMesh"/></summary>
		[BurstCompile]
		class JobWireMesh {
			public delegate void JobWireMeshDelegate(ref Mesh.MeshData rawMeshData, ref CommandBuilder draw);

			public static readonly JobWireMeshDelegate JobWireMeshFunctionPointer = BurstCompiler.CompileFunctionPointer<JobWireMeshDelegate>(Execute).Invoke;

			[BurstCompile]
			[AOT.MonoPInvokeCallback(typeof(JobWireMeshDelegate))]
			static void Execute (ref Mesh.MeshData rawMeshData, ref CommandBuilder draw) {
				var verts = new NativeArray<Vector3>(rawMeshData.vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

				rawMeshData.GetVertices(verts);
				int maxIndices = 0;
				for (int subMeshIndex = 0; subMeshIndex < rawMeshData.subMeshCount; subMeshIndex++) {
					maxIndices = math.max(maxIndices, rawMeshData.GetSubMesh(subMeshIndex).indexCount);
				}
				var tris = new NativeArray<int>(maxIndices, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
				var seenEdges = new NativeParallelHashMap<int2, bool>(maxIndices, Allocator.Temp);

				for (int subMeshIndex = 0; subMeshIndex < rawMeshData.subMeshCount; subMeshIndex++) {
					var submesh = rawMeshData.GetSubMesh(subMeshIndex);
					var indices = tris.GetSubArray(0, submesh.indexCount);
					rawMeshData.GetIndices(indices, subMeshIndex);
					seenEdges.Clear();
					for (int i = 0; i < indices.Length; i += 3) {
						var a = indices[i];
						var b = indices[i+1];
						var c = indices[i+2];
						int v1, v2;

						// Draw each edge of the triangle.
						// Check so that we do not draw an edge twice.
						v1 = math.min(a, b);
						v2 = math.max(a, b);
						if (!seenEdges.ContainsKey(new int2(v1, v2))) {
							seenEdges.Add(new int2(v1, v2), true);
							draw.Line(verts[v1], verts[v2]);
						}

						v1 = math.min(b, c);
						v2 = math.max(b, c);
						if (!seenEdges.ContainsKey(new int2(v1, v2))) {
							seenEdges.Add(new int2(v1, v2), true);
							draw.Line(verts[v1], verts[v2]);
						}

						v1 = math.min(c, a);
						v2 = math.max(c, a);
						if (!seenEdges.ContainsKey(new int2(v1, v2))) {
							seenEdges.Add(new int2(v1, v2), true);
							draw.Line(verts[v1], verts[v2]);
						}
					}
				}
			}
		}
#endif

		/// <summary>
		/// Draws a solid mesh.
		/// The mesh will be drawn with a solid color.
		///
		/// <code>
		/// var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		/// go.transform.position = new Vector3(0, 0, 0);
		/// using (Draw.InLocalSpace(go.transform)) {
		///     Draw.SolidMesh(go.GetComponent<MeshFilter>().sharedMesh, color);
		/// }
		/// </code>
		/// [Open online documentation to see images]
		///
		/// Note: This method is not thread safe and must not be used from the Unity Job System.
		/// TODO: Are matrices handled?
		///
		/// See: <see cref="WireMesh(Mesh)"/>
		/// </summary>
		public void SolidMesh (Mesh mesh) {
			SolidMeshInternal(mesh, false);
		}

		void SolidMeshInternal (Mesh mesh, bool temporary, Color color) {
			PushColor(color);
			SolidMeshInternal(mesh, temporary);
			PopColor();
		}


		void SolidMeshInternal (Mesh mesh, bool temporary) {
			var g = gizmos.Target as DrawingData;

			g.data.Get(uniqueID).meshes.Add(new SubmittedMesh {
				mesh = mesh,
				temporary = temporary,
			});
			// Internally we need to make sure to capture the current state
			// (which includes the current matrix and color) so that it
			// can be applied to the mesh.
			Reserve(4);
			Add(Command.CaptureState);
		}

		/// <summary>
		/// Draws a solid mesh with the given vertices.
		///
		/// Note: This method is not thread safe and must not be used from the Unity Job System.
		/// TODO: Are matrices handled?
		/// </summary>
		[BurstDiscard]
		public void SolidMesh (List<Vector3> vertices, List<int> triangles, List<Color> colors) {
			if (vertices.Count != colors.Count) throw new System.ArgumentException("Number of colors must be the same as the number of vertices");

			// TODO: Is this mesh getting recycled at all?
			var g = gizmos.Target as DrawingData;
			var mesh = g.GetMesh(vertices.Count);

			// Set all data on the mesh
			mesh.Clear();
			mesh.SetVertices(vertices);
			mesh.SetTriangles(triangles, 0);
			mesh.SetColors(colors);
			// Upload all data
			mesh.UploadMeshData(false);
			SolidMeshInternal(mesh, true);
		}

		/// <summary>
		/// Draws a solid mesh with the given vertices.
		///
		/// Note: This method is not thread safe and must not be used from the Unity Job System.
		/// TODO: Are matrices handled?
		/// </summary>
		[BurstDiscard]
		public void SolidMesh (Vector3[] vertices, int[] triangles, Color[] colors, int vertexCount, int indexCount) {
			if (vertices.Length != colors.Length) throw new System.ArgumentException("Number of colors must be the same as the number of vertices");

			// TODO: Is this mesh getting recycled at all?
			var g = gizmos.Target as DrawingData;
			var mesh = g.GetMesh(vertices.Length);

			// Set all data on the mesh
			mesh.Clear();
			mesh.SetVertices(vertices, 0, vertexCount);
			mesh.SetTriangles(triangles, 0, indexCount, 0);
			mesh.SetColors(colors, 0, vertexCount);
			// Upload all data
			mesh.UploadMeshData(false);
			SolidMeshInternal(mesh, true);
		}

		/// <summary>
		/// Draws a cross.
		///
		/// [Open online documentation to see images]
		/// </summary>
		public void Cross (float3 position, float size = 1) {
			size *= 0.5f;
			Line(position - new float3(size, 0, 0), position + new float3(size, 0, 0));
			Line(position - new float3(0, size, 0), position + new float3(0, size, 0));
			Line(position - new float3(0, 0, size), position + new float3(0, 0, size));
		}

		/// <summary>
		/// Draws a cross in the XZ plane.
		///
		/// [Open online documentation to see images]
		/// </summary>
		public void CrossXZ (float3 position, float size = 1) {
			size *= 0.5f;
			Line(position - new float3(size, 0, 0), position + new float3(size, 0, 0));
			Line(position - new float3(0, 0, size), position + new float3(0, 0, size));
		}

		/// <summary>
		/// Draws a cross in the XY plane.
		///
		/// [Open online documentation to see images]
		/// </summary>
		public void CrossXY (float3 position, float size = 1) {
			size *= 0.5f;
			Line(position - new float3(size, 0, 0), position + new float3(size, 0, 0));
			Line(position - new float3(0, size, 0), position + new float3(0, size, 0));
		}

		/// <summary>Returns a point on a cubic bezier curve. t is clamped between 0 and 1</summary>
		public static float3 EvaluateCubicBezier (float3 p0, float3 p1, float3 p2, float3 p3, float t) {
			t = math.clamp(t, 0, 1);
			float tr = 1-t;
			return tr*tr*tr * p0 + 3 * tr*tr * t * p1 + 3 * tr * t*t * p2 + t*t*t * p3;
		}

		/// <summary>
		/// Draws a cubic bezier curve.
		///
		/// [Open online documentation to see images]
		///
		/// [Open online documentation to see images]
		///
		/// TODO: Currently uses a fixed resolution of 20 segments. Resolution should depend on the distance to the camera.
		///
		/// See: https://en.wikipedia.org/wiki/Bezier_curve
		/// </summary>
		/// <param name="p0">Start point</param>
		/// <param name="p1">First control point</param>
		/// <param name="p2">Second control point</param>
		/// <param name="p3">End point</param>
		public void Bezier (float3 p0, float3 p1, float3 p2, float3 p3) {
			float3 prev = p0;

			for (int i = 1; i <= 20; i++) {
				float t = i/20.0f;
				float3 p = EvaluateCubicBezier(p0, p1, p2, p3, t);
				Line(prev, p);
				prev = p;
			}
		}

		/// <summary>
		/// Draws a smooth curve through a list of points.
		///
		/// A catmull-rom spline is equivalent to a bezier curve with control points determined by an algorithm.
		/// In fact, this package displays catmull-rom splines by first converting them to bezier curves.
		///
		/// [Open online documentation to see images]
		///
		/// See: https://en.wikipedia.org/wiki/Centripetal_Catmull%E2%80%93Rom_spline
		/// See: <see cref="CatmullRom(float3,float3,float3,float3)"/>
		/// </summary>
		/// <param name="points">The curve will smoothly pass through each point in the list in order.</param>
		public void CatmullRom (List<Vector3> points) {
			if (points.Count < 2) return;

			if (points.Count == 2) {
				Line(points[0], points[1]);
			} else {
				// count >= 3
				var count = points.Count;
				// Draw first curve, this is special because the first two control points are the same
				CatmullRom(points[0], points[0], points[1], points[2]);
				for (int i = 0; i + 3 < count; i++) {
					CatmullRom(points[i], points[i+1], points[i+2], points[i+3]);
				}
				// Draw last curve
				CatmullRom(points[count-3], points[count-2], points[count-1], points[count-1]);
			}
		}

		/// <summary>
		/// Draws a centripetal catmull rom spline.
		///
		/// The curve starts at p1 and ends at p2.
		///
		/// [Open online documentation to see images]
		/// [Open online documentation to see images]
		///
		/// See: <see cref="CatmullRom(List<Vector3>)"/>
		/// </summary>
		/// <param name="p0">First control point</param>
		/// <param name="p1">Second control point. Start of the curve.</param>
		/// <param name="p2">Third control point. End of the curve.</param>
		/// <param name="p3">Fourth control point.</param>
		public void CatmullRom (float3 p0, float3 p1, float3 p2, float3 p3) {
			// References used:
			// p.266 GemsV1
			//
			// tension is often set to 0.5 but you can use any reasonable value:
			// http://www.cs.cmu.edu/~462/projects/assn2/assn2/catmullRom.pdf
			//
			// bias and tension controls:
			// http://local.wasp.uwa.edu.au/~pbourke/miscellaneous/interpolation/

			// We will convert the catmull rom spline to a bezier curve for simplicity.
			// The end result of this will be a conversion matrix where we transform catmull rom control points
			// into the equivalent bezier curve control points.

			// Conversion matrix
			// =================

			// A centripetal catmull rom spline can be separated into the following terms:
			// 1 * p1 +
			// t * (-0.5 * p0 + 0.5*p2) +
			// t*t * (p0 - 2.5*p1  + 2.0*p2 + 0.5*t2) +
			// t*t*t * (-0.5*p0 + 1.5*p1 - 1.5*p2 + 0.5*p3)
			//
			// Matrix form:
			// 1     t   t^2 t^3
			// {0, -1/2, 1, -1/2}
			// {1, 0, -5/2, 3/2}
			// {0, 1/2, 2, -3/2}
			// {0, 0, -1/2, 1/2}

			// Transposed matrix:
			// M_1 = {{0, 1, 0, 0}, {-1/2, 0, 1/2, 0}, {1, -5/2, 2, -1/2}, {-1/2, 3/2, -3/2, 1/2}}

			// A bezier spline can be separated into the following terms:
			// (-t^3 + 3 t^2 - 3 t + 1) * c0 +
			// (3t^3 - 6*t^2 + 3t) * c1 +
			// (3t^2 - 3t^3) * c2 +
			// t^3 * c3
			//
			// Matrix form:
			// 1  t  t^2  t^3
			// {1, -3, 3, -1}
			// {0, 3, -6, 3}
			// {0, 0, 3, -3}
			// {0, 0, 0, 1}

			// Transposed matrix:
			// M_2 = {{1, 0, 0, 0}, {-3, 3, 0, 0}, {3, -6, 3, 0}, {-1, 3, -3, 1}}

			// Thus a bezier curve can be evaluated using the expression
			// output1 = T * M_1 * c
			// where T = [1, t, t^2, t^3] and c being the control points c = [c0, c1, c2, c3]^T
			//
			// and a catmull rom spline can be evaluated using
			//
			// output2 = T * M_2 * p
			// where T = same as before and p = [p0, p1, p2, p3]^T
			//
			// We can solve for c in output1 = output2
			// T * M_1 * c = T * M_2 * p
			// M_1 * c = M_2 * p
			// c = M_1^(-1) * M_2 * p
			// Thus a conversion matrix from p to c is M_1^(-1) * M_2
			// This can be calculated and the result is the following matrix:
			//
			// {0, 1, 0, 0}
			// {-1/6, 1, 1/6, 0}
			// {0, 1/6, 1, -1/6}
			// {0, 0, 1, 0}
			// ------------------------------------------------------------------
			//
			// Using this we can calculate c = M_1^(-1) * M_2 * p
			var c0 = p1;
			var c1 = (-p0 + 6*p1 + 1*p2)*(1/6.0f);
			var c2 = (p1 + 6*p2 - p3)*(1/6.0f);
			var c3 = p2;

			// And finally draw the bezier curve which is equivalent to the desired catmull-rom spline
			Bezier(c0, c1, c2, c3);
		}

		/// <summary>
		/// Draws an arrow between two points.
		///
		/// The size of the head defaults to 20% of the length of the arrow.
		///
		/// [Open online documentation to see images]
		///
		/// See: <see cref="ArrowheadArc"/>
		/// See: <see cref="Arrow(float3,float3,float3,float)"/>
		/// See: <see cref="ArrowRelativeSizeHead"/>
		/// </summary>
		/// <param name="from">Base of the arrow.</param>
		/// <param name="to">Head of the arrow.</param>
		public void Arrow (float3 from, float3 to) {
			ArrowRelativeSizeHead(from, to, new float3(0, 1, 0), 0.2f);
		}

		/// <summary>
		/// Draws an arrow between two points.
		///
		/// [Open online documentation to see images]
		///
		/// See: <see cref="ArrowRelativeSizeHead"/>
		/// See: <see cref="ArrowheadArc"/>
		/// </summary>
		/// <param name="from">Base of the arrow.</param>
		/// <param name="to">Head of the arrow.</param>
		/// <param name="up">Up direction of the world, the arrowhead plane will be as perpendicular as possible to this direction. Defaults to Vector3.up.</param>
		/// <param name="headSize">The size of the arrowhead in world units.</param>
		public void Arrow (float3 from, float3 to, float3 up, float headSize) {
			var length_sq = math.lengthsq(to - from);

			if (length_sq > 0.000001f) {
				ArrowRelativeSizeHead(from, to, up, headSize * math.rsqrt(length_sq));
			}
		}

		/// <summary>
		/// Draws an arrow between two points with a head that varies with the length of the arrow.
		///
		/// [Open online documentation to see images]
		///
		/// See: <see cref="ArrowheadArc"/>
		/// See: <see cref="Arrow"/>
		/// </summary>
		/// <param name="from">Base of the arrow.</param>
		/// <param name="to">Head of the arrow.</param>
		/// <param name="up">Up direction of the world, the arrowhead plane will be as perpendicular as possible to this direction. Defaults to Vector3.up.</param>
		/// <param name="headFraction">The length of the arrowhead is the distance between from and to multiplied by this fraction. Should be between 0 and 1.</param>
		public void ArrowRelativeSizeHead (float3 from, float3 to, float3 up, float headFraction) {
			Line(from, to);
			var dir = to - from;

			var normal = math.cross(dir, up);
			// Pick a different up direction if the direction happened to be colinear with that one.
			if (math.all(normal == 0)) normal = math.cross(new float3(1, 0, 0), dir);
			// Pick a different up direction if up=(1,0,0) and thus the above check would have generated a zero vector again
			if (math.all(normal == 0)) normal = math.cross(new float3(0, 1, 0), dir);

			Line(to, to - (dir + normal) * headFraction);
			Line(to, to - (dir - normal) * headFraction);
		}

		/// <summary>
		/// Draws an arrowhead at a point.
		///
		/// <code>
		/// Draw.WireTriangle(Vector3.zero, Quaternion.identity, 0.5f, color);
		/// </code>
		/// [Open online documentation to see images]
		///
		/// See: <see cref="Arrow"/>
		/// See: <see cref="ArrowRelativeSizeHead"/>
		/// </summary>
		/// <param name="center">Center of the arrowhead.</param>
		/// <param name="direction">Direction the arrow is pointing.</param>
		/// <param name="radius">Distance from the center to each corner of the arrowhead.</param>
		public void Arrowhead (float3 center, float3 direction, float radius) {
			Arrowhead(center, direction, new float3(0, 1, 0), radius);
		}

		/// <summary>
		/// Draws an arrowhead at a point.
		///
		/// <code>
		/// Draw.WireTriangle(Vector3.zero, Quaternion.identity, 0.5f, color);
		/// </code>
		/// [Open online documentation to see images]
		///
		/// See: <see cref="Arrow"/>
		/// See: <see cref="ArrowRelativeSizeHead"/>
		/// </summary>
		/// <param name="center">Center of the arrowhead.</param>
		/// <param name="direction">Direction the arrow is pointing.</param>
		/// <param name="up">Up direction of the world, the arrowhead plane will be as perpendicular as possible to this direction. Defaults to Vector3.up.</param>
		/// <param name="radius">Distance from the center to each corner of the arrowhead.</param>
		public void Arrowhead (float3 center, float3 direction, float3 up, float radius) {
			if (math.all(direction == 0)) return;
			direction = math.normalizesafe(direction);
			var normal = math.cross(direction, up);
			const float SinPiOver3 = 0.866025f;
			const float CosPiOver3 = 0.5f;
			var circleCenter = center - radius * (1 - CosPiOver3)*0.5f * direction;
			var p1 = circleCenter + radius * direction;
			var p2 = circleCenter - radius * CosPiOver3 * direction + radius * SinPiOver3 * normal;
			var p3 = circleCenter - radius * CosPiOver3 * direction - radius * SinPiOver3 * normal;
			Line(p1, p2);
			Line(p2, circleCenter);
			Line(circleCenter, p3);
			Line(p3, p1);
		}

		/// <summary>
		/// Draws an arrowhead centered around a circle.
		///
		/// This can be used to for example show the direction a character is moving in.
		///
		/// [Open online documentation to see images]
		///
		/// Note: In the image above the arrowhead is the only part that is drawn by this method. The cylinder is only included for context.
		///
		/// See: <see cref="Arrow"/>
		/// </summary>
		/// <param name="origin">Point around which the arc is centered</param>
		/// <param name="direction">Direction the arrow is pointing</param>
		/// <param name="offset">Distance from origin that the arrow starts.</param>
		/// <param name="width">Width of the arrowhead in degrees (defaults to 60). Should be between 0 and 90.</param>
		public void ArrowheadArc (float3 origin, float3 direction, float offset, float width = 60) {
			if (!math.any(direction)) return;
			if (offset < 0) throw new System.ArgumentOutOfRangeException(nameof(offset));
			if (offset == 0) return;

			var up = new Vector3(0, 1, 0);
			var rot = Quaternion.LookRotation(direction, up);
			PushMatrix(Matrix4x4.TRS(origin, rot, Vector3.one));
			var a1 = math.PI * 0.5f - width * (0.5f * Mathf.Deg2Rad);
			var a2 = math.PI * 0.5f + width * (0.5f * Mathf.Deg2Rad);
			CircleXZ(float3.zero, offset, a1, a2);
			var p1 = new float3(math.cos(a1), 0, math.sin(a1)) * offset;
			var p2 = new float3(math.cos(a2), 0, math.sin(a2)) * offset;
			const float sqrt2 = 1.4142f;
			var p3 = new float3(0, 0, sqrt2 * offset);
			Line(p1, p3);
			Line(p2, p3);
			PopMatrix();
		}

		/// <summary>
		/// Draws a grid of lines.
		///
		/// <code>
		/// Draw.WireGrid(Vector3.zero, Quaternion.identity, new int2(3, 3), new float2(1, 1), color);
		/// </code>
		/// [Open online documentation to see images]
		/// </summary>
		/// <param name="center">Center of the grid</param>
		/// <param name="rotation">Rotation of the grid. The grid will be aligned to the X and Z axes of the rotation.</param>
		/// <param name="cells">Number of cells of the grid. Should be greater than 0.</param>
		/// <param name="totalSize">Total size of the grid along the X and Z axes.</param>
		public void WireGrid (float3 center, quaternion rotation, int2 cells, float2 totalSize) {
			cells = math.max(cells, new int2(1, 1));
			PushMatrix(float4x4.TRS(center, rotation, new Vector3(totalSize.x, 0, totalSize.y)));
			int w = cells.x;
			int h = cells.y;
			for (int i = 0; i <= w; i++) Line(new float3(i/(float)w - 0.5f, 0, -0.5f), new float3(i/(float)w - 0.5f, 0, 0.5f));
			for (int i = 0; i <= h; i++) Line(new float3(-0.5f, 0, i/(float)h - 0.5f), new float3(0.5f, 0, i/(float)h - 0.5f));
			PopMatrix();
		}

		/// <summary>
		/// Draws a triangle outline.
		///
		/// <code>
		/// Draw.WireTriangle(new Vector3(-0.5f, 0, 0), new Vector3(0, 1, 0), new Vector3(0.5f, 0, 0), Color.black);
		/// </code>
		/// [Open online documentation to see images]
		///
		/// See: \reflink{WirePlane(float3,quaternion,float2)}
		/// See: <see cref="WirePolygon"/>
		/// </summary>
		/// <param name="a">First corner of the triangle</param>
		/// <param name="b">Second corner of the triangle</param>
		/// <param name="c">Third corner of the triangle</param>
		public void WireTriangle (float3 a, float3 b, float3 c) {
			Line(a, b);
			Line(b, c);
			Line(c, a);
		}

		/// <summary>
		/// Draws a rectangle outline.
		/// The rectangle will be aligned to the X and Z axes.
		///
		/// <code>
		/// Draw.WireRectangleXZ(new Vector3(0f, 0, 0), new Vector2(1, 1), Color.black);
		/// </code>
		/// [Open online documentation to see images]
		///
		/// See: <see cref="WirePolygon"/>
		/// </summary>
		public void WireRectangleXZ (float3 center, float2 size) {
			WireRectangle(center, quaternion.identity, size);
		}

		/// <summary>
		/// Draws a rectangle outline.
		/// The rectangle will be oriented along the rotation's X and Z axes.
		///
		/// <code>
		/// Draw.WireRectangle(new Vector3(0f, 0, 0), Quaternion.identity, new Vector2(1, 1), Color.black);
		/// </code>
		/// [Open online documentation to see images]
		///
		/// This is identical to \reflink{WirePlane(float3,quaternion,float2)}, but this name is added for consistency.
		///
		/// See: <see cref="WirePolygon"/>
		/// </summary>
		public void WireRectangle (float3 center, quaternion rotation, float2 size) {
			WirePlane(center, rotation, size);
		}

		/// <summary>
		/// Draws a rectangle outline.
		/// The rectangle corners are assumed to be in XY space.
		/// This is particularly useful when combined with <see cref="InScreenSpace"/>.
		///
		/// <code>
		/// using (Draw.InScreenSpace(Camera.main)) {
		///     Draw.WireRectangle(new Rect(10, 10, 100, 100), Color.black);
		/// }
		/// </code>
		/// [Open online documentation to see images]
		///
		/// See: <see cref="WireRectangleXZ"/>
		/// See: <see cref="WireRectangle(float3,quaternion,float2)"/>
		/// See: <see cref="WirePolygon"/>
		/// </summary>
		public void WireRectangle (Rect rect) {
			float2 min = rect.min;
			float2 max = rect.max;

			Line(new float3(min.x, min.y, 0), new float3(max.x, min.y, 0));
			Line(new float3(max.x, min.y, 0), new float3(max.x, max.y, 0));
			Line(new float3(max.x, max.y, 0), new float3(min.x, max.y, 0));
			Line(new float3(min.x, max.y, 0), new float3(min.x, min.y, 0));
		}


		/// <summary>
		/// Draws a triangle outline.
		///
		/// <code>
		/// Draw.WireTriangle(Vector3.zero, Quaternion.identity, 0.5f, color);
		/// </code>
		/// [Open online documentation to see images]
		///
		/// Note: This is a convenience wrapper for <see cref="WirePolygon(float3,int,quaternion,float)"/>
		///
		/// See: <see cref="WireTriangle(float3,float3,float3)"/>
		/// </summary>
		/// <param name="center">Center of the triangle.</param>
		/// <param name="rotation">Rotation of the triangle. The first vertex will be radius units in front of center as seen from the rotation's point of view.</param>
		/// <param name="radius">Distance from the center to each vertex.</param>
		public void WireTriangle (float3 center, quaternion rotation, float radius) {
			WirePolygon(center, 3, rotation, radius);
		}

		/// <summary>
		/// Draws a pentagon outline.
		///
		/// <code>
		/// Draw.WirePentagon(Vector3.zero, Quaternion.identity, 0.5f, color);
		/// </code>
		/// [Open online documentation to see images]
		///
		/// Note: This is a convenience wrapper for <see cref="WirePolygon(float3,int,quaternion,float)"/>
		/// </summary>
		/// <param name="center">Center of the polygon.</param>
		/// <param name="rotation">Rotation of the polygon. The first vertex will be radius units in front of center as seen from the rotation's point of view.</param>
		/// <param name="radius">Distance from the center to each vertex.</param>
		public void WirePentagon (float3 center, quaternion rotation, float radius) {
			WirePolygon(center, 5, rotation, radius);
		}

		/// <summary>
		/// Draws a hexagon outline.
		///
		/// <code>
		/// Draw.WireHexagon(Vector3.zero, Quaternion.identity, 0.5f, color);
		/// </code>
		/// [Open online documentation to see images]
		///
		/// Note: This is a convenience wrapper for <see cref="WirePolygon(float3,int,quaternion,float)"/>
		/// </summary>
		/// <param name="center">Center of the polygon.</param>
		/// <param name="rotation">Rotation of the polygon. The first vertex will be radius units in front of center as seen from the rotation's point of view.</param>
		/// <param name="radius">Distance from the center to each vertex.</param>
		public void WireHexagon (float3 center, quaternion rotation, float radius) {
			WirePolygon(center, 6, rotation, radius);
		}

		/// <summary>
		/// Draws a regular polygon outline.
		///
		/// <code>
		/// Draw.WirePolygon(new Vector3(-0.5f, 0, +0.5f), 3, Quaternion.identity, 0.4f, color);
		/// Draw.WirePolygon(new Vector3(+0.5f, 0, +0.5f), 4, Quaternion.identity, 0.4f, color);
		/// Draw.WirePolygon(new Vector3(-0.5f, 0, -0.5f), 5, Quaternion.identity, 0.4f, color);
		/// Draw.WirePolygon(new Vector3(+0.5f, 0, -0.5f), 6, Quaternion.identity, 0.4f, color);
		/// </code>
		/// [Open online documentation to see images]
		///
		/// See: <see cref="WireTriangle"/>
		/// See: <see cref="WirePentagon"/>
		/// See: <see cref="WireHexagon"/>
		/// </summary>
		/// <param name="center">Center of the polygon.</param>
		/// <param name="vertices">Number of corners (and sides) of the polygon.</param>
		/// <param name="rotation">Rotation of the polygon. The first vertex will be radius units in front of center as seen from the rotation's point of view.</param>
		/// <param name="radius">Distance from the center to each vertex.</param>
		public void WirePolygon (float3 center, int vertices, quaternion rotation, float radius) {
			PushMatrix(float4x4.TRS(center, rotation, new float3(radius, radius, radius)));
			float3 prev = new float3(0, 0, 1);
			for (int i = 1; i <= vertices; i++) {
				float a = 2 * math.PI * (i / (float)vertices);
				var p = new float3(math.sin(a), 0, math.cos(a));
				Line(prev, p);
				prev = p;
			}
			PopMatrix();
		}

		/// <summary>
		/// Draws a solid rectangle.
		/// The rectangle corners are assumed to be in XY space.
		/// This is particularly useful when combined with <see cref="InScreenSpace"/>.
		///
		/// Behind the scenes this is implemented using <see cref="SolidPlane"/>.
		///
		/// <code>
		/// using (Draw.InScreenSpace(Camera.main)) {
		///     Draw.SolidRectangle(new Rect(10, 10, 100, 100), Color.black);
		/// }
		/// </code>
		/// [Open online documentation to see images]
		///
		/// See: <see cref="WireRectangleXZ"/>
		/// See: <see cref="WireRectangle(float3,quaternion,float2)"/>
		/// See: <see cref="SolidBox"/>
		/// </summary>
		public void SolidRectangle (Rect rect) {
			SolidPlane(new float3(rect.center.x, rect.center.y, 0.0f), quaternion.Euler(-math.PI*0.5f, 0, 0), new float2(rect.width, rect.height));
		}

		/// <summary>
		/// Draws a solid plane.
		///
		/// <code>
		/// Draw.SolidPlane(new float3(0, 0, 0), new float3(0, 1, 0), 1.0f, color);
		/// </code>
		/// [Open online documentation to see images]
		/// </summary>
		/// <param name="center">Center of the visualized plane.</param>
		/// <param name="normal">Direction perpendicular to the plane. If this is (0,0,0) then nothing will be rendered.</param>
		/// <param name="size">Width and height of the visualized plane.</param>
		public void SolidPlane (float3 center, float3 normal, float2 size) {
			if (math.any(normal)) {
				SolidPlane(center, Quaternion.LookRotation(calculateTangent(normal), normal), size);
			}
		}

		/// <summary>
		/// Draws a solid plane.
		///
		/// The plane will lie in the XZ plane with respect to the rotation.
		///
		/// <code>
		/// Draw.SolidPlane(new float3(0, 0, 0), new float3(0, 1, 0), 1.0f, color);
		/// </code>
		/// [Open online documentation to see images]
		/// </summary>
		/// <param name="center">Center of the visualized plane.</param>
		/// <param name="size">Width and height of the visualized plane.</param>
		public void SolidPlane (float3 center, quaternion rotation, float2 size) {
			PushMatrix(float4x4.TRS(center, rotation, new float3(size.x, 0, size.y)));
			Reserve<BoxData>();
			Add(Command.Box);
			Add(new BoxData { center = 0, size = 1 });
			PopMatrix();
		}

		/// <summary>Returns an arbitrary vector which is orthogonal to the given one</summary>
		private static float3 calculateTangent (float3 normal) {
			var tangent = math.cross(new float3(0, 1, 0), normal);

			if (math.all(tangent == 0)) tangent = math.cross(new float3(1, 0, 0), normal);
			return tangent;
		}

		/// <summary>
		/// Draws a wire plane.
		///
		/// <code>
		/// Draw.WirePlane(new float3(0, 0, 0), new float3(0, 1, 0), 1.0f, color);
		/// </code>
		/// [Open online documentation to see images]
		/// </summary>
		/// <param name="center">Center of the visualized plane.</param>
		/// <param name="normal">Direction perpendicular to the plane. If this is (0,0,0) then nothing will be rendered.</param>
		/// <param name="size">Width and height of the visualized plane.</param>
		public void WirePlane (float3 center, float3 normal, float2 size) {
			if (math.any(normal)) {
				WirePlane(center, Quaternion.LookRotation(calculateTangent(normal), normal), size);
			}
		}

		/// <summary>
		/// Draws a wire plane.
		///
		/// This is identical to \reflink{WireRectangle(float3,quaternion,float2)}, but it is included for consistency.
		///
		/// <code>
		/// Draw.WirePlane(new float3(0, 0, 0), new float3(0, 1, 0), 1.0f, color);
		/// </code>
		/// [Open online documentation to see images]
		/// </summary>
		/// <param name="center">Center of the visualized plane.</param>
		/// <param name="rotation">Rotation of the plane. The plane will lie in the XZ plane with respect to the rotation.</param>
		/// <param name="size">Width and height of the visualized plane.</param>
		public void WirePlane (float3 center, quaternion rotation, float2 size) {
			Reserve<PlaneData>();
			Add(Command.WirePlane);
			Add(new PlaneData { center = center, rotation = rotation, size = size });
		}

		/// <summary>
		/// Draws a plane and a visualization of its normal.
		///
		/// <code>
		/// Draw.PlaneWithNormal(new float3(0, 0, 0), new float3(0, 1, 0), 1.0f, color);
		/// </code>
		/// [Open online documentation to see images]
		/// </summary>
		/// <param name="center">Center of the visualized plane.</param>
		/// <param name="normal">Direction perpendicular to the plane. If this is (0,0,0) then nothing will be rendered.</param>
		/// <param name="size">Width and height of the visualized plane.</param>
		public void PlaneWithNormal (float3 center, float3 normal, float2 size) {
			if (math.any(normal)) {
				PlaneWithNormal(center, Quaternion.LookRotation(calculateTangent(normal), normal), size);
			}
		}

		/// <summary>
		/// Draws a plane and a visualization of its normal.
		///
		/// <code>
		/// Draw.PlaneWithNormal(new float3(0, 0, 0), new float3(0, 1, 0), 1.0f, color);
		/// </code>
		/// [Open online documentation to see images]
		/// </summary>
		/// <param name="center">Center of the visualized plane.</param>
		/// <param name="rotation">Rotation of the plane. The plane will lie in the XZ plane with respect to the rotation.</param>
		/// <param name="size">Width and height of the visualized plane.</param>
		public void PlaneWithNormal (float3 center, quaternion rotation, float2 size) {
			SolidPlane(center, rotation, size);
			WirePlane(center, rotation, size);
			ArrowRelativeSizeHead(center, center + math.mul(rotation, new float3(0, 1, 0)) * 0.5f, math.mul(rotation, new float3(0, 0, 1)), 0.2f);
		}

		/// <summary>
		/// Draws a solid box.
		///
		/// <code>
		/// Draw.SolidBox(new float3(0, 0, 0), new float3(1, 1, 1), color);
		/// </code>
		/// [Open online documentation to see images]
		/// </summary>
		/// <param name="center">Center of the box</param>
		/// <param name="size">Width of the box along all dimensions</param>
		public void SolidBox (float3 center, float3 size) {
			Reserve<BoxData>();
			Add(Command.Box);
			Add(new BoxData { center = center, size = size });
		}

		/// <summary>
		/// Draws a solid box.
		///
		/// <code>
		/// Draw.SolidBox(new float3(0, 0, 0), new float3(1, 1, 1), color);
		/// </code>
		/// [Open online documentation to see images]
		/// </summary>
		/// <param name="bounds">Bounding box of the box</param>
		public void SolidBox (Bounds bounds) {
			SolidBox(bounds.center, bounds.size);
		}

		/// <summary>
		/// Draws a solid box.
		///
		/// <code>
		/// Draw.SolidBox(new float3(0, 0, 0), new float3(1, 1, 1), color);
		/// </code>
		/// [Open online documentation to see images]
		/// </summary>
		/// <param name="center">Center of the box</param>
		/// <param name="rotation">Rotation of the box</param>
		/// <param name="size">Width of the box along all dimensions</param>
		public void SolidBox (float3 center, quaternion rotation, float3 size) {
			PushMatrix(float4x4.TRS(center, rotation, size));
			SolidBox(float3.zero, Vector3.one);
			PopMatrix();
		}

		/// <summary>
		/// Draws a label in 3D space.
		///
		/// The default alignment is <see cref="Drawing.LabelAlignment.MiddleLeft"/>.
		///
		/// <code>
		/// Draw.Label3D(new float3(0.2f, -1f, 0.2f), Quaternion.Euler(45, -110, -90), "Label", 1, LabelAlignment.Center, color);
		/// </code>
		/// [Open online documentation to see images]
		///
		/// See: Label3D(float3,quaternion,string,float,LabelAlignment)
		///
		/// Note: Only ASCII is supported since the built-in font texture only includes ASCII. Other characters will be rendered as question marks (?).
		/// </summary>
		/// <param name="position">Position in 3D space.</param>
		/// <param name="rotation">Rotation in 3D space.</param>
		/// <param name="text">Text to display.</param>
		/// <param name="size">World size of the text. For large sizes an SDF (signed distance field) font is used and for small sizes a normal font texture is used.</param>
		public void Label3D (float3 position, quaternion rotation, string text, float size) {
			Label3D(position, rotation, text, size, LabelAlignment.MiddleLeft);
		}

		/// <summary>
		/// Draws a label in 3D space.
		///
		/// <code>
		/// Draw.Label3D(new float3(0.2f, -1f, 0.2f), Quaternion.Euler(45, -110, -90), "Label", 1, LabelAlignment.Center, color);
		/// </code>
		/// [Open online documentation to see images]
		///
		/// See: Label3D(float3,quaternion,string,float)
		///
		/// Note: Only ASCII is supported since the built-in font texture only includes ASCII. Other characters will be rendered as question marks (?).
		///
		/// Note: This method cannot be used in burst since managed strings are not suppported in burst. However, you can use the separate Label3D overload which takes a FixedString.
		/// </summary>
		/// <param name="position">Position in 3D space.</param>
		/// <param name="rotation">Rotation in 3D space.</param>
		/// <param name="text">Text to display.</param>
		/// <param name="size">World size of the text. For large sizes an SDF (signed distance field) font is used and for small sizes a normal font texture is used.</param>
		/// <param name="alignment">How to align the text relative to the given position.</param>
		public void Label3D (float3 position, quaternion rotation, string text, float size, LabelAlignment alignment) {
			AssertBufferExists();
			var g = gizmos.Target as DrawingData;
			Reserve<TextData3D>();
			Add(Command.Text3D);
			Add(new TextData3D { center = position, rotation = rotation, numCharacters = text.Length, size = size, alignment = alignment });

			Reserve(UnsafeUtility.SizeOf<System.UInt16>() * text.Length);
			for (int i = 0; i < text.Length; i++) {
				char c = text[i];
				System.UInt16 index = (System.UInt16)g.fontData.GetIndex(c);
				Add(index);
			}
		}

		/// <summary>
		/// Draws a label in 3D space aligned with the camera.
		///
		/// The default alignment is <see cref="Drawing.LabelAlignment.MiddleLeft"/>.
		///
		/// <code>
		/// Draw.Label2D(Vector3.zero, "Label", 48, LabelAlignment.Center, color);
		/// </code>
		/// [Open online documentation to see images]
		///
		/// See: Label2D(float3,string,float,LabelAlignment)
		///
		/// Note: Only ASCII is supported since the built-in font texture only includes ASCII. Other characters will be rendered as question marks (?).
		/// </summary>
		/// <param name="position">Position in 3D space.</param>
		/// <param name="text">Text to display.</param>
		/// <param name="sizeInPixels">Size of the text in screen pixels. For large sizes an SDF (signed distance field) font is used and for small sizes a normal font texture is used.</param>
		public void Label2D (float3 position, string text, float sizeInPixels = 14) {
			Label2D(position, text, sizeInPixels, LabelAlignment.MiddleLeft);
		}

		/// <summary>
		/// Draws a label in 3D space aligned with the camera.
		///
		/// <code>
		/// Draw.Label2D(Vector3.zero, "Label", 48, LabelAlignment.Center, color);
		/// </code>
		/// [Open online documentation to see images]
		///
		/// See: Label2D(float3,string,float)
		///
		/// Note: Only ASCII is supported since the built-in font texture only includes ASCII. Other characters will be rendered as question marks (?).
		///
		/// Note: This method cannot be used in burst since managed strings are not suppported in burst. However, you can use the separate Label2D overload which takes a FixedString.
		/// </summary>
		/// <param name="position">Position in 3D space.</param>
		/// <param name="text">Text to display.</param>
		/// <param name="sizeInPixels">Size of the text in screen pixels. For large sizes an SDF (signed distance field) font is used and for small sizes a normal font texture is used.</param>
		/// <param name="alignment">How to align the text relative to the given position.</param>
		public void Label2D (float3 position, string text, float sizeInPixels, LabelAlignment alignment) {
			AssertBufferExists();
			var g = gizmos.Target as DrawingData;
			Reserve<TextData>();
			Add(Command.Text);
			Add(new TextData { center = position, numCharacters = text.Length, sizeInPixels = sizeInPixels, alignment = alignment });

			Reserve(UnsafeUtility.SizeOf<System.UInt16>() * text.Length);
			for (int i = 0; i < text.Length; i++) {
				char c = text[i];
				System.UInt16 index = (System.UInt16)g.fontData.GetIndex(c);
				Add(index);
			}
		}

		#region Label2DFixedString
		/// <summary>
		/// Draws a label in 3D space aligned with the camera.
		///
		/// <code>
		/// // This part can be inside a burst job
		/// for (int i = 0; i < 10; i++) {
		///     Unity.Collections.FixedString32Bytes text = $"X = {i}";
		///     builder.Label2D(new float3(i, 0, 0), ref text, 12, LabelAlignment.Center);
		/// }
		/// </code>
		/// [Open online documentation to see images]
		///
		/// See: Label2D(float3,string,float)
		///
		/// Note: Only ASCII is supported since the built-in font texture only includes ASCII. Other characters will be rendered as question marks (?).
		///
		/// Note: This method requires the Unity.Collections package version 0.8 or later.
		/// </summary>
		/// <param name="position">Position in 3D space.</param>
		/// <param name="text">Text to display.</param>
		/// <param name="sizeInPixels">Size of the text in screen pixels. For large sizes an SDF (signed distance field) font is used and for small sizes a normal font texture is used.</param>
		public void Label2D (float3 position, ref FixedString32Bytes text, float sizeInPixels = 14) {
			Label2D(position, ref text, sizeInPixels, LabelAlignment.MiddleLeft);
		}

		/// <summary>\copydocref{Label2D(float3,FixedString32Bytes,float)}</summary>
		public void Label2D (float3 position, ref FixedString64Bytes text, float sizeInPixels = 14) {
			Label2D(position, ref text, sizeInPixels, LabelAlignment.MiddleLeft);
		}

		/// <summary>\copydocref{Label2D(float3,FixedString32Bytes,float)}</summary>
		public void Label2D (float3 position, ref FixedString128Bytes text, float sizeInPixels = 14) {
			Label2D(position, ref text, sizeInPixels, LabelAlignment.MiddleLeft);
		}

		/// <summary>\copydocref{Label2D(float3,FixedString32Bytes,float)}</summary>
		public void Label2D (float3 position, ref FixedString512Bytes text, float sizeInPixels = 14) {
			Label2D(position, ref text, sizeInPixels, LabelAlignment.MiddleLeft);
		}

		/// <summary>
		/// Draws a label in 3D space aligned with the camera.
		///
		/// <code>
		/// // This part can be inside a burst job
		/// for (int i = 0; i < 10; i++) {
		///     Unity.Collections.FixedString32Bytes text = $"X = {i}";
		///     builder.Label2D(new float3(i, 0, 0), ref text, 12, LabelAlignment.Center);
		/// }
		/// </code>
		/// [Open online documentation to see images]
		///
		/// See: Label2D(float3,string,float)
		///
		/// Note: Only ASCII is supported since the built-in font texture only includes ASCII. Other characters will be rendered as question marks (?).
		///
		/// Note: This method requires the Unity.Collections package version 0.8 or later.
		/// </summary>
		/// <param name="position">Position in 3D space.</param>
		/// <param name="text">Text to display.</param>
		/// <param name="sizeInPixels">Size of the text in screen pixels. For large sizes an SDF (signed distance field) font is used and for small sizes a normal font texture is used.</param>
		/// <param name="alignment">How to align the text relative to the given position.</param>
		public void Label2D (float3 position, ref FixedString32Bytes text, float sizeInPixels, LabelAlignment alignment) {
#if MODULE_COLLECTIONS_0_12_0_OR_NEWER
			unsafe {
				Label2D(position, text.GetUnsafePtr(), text.Length, sizeInPixels, alignment);
			}
#else
			Debug.LogError("The Label2D method which takes FixedStrings requires the Unity.Collections package version 0.12 or newer");
#endif
		}

		/// <summary>\copydocref{Label2D(float3,FixedString32Bytes,float,LabelAlignment)}</summary>
		public void Label2D (float3 position, ref FixedString64Bytes text, float sizeInPixels, LabelAlignment alignment) {
#if MODULE_COLLECTIONS_0_12_0_OR_NEWER
			unsafe {
				Label2D(position, text.GetUnsafePtr(), text.Length, sizeInPixels, alignment);
			}
#else
			Debug.LogError("The Label2D method which takes FixedStrings requires the Unity.Collections package version 0.12 or newer");
#endif
		}

		/// <summary>\copydocref{Label2D(float3,FixedString32Bytes,float,LabelAlignment)}</summary>
		public void Label2D (float3 position, ref FixedString128Bytes text, float sizeInPixels, LabelAlignment alignment) {
#if MODULE_COLLECTIONS_0_12_0_OR_NEWER
			unsafe {
				Label2D(position, text.GetUnsafePtr(), text.Length, sizeInPixels, alignment);
			}
#else
			Debug.LogError("The Label2D method which takes FixedStrings requires the Unity.Collections package version 0.12 or newer");
#endif
		}

		/// <summary>\copydocref{Label2D(float3,FixedString32Bytes,float,LabelAlignment)}</summary>
		public void Label2D (float3 position, ref FixedString512Bytes text, float sizeInPixels, LabelAlignment alignment) {
#if MODULE_COLLECTIONS_0_12_0_OR_NEWER
			unsafe {
				Label2D(position, text.GetUnsafePtr(), text.Length, sizeInPixels, alignment);
			}
#else
			Debug.LogError("The Label2D method which takes FixedStrings requires the Unity.Collections package version 0.12 or newer");
#endif
		}

		/// <summary>\copydocref{Label2D(float3,FixedString32Bytes,float,LabelAlignment)}</summary>
		internal unsafe void Label2D (float3 position, byte* text, int byteCount, float sizeInPixels, LabelAlignment alignment) {
#if MODULE_COLLECTIONS_0_12_0_OR_NEWER
			AssertBufferExists();
			Reserve<TextData>();
			Add(Command.Text);
			Add(new TextData { center = position, numCharacters = byteCount, sizeInPixels = sizeInPixels, alignment = alignment });

			Reserve(UnsafeUtility.SizeOf<System.UInt16>() * byteCount);
			for (int i = 0; i < byteCount; i++) {
				// The first 128 elements in the font data are guaranteed to be laid out as ascii.
				// We use this since we cannot use the dynamic font lookup.
				System.UInt16 c = *(text + i);
				if (c >= 128) c = (System.UInt16) '?';
				if (c == (byte)'\n') c = SDFLookupData.Newline;
				// Ignore carriage return instead of printing them as '?'. Windows encodes newlines as \r\n.
				if (c == (byte)'\r') continue;
				Add(c);
			}
#endif
		}
		#endregion

		#region Label3DFixedString
		/// <summary>
		/// Draws a label in 3D space.
		///
		/// <code>
		/// // This part can be inside a burst job
		/// for (int i = 0; i < 10; i++) {
		///     Unity.Collections.FixedString32Bytes text = $"X = {i}";
		///     builder.Label3D(new float3(i, 0, 0), quaternion.identity, ref text, 1, LabelAlignment.Center);
		/// }
		/// </code>
		/// [Open online documentation to see images]
		///
		/// See: Label3D(float3,quaternion,string,float)
		///
		/// Note: Only ASCII is supported since the built-in font texture only includes ASCII. Other characters will be rendered as question marks (?).
		///
		/// Note: This method requires the Unity.Collections package version 0.8 or later.
		/// </summary>
		/// <param name="position">Position in 3D space.</param>
		/// <param name="rotation">Rotation in 3D space.</param>
		/// <param name="text">Text to display.</param>
		/// <param name="size">World size of the text. For large sizes an SDF (signed distance field) font is used and for small sizes a normal font texture is used.</param>
		public void Label3D (float3 position, quaternion rotation, ref FixedString32Bytes text, float size) {
			Label3D(position, rotation, ref text, size, LabelAlignment.MiddleLeft);
		}

		/// <summary>\copydocref{Label3D(float3,quaternion,FixedString32Bytes,float)}</summary>
		public void Label3D (float3 position, quaternion rotation, ref FixedString64Bytes text, float size) {
			Label3D(position, rotation, ref text, size, LabelAlignment.MiddleLeft);
		}

		/// <summary>\copydocref{Label3D(float3,quaternion,FixedString32Bytes,float)}</summary>
		public void Label3D (float3 position, quaternion rotation, ref FixedString128Bytes text, float size) {
			Label3D(position, rotation, ref text, size, LabelAlignment.MiddleLeft);
		}

		/// <summary>\copydocref{Label3D(float3,quaternion,FixedString32Bytes,float)}</summary>
		public void Label3D (float3 position, quaternion rotation, ref FixedString512Bytes text, float size) {
			Label3D(position, rotation, ref text, size, LabelAlignment.MiddleLeft);
		}

		/// <summary>
		/// Draws a label in 3D space.
		///
		/// <code>
		/// // This part can be inside a burst job
		/// for (int i = 0; i < 10; i++) {
		///     Unity.Collections.FixedString32Bytes text = $"X = {i}";
		///     builder.Label3D(new float3(i, 0, 0), quaternion.identity, ref text, 1, LabelAlignment.Center);
		/// }
		/// </code>
		/// [Open online documentation to see images]
		///
		/// See: Label3D(float3,quaternion,string,float)
		///
		/// Note: Only ASCII is supported since the built-in font texture only includes ASCII. Other characters will be rendered as question marks (?).
		///
		/// Note: This method requires the Unity.Collections package version 0.8 or later.
		/// </summary>
		/// <param name="position">Position in 3D space.</param>
		/// <param name="rotation">Rotation in 3D space.</param>
		/// <param name="text">Text to display.</param>
		/// <param name="size">World size of the text. For large sizes an SDF (signed distance field) font is used and for small sizes a normal font texture is used.</param>
		/// <param name="alignment">How to align the text relative to the given position.</param>
		public void Label3D (float3 position, quaternion rotation, ref FixedString32Bytes text, float size, LabelAlignment alignment) {
#if MODULE_COLLECTIONS_0_12_0_OR_NEWER
			unsafe {
				Label3D(position, rotation, text.GetUnsafePtr(), text.Length, size, alignment);
			}
#else
			Debug.LogError("The Label3D method which takes FixedStrings requires the Unity.Collections package version 0.12 or newer");
#endif
		}

		/// <summary>\copydocref{Label3D(float3,quaternion,FixedString32Bytes,float,LabelAlignment)}</summary>
		public void Label3D (float3 position, quaternion rotation, ref FixedString64Bytes text, float size, LabelAlignment alignment) {
#if MODULE_COLLECTIONS_0_12_0_OR_NEWER
			unsafe {
				Label3D(position, rotation, text.GetUnsafePtr(), text.Length, size, alignment);
			}
#else
			Debug.LogError("The Label3D method which takes FixedStrings requires the Unity.Collections package version 0.12 or newer");
#endif
		}

		/// <summary>\copydocref{Label3D(float3,quaternion,FixedString32Bytes,float,LabelAlignment)}</summary>
		public void Label3D (float3 position, quaternion rotation, ref FixedString128Bytes text, float size, LabelAlignment alignment) {
#if MODULE_COLLECTIONS_0_12_0_OR_NEWER
			unsafe {
				Label3D(position, rotation, text.GetUnsafePtr(), text.Length, size, alignment);
			}
#else
			Debug.LogError("The Label3D method which takes FixedStrings requires the Unity.Collections package version 0.12 or newer");
#endif
		}

		/// <summary>\copydocref{Label3D(float3,quaternion,FixedString32Bytes,float,LabelAlignment)}</summary>
		public void Label3D (float3 position, quaternion rotation, ref FixedString512Bytes text, float size, LabelAlignment alignment) {
#if MODULE_COLLECTIONS_0_12_0_OR_NEWER
			unsafe {
				Label3D(position, rotation, text.GetUnsafePtr(), text.Length, size, alignment);
			}
#else
			Debug.LogError("The Label3D method which takes FixedStrings requires the Unity.Collections package version 0.12 or newer");
#endif
		}

		/// <summary>\copydocref{Label3D(float3,quaternion,FixedString32Bytes,float,LabelAlignment)}</summary>
		internal unsafe void Label3D (float3 position, quaternion rotation, byte* text, int byteCount, float size, LabelAlignment alignment) {
#if MODULE_COLLECTIONS_0_12_0_OR_NEWER
			AssertBufferExists();
			Reserve<TextData3D>();
			Add(Command.Text3D);
			Add(new TextData3D { center = position, rotation = rotation, numCharacters = byteCount, size = size, alignment = alignment });

			Reserve(UnsafeUtility.SizeOf<System.UInt16>() * byteCount);
			for (int i = 0; i < byteCount; i++) {
				// The first 128 elements in the font data are guaranteed to be laid out as ascii.
				// We use this since we cannot use the dynamic font lookup.
				System.UInt16 c = *(text + i);
				if (c >= 128) c = (System.UInt16) '?';
				if (c == (byte)'\n') c = SDFLookupData.Newline;
				Add(c);
			}
#endif
		}
		#endregion

		/// <summary>
		/// Helper for determining how large a pixel is at a given depth.
		/// A a distance D from the camera a pixel corresponds to roughly value.x * D + value.y world units.
		/// Where value is the return value from this function.
		/// </summary>
		private static float2 CameraDepthToPixelSize (Camera camera) {
			if (camera.orthographic) {
				return new float2(0.0f, 2.0f * camera.orthographicSize / camera.pixelHeight);
			} else {
				return new float2(Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) / (0.5f * camera.pixelHeight), 0.0f);
			}
		}

		private static NativeArray<T> ConvertExistingDataToNativeArray<T>(UnsafeAppendBuffer data) where T : struct {
			unsafe {
				var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(data.Ptr, data.GetLength() / UnsafeUtility.SizeOf<T>(), Allocator.Invalid);;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
				return arr;
			}
		}

		private static Mesh AssignMeshData<VertexType>(DrawingData gizmos, Bounds bounds, UnsafeAppendBuffer vertices, UnsafeAppendBuffer triangles, VertexAttributeDescriptor[] layout) where VertexType : struct {
			CommandBuilderSamplers.samplerConvert.Begin();
			var verticesView = ConvertExistingDataToNativeArray<VertexType>(vertices);
			var trianglesView = ConvertExistingDataToNativeArray<int>(triangles);
			CommandBuilderSamplers.samplerConvert.End();
			var mesh = gizmos.GetMesh(verticesView.Length);

			CommandBuilderSamplers.samplerLayout.Begin();
			// Resize the vertex buffer if necessary
			// Note: also resized if the vertex buffer is significantly larger than necessary.
			//       This is because apparently when executing the command buffer Unity does something with the whole buffer for some reason (shows up as Mesh.CreateMesh in the profiler)
			// TODO: This could potentially cause bad behaviour if multiple meshes are used each frame and they have differing sizes.
			// We should query for meshes that already have an appropriately sized buffer.
			// if (mesh.vertexCount < verticesView.Length || mesh.vertexCount > verticesView.Length * 2) {

			// }
			// TODO: Use Mesh.GetVertexBuffer/Mesh.GetIndexBuffer once they stop being buggy.
			// Currently they don't seem to get refreshed properly after resizing them (2022.2.0b1)
			mesh.SetVertexBufferParams(math.ceilpow2(verticesView.Length), layout);
			mesh.SetIndexBufferParams(math.ceilpow2(trianglesView.Length), IndexFormat.UInt32);
			CommandBuilderSamplers.samplerLayout.End();

			CommandBuilderSamplers.samplerUpdateVert.Begin();
			// Update the mesh data
			mesh.SetVertexBufferData(verticesView, 0, 0, verticesView.Length);
			CommandBuilderSamplers.samplerUpdateVert.End();
			CommandBuilderSamplers.samplerUpdateInd.Begin();
			// Update the index buffer and assume all our indices are correct
			mesh.SetIndexBufferData(trianglesView, 0, 0, trianglesView.Length, MeshUpdateFlags.DontValidateIndices);
			CommandBuilderSamplers.samplerUpdateInd.End();


			CommandBuilderSamplers.samplerSubmesh.Begin();
			mesh.subMeshCount = 1;
			var submesh = new SubMeshDescriptor(0, trianglesView.Length, MeshTopology.Triangles) {
				vertexCount = verticesView.Length,
				bounds = bounds
			};
			mesh.SetSubMesh(0, submesh, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);
			mesh.bounds = bounds;
			CommandBuilderSamplers.samplerSubmesh.End();
			return mesh;
		}

		internal static unsafe JobHandle Build (DrawingData gizmos, ProcessedBuilderData.MeshBuffers* buffers, Camera camera, JobHandle dependency) {
			// Create a new builder and schedule it.
			// Why is characterInfo passed as a pointer and a length instead of just a NativeArray?
			// 	This is because passing it as a NativeArray invokes the safety system which adds some tracking to the NativeArray.
			//  This is normally not a problem, but we may be scheduling hundreds of jobs that use that particular NativeArray and this causes a bit of a slowdown
			//  in the safety checking system. Passing it as a pointer + length makes the whole scheduling code about twice as fast compared to passing it as a NativeArray.
			return new Builder {
					   buffers = buffers,
					   currentMatrix = Matrix4x4.identity,
					   currentLineWidthData = new LineWidthData {
						   pixels = 1,
						   automaticJoins = false,
					   },
					   currentColor = (Color32)Color.white,
					   cameraPosition = camera != null ? camera.transform.position : Vector3.zero,
					   cameraRotation = camera != null ? camera.transform.rotation : Quaternion.identity,
					   cameraDepthToPixelSize = (camera != null ? CameraDepthToPixelSize(camera) : 0),
					   cameraIsOrthographic = camera != null ? camera.orthographic : false,
					   characterInfo = (SDFCharacter*)gizmos.fontData.characters.GetUnsafeReadOnlyPtr(),
					   characterInfoLength = gizmos.fontData.characters.Length,
			}.Schedule(dependency);
		}

		internal static unsafe void BuildMesh (DrawingData gizmos, List<MeshWithType> meshes, ProcessedBuilderData.MeshBuffers* inputBuffers) {
			if (inputBuffers->triangles.GetLength() > 0) {
				CommandBuilderSamplers.samplerUpdateBuffer.Begin();
				var mesh = AssignMeshData<Builder.Vertex>(gizmos, inputBuffers->bounds, inputBuffers->vertices, inputBuffers->triangles, MeshLayouts.MeshLayout);
				meshes.Add(new MeshWithType { mesh = mesh, type = MeshType.Lines });
				CommandBuilderSamplers.samplerUpdateBuffer.End();
			}

			if (inputBuffers->solidTriangles.GetLength() > 0) {
				var mesh = AssignMeshData<Builder.Vertex>(gizmos, inputBuffers->bounds, inputBuffers->solidVertices, inputBuffers->solidTriangles, MeshLayouts.MeshLayout);
				meshes.Add(new MeshWithType { mesh = mesh, type = MeshType.Solid });
			}

			if (inputBuffers->textTriangles.GetLength() > 0) {
				var mesh = AssignMeshData<Builder.TextVertex>(gizmos, inputBuffers->bounds, inputBuffers->textVertices, inputBuffers->textTriangles, MeshLayouts.MeshLayoutText);
				meshes.Add(new MeshWithType { mesh = mesh, type = MeshType.Text });
			}
		}

		[BurstCompile]
		internal struct PersistentFilterJob : IJob {
			[NativeDisableUnsafePtrRestriction]
			public unsafe UnsafeAppendBuffer* buffer;
			public float time;

			public void Execute () {
				var stackPersist = new NativeArray<bool>(Builder.MaxStackSize, Allocator.Temp, NativeArrayOptions.ClearMemory);
				var stackScope = new NativeArray<int>(Builder.MaxStackSize, Allocator.Temp, NativeArrayOptions.ClearMemory);
				// Size of all commands in bytes
				var commandSizes = new NativeArray<int>(20, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

				commandSizes[(int)Command.PushColor] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<Color32>();
				commandSizes[(int)Command.PopColor] = UnsafeUtility.SizeOf<Command>() + 0;
				commandSizes[(int)Command.PushMatrix] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<float4x4>();
				commandSizes[(int)Command.PushSetMatrix] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<float4x4>();
				commandSizes[(int)Command.PopMatrix] = UnsafeUtility.SizeOf<Command>() + 0;
				commandSizes[(int)Command.Line] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<LineData>();
				commandSizes[(int)Command.CircleXZ] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<CircleXZData>();
				commandSizes[(int)Command.SphereOutline] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<SphereData>();
				commandSizes[(int)Command.Circle] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<CircleData>();
				commandSizes[(int)Command.Disc] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<CircleData>();
				commandSizes[(int)Command.DiscXZ] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<CircleXZData>();
				commandSizes[(int)Command.Box] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<BoxData>();
				commandSizes[(int)Command.WirePlane] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<PlaneData>();
				commandSizes[(int)Command.PushPersist] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<PersistData>();
				commandSizes[(int)Command.PopPersist] = UnsafeUtility.SizeOf<Command>();
				commandSizes[(int)Command.Text] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<TextData>(); // Dynamically sized
				commandSizes[(int)Command.Text3D] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<TextData3D>(); // Dynamically sized
				commandSizes[(int)Command.PushLineWidth] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<LineWidthData>();
				commandSizes[(int)Command.PopLineWidth] = UnsafeUtility.SizeOf<Command>();
				commandSizes[(int)Command.CaptureState] = UnsafeUtility.SizeOf<Command>();

				unsafe {
					// Store in local variables for performance (makes it possible to use registers for a lot of fields)
					var bufferPersist = *buffer;

					long writeOffset = 0;
					long readOffset = 0;
					bool shouldWrite = false;
					int stackSize = 0;
					long lastNonMetaWrite = -1;

					while (readOffset < bufferPersist.GetLength()) {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
						UnityEngine.Assertions.Assert.IsTrue(readOffset + UnsafeUtility.SizeOf<Command>() <= bufferPersist.GetLength());
#endif
						var cmd = *(Command*)((byte*)bufferPersist.Ptr + readOffset);
						var cmdBit = 1 << ((int)cmd & 0xFF);
						bool isMeta = (cmdBit & StreamSplitter.MetaCommands) != 0;
						int size = commandSizes[(int)cmd & 0xFF] + ((cmd & Command.PushColorInline) != 0 ? UnsafeUtility.SizeOf<Color32>() : 0);

						if ((cmd & (Command)0xFF) == Command.Text) {
							// Very pretty way of reading the TextData struct right after the command label and optional Color32
							var data = *((TextData*)((byte*)bufferPersist.Ptr + readOffset + size) - 1);
							// Add the size of the embedded string in the buffer
							size += data.numCharacters * UnsafeUtility.SizeOf<System.UInt16>();
						} else if ((cmd & (Command)0xFF) == Command.Text3D) {
							// Very pretty way of reading the TextData struct right after the command label and optional Color32
							var data = *((TextData3D*)((byte*)bufferPersist.Ptr + readOffset + size) - 1);
							// Add the size of the embedded string in the buffer
							size += data.numCharacters * UnsafeUtility.SizeOf<System.UInt16>();
						}

#if ENABLE_UNITY_COLLECTIONS_CHECKS
						UnityEngine.Assertions.Assert.IsTrue(readOffset + size <= bufferPersist.GetLength());
						UnityEngine.Assertions.Assert.IsTrue(writeOffset + size <= bufferPersist.GetLength());
#endif

						if (shouldWrite || isMeta) {
							if (!isMeta) lastNonMetaWrite = writeOffset;
							if (writeOffset != readOffset) {
								// We need to use memmove instead of memcpy because the source and destination regions may overlap
								UnsafeUtility.MemMove((byte*)bufferPersist.Ptr + writeOffset, (byte*)bufferPersist.Ptr + readOffset, size);
							}
							writeOffset += size;
						}

						if ((cmdBit & StreamSplitter.PushCommands) != 0) {
							if ((cmd & (Command)0xFF) == Command.PushPersist) {
								// Very pretty way of reading the PersistData struct right after the command label and optional Color32
								// (even though a PushColorInline command is not usually combined with PushPersist)
								var data = *((PersistData*)((byte*)bufferPersist.Ptr + readOffset + size) - 1);
								// Scopes only survive if this condition is true
								shouldWrite = time <= data.endTime;
							}

							stackScope[stackSize] = (int)(writeOffset - size);
							stackPersist[stackSize] = shouldWrite;
							stackSize++;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
							if (stackSize >= Builder.MaxStackSize) throw new System.Exception("Push commands are too deeply nested. This can happen if you have deeply nested WithMatrix or WithColor scopes.");
#else
							if (stackSize >= Builder.MaxStackSize) {
								buffer->SetLength(0);
								return;
							}
#endif
						} else if ((cmdBit & StreamSplitter.PopCommands) != 0) {
							stackSize--;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
							if (stackSize < 0) throw new System.Exception("Trying to issue a pop command but there is no corresponding push command");
#else
							if (stackSize < 0) {
								buffer->SetLength(0);
								return;
							}
#endif
							// If a scope was pushed and later popped, but no actual draw commands were written to the buffers
							// inside that scope then we erase the whole scope.
							if ((int)lastNonMetaWrite < stackScope[stackSize]) {
								writeOffset = (long)stackScope[stackSize];
							}

							shouldWrite = stackPersist[stackSize];
						}

						readOffset += size;
					}

					bufferPersist.SetLength((int)writeOffset);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
					if (stackSize != 0) throw new System.Exception("Inconsistent push/pop commands. Are your push and pop commands properly matched?");
#else
					if (stackSize != 0) {
						buffer->SetLength(0);
						return;
					}
#endif

					*buffer = bufferPersist;
				}
			}
		}

		[BurstCompile]
		internal struct StreamSplitter : IJob {
			public NativeArray<UnsafeAppendBuffer> inputBuffers;
			[NativeDisableUnsafePtrRestriction]
			public unsafe UnsafeAppendBuffer* staticBuffer, dynamicBuffer, persistentBuffer;

			internal static readonly int PushCommands = (1 << (int)Command.PushColor) | (1 << (int)Command.PushMatrix) | (1 << (int)Command.PushSetMatrix) | (1 << (int)Command.PushPersist) | (1 << (int)Command.PushLineWidth);
			internal static readonly int PopCommands = (1 << (int)Command.PopColor) | (1 << (int)Command.PopMatrix) | (1 << (int)Command.PopPersist) | (1 << (int)Command.PopLineWidth);
			internal static readonly int MetaCommands = PushCommands | PopCommands;
			internal static readonly int DynamicCommands = (1 << (int)Command.SphereOutline) | (1 << (int)Command.CircleXZ) | (1 << (int)Command.Circle) | (1 << (int)Command.DiscXZ) | (1 << (int)Command.Disc) | (1 << (int)Command.Text) | (1 << (int)Command.Text3D) | (1 << (int)Command.CaptureState) | MetaCommands;
			internal static readonly int StaticCommands = (1 << (int)Command.Line) | (1 << (int)Command.Box) | (1 << (int)Command.WirePlane) | MetaCommands;

			public void Execute () {
				var lastWriteStatic = -1;
				var lastWriteDynamic = -1;
				var lastWritePersist = -1;
				var stackStatic = new NativeArray<int>(Builder.MaxStackSize, Allocator.Temp, NativeArrayOptions.ClearMemory);
				var stackDynamic = new NativeArray<int>(Builder.MaxStackSize, Allocator.Temp, NativeArrayOptions.ClearMemory);
				var stackPersist = new NativeArray<int>(Builder.MaxStackSize, Allocator.Temp, NativeArrayOptions.ClearMemory);

				// Size of all commands in bytes
				var commandSizes = new NativeArray<int>(20, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

				commandSizes[(int)Command.PushColor] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<Color32>();
				commandSizes[(int)Command.PopColor] = UnsafeUtility.SizeOf<Command>() + 0;
				commandSizes[(int)Command.PushMatrix] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<float4x4>();
				commandSizes[(int)Command.PushSetMatrix] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<float4x4>();
				commandSizes[(int)Command.PopMatrix] = UnsafeUtility.SizeOf<Command>() + 0;
				commandSizes[(int)Command.Line] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<LineData>();
				commandSizes[(int)Command.CircleXZ] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<CircleXZData>();
				commandSizes[(int)Command.SphereOutline] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<SphereData>();
				commandSizes[(int)Command.Circle] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<CircleData>();
				commandSizes[(int)Command.Disc] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<CircleData>();
				commandSizes[(int)Command.DiscXZ] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<CircleXZData>();
				commandSizes[(int)Command.Box] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<BoxData>();
				commandSizes[(int)Command.WirePlane] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<PlaneData>();
				commandSizes[(int)Command.PushPersist] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<PersistData>();
				commandSizes[(int)Command.PopPersist] = UnsafeUtility.SizeOf<Command>();
				commandSizes[(int)Command.Text] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<TextData>(); // Dynamically sized
				commandSizes[(int)Command.Text3D] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<TextData3D>(); // Dynamically sized
				commandSizes[(int)Command.PushLineWidth] = UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<LineWidthData>();
				commandSizes[(int)Command.PopLineWidth] = UnsafeUtility.SizeOf<Command>();
				commandSizes[(int)Command.CaptureState] = UnsafeUtility.SizeOf<Command>();

				unsafe {
					// Store in local variables for performance (makes it possible to use registers for a lot of fields)
					var bufferStatic = *staticBuffer;
					var bufferDynamic = *dynamicBuffer;
					var bufferPersist = *persistentBuffer;

					bufferStatic.Reset();
					bufferDynamic.Reset();
					bufferPersist.Reset();

					for (int i = 0; i < inputBuffers.Length; i++) {
						int stackSize = 0;
						int persist = 0;
						var reader = inputBuffers[i].AsReader();

						// Guarantee we have enough space for copying the whole buffer
						if (bufferStatic.Capacity < bufferStatic.GetLength() + reader.Size) bufferStatic.SetCapacity(math.ceilpow2(bufferStatic.GetLength() + reader.Size));
						if (bufferDynamic.Capacity < bufferDynamic.GetLength() + reader.Size) bufferDynamic.SetCapacity(math.ceilpow2(bufferDynamic.GetLength() + reader.Size));
						if (bufferPersist.Capacity < bufferPersist.GetLength() + reader.Size) bufferPersist.SetCapacity(math.ceilpow2(bufferPersist.GetLength() + reader.Size));

						// To ensure that even if exceptions are thrown the output buffers still point to valid memory regions
						*staticBuffer = bufferStatic;
						*dynamicBuffer = bufferDynamic;
						*persistentBuffer = bufferPersist;

						while (reader.Offset < reader.Size) {
							var cmd = *(Command*)((byte*)reader.Ptr + reader.Offset);
							var cmdBit = 1 << ((int)cmd & 0xFF);
							int size = commandSizes[(int)cmd & 0xFF] + ((cmd & Command.PushColorInline) != 0 ? UnsafeUtility.SizeOf<Color32>() : 0);
							bool isMeta = (cmdBit & MetaCommands) != 0;

							if ((cmd & (Command)0xFF) == Command.Text) {
								// Very pretty way of reading the TextData struct right after the command label and optional Color32
								var data = *((TextData*)((byte*)reader.Ptr + reader.Offset + size) - 1);
								// Add the size of the embedded string in the buffer
								// TODO: Unaligned memory access performance penalties??
								size += data.numCharacters * UnsafeUtility.SizeOf<System.UInt16>();
							} else if ((cmd & (Command)0xFF) == Command.Text3D) {
								// Very pretty way of reading the TextData struct right after the command label and optional Color32
								var data = *((TextData3D*)((byte*)reader.Ptr + reader.Offset + size) - 1);
								// Add the size of the embedded string in the buffer
								// TODO: Unaligned memory access performance penalties??
								size += data.numCharacters * UnsafeUtility.SizeOf<System.UInt16>();
							}

#if ENABLE_UNITY_COLLECTIONS_CHECKS
							UnityEngine.Assertions.Assert.IsTrue(reader.Offset + size <= reader.Size);
#endif

							if ((cmdBit & DynamicCommands) != 0 && persist == 0) {
								if (!isMeta) lastWriteDynamic = GetSize(bufferDynamic);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
								UnityEngine.Assertions.Assert.IsTrue(bufferDynamic.GetLength() + size <= bufferDynamic.Capacity);
#endif
								UnsafeUtility.MemCpy((byte*)bufferDynamic.Ptr + bufferDynamic.GetLength(), (byte*)reader.Ptr + reader.Offset, size);
								bufferDynamic.SetLength(bufferDynamic.GetLength() + size);
							}

							if ((cmdBit & StaticCommands) != 0 && persist == 0) {
								if (!isMeta) lastWriteStatic = bufferStatic.GetLength();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
								UnityEngine.Assertions.Assert.IsTrue(bufferStatic.GetLength() + size <= bufferStatic.Capacity);
#endif
								UnsafeUtility.MemCpy((byte*)bufferStatic.Ptr + bufferStatic.GetLength(), (byte*)reader.Ptr + reader.Offset, size);
								bufferStatic.SetLength(bufferStatic.GetLength() + size);
							}

							if ((cmdBit & MetaCommands) != 0 || persist > 0) {
								if (persist > 0 && !isMeta) lastWritePersist = bufferPersist.GetLength();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
								UnityEngine.Assertions.Assert.IsTrue(bufferPersist.GetLength() + size <= bufferPersist.Capacity);
#endif
								UnsafeUtility.MemCpy((byte*)bufferPersist.Ptr + bufferPersist.GetLength(), (byte*)reader.Ptr + reader.Offset, size);
								bufferPersist.SetLength(bufferPersist.GetLength() + size);
							}

							if ((cmdBit & PushCommands) != 0) {
								stackStatic[stackSize] = bufferStatic.GetLength() - size;
								stackDynamic[stackSize] = bufferDynamic.GetLength() - size;
								stackPersist[stackSize] = bufferPersist.GetLength() - size;
								stackSize++;
								if ((cmd & (Command)0xFF) == Command.PushPersist) {
									persist++;
								}
#if ENABLE_UNITY_COLLECTIONS_CHECKS
								if (stackSize >= Builder.MaxStackSize) throw new System.Exception("Push commands are too deeply nested. This can happen if you have deeply nested WithMatrix or WithColor scopes.");
#else
								if (stackSize >= Builder.MaxStackSize) {
									return;
								}
#endif
							} else if ((cmdBit & PopCommands) != 0) {
								stackSize--;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
								if (stackSize < 0) throw new System.Exception("Trying to issue a pop command but there is no corresponding push command");
#else
								if (stackSize < 0) return;
#endif
								// If a scope was pushed and later popped, but no actual draw commands were written to the buffers
								// inside that scope then we erase the whole scope.
								if (lastWriteStatic < stackStatic[stackSize]) {
									bufferStatic.SetLength(stackStatic[stackSize]);
								}
								if (lastWriteDynamic < stackDynamic[stackSize]) {
									bufferDynamic.SetLength(stackDynamic[stackSize]);
								}
								if (lastWritePersist < stackPersist[stackSize]) {
									bufferPersist.SetLength(stackPersist[stackSize]);
								}
								if ((cmd & (Command)0xFF) == Command.PopPersist) {
									persist--;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
									if (persist < 0) throw new System.Exception("Too many PopPersist commands. Are your PushPersist/PopPersist calls matched?");
#else
									if (persist < 0) return;
#endif
								}
							}

							reader.Offset += size;
						}

#if ENABLE_UNITY_COLLECTIONS_CHECKS
						if (stackSize != 0) throw new System.Exception("Too few pop commands and too many push commands. Are your push and pop commands properly matched?");
						if (reader.Offset != reader.Size) throw new System.Exception("Did not end up at the end of the buffer. This is a bug.");
#else
						if (stackSize != 0) return;
						if (reader.Offset != reader.Size) return;
#endif
					}

#if ENABLE_UNITY_COLLECTIONS_CHECKS
					if (bufferStatic.GetLength() > bufferStatic.Capacity) throw new System.Exception("Buffer overrun. This is a bug");
					if (bufferDynamic.GetLength() > bufferDynamic.Capacity) throw new System.Exception("Buffer overrun. This is a bug");
					if (bufferPersist.GetLength() > bufferPersist.Capacity) throw new System.Exception("Buffer overrun. This is a bug");
#endif

					*staticBuffer = bufferStatic;
					*dynamicBuffer = bufferDynamic;
					*persistentBuffer = bufferPersist;
				}
			}
		}

		// Note: Setting FloatMode to Fast causes visual artificats when drawing circles.
		// I think it is because math.sin(float4) produces slightly different results
		// for each component in the input.
		[BurstCompile(FloatMode = FloatMode.Default)]
		internal struct Builder : IJob {
			[NativeDisableUnsafePtrRestriction]
			public unsafe ProcessedBuilderData.MeshBuffers* buffers;

			[NativeDisableUnsafePtrRestriction]
			public unsafe SDFCharacter* characterInfo;
			public int characterInfoLength;

			public Color32 currentColor;
			public float4x4 currentMatrix;
			public LineWidthData currentLineWidthData;
			float3 minBounds;
			float3 maxBounds;
			public float3 cameraPosition;
			public Quaternion cameraRotation;
			public float2 cameraDepthToPixelSize;
			public bool cameraIsOrthographic;

			[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
			public struct Vertex {
				public float3 position;
				public float3 uv2;
				public Color32 color;
				public float2 uv;
			}

			[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
			public struct TextVertex {
				public float3 position;
				public Color32 color;
				public float2 uv;
			}

			static unsafe void Add<T>(UnsafeAppendBuffer* buffer, T value) where T : struct {
				int size = UnsafeUtility.SizeOf<T>();
				// We know that the buffer has enough capacity, so we can just write to the buffer without
				// having to add branches for the overflow case (like buffer->Add will do).
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				UnityEngine.Assertions.Assert.IsTrue(buffer->GetLength() + size <= buffer->Capacity);
#endif
				UnsafeUtility.WriteArrayElement((byte*)buffer->Ptr + buffer->GetLength(), 0, value);
				buffer->SetLength(buffer->GetLength() + size);
			}

			static unsafe void Reserve (UnsafeAppendBuffer* buffer, int size) {
				var newSize = buffer->GetLength() + size;

				if (newSize > buffer->Capacity) {
					buffer->SetCapacity(math.max(newSize, buffer->Capacity * 2));
				}
			}

			static float3 PerspectiveDivide (float4 p) {
				return p.xyz / p.w;
			}

			unsafe void AddText (System.UInt16* text, TextData textData, Color32 color) {
				var pivot = PerspectiveDivide(math.mul(currentMatrix, new float4(textData.center, 1.0f)));

				AddTextInternal(
					text,
					pivot,
					math.mul(cameraRotation, new float3(1, 0, 0)),
					math.mul(cameraRotation, new float3(0, 1, 0)),
					textData.alignment,
					textData.sizeInPixels,
					true,
					textData.numCharacters,
					color
					);
			}

			unsafe void AddText3D (System.UInt16* text, TextData3D textData, Color32 color) {
				var pivot = PerspectiveDivide(math.mul(currentMatrix, new float4(textData.center, 1.0f)));
				var m = math.mul(currentMatrix, new float4x4(textData.rotation, float3.zero));

				AddTextInternal(
					text,
					pivot,
					m.c0.xyz,
					m.c1.xyz,
					textData.alignment,
					textData.size,
					false,
					textData.numCharacters,
					color
					);
			}


			unsafe void AddTextInternal (System.UInt16* text, float3 pivot, float3 right, float3 up, LabelAlignment alignment, float size, bool sizeIsInPixels, int numCharacters, Color32 color) {
				var distance = math.abs(math.dot(pivot - cameraPosition, math.mul(cameraRotation, new float3(0, 0, 1)))); // math.length(pivot - cameraPosition);
				var pixelSize = cameraDepthToPixelSize.x * distance + cameraDepthToPixelSize.y;
				float fontWorldSize = size;

				if (sizeIsInPixels) fontWorldSize *= pixelSize;

				right *= fontWorldSize;
				up *= fontWorldSize;

				// Calculate the total width (in pixels divided by fontSize) of the text
				float maxWidth = 0;
				float currentWidth = 0;
				float numLines = 1;

				for (int i = 0; i < numCharacters; i++) {
					var characterInfoIndex = text[i];
					if (characterInfoIndex == SDFLookupData.Newline) {
						maxWidth = math.max(maxWidth, currentWidth);
						currentWidth = 0;
						numLines++;
					} else {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
						if (characterInfoIndex >= characterInfoLength) throw new System.Exception("Invalid character. No info exists. This is a bug.");
#endif
						currentWidth += characterInfo[characterInfoIndex].advance;
					}
				}
				maxWidth = math.max(maxWidth, currentWidth);

				// Calculate the world space position of the text given the camera and text alignment
				var pos = pivot;
				pos -= right * maxWidth * alignment.relativePivot.x;
				// Size of a character as a fraction of a whole line using the current font
				const float FontCharacterFractionOfLine = 0.75f;
				// Where the upper and lower parts of the text will be assuming we start to write at y=0
				var lower = 1 - numLines;
				var upper = FontCharacterFractionOfLine;
				var yAdjustment = math.lerp(lower, upper, alignment.relativePivot.y);
				pos -= up * yAdjustment;
				pos += (float3)(cameraRotation * Vector3.right * (pixelSize * alignment.pixelOffset.x));
				pos += (float3)(cameraRotation * Vector3.up * (pixelSize * alignment.pixelOffset.y));

				var textVertices = &buffers->textVertices;
				var textTriangles = &buffers->textTriangles;
				const int VerticesPerCharacter = 4;
				const int TrianglesPerCharacter = 6;

				// Reserve all buffer space beforehand
				Reserve(textVertices, numCharacters * VerticesPerCharacter * UnsafeUtility.SizeOf<TextVertex>());
				Reserve(textTriangles, numCharacters * TrianglesPerCharacter * UnsafeUtility.SizeOf<int>());

				var lineStart = pos;

				for (int i = 0; i < numCharacters; i++) {
					var characterInfoIndex = text[i];

					if (characterInfoIndex == SDFLookupData.Newline) {
						lineStart -= up;
						pos = lineStart;
						continue;
					}

					// Get character rendering information from the font
					SDFCharacter ch = characterInfo[characterInfoIndex];

					int vertexIndexStart = textVertices->GetLength() / UnsafeUtility.SizeOf<TextVertex>();

					float3 v;

					v = pos + ch.vertexTopLeft.x * right + ch.vertexTopLeft.y * up;
					minBounds = math.min(minBounds, v);
					maxBounds = math.max(maxBounds, v);
					Add(textVertices, new TextVertex {
						position = v,
						uv = ch.uvTopLeft,
						color = color,
					});

					v = pos + ch.vertexTopRight.x * right + ch.vertexTopRight.y * up;
					minBounds = math.min(minBounds, v);
					maxBounds = math.max(maxBounds, v);
					Add(textVertices, new TextVertex {
						position = v,
						uv = ch.uvTopRight,
						color = color,
					});

					v = pos + ch.vertexBottomRight.x * right + ch.vertexBottomRight.y * up;
					minBounds = math.min(minBounds, v);
					maxBounds = math.max(maxBounds, v);
					Add(textVertices, new TextVertex {
						position = v,
						uv = ch.uvBottomRight,
						color = color,
					});

					v = pos + ch.vertexBottomLeft.x * right + ch.vertexBottomLeft.y * up;
					minBounds = math.min(minBounds, v);
					maxBounds = math.max(maxBounds, v);
					Add(textVertices, new TextVertex {
						position = v,
						uv = ch.uvBottomLeft,
						color = color,
					});

					Add(textTriangles, vertexIndexStart + 0);
					Add(textTriangles, vertexIndexStart + 1);
					Add(textTriangles, vertexIndexStart + 2);

					Add(textTriangles, vertexIndexStart + 0);
					Add(textTriangles, vertexIndexStart + 2);
					Add(textTriangles, vertexIndexStart + 3);

					// Advance character position
					pos += right * ch.advance;
				}
			}

			float3 lastNormalizedLineDir;
			float lastLineWidth;

			const float MaxCirclePixelError = 0.5f;

			/// <summary>
			/// Joins the end of the last line segment to the start of the line segment at the given byte offset.
			/// The byte offset refers to the buffers->vertices array. It is assumed that the start of the line segment is given by the
			/// two vertices that start at the given offset into the array.
			/// </summary>
			void Join (int lineByteOffset) {
				unsafe {
					var outlineVertices = &buffers->vertices;
					var nextLineByteOffset = outlineVertices->GetLength() - 4*UnsafeUtility.SizeOf<Vertex>();

					if (nextLineByteOffset < 0) throw new System.Exception("Cannot call Join when there are no line segments written");

					// Cannot join line to itself
					if (lineByteOffset == nextLineByteOffset) return;

					// Third and fourth vertices in the previous line
					var prevLineVertex1 = (Vertex*)((byte*)outlineVertices->Ptr + lineByteOffset) + 0;
					var prevLineVertex2 = prevLineVertex1 + 1;
					// First and second vertices in the next line
					var nextLineVertex1 = (Vertex*)((byte*)outlineVertices->Ptr + nextLineByteOffset) + 2;
					var nextLineVertex2 = nextLineVertex1 + 1;

					var normalizedLineDir = lastNormalizedLineDir;
					var prevNormalizedLineDir = math.normalize(prevLineVertex1->uv2);
					var lineWidth = lastLineWidth;

					var cosAngle = math.dot(normalizedLineDir, prevNormalizedLineDir);
					if (math.lengthsq(prevLineVertex1->position - nextLineVertex1->position) < 0.001f*0.001f && cosAngle >= -0.87f) {
						// Safety: tangent cannot be 0 because cosAngle > -1
						var tangent = normalizedLineDir + prevNormalizedLineDir;
						// From the law of cosines we get that
						// tangent.magnitude = sqrt(2)*sqrt(1+cosAngle)

						// Create join!
						// Trigonometry gives us
						// joinRadius = lineWidth / (2*cos(alpha / 2))
						// Using half angle identity for cos we get
						// joinRadius = lineWidth / (sqrt(2)*sqrt(1 + cos(alpha))
						// Since the tangent already has mostly the same factors we can simplify the calculation
						// normalize(tangent) * joinRadius * 2
						// = tangent / (sqrt(2)*sqrt(1+cosAngle)) * joinRadius * 2
						// = tangent * lineWidth / (1 + cos(alpha)
						var joinLineDir = tangent * lineWidth / (1 + cosAngle);

						prevLineVertex1->uv2 = joinLineDir;
						prevLineVertex2->uv2 = joinLineDir;
						nextLineVertex1->uv2 = joinLineDir;
						nextLineVertex2->uv2 = joinLineDir;
					}
				}

				lastLineWidth = 0;
			}

			void AddLine (LineData line) {
				// Store the line direction in the vertex.
				// A line consists of 4 vertices. The line direction will be used to
				// offset the vertices to create a line with a fixed pixel thickness
				var a = PerspectiveDivide(math.mul(currentMatrix, new float4(line.a, 1.0f)));
				var b = PerspectiveDivide(math.mul(currentMatrix, new float4(line.b, 1.0f)));

				float lineWidth = currentLineWidthData.pixels;
				var normalizedLineDir = math.normalizesafe(b - a);

				if (math.any(math.isnan(normalizedLineDir))) throw new Exception("Nan line coordinates");
				if (lineWidth <= 0) {
					return;
				}

				// Update the bounding box
				minBounds = math.min(minBounds, math.min(a, b));
				maxBounds = math.max(maxBounds, math.max(a, b));

				unsafe {
					var outlineVertices = &buffers->vertices;

					// Make sure there is enough allocated capacity for 4 more vertices
					Reserve(outlineVertices, 4 * UnsafeUtility.SizeOf<Vertex>());

					// Insert 4 vertices
					// Doing it with pointers is faster, and this is the hottest
					// code of the whole gizmo drawing process.
					var ptr = (Vertex*)((byte*)outlineVertices->Ptr + outlineVertices->GetLength());

					var startLineDir = normalizedLineDir * lineWidth;
					var endLineDir = normalizedLineDir * lineWidth;

					// If dot(last dir, this dir) >= 0 => use join
					if (lineWidth > 1 && currentLineWidthData.automaticJoins && outlineVertices->GetLength() > 2*UnsafeUtility.SizeOf<Vertex>()) {
						// has previous vertex
						Vertex* lastVertex1 = (Vertex*)(ptr - 1);
						Vertex* lastVertex2 = (Vertex*)(ptr - 2);

						var cosAngle = math.dot(normalizedLineDir, lastNormalizedLineDir);
						if (math.all(lastVertex2->position == a) && lastLineWidth == lineWidth && cosAngle >= -0.6f) {
							// Safety: tangent cannot be 0 because cosAngle > -1
							var tangent = normalizedLineDir + lastNormalizedLineDir;
							// From the law of cosines we get that
							// tangent.magnitude = sqrt(2)*sqrt(1+cosAngle)

							// Create join!
							// Trigonometry gives us
							// joinRadius = lineWidth / (2*cos(alpha / 2))
							// Using half angle identity for cos we get
							// joinRadius = lineWidth / (sqrt(2)*sqrt(1 + cos(alpha))
							// Since the tangent already has mostly the same factors we can simplify the calculation
							// normalize(tangent) * joinRadius * 2
							// = tangent / (sqrt(2)*sqrt(1+cosAngle)) * joinRadius * 2
							// = tangent * lineWidth / (1 + cos(alpha)
							var joinLineDir = tangent * lineWidth / (1 + cosAngle);

							startLineDir = joinLineDir;
							lastVertex1->uv2 = startLineDir;
							lastVertex2->uv2 = startLineDir;
						}
					}

					outlineVertices->SetLength(outlineVertices->GetLength() + 4 * UnsafeUtility.SizeOf<Vertex>());
					*ptr++ = new Vertex {
						position = a,
						color = currentColor,
						uv = new float2(0, 0),
						uv2 = startLineDir,
					};
					*ptr++ = new Vertex {
						position = a,
						color = currentColor,
						uv = new float2(1, 0),
						uv2 = startLineDir,
					};

					*ptr++ = new Vertex {
						position = b,
						color = currentColor,
						uv = new float2(0, 1),
						uv2 = endLineDir,
					};
					*ptr++ = new Vertex {
						position = b,
						color = currentColor,
						uv = new float2(1, 1),
						uv2 = endLineDir,
					};

					lastNormalizedLineDir = normalizedLineDir;
					lastLineWidth = lineWidth;
				}
			}

			/// <summary>Calculate number of steps to use for drawing a circle at the specified point and radius to get less than the specified pixel error.</summary>
			int CircleSteps (float3 center, float radius, float maxPixelError) {
				var centerv4 = math.mul(currentMatrix, new float4(center, 1.0f));

				if (math.abs(centerv4.w) < 0.0000001f) return 3;
				var cc = PerspectiveDivide(centerv4);
				// Take the maximum scale factor among the 3 axes.
				// If the current matrix has a uniform scale then they are all the same.
				var maxScaleFactor = math.sqrt(math.max(math.max(math.lengthsq(currentMatrix.c0.xyz), math.lengthsq(currentMatrix.c1.xyz)), math.lengthsq(currentMatrix.c2.xyz))) / centerv4.w;
				var realWorldRadius = radius * maxScaleFactor;
				var distance = math.length(cc - cameraPosition);

				var pixelSize = cameraDepthToPixelSize.x * distance + cameraDepthToPixelSize.y;
				// realWorldRadius += pixelSize * this.currentLineWidthData.pixels * 0.5f;
				var cosAngle = 1 - (maxPixelError * pixelSize) / realWorldRadius;
				int steps = cosAngle < 0 ? 3 : (int)math.ceil(math.PI / (math.acos(cosAngle)));

				return steps;
			}

			void AddCircle (CircleData circle) {
				// If the circle has a zero normal then just ignore it
				if (math.all(circle.normal == 0)) return;

				var steps = CircleSteps(circle.center, circle.radius, MaxCirclePixelError);

				// Round up to nearest multiple of 3 (required for the SIMD to work)
				steps = ((steps + 2) / 3) * 3;

				circle.normal = math.normalize(circle.normal);
				float3 tangent1;
				if (math.all(math.abs(circle.normal - new float3(0, 1, 0)) < 0.001f)) {
					// The normal was (almost) identical to (0, 1, 0)
					tangent1 = new float3(0, 0, 1);
				} else {
					// Common case
					tangent1 = math.cross(circle.normal, new float3(0, 1, 0));
				}

				var oldMatrix = currentMatrix;
				currentMatrix = math.mul(currentMatrix, Matrix4x4.TRS(circle.center, Quaternion.LookRotation(circle.normal, tangent1), new Vector3(circle.radius, circle.radius, circle.radius)));

				int firstLineByteOffset;
				unsafe {
					firstLineByteOffset = buffers->vertices.GetLength();
				}

				float invSteps = 1.0f / steps;
				bool tmpJoins = currentLineWidthData.automaticJoins;
				currentLineWidthData.automaticJoins = true;
				for (int i = 0; i < steps; i += 3) {
					var i4 = math.lerp(0, 2*Mathf.PI, new float4(i, i + 1, i + 2, i + 3) * invSteps);
					// Calculate 4 sines and cosines at the same time using SIMD
					math.sincos(i4, out float4 sin, out float4 cos);
					AddLine(new LineData { a = new float3(cos.x, sin.x, 0), b = new float3(cos.y, sin.y, 0) });
					AddLine(new LineData { a = new float3(cos.y, sin.y, 0), b = new float3(cos.z, sin.z, 0) });
					AddLine(new LineData { a = new float3(cos.z, sin.z, 0), b = new float3(cos.w, sin.w, 0) });
				}

				int lastLineByteOffset;

				unsafe {
					lastLineByteOffset = buffers->vertices.GetLength();
				}
				if (firstLineByteOffset != lastLineByteOffset) {
					// Join the start and end segments.
					// Only do this if any lines were actually added.
					Join(firstLineByteOffset);
				}
				currentLineWidthData.automaticJoins = tmpJoins;

				currentMatrix = oldMatrix;
			}

			void AddDisc (CircleData circle) {
				// If the circle has a zero normal then just ignore it
				if (math.all(circle.normal == 0)) return;

				var steps = CircleSteps(circle.center, circle.radius, MaxCirclePixelError);

				// Round up to nearest multiple of 3 (required for the SIMD to work)
				steps = ((steps + 2) / 3) * 3;

				circle.normal = math.normalize(circle.normal);
				float3 tangent1;
				if (math.all(math.abs(circle.normal - new float3(0, 1, 0)) < 0.001f)) {
					// The normal was (almost) identical to (0, 1, 0)
					tangent1 = new float3(0, 0, 1);
				} else {
					// Common case
					tangent1 = math.cross(circle.normal, new float3(0, 1, 0));
				}

				float invSteps = 1.0f / steps;

				unsafe {
					var solidVertices = &buffers->solidVertices;
					var solidTriangles = &buffers->solidTriangles;
					Reserve(solidVertices, steps * UnsafeUtility.SizeOf<Vertex>());
					Reserve(solidTriangles, 3*(steps-2) * UnsafeUtility.SizeOf<int>());

					var matrix = math.mul(currentMatrix, Matrix4x4.TRS(circle.center, Quaternion.LookRotation(circle.normal, tangent1), new Vector3(circle.radius, circle.radius, circle.radius)));

					var mn = minBounds;
					var mx = maxBounds;
					int vertexCount = solidVertices->GetLength() / UnsafeUtility.SizeOf<Vertex>();

					for (int i = 0; i < steps; i++) {
						var t = math.lerp(0, 2*Mathf.PI, i * invSteps);
						math.sincos(t, out float sin, out float cos);

						var p = PerspectiveDivide(math.mul(matrix, new float4(cos, sin, 0, 1)));
						// Update the bounding box
						mn = math.min(mn, p);
						mx = math.max(mx, p);

						Add(solidVertices, new Vertex {
							position = p,
							color = currentColor,
							uv = new float2(0, 0),
							uv2 = new float3(0, 0, 0),
						});
					}

					minBounds = mn;
					maxBounds = mx;

					for (int i = 0; i < steps - 2; i++) {
						Add(solidTriangles, vertexCount);
						Add(solidTriangles, vertexCount + i + 1);
						Add(solidTriangles, vertexCount + i + 2);
					}
				}
			}

			void AddSphereOutline (SphereData circle) {
				var centerv4 = math.mul(currentMatrix, new float4(circle.center, 1.0f));

				if (math.abs(centerv4.w) < 0.0000001f) return;
				var center = PerspectiveDivide(centerv4);
				// Figure out the actual radius of the sphere after all the matrix multiplications.
				// In case of a non-uniform scale, pick the largest radius
				var maxScaleFactor = math.sqrt(math.max(math.max(math.lengthsq(currentMatrix.c0.xyz), math.lengthsq(currentMatrix.c1.xyz)), math.lengthsq(currentMatrix.c2.xyz))) / centerv4.w;
				var realWorldRadius = circle.radius * maxScaleFactor;

				if (cameraIsOrthographic) {
					var prevMatrix = this.currentMatrix;
					this.currentMatrix = float4x4.identity;
					AddCircle(new CircleData {
						center = center,
						normal = new float3(this.cameraRotation * Vector3.forward),
						radius = realWorldRadius,
					});
					this.currentMatrix = prevMatrix;
				} else {
					var dist = math.length(this.cameraPosition - center);
					// Camera is inside the sphere, cannot draw
					if (dist <= realWorldRadius) return;

					var offsetTowardsCamera = realWorldRadius * realWorldRadius / dist;
					var outlineRadius = math.sqrt(realWorldRadius * realWorldRadius - offsetTowardsCamera * offsetTowardsCamera);
					var normal = math.normalize(this.cameraPosition - center);
					var prevMatrix = this.currentMatrix;
					this.currentMatrix = float4x4.identity;
					AddCircle(new CircleData {
						center = center + normal * offsetTowardsCamera,
						normal = normal,
						radius = outlineRadius,
					});
					this.currentMatrix = prevMatrix;
				}
			}

			void AddCircle (CircleXZData circle) {
				var steps = CircleSteps(circle.center, circle.radius, MaxCirclePixelError);

				// Round up to nearest multiple of 3 (required for the SIMD to work)
				steps = ((steps + 2) / 3) * 3;

				circle.endAngle = math.clamp(circle.endAngle, circle.startAngle - Mathf.PI * 2, circle.startAngle + Mathf.PI * 2);

				var oldMatrix = currentMatrix;
				currentMatrix = math.mul(currentMatrix, Matrix4x4.Translate(circle.center) * Matrix4x4.Scale(new Vector3(circle.radius, circle.radius, circle.radius)));

				int firstLineByteOffset;
				unsafe {
					firstLineByteOffset = buffers->vertices.GetLength();
				}

				float invSteps = 1.0f / steps;
				bool tmpJoins = currentLineWidthData.automaticJoins;
				currentLineWidthData.automaticJoins = true;
				for (int i = 0; i < steps; i += 3) {
					var i4 = math.lerp(circle.startAngle, circle.endAngle, new float4(i, i + 1, i + 2, i + 3) * invSteps);
					// Calculate 4 sines and cosines at the same time using SIMD
					math.sincos(i4, out float4 sin, out float4 cos);
					AddLine(new LineData { a = new float3(cos.x, 0, sin.x), b = new float3(cos.y, 0, sin.y) });
					AddLine(new LineData { a = new float3(cos.y, 0, sin.y), b = new float3(cos.z, 0, sin.z) });
					AddLine(new LineData { a = new float3(cos.z, 0, sin.z), b = new float3(cos.w, 0, sin.w) });
				}

				int lastLineByteOffset;

				unsafe {
					lastLineByteOffset = buffers->vertices.GetLength();
				}
				if (firstLineByteOffset != lastLineByteOffset) {
					// Join the start and end segments.
					// Only do this if any lines were actually added.
					Join(firstLineByteOffset);
				}
				currentLineWidthData.automaticJoins = tmpJoins;

				currentMatrix = oldMatrix;
			}

			void AddDisc (CircleXZData circle) {
				var steps = CircleSteps(circle.center, circle.radius, MaxCirclePixelError);

				// Round up to nearest multiple of 3 (required for the SIMD to work)
				// We don't actually use SIMD here, but it's good that it's consistent with AddCircle.
				steps = ((steps + 2) / 3) * 3;

				circle.endAngle = math.clamp(circle.endAngle, circle.startAngle - Mathf.PI * 2, circle.startAngle + Mathf.PI * 2);

				float invSteps = 1.0f / steps;

				unsafe {
					var solidVertices = &buffers->solidVertices;
					var solidTriangles = &buffers->solidTriangles;
					Reserve(solidVertices, (2+steps) * UnsafeUtility.SizeOf<Vertex>());
					Reserve(solidTriangles, 3*steps * UnsafeUtility.SizeOf<int>());

					var matrix = math.mul(currentMatrix, Matrix4x4.Translate(circle.center) * Matrix4x4.Scale(new Vector3(circle.radius, circle.radius, circle.radius)));

					var worldCenter = PerspectiveDivide(math.mul(matrix, new float4(0, 0, 0, 1)));
					Add(solidVertices, new Vertex {
						position = worldCenter,
						color = currentColor,
						uv = new float2(0, 0),
						uv2 = new float3(0, 0, 0),
					});

					var mn = math.min(minBounds, worldCenter);
					var mx = math.max(maxBounds, worldCenter);
					int vertexCount = solidVertices->GetLength() / UnsafeUtility.SizeOf<Vertex>();

					for (int i = 0; i <= steps; i++) {
						var t = math.lerp(circle.startAngle, circle.endAngle, i * invSteps);
						math.sincos(t, out float sin, out float cos);

						var p = PerspectiveDivide(math.mul(matrix, new float4(cos, 0, sin, 1)));
						// Update the bounding box
						mn = math.min(mn, p);
						mx = math.max(mx, p);

						Add(solidVertices, new Vertex {
							position = p,
							color = currentColor,
							uv = new float2(0, 0),
							uv2 = new float3(0, 0, 0),
						});
					}

					minBounds = mn;
					maxBounds = mx;

					for (int i = 0; i < steps; i++) {
						// Center vertex
						Add(solidTriangles, vertexCount - 1);
						Add(solidTriangles, vertexCount + i + 0);
						Add(solidTriangles, vertexCount + i + 1);
					}
				}
			}

			void AddPlane (PlaneData plane) {
				var oldMatrix = currentMatrix;

				currentMatrix = math.mul(currentMatrix, float4x4.TRS(plane.center, plane.rotation, new float3(plane.size.x * 0.5f, 1, plane.size.y * 0.5f)));

				AddLine(new LineData { a = new float3(-1, 0, -1), b = new float3(1, 0, -1) });
				AddLine(new LineData { a = new float3(1, 0, -1), b = new float3(1, 0, 1) });
				AddLine(new LineData { a = new float3(1, 0, 1), b = new float3(-1, 0, 1) });
				AddLine(new LineData { a = new float3(-1, 0, 1), b = new float3(-1, 0, -1) });

				currentMatrix = oldMatrix;
			}

			static readonly float4[] BoxVertices = {
				new float4(-1, -1, -1, 1),
				new float4(-1, -1, +1, 1),
				new float4(-1, +1, -1, 1),
				new float4(-1, +1, +1, 1),
				new float4(+1, -1, -1, 1),
				new float4(+1, -1, +1, 1),
				new float4(+1, +1, -1, 1),
				new float4(+1, +1, +1, 1),
			};

			static readonly int[] BoxTriangles = {
				// Bottom two triangles
				0, 1, 5,
				0, 5, 4,

				// Top
				7, 3, 2,
				7, 2, 6,

				// -X
				0, 1, 3,
				0, 3, 2,

				// +X
				4, 5, 7,
				4, 7, 6,

				// +Z
				1, 3, 7,
				1, 7, 5,

				// -Z
				0, 2, 6,
				0, 6, 4,
			};

			void AddBox (BoxData box) {
				unsafe {
					var solidVertices = &buffers->solidVertices;
					var solidTriangles = &buffers->solidTriangles;
					Reserve(solidVertices, BoxVertices.Length * UnsafeUtility.SizeOf<Vertex>());
					Reserve(solidTriangles, BoxTriangles.Length * UnsafeUtility.SizeOf<int>());

					var matrix = math.mul(currentMatrix, Matrix4x4.Translate(box.center) * Matrix4x4.Scale(box.size * 0.5f));

					var mn = minBounds;
					var mx = maxBounds;
					int vertexCount = solidVertices->GetLength() / UnsafeUtility.SizeOf<Vertex>();
					for (int i = 0; i < BoxVertices.Length; i++) {
						var p = PerspectiveDivide(math.mul(matrix, BoxVertices[i]));
						// Update the bounding box
						mn = math.min(mn, p);
						mx = math.max(mx, p);

						Add(solidVertices, new Vertex {
							position = p,
							color = currentColor,
							uv = new float2(0, 0),
							uv2 = new float3(0, 0, 0),
						});
					}

					minBounds = mn;
					maxBounds = mx;

					for (int i = 0; i < BoxTriangles.Length; i++) {
						Add(solidTriangles, vertexCount + BoxTriangles[i]);
					}
				}
			}

			public void Next (ref UnsafeAppendBuffer.Reader reader, ref NativeArray<float4x4> matrixStack, ref NativeArray<Color32> colorStack, ref NativeArray<LineWidthData> lineWidthStack, ref int matrixStackSize, ref int colorStackSize, ref int lineWidthStackSize) {
				var fullCmd = reader.ReadNext<Command>();
				var cmd = fullCmd & (Command)0xFF;
				Color32 oldColor = default;

				if ((fullCmd & Command.PushColorInline) != 0) {
					oldColor = currentColor;
					currentColor = reader.ReadNext<Color32>();
				}

				switch (cmd) {
				case Command.PushColor:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
					if (colorStackSize >= colorStack.Length) throw new System.Exception("Too deeply nested PushColor calls");
#else
					if (colorStackSize >= colorStack.Length) colorStackSize--;
#endif
					colorStack[colorStackSize] = currentColor;
					colorStackSize++;
					currentColor = reader.ReadNext<Color32>();
					break;
				case Command.PopColor:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
					if (colorStackSize <= 0) throw new System.Exception("PushColor and PopColor are not matched");
#else
					if (colorStackSize <= 0) break;
#endif
					colorStackSize--;
					currentColor = colorStack[colorStackSize];
					break;
				case Command.PushMatrix:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
					if (matrixStackSize >= matrixStack.Length) throw new System.Exception("Too deeply nested PushMatrix calls");
#else
					if (matrixStackSize >= matrixStack.Length) matrixStackSize--;
#endif
					matrixStack[matrixStackSize] = currentMatrix;
					matrixStackSize++;
					currentMatrix = math.mul(currentMatrix, reader.ReadNext<float4x4>());
					break;
				case Command.PushSetMatrix:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
					if (matrixStackSize >= matrixStack.Length) throw new System.Exception("Too deeply nested PushMatrix calls");
#else
					if (matrixStackSize >= matrixStack.Length) matrixStackSize--;
#endif
					matrixStack[matrixStackSize] = currentMatrix;
					matrixStackSize++;
					currentMatrix = reader.ReadNext<float4x4>();
					break;
				case Command.PopMatrix:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
					if (matrixStackSize <= 0) throw new System.Exception("PushMatrix and PopMatrix are not matched");
#else
					if (matrixStackSize <= 0) break;
#endif
					matrixStackSize--;
					currentMatrix = matrixStack[matrixStackSize];
					break;
				case Command.PushLineWidth:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
					if (lineWidthStackSize >= lineWidthStack.Length) throw new System.Exception("Too deeply nested PushLineWidth calls");
#else
					if (lineWidthStackSize >= lineWidthStack.Length) lineWidthStackSize--;
#endif
					lineWidthStack[lineWidthStackSize] = currentLineWidthData;
					lineWidthStackSize++;
					currentLineWidthData = reader.ReadNext<LineWidthData>();
					break;
				case Command.PopLineWidth:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
					if (lineWidthStackSize <= 0) throw new System.Exception("PushLineWidth and PopLineWidth are not matched");
#else
					if (lineWidthStackSize <= 0) break;
#endif
					lineWidthStackSize--;
					currentLineWidthData = lineWidthStack[lineWidthStackSize];
					break;
				case Command.Line:
					AddLine(reader.ReadNext<LineData>());
					break;
				case Command.SphereOutline:
					AddSphereOutline(reader.ReadNext<SphereData>());
					break;
				case Command.CircleXZ:
					AddCircle(reader.ReadNext<CircleXZData>());
					break;
				case Command.Circle:
					AddCircle(reader.ReadNext<CircleData>());
					break;
				case Command.DiscXZ:
					AddDisc(reader.ReadNext<CircleXZData>());
					break;
				case Command.Disc:
					AddDisc(reader.ReadNext<CircleData>());
					break;
				case Command.Box:
					AddBox(reader.ReadNext<BoxData>());
					break;
				case Command.WirePlane:
					AddPlane(reader.ReadNext<PlaneData>());
					break;
				case Command.PushPersist:
					// This command does not need to be handled by the builder
					reader.ReadNext<PersistData>();
					break;
				case Command.PopPersist:
					// This command does not need to be handled by the builder
					break;
				case Command.Text:
					var data = reader.ReadNext<TextData>();
					unsafe {
						System.UInt16* ptr = (System.UInt16*)reader.ReadNext(UnsafeUtility.SizeOf<System.UInt16>() * data.numCharacters);
						AddText(ptr, data, currentColor);
					}
					break;
				case Command.Text3D:
					var data2 = reader.ReadNext<TextData3D>();
					unsafe {
						System.UInt16* ptr = (System.UInt16*)reader.ReadNext(UnsafeUtility.SizeOf<System.UInt16>() * data2.numCharacters);
						AddText3D(ptr, data2, currentColor);
					}
					break;
				case Command.CaptureState:
					unsafe {
						buffers->capturedState.Add(new ProcessedBuilderData.CapturedState {
							color = this.currentColor,
							matrix = this.currentMatrix,
						});
					}
					break;
				default:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
					throw new System.Exception("Unknown command");
#else
					break;
#endif
				}

				if ((fullCmd & Command.PushColorInline) != 0) {
					currentColor = oldColor;
				}
			}

			void CreateTriangles () {
				// Create triangles for all lines
				// A triangle consists of 3 indices
				// A line (4 vertices) consists of 2 triangles, so 6 triangle indices
				unsafe {
					var outlineVertices = &buffers->vertices;
					var outlineTriangles = &buffers->triangles;
					var vertexCount = outlineVertices->GetLength() / UnsafeUtility.SizeOf<Vertex>();
					// Each line is made out of 4 vertices
					var lineCount = vertexCount / 4;
					var trianglesSizeInBytes = lineCount * 6 * UnsafeUtility.SizeOf<int>();
					if (trianglesSizeInBytes >= outlineTriangles->Capacity) {
						outlineTriangles->SetCapacity(math.ceilpow2(trianglesSizeInBytes));
					}

					int* ptr = (int*)outlineTriangles->Ptr;
					for (int i = 0, vi = 0; i < lineCount; i++, vi += 4) {
						// First triangle
						*ptr++ = vi + 0;
						*ptr++ = vi + 1;
						*ptr++ = vi + 2;

						// Second triangle
						*ptr++ = vi + 1;
						*ptr++ = vi + 3;
						*ptr++ = vi + 2;
					}
					outlineTriangles->SetLength(trianglesSizeInBytes);
				}
			}

			public const int MaxStackSize = 32;

			public void Execute () {
				unsafe {
					buffers->vertices.Reset();
					buffers->triangles.Reset();
					buffers->solidVertices.Reset();
					buffers->solidTriangles.Reset();
					buffers->textVertices.Reset();
					buffers->textTriangles.Reset();
					buffers->capturedState.Reset();
				}

				minBounds = new float3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
				maxBounds = new float3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

				var matrixStack = new NativeArray<float4x4>(MaxStackSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
				var colorStack = new NativeArray<Color32>(MaxStackSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
				var lineWidthStack = new NativeArray<LineWidthData>(MaxStackSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
				int matrixStackSize = 0;
				int colorStackSize = 0;
				int lineWidthStackSize = 0;

				unsafe {
					var reader = buffers->splitterOutput.AsReader();
					while (reader.Offset < reader.Size) Next(ref reader, ref matrixStack, ref colorStack, ref lineWidthStack, ref matrixStackSize, ref colorStackSize, ref lineWidthStackSize);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
					if (reader.Offset != reader.Size) throw new Exception("Didn't reach the end of the buffer");
#endif
				}

				CreateTriangles();

				unsafe {
					var outBounds = &buffers->bounds;
					*outBounds = new Bounds((minBounds + maxBounds) * 0.5f, maxBounds - minBounds);

					if (math.any(math.isnan(outBounds->min)) && (buffers->vertices.GetLength() > 0 || buffers->solidTriangles.GetLength() > 0)) {
						// Fall back to a bounding box that covers everything
						*outBounds = new Bounds(Vector3.zero, new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity));
#if ENABLE_UNITY_COLLECTIONS_CHECKS
						throw new Exception("NaN bounds. A Draw.* command may have been given NaN coordinates.");
#endif
					}
				}
			}
		}
	}
}
