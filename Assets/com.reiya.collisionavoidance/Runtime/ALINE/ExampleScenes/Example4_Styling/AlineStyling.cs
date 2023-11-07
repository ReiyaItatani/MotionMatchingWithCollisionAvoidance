using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Drawing;
using Unity.Mathematics;

namespace Drawing.Examples {
	/// <summary>Example that shows line widths, colors and line joins</summary>
	public class AlineStyling : MonoBehaviour {
		public Color gizmoColor = new Color(1.0f, 88/255f, 85/255f);
		public Color gizmoColor2 = new Color(79/255f, 204/255f, 237/255f);

		// Update is called once per frame
		void Update () {
			// Draw in-game.
			// This will draw the things even in standalone games
			var draw = Draw.ingame;

			using (draw.InScreenSpace(Camera.main)) {
				// Use a matrix to be able to draw in normalized space. I.e. (0,0) is the center of the screen, (0.5, 0.0) is the right side of the screen etc.
				using (draw.WithMatrix(Matrix4x4.TRS(new Vector3(Screen.width/2.0f, Screen.height/2.0f, 0), Quaternion.identity, new Vector3(Screen.width, Screen.width, 1)))) {
					for (int k = 0; k < 2; k++) {
						for (int i = 0; i < 4; i++) {
							// Draw with a few different line widths
							using (draw.WithLineWidth(i*i+1, true)) {
								float angle = Mathf.PI * 0.25f * (i+1) + Time.time * i;
								Vector3 offset = new Vector3(-0.3f + i * 0.2f, k * 0.2f - 0.1f);
								float radius = 0.075f;
								// Draw a rotating line
								draw.Line(offset + new Vector3(math.cos(angle)*radius, math.sin(angle)*radius, 0), offset, gizmoColor);
								// Draw a fixed line
								draw.Line(offset, offset + new Vector3(radius, 0, 0), gizmoColor);
								// Draw a circle
								draw.CircleXY(offset, radius, gizmoColor2);
							}
						}
					}
				}
			}
		}
	}
}
