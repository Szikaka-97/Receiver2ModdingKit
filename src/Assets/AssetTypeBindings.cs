using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Receiver2ModdingKit.Assets {
	public delegate string AssetNameFunction(SerializedAsset asset);
	public delegate SerializedAsset[] AssetDependencyFunction(SerializedAsset asset);

	public static class AssetTypeBindings {
		private static Dictionary<AssetType, AssetNameFunction> asset_name_bindings;

		private static Dictionary<AssetType, AssetDependencyFunction> asset_dependency_bindings;
	}
}
