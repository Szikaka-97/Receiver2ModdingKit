#if UNITY_EDITOR

using UnityEditor;
using FMOD.Studio;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

namespace Receiver2ModdingKit.Editor {
	[CustomEditor(typeof(BankList))]
	public class BankListEditor : UnityEditor.Editor {
		private FMOD.Studio.System studio_system;
		private List<Bank> loaded_banks = new List<Bank>();
		private List<string> event_paths = new List<string>();

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

			if (serializedObject.ApplyModifiedProperties() || GUILayout.Button("Reload Banks")) {
				bank_list.ReloadBanks();

				loaded_banks.Clear();

				Receiver2ModdingKit.CustomSounds.Utility.IsError(studio_system.unloadAll(), "Unload");
			};

			if (loaded_banks.Count == 0 && bank_list.ready) {
				var bank_data_list = bank_list.GetBanks();

				foreach (var bank_data in bank_data_list) {
					studio_system.getCoreSystem(out var core_system);

					string resonance_audio_path = null;

					foreach (var file in Directory.EnumerateFiles(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Packages", "Receiver2"), "*resonanceaudio*", SearchOption.AllDirectories)) {
						if (!file.EndsWith(".meta")) {
							resonance_audio_path = file;
						}
					}

					Receiver2ModdingKit.CustomSounds.Utility.IsError(core_system.loadPlugin(resonance_audio_path, out _), "Couldn't load resonance audio library, damn");

					Receiver2ModdingKit.CustomSounds.Utility.IsError(studio_system.loadBankMemory(bank_data, LOAD_BANK_FLAGS.NORMAL, out Bank bank), "Load Bank");

					loaded_banks.Add(bank);
				}
			}

			studio_system.update();

			event_paths = new List<string>();

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

			if (event_paths.Count > 0) {
				if (GUILayout.Button("Export paths to constants")) {
					var fullAssetPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), Path.GetDirectoryName(AssetDatabase.GetAssetPath(this.target.GetInstanceID())));

					//if null is returned, just use the current path, yeah
					var foundPath = SearchForAsmdefFileLocation(fullAssetPath) ?? fullAssetPath;

					using (var fs = new FileStream(Path.Combine(Path.GetDirectoryName(foundPath), "FMODConsts.cs"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite)) {
						using (var sw = new StreamWriter(fs)) {
							sw.AutoFlush = false;

							var namespaceName = Path.GetFileNameWithoutExtension(target.name);

							//identifiers in C# can't start with a digit, anywhere else is fine though
							if (char.IsDigit(target.name[0])) {
								namespaceName = "_" + namespaceName;
							}

							const string k_IdiotProoferRegex = @"[\\\-()`^)\]\[{}+=\-*¨$€£ù%µ?!:;, °""']";

							//if you manage to break this, not my fault
							namespaceName = Regex.Replace(namespaceName, k_IdiotProoferRegex, "");

							sw.WriteLine($"namespace {namespaceName}.FMODConsts");
							sw.WriteLine("{");
							sw.WriteLine($"\tpublic static class FMODConsts");
							sw.WriteLine("\t{");

							foreach (var path in event_paths) {
								var stringDeclaration = $"public const string {Regex.Replace(Regex.Replace(path.Substring("event:/".Length), k_IdiotProoferRegex, ""), @"[\/]", "_")}";

								sw.WriteLine($"\t\t{stringDeclaration} = \"{path}\";");
							}

							sw.WriteLine("\t}");
							sw.WriteLine("}");
							sw.Flush();
						}
					}
				}
			}
		}

		private string SearchForAsmdefFileLocation(string startLocation) {
			var dirInfo = new DirectoryInfo(startLocation);
			var files = dirInfo.GetFiles("*.asmdef", SearchOption.AllDirectories);

			if (files != null && files.Length > 0) {
				return files[0].FullName;
			}

			//don't want to go outside the Unity project path
			if (startLocation == Application.dataPath)
				return null;

			return SearchForAsmdefFileLocation(dirInfo.Parent.FullName);
		}

		public void OnDestroy() {
			studio_system.release();
		}

		private void OnValidate() {
			studio_system.release();
			studio_system.unloadAll();

			foreach (var bank in loaded_banks) {
				bank.unload();
			}
		}
	}
}

#endif