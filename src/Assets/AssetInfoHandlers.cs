using System;
using System.Collections.Generic;

namespace Receiver2ModdingKit.Assets {
	public static class AssetInfoHandlers {
		public delegate AssetInfo Populate(ref AssetInfo pre_info);

		public static Populate GetHandler(AssetIDType type) {
			if (populate_handlers.ContainsKey(type)) {
				return populate_handlers[type];
			}
			throw new NotImplementedException("Assets with type " + type + " aren't supported yet!");
		}

		public static bool TryGetHandler(AssetIDType type, out Populate handler) {
			if (populate_handlers.ContainsKey(type)) {
				handler = populate_handlers[type];

				return true;
			}

			handler = null;

			return false;
		}

		private static Dictionary<AssetIDType, Populate> populate_handlers = new Dictionary<AssetIDType, Populate>() {
			{ AssetIDType.Texture2D, (ref AssetInfo asset) => {
				FileEncoder encoder = asset.GetEncoder();

				asset.name = encoder.ReadStringWithPrefix();

				encoder.AlignStream(4);

				encoder.Position += 0x4c;

				int length = encoder.ReadInt32();

				if (length == 0) {
					asset.StreamingData = new StreamingInfo() {
						offset = encoder.ReadInt32(),
						size = encoder.ReadInt32(),
						path = encoder.ReadStringWithPrefix()
					};
				}

				return asset;
			} }
		};
	}
}