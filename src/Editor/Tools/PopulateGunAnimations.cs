#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using Receiver2;
using System;
using UnityEngine;

namespace Receiver2ModdingKit.Editor.Tools {
	[EditorTool("Populate Gun Animations", typeof(GunScript))]
    public class PopulateGunAnimations : EditorTool {
        private float[] KeyframeArrayToFloatArray(AnimatedComponent.Keyframe[] keyframes) {
            List<float> float_array = new List<float>();

            foreach (var keyframe in keyframes) {
                float_array.Add(keyframe.time);
                float_array.Add(keyframe.value);
            }

            return float_array.ToArray();
        }

        private void Apply() {
            GunScript gun = target as GunScript;

            Dictionary<string, float[]> animations = new Dictionary<string, float[]>();
            List<AnimatedMover> mover_components = new List<AnimatedMover>();

            foreach (var anim_index in gun.gun_animations.indices) {
                float[] keyframes = new float[anim_index.length * 2];

                Array.Copy(gun.gun_animations.keyframes, anim_index.index, keyframes, 0, anim_index.length * 2);

                animations[anim_index.path] = keyframes;
            }

            mover_components.AddRange(gun.animated_components);

            foreach (var animated_component in gun.GetComponentsInChildren<AnimatedComponent>()) {
                string base_path = animated_component.path + " / ";

                if (animated_component.X_pos_keyframes.Length > 0) {
                    animations[base_path + "m_LocalPosition.x"] = KeyframeArrayToFloatArray(animated_component.X_pos_keyframes);
                }
                if (animated_component.Y_pos_keyframes.Length > 0) {
                    animations[base_path + "m_LocalPosition.y"] = KeyframeArrayToFloatArray(animated_component.Y_pos_keyframes);
                }
                if (animated_component.Z_pos_keyframes.Length > 0) {
                    animations[base_path + "m_LocalPosition.z"] = KeyframeArrayToFloatArray(animated_component.Z_pos_keyframes);
                }
                
                if (animated_component.X_rot_keyframes.Length > 0) {
                    animations[base_path + "localEulerAnglesRaw.x"] = KeyframeArrayToFloatArray(animated_component.X_rot_keyframes);
                }
                if (animated_component.Y_rot_keyframes.Length > 0) {
                    animations[base_path + "localEulerAnglesRaw.y"] = KeyframeArrayToFloatArray(animated_component.Y_rot_keyframes);
                }
                if (animated_component.Z_rot_keyframes.Length > 0) {
                    animations[base_path + "localEulerAnglesRaw.z"] = KeyframeArrayToFloatArray(animated_component.Z_rot_keyframes);
                }

                if (!string.IsNullOrEmpty(animated_component.mover_name)) {
                    bool flag = true;

                    foreach (var component in mover_components) {
                        if (component.anim_path == animated_component.path && component.mover_name == animated_component.mover_name && (component.component == animated_component.transform || component.component_name == animated_component.name)) {
                            flag = false;

                            break;
                        }
                    }

                    if (flag) {
                        mover_components.Add(new AnimatedMover() {
                            anim_path = animated_component.path,
                            mover_name = animated_component.mover_name,
                            component = animated_component.transform,
                        });
                    }
                }
            }

            int keyframes_index = 0;
            SerializedObject serialized_gun = new SerializedObject(gun);

            SerializedProperty gun_animations = serialized_gun.FindProperty("gun_animations");

            gun_animations.FindPropertyRelative("keyframes").arraySize = 0;
            gun_animations.FindPropertyRelative("indices").arraySize = 0;

            foreach (var anim_pair in animations) {
                int index = gun_animations.FindPropertyRelative("indices").arraySize++;

                gun_animations.FindPropertyRelative("indices").GetArrayElementAtIndex(index).FindPropertyRelative("path").stringValue = anim_pair.Key;
                gun_animations.FindPropertyRelative("indices").GetArrayElementAtIndex(index).FindPropertyRelative("index").intValue = keyframes_index;
                gun_animations.FindPropertyRelative("indices").GetArrayElementAtIndex(index).FindPropertyRelative("length").intValue = anim_pair.Value.Length / 2;

                gun_animations.FindPropertyRelative("keyframes").arraySize += anim_pair.Value.Length;

                foreach (float keyframe in anim_pair.Value) {
                    gun_animations.FindPropertyRelative("keyframes").GetArrayElementAtIndex(keyframes_index++).floatValue = keyframe;
                }

                serialized_gun.FindProperty("animated_components").arraySize = mover_components.Count;

                for (int i = 0; i < mover_components.Count; i++) {
                    SerializedProperty mover_property = serialized_gun.FindProperty("animated_components").GetArrayElementAtIndex(i);
                    AnimatedMover animated_mover = mover_components[i];

                    mover_property.FindPropertyRelative("anim_path").stringValue = animated_mover.anim_path;
                    mover_property.FindPropertyRelative("mover_name").stringValue = animated_mover.mover_name;
                    mover_property.FindPropertyRelative("component").objectReferenceValue = animated_mover.component;
                    mover_property.FindPropertyRelative("component_name").stringValue = animated_mover.component_name;
                }
            }

            serialized_gun.ApplyModifiedProperties();
        }

        public override void OnToolGUI(EditorWindow window) {
            GUILayout.BeginArea(new Rect(10, 10, 170, 100));

            if (GUILayout.Button("Populate Animations")) Apply();

            GUILayout.EndArea();
        }
    }
}

#endif