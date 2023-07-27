#if MODULE_RENDER_PIPELINES_HIGH_DEFINITION
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace Drawing {
	/// <summary>Custom High Definition Render Pipeline Render Pass for ALINE</summary>
	class AlineHDRPCustomPass : CustomPass {
		protected override void Setup (ScriptableRenderContext renderContext, CommandBuffer cmd) {
		}

#if MODULE_RENDER_PIPELINES_HIGH_DEFINITION_9_0_OR_NEWER
		protected override void Execute (CustomPassContext context) {
			UnityEngine.Profiling.Profiler.BeginSample("ALINE");
			DrawingManager.instance.SubmitFrame(context.hdCamera.camera, context.cmd, true);
			UnityEngine.Profiling.Profiler.EndSample();
		}
#else
		protected override void Execute (ScriptableRenderContext context, CommandBuffer cmd, HDCamera camera, CullingResults cullingResult) {
			UnityEngine.Profiling.Profiler.BeginSample("ALINE");
			DrawingManager.instance.SubmitFrame(camera.camera, cmd, true);
			UnityEngine.Profiling.Profiler.EndSample();
		}
#endif

		protected override void Cleanup () {
		}
	}
}
#endif
