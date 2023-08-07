#if UNITY_EDITOR

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

		GunScript gun = (GunScript) target;
		SerializedObject serializedObject = new SerializedObject(target);

		serializedObject.FindProperty("gun_part_materials").ClearArray();

		foreach (GunPartMaterial material in gun.GetComponentsInChildren<GunPartMaterial>()) {
			serializedObject.FindProperty("gun_part_materials").InsertArrayElementAtIndex(0);
			serializedObject.FindProperty("gun_part_materials").GetArrayElementAtIndex(0).objectReferenceValue = material;
		}

		serializedObject.ApplyModifiedProperties();
	}

	public override GUIContent toolbarIcon {
		get { return guiContent; }
	}
}

#endif