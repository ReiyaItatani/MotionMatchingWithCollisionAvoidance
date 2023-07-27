using System.Collections.Generic;
using UnityEngine;
using Drawing;

namespace Drawing.Examples {
	/// <summary>Simple bezier curve editor</summary>
	public class CurveEditor : MonoBehaviour {
		List<CurvePoint> curves = new List<CurvePoint>();
		Camera cam;
		public Color curveColor;

		class CurvePoint {
			public Vector2 position, controlPoint0, controlPoint1;
		}

		void Awake () {
			cam = Camera.main;
		}

		void Update () {
			// Add a new control point when clicking
			if (Input.GetKeyDown(KeyCode.Mouse0)) {
				curves.Add(new CurvePoint {
					position = (Vector2)Input.mousePosition,
					controlPoint0 = Vector2.zero,
					controlPoint1 = Vector2.zero,
				});
			}

			// Keep adjusting the position of the control point while the mouse is pressed
			if (curves.Count > 0 && Input.GetKey(KeyCode.Mouse0) && ((Vector2)Input.mousePosition - curves[curves.Count - 1].position).magnitude > 2*2) {
				var point = curves[curves.Count - 1];
				point.controlPoint1 = (Vector2)Input.mousePosition - point.position;
				point.controlPoint0 = -point.controlPoint1;
			}

			Render();
		}

		void Render () {
			// Use a custom builder which renders even in standalone games
			// and in the editor even if gizmos are disabled.
			// Usually you would use the static Draw class instead.
			using (var draw = DrawingManager.GetBuilder(true)) {
				// Draw the curves in 2D using pixel coordinates
				using (draw.InScreenSpace(cam)) {
					// Draw a circle at each curve control point
					for (int i = 0; i < curves.Count; i++) {
						draw.CircleXY((Vector3)curves[i].position, 2, Color.blue);
					}

					// Draw each bezier curve segment
					for (int i = 0; i < curves.Count - 1; i++) {
						var p0 = curves[i].position;
						var p1 = p0 + curves[i].controlPoint1;
						var p3 = curves[i+1].position;
						var p2 = p3 + curves[i+1].controlPoint0;
						draw.Bezier((Vector3)p0, (Vector3)p1, (Vector3)p2, (Vector3)p3, curveColor);
					}
				}
			}
		}
	}
}
