using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using Receiver2;

[EditorTool("Populate Gun Part Materials list", typeof(GunScript))]
public class PopulateGunPartMaterials : EditorTool {
	[SerializeField]
	Texture2D m_icon;

	GUIContent guiContent;

	void OnEnable() {
		guiContent = new GUIContent() {
			image = m_icon,
			text = "Populate Gun Part Materials",
			tooltip = "Use this tool to populate gun's Gun Part Materials list"
		};
	}

	public override GUIContent toolbarIcon {
		get { return guiContent; }
	}

	public override void OnToolGUI(EditorWindow window) {
		GunScript gun = (GunScript) target;

		gun.gun_part_materials = gun.GetComponentsInChildren<GunPartMaterial>();
	}
}
