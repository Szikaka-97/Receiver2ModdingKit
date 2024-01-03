using System;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using UnityEngine;

namespace Receiver2ModdingKit.Assets {
	public class AssetDatabase {
		public AssetDomain Domain {
			get;
			private set;
		}
		public string FileName {
			get;
			private set;
		}
		public AssetFileHeader FileHeader {
			get;
			private set;
		}
		public AssetDatabase[] Dependencies {
			get;
			private set;
		}
		public AssetInfo[] Assets {
			get;
			private set;
		}
		public DateTime LastModifyTime {
			get;
			private set;
		}

		public JSONObject Save() {
			JSONObject root = this.FileHeader.Save();

			foreach (var asset in Assets) {
				root["objects"][(int) asset.path_id - 1]["name"] = asset.name;

				root["objects"][(int) asset.path_id - 1]["dependencies"] = new JSONArray();

				if (asset.StreamingData != null) {
					var streaming_info = new JSONObject();

					streaming_info["offset"] = asset.StreamingData.offset;
					streaming_info["size"] = asset.StreamingData.size;
					streaming_info["path"] = asset.StreamingData.path;

					root["objects"][(int) asset.path_id - 1]["streaming_info"] = streaming_info;
				}
			}

			return root;
		}

		public static AssetDatabase Open(AssetDomain domain, JSONObject json) {
			return null;
		}

		public static AssetDatabase Create(AssetDomain domain, string file_path) {
			return Create(domain, new FileInfo(file_path));
		}

		public static AssetDatabase Create(AssetDomain domain, FileInfo file) {
			AssetFileHeader asset_header = AssetFileHeader.CreateFromFile(file);

			return Create(domain, asset_header);
		}

		public static AssetDatabase Create(AssetDomain domain, AssetFileHeader asset_header) {
			if (asset_header == null) {
#warning Implement some error message
				return null;
			}

			AssetDatabase database = new AssetDatabase() {
				Domain = domain,
				FileName = asset_header.file_name,
				LastModifyTime = File.GetLastWriteTime(asset_header.file_name),
				FileHeader = asset_header,
			};

			List<AssetInfo> assets = new List<AssetInfo>();

			AssetInfoHandlers.Populate asset_handler;

			foreach (var obj in asset_header.Objects) {
				if (AssetInfoHandlers.TryGetHandler(asset_header.Types[obj.type_id].GetAssetType(), out asset_handler)) {
					AssetInfo info = AssetInfo.FromObjectStruct(obj, asset_header);

					asset_handler(ref info);

					assets.Add(info);
				}
			}

			database.Assets = assets.ToArray();

			return database;
		}
	}
}