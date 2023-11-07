using Drawing;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;
using Unity.Mathematics;

namespace Drawing.Examples {
	public class BurstExample : MonoBehaviour {
		// Use [BurstCompile] to allow Unity to compile the job using the Burst compiler
		[BurstCompile]
		struct DrawingJob : IJob {
			public float2 offset;
			// The job takes a command builder which we can use to draw things with
			public CommandBuilder builder;

			Color Colormap (float x) {
				// Simple color map that goes from black through red to yellow
				float r = math.clamp(8.0f / 3.0f * x, 0.0f, 1.0f);
				float g = math.clamp(8.0f / 3.0f * x - 1.0f, 0.0f, 1.0f);
				float b = math.clamp(4.0f * x - 3.0f, 0.0f, 1.0f);

				return new Color(r, g, b, 1.0f);
			}

			public void Execute (int index) {
				int x = index / 100;
				int z = index % 100;

				// Draw a solid box and a wire box
				// Use Perlin noise to generate a procedural height map
				var noise = Mathf.PerlinNoise(x * 0.05f + offset.x, z * 0.05f + offset.y);
				Bounds bounds = new Bounds(new float3(x, 0, z), new float3(1, 14 * noise, 1));

				//builder.WireBox(bounds, new Color(0, 0, 0, 0.2f));
				builder.SolidBox(bounds, Colormap(noise));
			}

			public void Execute () {
				for (int index = 0; index < 100 * 100; index++) {
					Execute(index);
				}
			}
		}

		public void Update () {
			var builder = DrawingManager.GetBuilder(true);

			// Create a new job struct and schedule it using the Unity Job System
			var job = new DrawingJob {
				builder = builder,
				offset = new float2(Time.time * 0.2f, Time.time * 0.2f),
			}.Schedule();
			// Dispose the builder after the job is complete
			builder.DisposeAfter(job);

			job.Complete();
		}
	}
}
