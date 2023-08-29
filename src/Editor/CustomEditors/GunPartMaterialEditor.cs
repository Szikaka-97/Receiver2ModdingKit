#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace Receiver2ModdingKit.Editor {
    [CustomEditor(typeof(GunPartMaterial))]
    public class GunPartMaterialEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            serializedObject.Update();
            var gun_part = (target as Component).gameObject;

            if (gun_part.TryGetComponent<Renderer>(out var renderer)) {
                if (renderer.sharedMaterials.Length > 1) {
                    EditorGUILayout.LabelField("Only the first material in the list will change!", new GUIStyle(GUI.skin.label) { normal = new GUIStyleState() { textColor = Color.red } });
                }

                Material mat = renderer.sharedMaterial;

                if (GUILayout.Button("From Renderer")) {
                    if (mat.GetTag("RenderType", false).IndexOf("Transparent") >= 0) {
                        serializedObject.FindProperty("xray_material").objectReferenceValue = mat;
                    }
                    else {
                        serializedObject.FindProperty("material").objectReferenceValue = mat;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();

            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif