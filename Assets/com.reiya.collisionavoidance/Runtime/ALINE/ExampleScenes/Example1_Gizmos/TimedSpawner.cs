using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Drawing.Examples {
	public class TimedSpawner : MonoBehaviour {
		public float interval = 1;
		public float lifeTime = 5;
		public GameObject prefab;

		// Start is called before the first frame update
		IEnumerator Start () {
			while (true) {
				var go = GameObject.Instantiate(prefab, transform.position + Random.insideUnitSphere * 0.01f, Random.rotation);
				StartCoroutine(DestroyAfter(go, lifeTime));
				yield return new WaitForSeconds(interval);
			}
		}

		IEnumerator DestroyAfter (GameObject go, float delay) {
			yield return new WaitForSeconds(delay);
			GameObject.Destroy(go);
		}
	}
}
