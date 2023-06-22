using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using SimpleJSON;
using Receiver2;
using System.Linq;
using UnityEngine.UI;

namespace Receiver2ModdingKit.Thunderstore {
	/// <summary>
	/// Class containing all the mod info regarding a Thunderstore mod
	/// </summary>
	internal class ModSpec {
		private static readonly string[] ignored_files = new string[] {
			@"manifest\.json",
			@"modinfo\.json"
		};

		#region File Type Checking

		private static bool CheckGBCFile(JSONNode json) {
			try {
				GlobalBaseConfiguration.Deserialize(json);

				return true;
			} catch {
				return false;
			}
		}
		
		private static bool CheckRPCFile(JSONNode json) {
			return json.HasKey("ranks");
		}

		private static bool CheckWGCFile(JSONNode json) {
			return json.HasKey("configuration_name");
		}

		private static bool CheckLoadoutFile(JSONNode json) {
			if (!json.HasKey("content")) return false;

			try {
				PlayerLoadout.Deserialize(json["content"]);

				return true;
			} catch {
				return false;
			}
		}

		#endregion

		public string Name {
			get;
		}

		public Sprite icon {
			get;
		}

		public bool Installed {
			get;
			private set;
		}

		public DirectoryInfo BaseDir {
			get;
			private set;
		}

		public JSONNode manifest {
			get;
		}
		
		public List<FileInfo> GlobalBaseConfigs {
			get;
		} = new List<FileInfo>();
		public List<FileInfo> Campaigns {
			get;
		} = new List<FileInfo>();
		public List<FileInfo> Loadouts {
			get;
		} = new List<FileInfo>();
		public List<FileInfo> WorldGenerationsConfigs {
			get;
		} = new List<FileInfo>();
		public List<FileInfo> Assets {
			get;
		} = new List<FileInfo>();
		public List<FileInfo> TapeSubtitles {
			get;
		} = new List<FileInfo>();
		public List<FileInfo> Tapes {
			get;
		} = new List<FileInfo>();

		private ModSpec(string modName, JSONNode manifest, Sprite icon) {
			this.Name = modName;
			this.manifest = manifest;
			this.icon = icon;
		}

		public void InstallCampaign() {
			if (Installed) return;

			DirectoryInfo	game_data_dir = new DirectoryInfo(Application.persistentDataPath),
							gbc_dir,
							rpc_dir,
							loadouts_dir,
							wgc_dir;

			// These folders are already created at the startup of the plugin, but it's best to check anyway
			if (!game_data_dir.TryGetChild("GlobalBaseConfiguration", out gbc_dir))
				gbc_dir.Create();
			if (!game_data_dir.TryGetChild("RankingProgressionCampaigns", out rpc_dir))
				rpc_dir.Create();
			if (!game_data_dir.TryGetChild("PlayerLoadouts", out loadouts_dir))
				loadouts_dir.Create();
			if (!game_data_dir.TryGetChild("WorldGenerationConfigurations", out wgc_dir))
				wgc_dir.Create();

			//If the file is already present in the game, we change its extension to .standaloneold so it doesn't get picked up by the game
			//It gets changed back in Thunderstore.CleanupMods()

			for (int i = 0; i < GlobalBaseConfigs.Count; i++) {
				var gbc_file = GlobalBaseConfigs[i];

				try {
					string dest_file_name = Path.Combine(gbc_dir.FullName, gbc_file.Name);

					if (File.Exists(dest_file_name)) {
						if (File.Exists(dest_file_name + ".standaloneold")) File.Delete(dest_file_name + ".standaloneold");

						File.Move(dest_file_name, dest_file_name + ".standaloneold");
					}

					GlobalBaseConfigs[i] = gbc_file.CopyTo(dest_file_name, true);
				} catch (Exception e) {
					Debug.LogError("Couldn't copy Global Base Configuration file " + gbc_file.Name + " from " + BaseDir + " to " + gbc_dir.FullName);
					Debug.LogException(e);
				}
			}
			for (int i = 0; i < Campaigns.Count; i++) {
				var campaign_file = Campaigns[i];

				try {
					string dest_file_name = Path.Combine(rpc_dir.FullName, campaign_file.Name);

					if (File.Exists(dest_file_name)) {
						if (File.Exists(dest_file_name + ".standaloneold")) File.Delete(dest_file_name + ".standaloneold");

						File.Move(dest_file_name, dest_file_name + ".standaloneold");
					}

					Campaigns[i] = campaign_file.CopyTo(dest_file_name, true);
				} catch (Exception e) {
					Debug.LogError("Couldn't copy Ranking Progression Campaign file " + campaign_file.Name + " from " + BaseDir + " to " + rpc_dir.FullName);
					Debug.LogException(e);
				}
			}
			for (int i = 0; i < Loadouts.Count; i++) {
				var loadout_file = Loadouts[i];

				try {
					string dest_file_name = Path.Combine(loadouts_dir.FullName, loadout_file.Name);

					if (File.Exists(dest_file_name)) {
						if (File.Exists(dest_file_name + ".standaloneold")) File.Delete(dest_file_name + ".standaloneold");

						File.Move(dest_file_name, dest_file_name + ".standaloneold");
					}

					Loadouts[i] = loadout_file.CopyTo(dest_file_name, true);
				} catch (Exception e) {
					Debug.LogError("Couldn't copy Player Loadout file " + loadout_file.Name + " from " + BaseDir + " to " + loadouts_dir.FullName);
					Debug.LogException(e);
				}
			}
			for (int i = 0; i < WorldGenerationsConfigs.Count; i++) {
				var wgc_file = WorldGenerationsConfigs[i];

				try {
					string dest_file_name = Path.Combine(wgc_dir.FullName, wgc_file.Name);

					if (File.Exists(dest_file_name)) {
						if (File.Exists(dest_file_name + ".standaloneold")) File.Delete(dest_file_name + ".standaloneold");

						File.Move(dest_file_name, dest_file_name + ".standaloneold");
					}

					WorldGenerationsConfigs[i] = wgc_file.CopyTo(dest_file_name, true);
				} catch (Exception e) {
					Debug.LogError("Couldn't copy World Generation Configuration file " + wgc_file.Name + " from " + BaseDir + " to " + wgc_dir.FullName);
					Debug.LogException(e);
				}
			}

			Installed = true;
		}

