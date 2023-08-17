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
	}

	public override void OnToolGUI(EditorWindow window) {
		GUILayout.BeginArea(new Rect(10, 10, 170, 100));

		GunScript gun = (GunScript) target;

		if (GUILayout.Button("Populate GunPartMaterials list")) {
			SerializedObject serializedObject = new SerializedObject(target);

			serializedObject.FindProperty("gun_part_materials").ClearArray();

			foreach (GunPartMaterial material in gun.GetComponentsInChildren<GunPartMaterial>()) {
				serializedObject.FindProperty("gun_part_materials").InsertArrayElementAtIndex(0);
				serializedObject.FindProperty("gun_part_materials").GetArrayElementAtIndex(0).objectReferenceValue = material;
			}

			serializedObject.ApplyModifiedProperties();
		}

		if (GUILayout.Button("Turn On X-Ray")) {
			foreach (GunPartMaterial gunPartMaterial in gun.GetComponentsInChildren<GunPartMaterial>()) {
				if (gunPartMaterial && gunPartMaterial.TryGetComponent(out Renderer renderer)) {
					renderer.sharedMaterial = gunPartMaterial.xray_material;
				}
			}
		}
		if (GUILayout.Button("Turn Off X-Ray")) {
			foreach (GunPartMaterial gunPartMaterial in gun.GetComponentsInChildren<GunPartMaterial>()) {
				if (gunPartMaterial && gunPartMaterial.TryGetComponent(out Renderer renderer)) {
					renderer.sharedMaterial = gunPartMaterial.material;
				}
			}
		}

		GUILayout.EndArea();
	}

	public override GUIContent toolbarIcon {
		get { return guiContent; }
	}
}

#endif