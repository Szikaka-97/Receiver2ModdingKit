using System.Collections;
using UnityEngine;
using UnityEditor;

public class CustomAssetBundleDirectory : EditorWindow {

	[MenuItem("Modding/Custom AssetBundle directories")]
	static void Init() {
		EditorWindow.GetWindow(typeof(CustomAssetBundleDirectory)).Show();	
	}

	public static AssetBundleDirectoryList directoryList {
		get {
			if (m_directoryList == null) {
				var lists = AssetDatabase.FindAssets("t:AssetBundleDirectoryList");
				if (lists.Length == 0) {
					Debug.LogError("No AssetBundle Lists found. Create a new AssetBundleDirectoryList from the context menu to enable automatic copying of assetbundles");
				}
				else {
					m_directoryList = AssetDatabase.LoadAssetAtPath<AssetBundleDirectoryList>(AssetDatabase.GUIDToAssetPath(lists[0]));
				}
			}

			return m_directoryList;
		}
	}

	private static AssetBundleDirectoryList m_directoryList;

	private void Awake() {
		var lists = AssetDatabase.FindAssets("t:AssetBundleDirectoryList");
		if (lists.Length == 0) {
			Debug.LogError("No AssetBundle Lists found. Create a new AssetBundleDirectoryList from the context menu to enable automatic copying of assetbundles");
		}
		else {
			m_directoryList = AssetDatabase.LoadAssetAtPath<AssetBundleDirectoryList>(AssetDatabase.GUIDToAssetPath(lists[0]));
		}
	}

	void OnGUI() {
		if (directoryList == null) {
			EditorGUILayout.LabelField("No AssetBundle Directory List present in the editor");

			return;
		}

		var scriptableObject = new SerializedObject(directoryList);

		if (scriptableObject.FindProperty("directoryTuples").arraySize == 0) {
			EditorGUILayout.LabelField("No AssetBundles set in the editor");

			return;
		}

		IEnumerator enumerator = scriptableObject.FindProperty("directoryTuples").GetEnumerator();
		while (enumerator.MoveNext()) {
			var tuple = enumerator.Current as SerializedProperty;

			EditorGUILayout.LabelField("Bundle: " + tuple.FindPropertyRelative("assetBundleName").stringValue);
			EditorGUILayout.PropertyField(tuple.FindPropertyRelative("path"), new GUIContent());
		}

		scriptableObject.ApplyModifiedProperties();
	}
}
