using HarmonyLib;
using Receiver2;
using SimpleJSON;

namespace Receiver2ModdingKit {
	public static class ModPersistentData {
		const string k_PersistentDataKeyName = "mod_persistent_data";

		private static JSONObject persistent_data = new JSONObject();

		public static void AddPersistentData(BepInEx.PluginInfo pluginInfo, JSONObject data) {
			persistent_data.Add(pluginInfo.Metadata.GUID, data);
		}

		public static JSONObject GetPersistentData(BepInEx.PluginInfo pluginInfo) {
			return persistent_data[pluginInfo.Metadata.GUID].AsObject;
		}

		[HarmonyPatch(typeof(PersistentPlayerData), nameof(PersistentPlayerData.Serialize))]
		[HarmonyPostfix]
		private static void AddDataToSave(JSONObject __result) {
			try
			{
				__result.Add(k_PersistentDataKeyName, persistent_data);
			}
			catch { }
		}

		[HarmonyPatch(typeof(PersistentPlayerData), nameof(PersistentPlayerData.Deserialize))]
		[HarmonyPostfix]
		private static void RetrieveDataFromSave(JSONNode jn) {
			try
			{
				persistent_data = jn[k_PersistentDataKeyName].AsObject;
			}
			catch { }
		}
	}
}