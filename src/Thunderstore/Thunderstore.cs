using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BepInEx;
using Receiver2;
using System.Runtime.Remoting.Messaging;

namespace Receiver2ModdingKit.Thunderstore {
	internal static class Thunderstore {

		private static bool initialized;
		private static bool m_launched_with_r2mm;
		private static DirectoryInfo m_r2mm_plugin_dir = null;

		private static List<ModSpec> installed_mods = new List<ModSpec>();

		public static bool LaunchedWithR2ModMan {
			get {
				if (!initialized) Initialize();
				return m_launched_with_r2mm;
			}
		}

		public static DirectoryInfo R2ModManPluginDir {
			get {
				if (!initialized) Initialize();
				return m_r2mm_plugin_dir;
			}
		}

		public static void Initialize() {
			var cmd_args = Environment.GetCommandLineArgs();

			if (cmd_args.Length > 1) {
				//Started with command line arguments, likely from r2modman

				int doorstop_arg_index = 0;

				if (
					(doorstop_arg_index = Array.FindIndex(cmd_args, arg => arg == "--doorstop-enable")) >= 0
					&&
					doorstop_arg_index < cmd_args.Length && cmd_args[doorstop_arg_index + 1] == "true"
				) {
					m_launched_with_r2mm = true;
				}
				else {
					m_launched_with_r2mm = false;

					initialized = true;

					return;
				}

				m_r2mm_plugin_dir = new DirectoryInfo(Paths.PluginPath);

				foreach (var mod_dir in m_r2mm_plugin_dir.GetDirectories()) {
					ModSpec mod = ModSpec.Create(mod_dir);

					if (mod == null) continue;

					installed_mods.Add(mod);

					#if DEBUG
					Debug.Log("Loading mod " + mod_dir.Name);
					#endif
				}

				initialized = true;
			}
		}

		public static void InstallMods() {
			if (LaunchedWithR2ModMan) {
				foreach (var mod in installed_mods) {
					mod.InstallCampaign();

					#if DEBUG
					Debug.Log("Installing mod " + mod.Name);
					#endif
				}
			}
		}

		public static void InstallGuns() {
			if (LaunchedWithR2ModMan) {
				#if DEBUG
				Debug.Log("Installing guns");
				#endif

				foreach (var mod in installed_mods) {
					mod.InstallGuns();
				}
			}
		}

		public static void InstallTapes(TapeManager instance) {
			if (LaunchedWithR2ModMan) {
				#if DEBUG
				Debug.Log("Installing tapes");
				#endif

				foreach (var mod in installed_mods) {
					mod.InstallTapes(instance);
				}
			}
		}

		public static void InstallTiles(ModulePrefabsList instance) {
			if (LaunchedWithR2ModMan) {
				#if DEBUG
				Debug.Log("Installing tiles");
				#endif

				foreach (var mod in installed_mods) {
					mod.InstallTiles(instance);
				}
			}
		}

		public static void CleanupMods() {
			if (LaunchedWithR2ModMan) {
				foreach (var mod in installed_mods) {
					mod.UninstallCampaign();

					#if DEBUG
					Debug.Log("Uninstalling mod " + mod.Name);
					#endif
				}
			}

			// Reverting all changed files back to normal, see ModSpec@114

			DirectoryInfo gbc_dir = new DirectoryInfo(Path.Combine(Application.persistentDataPath, "GlobalBaseConfiguration"));
			if (gbc_dir.Exists) {
				foreach (var old_file in gbc_dir.GetFiles("*.json.standaloneold")) {
					try {
						old_file.MoveTo(Path.ChangeExtension(old_file.FullName, old_file.FullName.Replace(".standaloneold", "")));
					} catch { }
				}
			}

			DirectoryInfo loadouts_dir = new DirectoryInfo(Path.Combine(Application.persistentDataPath, "PlayerLoadouts"));
			if (loadouts_dir.Exists) {
				foreach (var old_file in loadouts_dir.GetFiles("*.json.standaloneold")) {
					try {
						old_file.MoveTo(Path.ChangeExtension(old_file.FullName, old_file.FullName.Replace(".standaloneold", "")));
					} catch { }
				}
			}

			DirectoryInfo rpc_dir = new DirectoryInfo(Path.Combine(Application.persistentDataPath, "RankingProgressionCampaigns"));
			if (rpc_dir.Exists) {
				foreach (var old_file in rpc_dir.GetFiles("*.json.standaloneold")) {
					try {
						old_file.MoveTo(Path.ChangeExtension(old_file.FullName, old_file.FullName.Replace(".standaloneold", "")));
					} catch { }
				}
			}

			DirectoryInfo wcg_dir = new DirectoryInfo(Path.Combine(Application.persistentDataPath, "WorldGenerationConfigurations"));
			if (wcg_dir.Exists) {
				foreach (var old_file in gbc_dir.GetFiles("*.json.standaloneold")) {
					try {
						old_file.MoveTo(Path.ChangeExtension(old_file.FullName, old_file.FullName.Replace(".standaloneold", "")));
					} catch { }
				}
			}
		}
	}
}
