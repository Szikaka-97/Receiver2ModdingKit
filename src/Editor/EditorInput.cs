#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace Receiver2ModdingKit {
	class EditorInput {
		public static bool GetKeyDown(KeyCode key) {
			return Input.GetKeyDown(key);
		}

		public static bool GetKeyUp(KeyCode key) {
			return Input.GetKeyUp(key);
		}

		public static bool GetKey(KeyCode key) {
			return Input.GetKey(key);
		}

		public static Vector3 GetMousePos() {
			return Input.mousePosition;
		}

		public static Ray MouseRay() {
			Camera cam = Camera.main;

			if (cam == null) {
				cam = GameObject.FindObjectOfType<Camera>();
			}

			if (cam == null) {
				return new Ray();
			}

			return cam.ScreenPointToRay(GetMousePos());
		}
	}
}
#else
using UnityEngine;

namespace Receiver2ModdingKit {
	class EditorInput {
		public static bool GetKeyDown(KeyCode key) { return false; }

		public static bool GetKeyUp(KeyCode key) { return false; }

		public static bool GetKey(KeyCode key) { return false; }

		public static Vector3 GetMousePos() { return Vector3.zero; }

		public static Ray MouseRay() { return new Ray(); }
	}
}
#endif