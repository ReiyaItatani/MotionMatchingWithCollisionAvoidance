using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Drawing;

namespace Drawing.Examples {
	public class GizmoCharacterExample : MonoBehaviourGizmos {
		public Color gizmoColor = new Color(1.0f, 88/255f, 85/255f);
		public Color gizmoColor2 = new Color(79/255f, 204/255f, 237/255f);

		public float movementNoiseScale = 0.2f;
		public float startPointAttractionStrength = 0.05f;
		public int futurePathPlotSteps = 100;
		public int plotStartStep = 10;
		public int plotEveryNSteps = 10;

		float seed;
		Vector3 startPosition;
		void Start () {
			seed = Random.value * 1000;
			startPosition = transform.position;
		}

		Vector3 GetSmoothRandomVelocity (float time, Vector3 position) {
			// Use perlin noise to get a smoothly varying vector
			float t = time * movementNoiseScale + seed;
			var dx = 2*Mathf.PerlinNoise(t, t + 5341.23145f) - 1;
			var dy = 2*Mathf.PerlinNoise(t + 92.9842f, -t + 231.85145f) - 1;
			var velocity = new Vector3(dx, 0, dy);

			// Make a weak attractor to the start position of the agent. To make sure the agent doesn't move too far out of view
			velocity += (startPosition - position) * startPointAttractionStrength;
			velocity.y = 0;
			return velocity;
		}

		void PlotFuturePath (float time, Vector3 position) {
			float dt = 0.05f;

			for (int i = 0; i < futurePathPlotSteps; i++) {
				var v = GetSmoothRandomVelocity(time + i*dt, position);

				var idx = i - plotStartStep;
				if (idx >= 0 && idx % plotEveryNSteps == 0) {
					Draw.Arrowhead(position, v, 0.1f, gizmoColor);
				}
				position += v.normalized * dt;
			}
		}

		// Update is called once per frame
		void Update () {
			PlotFuturePath(Time.time, transform.position);
			Vector3 velocity = GetSmoothRandomVelocity(Time.time, transform.position);
			transform.rotation = Quaternion.LookRotation(velocity);
			transform.position += transform.forward * Time.deltaTime;
		}

		public override void DrawGizmos () {
			using (Draw.InLocalSpace(transform)) {
				Draw.WireCylinder(Vector3.zero, Vector3.up, 2, 0.5f, gizmoColor);
				Draw.ArrowheadArc(Vector3.zero, Vector3.forward, 0.55f, gizmoColor);
				Draw.Label2D(Vector3.zero, gameObject.name, 14, LabelAlignment.TopCenter.withPixelOffset(0, -20), gizmoColor2);
			}
		}
	}
}
