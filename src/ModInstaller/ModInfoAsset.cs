using ImGuiNET;
using Receiver2;
using SimpleJSON;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Receiver2ModdingKit.ModInstaller {
	public class ModInfoAsset {
		
		public string ModName {
			get;
			internal set;
		}
		public string CreatorName {
			get;
			internal set;
		}
		public string Version {
			get;
			internal set;
		}
		public string AssetBundleFolderPath {
			get;
			internal set;
		}
		public string PluginFolderPath {
			get;
			internal set;
		}
		public OperatingSystemFamily OperatingSystem {
			get;
			internal set;
		}
		public string GameVersion {
			get;
			internal set;
		}
		public string ModdingKitVersion {
			get;
			internal set;
		}

		private static ModInfoAsset edited_asset = new ModInfoAsset();

		private static char[] mod_name_buffer = new char[256];
		private static char[] creator_name_buffer = new char[256];
		private static char[] mod_version_buffer = new char[16];
		private static char[] asset_path_buffer = new char[256];
		private static char[] plugin_path_buffer = new char[256];
		private static int os_index = 0;
		private static OperatingSystemFamily[] operating_systems = new OperatingSystemFamily[] {
			OperatingSystemFamily.Windows,
			OperatingSystemFamily.MacOSX,
			OperatingSystemFamily.Linux,
			OperatingSystemFamily.Other
		};

		internal static void DisplayImGuiControls() {
			if (ImGui.BeginMenu("ModInfo generator")) {
				ImGui.InputText("Mod Name", mod_name_buffer);
				edited_asset.ModName = ImGuiUtility.AsString(mod_name_buffer);

				ImGui.InputText("Creator's Name", creator_name_buffer);
				edited_asset.CreatorName = ImGuiUtility.AsString(creator_name_buffer);

				char[] prev_buffer = mod_version_buffer.Clone() as char[];
 				ImGui.InputText("Mod Version", mod_version_buffer);
				string ver = ImGuiUtility.AsString(mod_version_buffer);
				if (Regex.IsMatch(ver, "[0-9]+\\.[0-9]+\\.[0-9]+$")) {
					edited_asset.Version = ver;
				}
				else {
					mod_version_buffer = prev_buffer;
				}

				ImGui.InputText("Assetbundle Folder Path", asset_path_buffer);
				edited_asset.AssetBundleFolderPath = ImGuiUtility.AsString(asset_path_buffer);

				ImGui.InputText("Plugin Folder Path", plugin_path_buffer);
				edited_asset.PluginFolderPath = ImGuiUtility.AsString(plugin_path_buffer);

				var os_list = operating_systems.Select(os => os.ToString()).ToArray();
				os_list[3] = "All";

				ImGui.ListBox("Operating System", ref os_index, os_list, 4);
				edited_asset.OperatingSystem = operating_systems[os_index];

				edited_asset.GameVersion = ReceiverCoreScript.Instance().build_info.version;
				edited_asset.ModdingKitVersion = ModdingKitCorePlugin.instance.Info.Metadata.Version.ToString();

				if (ImGui.Button("Clear")) {
					edited_asset = new ModInfoAsset();

					mod_name_buffer = new char[256];
					creator_name_buffer = new char[256];
					mod_version_buffer = new char[16];
					asset_path_buffer = new char[256];
					plugin_path_buffer = new char[256];
					os_index = 0;
				}

				if (ImGui.Button("Copy .json file")) {
					var json = new JSONObject();

					json["modName"] = edited_asset.ModName;
					json["creatorName"] = edited_asset.CreatorName;
					json["version"] = edited_asset.Version;
					json["assetBundleFolder"] = edited_asset.AssetBundleFolderPath;
					json["pluginFolder"] = edited_asset.PluginFolderPath;
					json["operatingSystem"] = edited_asset.OperatingSystem.ToString();
					json["gameVersion"] = edited_asset.GameVersion;
					json["moddingKitVersion"] = edited_asset.ModdingKitVersion;

					var editor = new TextEditor() { text = json.ToString() };
					editor.SelectAll();
					editor.Copy();
				}

				ImGui.EndMenu();
			}
		}

		public static ModInfoAsset FromJSONFile(JSONNode jsonFile, string defaultName) {
			var asset = new ModInfoAsset();

			if (jsonFile.HasKey("assetBundleFolder")) {
				asset.AssetBundleFolderPath = jsonFile["assetBundleFolder"];
			} else {
				Debug.LogError("\"assetBundleFolder\" key is not present in the json file, mod will not be loaded");

				return null;
			}

			if (jsonFile.HasKey("pluginFolder")) {
				asset.PluginFolderPath = jsonFile["pluginFolder"];
			} else {
				Debug.LogError("\"pluginFolder\" key is not present in the json file, mod will not be loaded");

				return null;
			}

			asset.ModName = jsonFile.HasKey("modName") ? (string) jsonFile["modName"] : defaultName;
			asset.CreatorName = jsonFile.HasKey("creatorName") ? (string) jsonFile["creatorName"] : "";
			asset.Version = jsonFile.HasKey("version") ? (string) jsonFile["version"] : "";

			if (jsonFile.HasKey("operatingSystem")) {
				if (Enum.TryParse(jsonFile["operatingSystem"], out OperatingSystemFamily operatingSystem)) {
					asset.OperatingSystem = operatingSystem;
				}
				else if (jsonFile["operatingSystem"] == "ALL") {
					asset.OperatingSystem = OperatingSystemFamily.Other;
				}
				else {
					asset.OperatingSystem = OperatingSystemFamily.Windows;
				}
			}
			else {
				asset.OperatingSystem = OperatingSystemFamily.Windows;
			}

			asset.GameVersion = jsonFile.HasKey("gameVersion") ? (string) jsonFile["gameVersion"] : "2.2.3";
			asset.ModdingKitVersion = jsonFile.HasKey("moddingKitVersion") ? (string) jsonFile["moddingKitVersion"] : "0.0.0";
			return asset;
		}

		private ModInfoAsset() { }
	}
}
