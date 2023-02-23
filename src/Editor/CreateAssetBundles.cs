using UnityEditor;
using UnityEngine;
using System.IO;
using System;

public class CreateAssetBundles {
	static BuildTarget mainTarget = BuildTarget.StandaloneWindows; //Change this if you're on different OS
	static string assetBundleDirectory = "Assets/AssetBundles";

	static Tuple<BuildTarget, OperatingSystemFamily>[] targets = new Tuple<BuildTarget, OperatingSystemFamily>[] {
		new Tuple<BuildTarget, OperatingSystemFamily>(BuildTarget.StandaloneWindows, OperatingSystemFamily.Windows),
		new Tuple<BuildTarget, OperatingSystemFamily>(BuildTarget.StandaloneLinux64, OperatingSystemFamily.Linux),
		new Tuple<BuildTarget, OperatingSystemFamily>(BuildTarget.StandaloneOSX, OperatingSystemFamily.MacOSX)
	};

	static void buildSystemAssetBundles(Tuple<BuildTarget, OperatingSystemFamily> target) {
		BuildPipeline.BuildAssetBundles(Path.Combine(assetBundleDirectory, target.Item1.ToString()), 
	        BuildAssetBundleOptions.UncompressedAssetBundle, 
	        target.Item1
	    );

		foreach (var file in new DirectoryInfo(Path.Combine(assetBundleDirectory, target.Item1.ToString())).GetFiles()) {
			if (file.Extension.Contains(".meta") || file.Extension.Contains(".manifest")) continue;

			string fileNameWithExtension = Path.ChangeExtension(file.FullName, target.Item2.ToString().ToLower());

			if (File.Exists(fileNameWithExtension) && file.Extension != "." + target.Item2.ToString().ToLower()) {
				File.Delete(fileNameWithExtension);
			}

			File.Move(file.FullName, fileNameWithExtension);

			if (File.Exists(file.FullName + ".meta")) {
				if (File.Exists(fileNameWithExtension + ".meta")) {
					File.Delete(fileNameWithExtension);
				}
				File.Move(file.FullName + ".meta", fileNameWithExtension + ".meta");
			}
		}

		foreach (var file in new DirectoryInfo(Path.Combine(assetBundleDirectory, target.Item1.ToString())).GetFiles()) { //Calling twice cuz the filename might have changed
			if (file.Extension.Contains(".meta") || file.Extension.Contains(".manifest")) continue;

			string fileNameWoutExtension = file.Name.Substring(0, file.Name.IndexOf("."));

			if (CustomAssetBundleDirectory.directoryList != null && CustomAssetBundleDirectory.directoryList.hasPath(fileNameWoutExtension)) {
				try {
					File.Copy(file.FullName, CustomAssetBundleDirectory.directoryList.getPath(fileNameWoutExtension) + "/" + file.Name, true);
				} catch (Exception e) {
					Debug.LogError(e.Message);
					Debug.LogError("Failed to copy bundle " + file.Name);
				}
			};
		}
	}

	[MenuItem("Assets/Build AssetBundles")]
	static void BuildMainAssetBundles() {
		if(!Directory.Exists(assetBundleDirectory)) {
			Directory.CreateDirectory(assetBundleDirectory);
		}

		buildSystemAssetBundles(new Tuple<BuildTarget, OperatingSystemFamily>(mainTarget, SystemInfo.operatingSystemFamily));
	}
	
	[MenuItem("Assets/Build All AssetBundles")]
	static void BuildAllAssetBundles() {
		if(!Directory.Exists(assetBundleDirectory)) {
			Directory.CreateDirectory(assetBundleDirectory);
		}

		foreach (var tuple in targets) {
			buildSystemAssetBundles(tuple);
		}
	}
}
