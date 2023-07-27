using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Drawing {
	/// <summary>
	/// Inherit from this class to draw gizmos.
	/// See: getstarted (view in online documentation for working links)
	/// </summary>
	public abstract class MonoBehaviourGizmos : MonoBehaviour, IDrawGizmos {
		public MonoBehaviourGizmos() {
#if UNITY_EDITOR
			DrawingManager.Register(this);
#endif
		}

		/// <summary>
		/// An empty OnDrawGizmosSelected method.
		/// Why an empty OnDrawGizmosSelected method?
		/// This is because only objects with an OnDrawGizmos/OnDrawGizmosSelected method will show up in Unity's menu for enabling/disabling
		/// the gizmos per object type (upper right corner of the scene view). So we need it here even though we don't use normal gizmos.
		///
		/// By using OnDrawGizmosSelected instead of OnDrawGizmos we minimize the overhead of Unity calling this empty method.
		/// </summary>
		void OnDrawGizmosSelected () {
		}

		public virtual void DrawGizmos () {
		}
	}
}
