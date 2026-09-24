using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SharpCompress.Archives;

namespace Receiver2ModdingKit.Helpers {
	public static class AssetHelper {
		public static void DecompressSigmaBundles(string path) { //hello my name is sizwaz and I love K&R braces
			const string k_CompressedBundlesExtension = ".sigma";

			bool valid;

			foreach (var file in Directory.GetFiles(path)) {
				valid = false;

				if (Path.GetExtension(file) == k_CompressedBundlesExtension) {
					using (var archive = ArchiveFactory.Open(file)) {
						valid = true;

						foreach (var entry in archive.Entries) {
							if (Path.GetExtension(entry.Key).Contains(SystemInfo.operatingSystemFamily.ToString().ToLower())) {
								using (var bundleStream = entry.OpenEntryStream()) {
									byte[] buffer = new byte[entry.Size];
									bundleStream.Read(buffer, 0, (int)entry.Size);
									using (var decompressedFile = File.Create(Path.Combine(path, entry.Key))) {
										decompressedFile.Write(buffer, 0, buffer.Length);
									}
								}
							}
						}
					}
					if (valid) {
						File.Delete(file);
					}
				}
			}
		}

		public static bool FindAssetBundle<T>(string path, out T asset) where T : class
		{
			asset = null;
			string current_directory = path;
			Debug.Log(current_directory);
			if (Directory.Exists(current_directory)) //don't need to check but whatevs
			{
				var files_in_directory = Directory.GetFiles(current_directory);
				for (int i = 0; i < files_in_directory.Length; i++)
				{
					string fileName = files_in_directory[i];
					if (fileName.Contains(SystemInfo.operatingSystemFamily.ToString().ToLower()))
					{
						AssetBundle assetBundle = AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault((AssetBundle bundle) => bundle.name.Contains(Path.GetFileNameWithoutExtension(fileName)));
						if (assetBundle == null)
						{
							assetBundle = AssetBundle.LoadFromFile(fileName);
							if (assetBundle == null)
							{
								Debug.LogWarning("Failed to load AssetBundle " + fileName);
								return false;
							}
						}
						foreach (string asset_name in assetBundle.GetAllAssetNames())
						{
							GameObject gameObject = assetBundle.LoadAsset<GameObject>(asset_name);
							if (gameObject != null)
							{
								if (gameObject.TryGetComponent<T>(out asset))
								{
									Debug.Log("Found " + asset.GetType().ToString() + " bundle");
									return true;
								}
							}
						}
					}
				}
			}
			else Debug.LogError("Specified path doesn't exist");
			return false;
		}

		public static bool FindAssetBundle<T>(out T asset) where T : class
		{
			asset = null;
			string current_directory = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
			Debug.Log(current_directory);
			if (Directory.Exists(current_directory)) //don't need to check but whatevs
			{
				var files_in_directory = Directory.GetFiles(current_directory);
				for (int i = 0; i < files_in_directory.Length; i++)
				{
					string fileName = files_in_directory[i];
					if (fileName.Contains(SystemInfo.operatingSystemFamily.ToString().ToLower()))
					{
						AssetBundle assetBundle = AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault((AssetBundle bundle) => bundle.name.Contains(Path.GetFileNameWithoutExtension(fileName)));
						if (assetBundle == null)
						{
							assetBundle = AssetBundle.LoadFromFile(fileName);
							if (assetBundle == null)
							{
								Debug.LogWarning("Failed to load AssetBundle " + fileName);
								return false;
							}
						}
						foreach (string asset_name in assetBundle.GetAllAssetNames())
						{
							GameObject gameObject = assetBundle.LoadAsset<GameObject>(asset_name);
							if (gameObject != null)
							{
								if (gameObject.TryGetComponent<T>(out asset))
								{
									Debug.Log("Found " + asset.GetType().ToString() + " bundle");
									return true;
								}
							}
						}
					}
				}
			}
			else Debug.LogError("Specified path doesn't exist");
			return false;
		}
	}
}