		public void InstallGuns() {
			ReceiverCoreScript.Instance().LoadModGun(BaseDir.FullName, true);
		}

		public void InstallTapes(TapeManager instance) {
			instance.LoadModTape(BaseDir.FullName, true);

			foreach (var subtitle_file in TapeSubtitles) {
				string tape_id = subtitle_file.Name.Replace(".srt", "");

				ModTapeManager.RegisterSubtitles(tape_id, subtitle_file);
			}
		}

		public void InstallTiles(ModulePrefabsList instance) {
			instance.LoadModTile(BaseDir.FullName, true);
		}

		public void UninstallCampaign() {
			if (!Installed) return;

			foreach (var file in GlobalBaseConfigs) file.Delete();
			
			foreach (var file in Campaigns) file.Delete();
			
			foreach (var file in Loadouts) file.Delete();
			
			foreach (var file in WorldGenerationsConfigs) file.Delete();
		}

		public static ModSpec Create(DirectoryInfo mod_dir) {
			var manifest_file = new FileInfo(Path.Combine(mod_dir.FullName, "manifest.json"));

			if (!manifest_file.Exists) {
				return null;
			}

			var manifest = ReceiverCoreScript.LoadJSONFile(manifest_file.FullName);

			Sprite mod_icon = null;

			if (File.Exists(Path.Combine(mod_dir.FullName, "icon.png"))) {
				using (var image_stream = File.OpenRead(Path.Combine(mod_dir.FullName, "icon.png"))) {
					byte[] data = new byte[image_stream.Length];

					image_stream.Read(data, 0, (int) image_stream.Length);

					var temp_texture = new Texture2D(256, 256);
					temp_texture.LoadImage(data);

					mod_icon = Sprite.Create(temp_texture, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
				}
			}

			ModSpec spec = new ModSpec(mod_dir.Name, manifest, mod_icon) {
				BaseDir = mod_dir
			};

			foreach (var mod_file in mod_dir.EnumerateFiles()) {
				if (ignored_files.Any( filename => Regex.IsMatch(mod_file.Name, filename))) continue;

				switch (mod_file.Extension) {
					case ".json":
						JSONNode loaded_file;

						try {
							loaded_file = ReceiverCoreScript.LoadJSONFile(mod_file.FullName);
						}
						catch (Exception e) {
							Debug.Log("File " + mod_file + " couldn't be loaded");
							Debug.LogError(e);

							break;
						}

						if (CheckGBCFile(loaded_file)) 
							spec.GlobalBaseConfigs.Add(mod_file);
						else if (CheckRPCFile(loaded_file)) 
							spec.Campaigns.Add(mod_file);
						else if (CheckLoadoutFile(loaded_file)) 
							spec.Loadouts.Add(mod_file);
						else if (CheckWGCFile(loaded_file)) 
							spec.WorldGenerationsConfigs.Add(mod_file);
						
						break;
					case ".oog":
					case ".mp3":
					case ".wav":
						spec.Tapes.Add(mod_file);

						break;
					case ".srt":
						spec.TapeSubtitles.Add(mod_file);

						break;
					default:
						break;
				}

				if (mod_file.Extension == "." + SystemInfo.operatingSystemFamily.ToString().ToLower()) 
					spec.Assets.Add(mod_file);
			}

			return spec;
		}
	}
}
