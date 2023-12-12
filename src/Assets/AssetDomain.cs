using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SimpleJSON;
using UnityEngine;
using Wolfire;

namespace Receiver2ModdingKit.Assets {
	public class AssetDomain {
		public string Name {
			get;
			private set;
		}
		public DirectoryInfo Directory {
			get;
			private set;
		}
        public string UnityVersion {
			get;
			private set;
		}
		public string UnityRevision {
			get;
			private set;
		}
		public AssetBuildTarget BuildTarget {
			get;
			private set;
		}
		public Dictionary<string, AssetDatabase> Databases {
			get;
			private set;
		}
		public string DatabasePath {
			get {
				if (string.IsNullOrWhiteSpace(this.Name)) {
					return ModdingKitConfig.asset_database_path.Value;
				}
				else {
					return Path.Combine(ModdingKitConfig.asset_database_path.Value, this.Name);
				}
			}
		}

		private AssetDomain(string name, DirectoryInfo directory) {
			this.Directory = directory;
			this.BuildTarget = AssetBuildTarget.UnknownPlatform;
		}

		private AssetDomain(string name, DirectoryInfo directory, string unity_version, string unity_revision, AssetBuildTarget build_target) {
			this.Directory = directory;
			this.UnityVersion = unity_version;
			this.UnityRevision = unity_revision;
			this.BuildTarget = build_target;
		}

		public bool IsValid() {
			return this.Directory.Exists;
		}

		public static AssetDomain Create(string path, string name = "") {
			var directory = new DirectoryInfo(path);

			if (!directory.Exists) {
				Debug.LogError("AssetDomain::Create(string): path must point to a directory that exists");

				return null;
			}

			AssetDomain domain = new AssetDomain(name, directory);

			foreach (var assetFile in directory.GetFiles()) {
				if (!Regex.IsMatch(assetFile.Name, @"\.assets$")) continue;

				var database = AssetDatabase.Create(domain, assetFile);

				if (database == null) {
					continue;
				}

				if (string.IsNullOrEmpty(domain.UnityVersion)) {
					domain.UnityVersion = database.FileHeader.unity_version;
				}
				else if (domain.UnityVersion != database.FileHeader.unity_version) {
					Debug.LogError("AssetDomain::Create(string): Error! Unity version is mismatched between files!");
					Debug.LogError("AssetDomain::Create(string): In file " + assetFile.FullName + " got version " + database.FileHeader.unity_version + " expected " + domain.UnityVersion);
				}

				if (domain.BuildTarget == AssetBuildTarget.UnknownPlatform) {
					domain.BuildTarget = database.FileHeader.build_target;
				}
				else if (domain.BuildTarget != database.FileHeader.build_target) {
					Debug.LogError("AssetDomain::Create(string): Error! Unity build target is mismatched between files!");
					Debug.LogError("AssetDomain::Create(string): In file " + assetFile.FullName + " got target " + database.FileHeader.build_target + " expected " + domain.BuildTarget);
				}

				var database_file = JSONUtil.FormatJson(database.Save().ToString(), "  ");

				File.WriteAllText(Path.Combine(domain.DatabasePath, Path.GetFileNameWithoutExtension(assetFile.Name)) + ".json", database_file);
			}

			return null;
		}

		public static AssetDomain CreateTrusted(string path, string unity_version, string unity_revision, AssetBuildTarget build_target, string name = "") {
			return new AssetDomain(
				name,
				new DirectoryInfo(path),
				unity_version,
				unity_revision,
				build_target
			);
		}
	}
}