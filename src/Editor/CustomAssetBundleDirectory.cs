using UnityEngine;
using UnityEditor;

public class CustomAssetBundleDirectory : EditorWindow {

	[MenuItem("Modding/Custom AssetBundle directories")]
	static void Init() {
		EditorWindow.GetWindow(typeof(CustomAssetBundleDirectory)).Show();
	}

	public static AssetBundleDirectoryList directory_list {
		get;
		private set;
	}

	private static bool TryFindDirectoryList() {
		var lists = AssetDatabase.FindAssets("t:AssetBundleDirectoryList");
		if (lists.Length == 0) {
			return false;
		}
		else {
			directory_list = AssetDatabase.LoadAssetAtPath<AssetBundleDirectoryList>(AssetDatabase.GUIDToAssetPath(lists[0]));
			return true;
		}
	}

	public static void Refresh() {
		TryFindDirectoryList();

		if (directory_list) directory_list.Refresh();
	}

	void OnFocus() {
		Refresh();
	}

	void OnGUI() {
		if (directory_list == null) {
			EditorGUILayout.LabelField("No AssetBundle Directory List present in the editor");

			return;
		}

		var dir_list = new SerializedObject(directory_list);

		dir_list.Update();

		var tuple_list = dir_list.FindProperty("directory_tuples");

		if (tuple_list.arraySize == 0) {
			EditorGUILayout.LabelField("No AssetBundles present in the editor");

			return;
		}

		//EditorGUILayout.LabelField()

		foreach (SerializedProperty tuple in tuple_list) { 
			EditorGUILayout.LabelField(new GUIContent("Bundle:"), new GUIContent(tuple.FindPropertyRelative("asset_bundle_name").stringValue));
			EditorGUILayout.PropertyField(tuple.FindPropertyRelative("path"), new GUIContent("Path:"));
			EditorGUILayout.Separator();
		}

		dir_list.ApplyModifiedProperties();
	}
}
