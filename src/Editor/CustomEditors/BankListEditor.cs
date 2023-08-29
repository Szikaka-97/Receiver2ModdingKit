#if UNITY_EDITOR

using UnityEditor;
using FMOD.Studio;
using System.Collections.Generic;
using UnityEngine;

namespace Receiver2ModdingKit.Editor {
	[CustomEditor(typeof(BankList))]
	public class BankListEditor : UnityEditor.Editor {
		private FMOD.Studio.System studio_system;
		private List<Bank> loaded_banks = new List<Bank>();

		public void OnEnable() {
			Receiver2ModdingKit.CustomSounds.Utility.IsError(FMOD.Studio.System.create(out studio_system), "Create system");

			Receiver2ModdingKit.CustomSounds.Utility.IsError(studio_system.initialize(512, INITFLAGS.NORMAL, FMOD.INITFLAGS.NORMAL, System.IntPtr.Zero), "Init");
		}

		private bool BankListContains(UnityEngine.Object obj) {
			foreach (SerializedProperty field in serializedObject.FindProperty("banks")) {
				if (field.objectReferenceValue == obj) return true;
			}

			return false;
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();
			BankList bank_list = ((BankList) target);

			var banks_field = serializedObject.FindProperty("banks");

			using (new EditorGUILayout.HorizontalScope()) { 
				GUILayout.Label("Banks count: " + banks_field.arraySize);

				if (GUILayout.Button("+")) {
					banks_field.arraySize++;
					banks_field.GetArrayElementAtIndex(banks_field.arraySize - 1).objectReferenceValue = null;
				}
				if (GUILayout.Button("-") && banks_field.arraySize > 0) banks_field.arraySize--;
			}

			foreach (SerializedProperty bank_slot in banks_field) {
				var new_obj = EditorGUILayout.ObjectField(bank_slot.objectReferenceValue, typeof(DefaultAsset), false);

				if (new_obj && AssetDatabase.GetAssetPath(new_obj).EndsWith(".bank") && !BankListContains(new_obj)) bank_slot.objectReferenceValue = new_obj;
			}

			if (serializedObject.ApplyModifiedProperties()) {
				bank_list.ReloadBanks();

				loaded_banks.Clear();

				Receiver2ModdingKit.CustomSounds.Utility.IsError(studio_system.unloadAll(), "Unload");
			};

			if (loaded_banks.Count == 0 && bank_list.ready) {
				var bank_data_list = bank_list.GetBanks();

				foreach (var bank_data in bank_data_list) {
					Receiver2ModdingKit.CustomSounds.Utility.IsError(studio_system.loadBankMemory(bank_data, LOAD_BANK_FLAGS.NORMAL, out Bank bank), "Load Bank");

					loaded_banks.Add(bank);
				}
			}

			studio_system.update();

			List<string> event_paths = new List<string>();

			GUILayout.Label("Events:");

			foreach (var bank in loaded_banks) {
				bank.getEventList(out var event_desc_array);

				if (event_desc_array == null) continue;

				foreach (var event_desc in event_desc_array) {
					event_desc.getPath(out string event_path);

					event_paths.Add(event_path);
				}
			}

			EditorGUI.indentLevel++;
			foreach(string event_path in event_paths) { 
				EditorGUILayout.SelectableLabel(event_path, GUILayout.Height(20));
			}
			EditorGUI.indentLevel++;
		}

		public void OnDestroy() {
			studio_system.release();
		}
	}
}

#endif