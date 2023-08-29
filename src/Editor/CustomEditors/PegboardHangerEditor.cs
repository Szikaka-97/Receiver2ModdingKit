#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using Receiver2;

namespace Receiver2ModdingKit.Editor {
    [CustomEditor(typeof(PegboardHanger))]
    public class PegboardHangerEditor : UnityEditor.Editor{
        private BoxBoundsHandle box = new BoxBoundsHandle();
        private float scale = 1;

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("bounds"));

            GUILayout.Space(20);

            if (GUILayout.Button("From Renderers")) {
                Bounds new_bounds = new Bounds();

                GameObject hangable_item = (serializedObject.targetObject as PegboardHanger).gameObject;

                while (hangable_item.GetComponent<PegboardHangableItem>() == null) {
                    hangable_item = hangable_item.transform.parent.gameObject;
                }

                foreach (var renderer in hangable_item.GetComponentsInChildren<Renderer>()) {
                    new_bounds.Encapsulate(renderer.bounds);
                }

                new_bounds.size = new_bounds.size * scale;

                if (new_bounds.size.x > new_bounds.size.z) {
                    new_bounds.size = new Vector3(new_bounds.size.x, new_bounds.size.y, new_bounds.size.x);
                }
                else {
                    new_bounds.size = new Vector3(new_bounds.size.z, new_bounds.size.y, new_bounds.size.z);
                }

                serializedObject.FindProperty("bounds").boundsValue = new_bounds;
            }

            scale = EditorGUILayout.FloatField("Expansion:", scale);

            serializedObject.ApplyModifiedProperties();
        }

        protected void OnSceneGUI() {
            box.center = serializedObject.FindProperty("bounds").boundsValue.center;
            box.size = serializedObject.FindProperty("bounds").boundsValue.size;

            EditorGUI.BeginChangeCheck();

            box.DrawHandle();

            if (EditorGUI.EndChangeCheck()) {
                Bounds new_bounds = new Bounds(box.center, box.size);

                serializedObject.FindProperty("bounds").boundsValue = new_bounds;

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}

#endif