#if MODULE_RENDER_PIPELINES_UNIVERSAL
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Drawing {
	/// <summary>Custom Universal Render Pipeline Render Pass for ALINE</summary>
	public class AlineURPRenderPassFeature : ScriptableRendererFeature {
		/// <summary>Custom Universal Render Pipeline Render Pass for ALINE</summary>
		public class AlineURPRenderPass : ScriptableRenderPass {
			/// <summary>This method is called before executing the render pass</summary>
			public override void Configure (CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
			}

			public override void Execute (ScriptableRenderContext context, ref RenderingData renderingData) {
				DrawingManager.instance.ExecuteCustomRenderPass(context, renderingData.cameraData.camera);
			}

			public override void FrameCleanup (CommandBuffer cmd) {
			}
		}

		AlineURPRenderPass m_ScriptablePass;

		public override void Create () {
			m_ScriptablePass = new AlineURPRenderPass();

			// Configures where the render pass should be injected.
			// URP's post processing actually happens in BeforeRenderingPostProcessing, not after BeforeRenderingPostProcessing as one would expect.
			// Use BeforeRenderingPostProcessing-1 to ensure this pass gets executed before post processing effects.
			m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing-1;
		}

		/// <summary>This method is called when setting up the renderer once per-camera</summary>
		public override void AddRenderPasses (ScriptableRenderer renderer, ref RenderingData renderingData) {
			AddRenderPasses(renderer);
		}

		public void AddRenderPasses (ScriptableRenderer renderer) {
			renderer.EnqueuePass(m_ScriptablePass);
		}
	}
}
#endif
