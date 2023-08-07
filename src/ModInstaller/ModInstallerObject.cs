using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImGuiNET;
using UnityEngine;
using SimpleJSON;
using Receiver2;

namespace Receiver2ModdingKit.ModInstaller {
	internal class ModInstallerObject : MonoBehaviour {
		internal class ModDirectoryInfo {
			public DirectoryInfo directory {
				get;
				private set;
			}

			public ModInfoAsset mod_info {
				get;
				private set;
			}

			public ModDirectoryInfo(DirectoryInfo directory, ModInfoAsset mod_info) {
				this.directory = directory;
				this.mod_info = mod_info;
			}
		}

		internal enum InstallerState {
			PickingDirectory,
			ReadyToInstall,
			InstallingMod,
			Finishing,
		}

		private bool displaying_prompt = false;
		private string prompt_message = "";
		private bool? prompt_result = null;

		private bool successful = true;

		private InstallerState current_state;

		private DirectoryInfo current_path;

		private ModDirectoryInfo mod_dir_info;
		private bool plugins_loaded;

		private void CopyDirectory(string from, string to) {
			DirectoryInfo dir = new DirectoryInfo(from);

			if (!dir.Exists) return;

			try {
				string to_dir = Path.Combine(to, dir.Name);

				if (!Directory.Exists(to_dir)) Directory.CreateDirectory(to_dir);

				foreach (var file in dir.GetFiles()) {
					file.CopyTo(Path.Combine(to_dir, file.Name), true);
				}

				foreach (var child in dir.GetDirectories()) {
					CopyDirectory(child.FullName, to_dir);
				}
			} catch (Exception e) {
				Debug.LogError(e);

				return;
			}
		}

		private void ShowPickDirectoryMenu() {
			if (ImGui.Begin("Path selector", ImGuiWindowFlags.NoCollapse)) {
				if (current_path == null) {
					ImGui.Text("Current path: ");

					int maxLength = Mathf.Min(20, Directory.GetLogicalDrives().Max(drive => drive.Length));

					ImGui.Separator();

					foreach (var drive in Directory.GetLogicalDrives()) {
						if (ImGui.Button("|   \uf0a0   " + drive.PadRight(maxLength))) {
							current_path = new DirectoryInfo(drive);
						}
					}
				}
				else {
					if (current_path.GetFiles("modinfo.json").Count() > 0) {
						var modinfo = ModInfoAsset.FromJSONFile(JSON.Parse(File.ReadAllText(current_path.FullName + "/modinfo.json")), current_path.Name);

						if (modinfo != null && (modinfo.OperatingSystem == OperatingSystemFamily.Other || modinfo.OperatingSystem == SystemInfo.operatingSystemFamily)) {
							ImGui.Text("Located mod: " + modinfo.ModName + ". Do you want to install it?");

							if (ImGui.Button("Install")) {
								this.mod_dir_info = new ModDirectoryInfo(this.current_path, modinfo);

								current_state = InstallerState.ReadyToInstall;
							}

							ImGui.Separator();
						}
					}

					ImGui.Text("Current path: " + current_path.FullName);

					ImGui.Separator();

					if (ImGui.Button("<--")) {
						if (current_path.Parent == null) current_path = null;
						else current_path = current_path.Parent;

						ImGui.End();
						return;
					}

					if (current_path.GetDirectories().Length > 0) {
						int max_length = Mathf.Min(50, current_path.GetDirectories().Max(dir => dir.Name.Length));

						foreach (var dir in current_path.GetDirectories()) {

							bool locked = false;
							bool folder_contains_json = false;

							try {
								folder_contains_json = dir.GetFiles("modinfo.json").Count() > 0;
							} catch (UnauthorizedAccessException) {
								locked = true;
								folder_contains_json = false;
							}

							if (
								ImGui.Button(
									"|" +
									(locked ? "\uf023 " : "   ") +
									"\uf07b" + 
									(folder_contains_json ? " \uf005 " : "   ") + 
									dir.Name.PadRight(max_length)
								) && !locked
							) {
								current_path = dir;
							}
						}
					}
					else {
						ImGui.Text("No subdirectories present");
					}
				}

				ImGui.Separator();

				if (ImGui.Button("Cancel")) {
					//Time.timeScale = original_time_scale;

					successful = false;
					current_state = InstallerState.Finishing;
				}

				ImGui.End();
			}
		}

