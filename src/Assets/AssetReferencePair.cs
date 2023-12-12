using System;

namespace Receiver2ModdingKit.Assets {
	public class AssetReferencePair : Tuple<int, long> {
		public AssetReferencePair(int file_id, long path_id) : base(file_id, path_id) { }
	}
}