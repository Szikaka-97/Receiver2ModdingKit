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

	public override GUIContent toolbarIcon {
		get { return guiContent; }
	}

	public override void OnToolGUI(EditorWindow window) {
		InventoryItem item = (InventoryItem) target;

		item.colliders.Clear();

		foreach (Collider col in item.GetComponentsInChildren<Collider>()) {
			item.colliders.Add(col);

			if (!col.GetComponent<ItemColliderOwner>()) {
				ItemColliderOwner ico = col.gameObject.AddComponent<ItemColliderOwner>();
				ico.item_owner = item;
			}

			col.gameObject.layer = 8;
		}
	}
}