		private void InstallMod() {
			if (this.mod_dir_info == null || this.mod_dir_info.directory == null || this.mod_dir_info.mod_info == null) {
				Debug.LogError("Something went wrong, aborting installation");
				
				successful = false;
				current_state = InstallerState.Finishing;

				return;
			}

			var modinfo = mod_dir_info.mod_info;

			if (modinfo.ModdingKitVersion != ModdingKitCorePlugin.instance.Info.Metadata.Version.ToString()) {
				if (prompt_result != null) {
					if (prompt_result == true) modinfo.ModdingKitVersion = ModdingKitCorePlugin.instance.Info.Metadata.Version.ToString();
					else {
						current_state = InstallerState.Finishing;
						return;
					}
				}
				else {
					displaying_prompt = true;
					prompt_message = "It seems your mod was created using a different version of the Modding Kit, do you want to install it anyway?";
					return;
				}

				prompt_result = null;
			}

			if (modinfo.GameVersion != ReceiverCoreScript.Instance().build_info.version) {
				if (prompt_result != null) {
					if (prompt_result == true) modinfo.GameVersion = ReceiverCoreScript.Instance().build_info.version;
					else {
						current_state = InstallerState.Finishing;
						return;
					}
				}
				else {
					displaying_prompt = true;
					prompt_message = "It seems your mod was created for a different version of the game, do you want to install it anyway?";
					return;
				}
			}

			if (modinfo.PluginFolderPath != null) {
				string sourcePath = mod_dir_info.directory.FullName + "/" + modinfo.PluginFolderPath;

				if (Directory.Exists(sourcePath)) {
					try {
						var plugin_files = new DirectoryInfo(sourcePath).GetFiles("*.dll", SearchOption.AllDirectories);

						if (Directory.Exists(Path.Combine(BepInEx.Paths.PluginPath, new DirectoryInfo(sourcePath).Name))) Directory.Delete(Path.Combine(BepInEx.Paths.PluginPath, new DirectoryInfo(sourcePath).Name));
						CopyDirectory(sourcePath, BepInEx.Paths.PluginPath);

						/*
						if (!Directory.Exists(ScriptEngine.ScriptDirectory)) Directory.CreateDirectory(ScriptEngine.ScriptDirectory);

						foreach (var file in plugin_files) {
							file.CopyTo(BepInEx.Paths.PluginPath + "/" + modinfo.ModName + "/" + file.Name, true);
							file.CopyTo(ScriptEngine.ScriptDirectory + "/" + file.Name, true);
						}
						ScriptEngine.ReloadPlugins();
						*/
					} catch (IOException e) {
						Debug.LogError("Couldn't copy plugin files for mod: " + modinfo.ModName);
						Debug.LogError("Error:\n" + e.Message);
						try {
							if (Directory.Exists(Path.Combine(BepInEx.Paths.PluginPath, new DirectoryInfo(sourcePath).Name))) Directory.Delete(Path.Combine(BepInEx.Paths.PluginPath, new DirectoryInfo(sourcePath).Name));
						} catch { }
					} catch (UnauthorizedAccessException e) {
						Debug.LogError("Couldn't copy plugin files for mod: " + modinfo.ModName);
						Debug.LogError("Error:\n" + e.Message);
						try {
							if (Directory.Exists(Path.Combine(BepInEx.Paths.PluginPath, new DirectoryInfo(sourcePath).Name))) Directory.Delete(Path.Combine(BepInEx.Paths.PluginPath, new DirectoryInfo(sourcePath).Name));
						} catch { }
					}
				}
			}

			if (modinfo.AssetBundleFolderPath != null) {
				string sourcePath = mod_dir_info.directory.FullName + "/" + modinfo.AssetBundleFolderPath;

				if (Directory.Exists(sourcePath)) {
					try {
						var asset_files = new DirectoryInfo(sourcePath).GetFiles("*." + SystemInfo.operatingSystemFamily.ToString().ToLower());
						var campaign_folders = new DirectoryInfo(sourcePath).GetDirectories();

						if (Directory.Exists(Application.persistentDataPath + "/Guns/" + new DirectoryInfo(sourcePath).Name)) Directory.Delete(Application.persistentDataPath + "/Guns/" + new DirectoryInfo(sourcePath).Name, true);

						CopyDirectory(sourcePath, Application.persistentDataPath + "/Guns/");

						//Code from now on does nothing, but I'm keeping it here in case I fix the underlying issue
						int previous_guns_count = ((List<GameObject>)ReflectionManager.RCS_gun_prefabs_all.GetValue(ReceiverCoreScript.Instance())).Count;

						ReceiverCoreScript.Instance().LoadModGun(Application.persistentDataPath + "/Guns/" + new DirectoryInfo(sourcePath).Name, true);

						var new_guns = (List<GameObject>) ReflectionManager.RCS_gun_prefabs_all.GetValue(ReceiverCoreScript.Instance());

						if (previous_guns_count < new_guns.Count && new_guns.Last().GetComponent<GunScript>() is ModGunScript) {
							ModLoader.LoadGun(new_guns.Last().GetComponent<ModGunScript>());
						}
					} catch (IOException e) {
						Debug.LogError("Couldn't copy assets for mod: " + modinfo.ModName);
						Debug.LogError("Error:\n" + e.Message);
						try {
							if (Directory.Exists(Application.persistentDataPath + "/Guns/" + new DirectoryInfo(sourcePath).Name)) Directory.Delete(Application.persistentDataPath + "/Guns/" + new DirectoryInfo(sourcePath).Name, true);
						} catch { }
					} catch (UnauthorizedAccessException e) {
						Debug.LogError("Couldn't copy assets for mod: " + modinfo.ModName);
						Debug.LogError("Error:\n" + e.Message);
						try {
							if (Directory.Exists(Application.persistentDataPath + "/Guns/" + new DirectoryInfo(sourcePath).Name)) Directory.Delete(Application.persistentDataPath + "/Guns/" + new DirectoryInfo(sourcePath).Name, true);
						} catch { }
					}
				}
			}

			current_state = InstallerState.Finishing;
		}

