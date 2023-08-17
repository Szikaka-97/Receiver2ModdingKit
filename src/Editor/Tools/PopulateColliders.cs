#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using Receiver2;

[EditorTool("Populate Colliders list", typeof(InventoryItem))]
public class PopulateColliders : EditorTool {
	[SerializeField]
	Texture2D icon;

	GUIContent guiContent;

	void OnEnable() {
		guiContent = new GUIContent() {
			image = icon,
			text = "Populate Colliders",
			tooltip = "Use this tool to populate item's colliders list"
		};
	}

	public override void OnToolGUI(EditorWindow window) {
		GUILayout.BeginArea(new Rect(10, 10, 170, 100));

		if (GUILayout.Button("Populate Colliders")) {
			InventoryItem item = (InventoryItem) target;
			SerializedObject serializedObject = new SerializedObject(target);

			serializedObject.FindProperty("colliders").ClearArray();

			foreach (Collider col in item.GetComponentsInChildren<Collider>()) {
				serializedObject.FindProperty("colliders").InsertArrayElementAtIndex(0);
				serializedObject.FindProperty("colliders").GetArrayElementAtIndex(0).objectReferenceValue = col;

				if (!col.GetComponent<ItemColliderOwner>()) {
					ItemColliderOwner ico = col.gameObject.AddComponent<ItemColliderOwner>();
					ico.item_owner = item;
				}

				col.gameObject.layer = 8;
			}

			serializedObject.ApplyModifiedProperties();
		}

		GUILayout.EndArea();
	}

	public override GUIContent toolbarIcon {
		get { return guiContent; }
	}
}

#endif