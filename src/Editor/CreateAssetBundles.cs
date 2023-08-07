#if UNITY_EDITOR

using System;
using System.IO;
using UnityEngine;
using UnityEditor;

public class CreateAssetBundles {
	static BuildTarget mainTarget = BuildTarget.StandaloneWindows;
	static string asset_bundle_directory = "Assets/AssetBundles";

	static Tuple<BuildTarget, OperatingSystemFamily>[] targets = new Tuple<BuildTarget, OperatingSystemFamily>[] {
		new Tuple<BuildTarget, OperatingSystemFamily>(BuildTarget.StandaloneWindows, OperatingSystemFamily.Windows),
		new Tuple<BuildTarget, OperatingSystemFamily>(BuildTarget.StandaloneLinux64, OperatingSystemFamily.Linux),
		new Tuple<BuildTarget, OperatingSystemFamily>(BuildTarget.StandaloneOSX, OperatingSystemFamily.MacOSX)
	};

	static void buildSystemAssetBundles(Tuple<BuildTarget, OperatingSystemFamily> target) {
		var target_path = Path.Combine(asset_bundle_directory, target.Item2.ToString());

		if (!Directory.Exists(target_path)) Directory.CreateDirectory(target_path);

		var manifest = BuildPipeline.BuildAssetBundles(
			target_path, 
	        BuildAssetBundleOptions.UncompressedAssetBundle, 
	        target.Item1
	    );
		
		string extension = target.Item2.ToString().ToLower();

		bool notified = false;

		foreach (var assetbundle_name in manifest.GetAllAssetBundles()) {
			var ab_path = Path.Combine(target_path, assetbundle_name);

			if (File.Exists(ab_path)) {

				if (File.Exists(Path.ChangeExtension(ab_path, extension))) File.Delete(Path.ChangeExtension(ab_path, extension));

				if (File.Exists(Path.ChangeExtension(ab_path, "meta"))) File.Delete(Path.ChangeExtension(ab_path, "meta"));

				File.Move(ab_path, ab_path = Path.ChangeExtension(ab_path, extension));

				CustomAssetBundleDirectory.Refresh();

				if (CustomAssetBundleDirectory.directory_list != null && CustomAssetBundleDirectory.directory_list.hasPath(assetbundle_name)) {
					var copy_path = CustomAssetBundleDirectory.directory_list.GetPath(assetbundle_name);

					if (Directory.Exists(copy_path)) {
						try {
							File.Copy(ab_path, Path.ChangeExtension(Path.Combine(copy_path, assetbundle_name), extension), true);
						} catch (Exception e) {
							Debug.LogError("Failed to copy bundle " + assetbundle_name + " because of exception:");
							Debug.LogException(e);
						}
					}
					else {
						Debug.LogError("Directory doesn't exist:\n" + copy_path);
					}
				}
				else if (!notified) {
					Debug.Log("No directory list is specified, will not copy bundle " + assetbundle_name);

					notified = true;
				}
			}
		}
	}

	[MenuItem("Assets/Build AssetBundles")]
	static void BuildMainAssetBundles() {
		if(!Directory.Exists(asset_bundle_directory)) {
			Directory.CreateDirectory(asset_bundle_directory);
		}

		switch (SystemInfo.operatingSystemFamily) {
			case OperatingSystemFamily.Windows:
				mainTarget = BuildTarget.StandaloneWindows;
				break;
			case OperatingSystemFamily.Linux:
				mainTarget = BuildTarget.StandaloneLinux64;
				break;
			case OperatingSystemFamily.MacOSX:
				mainTarget = BuildTarget.StandaloneOSX;
				break;
		}

		buildSystemAssetBundles(new Tuple<BuildTarget, OperatingSystemFamily>(mainTarget, SystemInfo.operatingSystemFamily));
	}
	
	[MenuItem("Assets/Build All AssetBundles")]
	static void BuildAllAssetBundles() {
		if(!Directory.Exists(asset_bundle_directory)) {
			Directory.CreateDirectory(asset_bundle_directory);
		}

		switch (SystemInfo.operatingSystemFamily) {
			case OperatingSystemFamily.Windows:
				mainTarget = BuildTarget.StandaloneWindows;
				break;
			case OperatingSystemFamily.Linux:
				mainTarget = BuildTarget.StandaloneLinux64;
				break;
			case OperatingSystemFamily.MacOSX:
				mainTarget = BuildTarget.StandaloneOSX;
				break;
		}

		foreach (var tuple in targets) {
			buildSystemAssetBundles(tuple);
		}
	}
}

#endif