		private void DisplayPrompt() {
			if (ImGui.Begin("Installer", ImGuiWindowFlags.NoCollapse)) {
				
				if (!String.IsNullOrEmpty(prompt_message)) {
					ImGui.TextWrapped(prompt_message);
				}

				ImGui.Spacing();

				if (ImGui.Button("   Yes   ")) {
					prompt_result = true;

					displaying_prompt = false;
				}

				ImGui.SameLine(0, 20);

				if (ImGui.Button("   No    ")) {
					prompt_result = false;

					displaying_prompt = false;
				}

				ImGui.End();
			}
		}

		private void Finish() {
			if (ImGui.Begin("Installer", ImGuiWindowFlags.NoCollapse)) {
				if (successful) {
					ImGui.Text("Installation finished");
					ImGui.Text("Restart the game to load installed addons");
				}
				else {
					ImGui.Text("Installation aborted");
				}

				ImGui.Spacing();

				if (plugins_loaded) {
					ImGui.Text("There are plugins waiting to be loaded, do you want to load them now?");

					if (ImGui.Button("Yes")) {
						
						plugins_loaded = false;
					}

					if (ImGui.Button("No")) {
						plugins_loaded = false;
					}
				}

				if (ImGui.Button("OK")) {
					ModLoader.mod_installer = null;

					Destroy(this);
				}

				ImGui.End();
			}
		}

		private void Awake() {
			//original_time_scale = Time.timeScale;
		}

		private void Update() {
			//Time.timeScale = 0;

			if (displaying_prompt) {
				DisplayPrompt();

				return;
			}

			switch (current_state) {
				case InstallerState.PickingDirectory:
					ShowPickDirectoryMenu();
					break;
				case InstallerState.ReadyToInstall:
					InstallMod();
					break;
				case InstallerState.Finishing:
					Finish();
					break;
			}

			prompt_result = null;
		}
	}
}
