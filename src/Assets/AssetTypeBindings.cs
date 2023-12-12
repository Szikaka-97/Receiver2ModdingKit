using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Receiver2ModdingKit.Assets {
	public delegate string AssetNameFunction(SerializedAsset asset);
	public delegate SerializedAsset[] AssetDependencyFunction(SerializedAsset asset);

	public static class AssetTypeBindings {
		private static Dictionary<AssetIDType, AssetNameFunction> asset_name_bindings;

		private static Dictionary<AssetIDType, AssetDependencyFunction> asset_dependency_bindings;
	}
}
