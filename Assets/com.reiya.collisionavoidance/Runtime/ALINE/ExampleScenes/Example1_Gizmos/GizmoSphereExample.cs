using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Drawing;
using System.Linq;

namespace Drawing.Examples {
	public class GizmoSphereExample : MonoBehaviourGizmos {
		public Color gizmoColor = new Color(1.0f, 88/255f, 85/255f);
		public Color gizmoColor2 = new Color(79/255f, 204/255f, 237/255f);

		public override void DrawGizmos () {
			using (Draw.InLocalSpace(transform)) {
				Draw.WireSphere(Vector3.zero, 0.5f, gizmoColor);

				foreach (var contact in contactForces.Values) {
					Draw.Circle(contact.lastPoint, contact.lastNormal, 0.1f * contact.impulse, gizmoColor2);
					Draw.SolidCircle(contact.lastPoint, contact.lastNormal, 0.1f * contact.impulse, gizmoColor2);
				}
			}
		}

		void FixedUpdate () {
			foreach (var collider in contactForces.Keys.ToList()) {
				var c = contactForces[collider];
				if (c.impulse > 0.1f) {
					c.impulse = Mathf.Lerp(c.impulse, 0, 10 * Time.fixedDeltaTime);
					c.smoothImpulse = Mathf.Lerp(c.impulse, c.smoothImpulse, 20 * Time.fixedDeltaTime);
					contactForces[collider] = c;
				} else {
					contactForces.Remove(collider);
				}
			}
		}

		struct Contact {
			public float impulse;
			public float smoothImpulse;
			public Vector3 lastPoint;
			public Vector3 lastNormal;
		}
		Dictionary<Collider, Contact> contactForces = new Dictionary<Collider, Contact>();

		void OnCollisionStay (Collision collision) {
			foreach (ContactPoint contact in collision.contacts) {
				if (!contactForces.ContainsKey(collision.collider)) {
					contactForces.Add(collision.collider, new Contact { impulse = 2f });
				}

				var c = contactForces[collision.collider];
				c.impulse = Mathf.Max(c.impulse, 1);
				c.lastPoint = transform.InverseTransformPoint(contact.point);
				c.lastNormal = transform.InverseTransformVector(contact.normal);
				contactForces[collision.collider] = c;
				break;
			}
		}
	}
}
