#if UNITY_EDITOR
using System.Collections.Generic;
using Receiver2;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Receiver2ModdingKit.Editor {
	[CustomEditor(typeof(RoomDividerScript), true)]
	public class TileNodeEditor : UnityEditor.Editor {
		void OnSceneGUI() {
			var roomDivider = target as RoomDividerScript;

			var roomRoot = roomDivider.room_data.root;

			var doorIndex = 0;
			foreach (var door in roomDivider.room_data.GetDoors(roomDivider.transform)) {
				Handles.Label(door, door + " " + doorIndex++);
				Handles.SphereHandleCap(0, door, Quaternion.identity, 0.25f, EventType.Repaint);
			}

			if (roomDivider.show_room_gizmos) {
				var rd = roomRoot.GetAABB(Vector3.zero, 0);

				var roomNames = new List<string>();

				foreach (var room in roomRoot.GetLeafs(roomDivider.transform.position)) {
					roomNames.Add(room.name);
				}

				var linesList = new List<Vector3>();

				var minZ = roomDivider.transform.root.position.z + (roomRoot.dimensions.z + roomRoot.offset.z) - 0.01f;
				var maxZ = roomDivider.transform.root.position.z + roomRoot.offset.z + 0.01f;

				var leaves = roomDivider.leafs_only ? roomRoot.GetLeafs(roomDivider.transform.root.position) : roomRoot.GetNodes(roomDivider.transform.root.position);

				var bounds = rd.Value.GetBounds();

				for (int i = leaves.Length - 1; i > 0; i--) {
					var currentLeaf = leaves[i];

					var outList = new List<Vector3>();

					RoomDividerHelpers.DrawRoom(currentLeaf, Matrix4x4.identity, roomDivider.show_external_see_ns, roomDivider.show_external_see_ew, outList);

					linesList.AddRange(outList);
				}

				var editorCamera = SceneView.currentDrawingSceneView.camera;

				using (new Handles.DrawingScope()) {
					var mousePos = Event.current.mousePosition;

					mousePos.y = Mathf.Lerp(editorCamera.pixelHeight, 0, mousePos.y / editorCamera.pixelHeight);

					var ray = editorCamera.ScreenPointToRay(mousePos);

					//var colours = new Color[] { Color.black, Color.blue, Color.yellow, Color.green, Color.white, Color.red, Color.cyan, Color.magenta };

					for (int k = 0; k < linesList.Count;) {
						//var colour = colours[((k % 24) * (k / 24)) % colours.Length];

						Vector3 lineStart = linesList[k];
						Vector3 lineEnd = linesList[k + 1];

						if ((lineStart.z >= maxZ || lineEnd.z >= maxZ) || (lineStart.z <= minZ || lineEnd.z <= minZ)) {
							//root has 24 points
							Color colour = k <= 24 ? Color.yellow : Color.white;

							colour.a = 1f - 0.6f * Mathf.InverseLerp(0f, 20f, Mathf.Clamp(Vector3.Distance(editorCamera.transform.position, (lineStart + lineEnd) * 0.5f), 0f, 20f));
							colour.a *= colour.a;

							if (RayIntersectsLine(ray, lineStart, lineEnd, out var intersect)) {
								colour = Color.green;
							}

							Handles.color = colour;

							Handles.DrawLine(lineStart, lineEnd);
						}

						k += 2;
					}
				}
			}
		}

		public override void OnInspectorGUI() {
			var roomDivider = target as RoomDividerScript;

			var property = serializedObject.GetIterator();

			if (property.NextVisible(true)) {
				while (property.NextVisible(false)) {
					if (property.name != nameof(TileRoomData.root) & property.name != nameof(TileRoomData.room_contents)) {
						EditorGUILayout.PropertyField(property);
					}
				}
			}

			if (GUILayout.Button("Apply Room Data")) {
				if (roomDivider.transform.root.TryGetComponent<TileScript>(out var tileScript)) {
					tileScript.room_data = roomDivider.room_data;
				} else {
					Debug.LogError("Your prefab's root should have a TileScript component!!!");
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		//code from https://www.codefull.net/2015/06/intersection-of-a-ray-and-a-line-segment-in-3d/ && https://stackoverflow.com/a/33825948
		//I don't understand any of this, but cool
		static bool RayIntersectsLine(Ray ray, Vector3 lineStart, Vector3 lineEnd, out Vector3 intersection) {
			//we need an unnormalised direction for some cool epic math reason I guess
			Vector3 da = ray.direction * 1000f;
			Vector3 db = lineEnd - lineStart;
			Vector3 dc = lineStart - ray.origin;

			intersection = Vector3.zero;

			//how close do we have to be to the actual line for it to count as being intersected
			const float k_CoPlanerThreshold = 2000f;

			var shit = Mathf.Abs(Vector3.Dot(dc, Vector3.Cross(da, db)));

			if (shit >= k_CoPlanerThreshold)
				return false;

			//this one's for if it's on the line
			float s = Vector3.Dot(Vector3.Cross(dc, db), Vector3.Cross(da, db)) / Vector3.Cross(da, db).sqrMagnitude;

			//this one's for if it's outside the line's extents
			float t = Vector3.Dot(Vector3.Cross(dc, da), Vector3.Cross(da, db)) / Vector3.Cross(da, db).sqrMagnitude;

			if ((s >= .0f && s <= 1.0f) && (t >= .0f && t <= 1.0f)) {
				//see if this lies on the segment
				if ((intersection - lineStart).sqrMagnitude + (intersection - lineEnd).sqrMagnitude <= (lineStart.sqrMagnitude + lineEnd.sqrMagnitude)) {
					intersection = ray.origin + s * da;

					return true;
				}
			}

			return false;
		}
	}
}

#endif