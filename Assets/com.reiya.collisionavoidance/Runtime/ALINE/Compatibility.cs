using Unity.Collections.LowLevel.Unsafe;

namespace Drawing {
	/// <summary>Compatibility code for handling different versions of the Unity.Collections package</summary>
	static class UnsafeAppendBufferCompatibility {
		public static int GetLength (this ref UnsafeAppendBuffer buffer) {
#if MODULE_COLLECTIONS_0_6_0_OR_NEWER
			return buffer.Length;
#else
			return buffer.Size;
#endif
		}

		public static void SetLength (this ref UnsafeAppendBuffer buffer, int value) {
#if MODULE_COLLECTIONS_0_6_0_OR_NEWER
			buffer.Length = value;
#else
			buffer.Size = value;
#endif
		}
	}
}
