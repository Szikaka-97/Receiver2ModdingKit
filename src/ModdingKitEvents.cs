using System.Collections.Generic;
using Receiver2;

namespace Receiver2ModdingKit {
    public static class ModdingKitEvents {

        public delegate void StartupAction();
        public delegate void OnItemSpawn(ref ActiveItem spawned_item);

		/// <summary>
		/// Add a task to be executed after ReceiverCoreScript awakes, eliminating the need for a patch
		/// </summary>
		/// <param name="action"> Function to be executed after ReceiverCoreScript awakes </param>
		public static void AddTaskAtCoreStartup(StartupAction action) {
			ExecuteOnStartup += action;
		}
		internal static StartupAction ExecuteOnStartup = new StartupAction(() => { });

        /// <summary>
        /// Register a handler to modify magazines spawned in the tape collection gamemode for a given gun
        /// </summary>
        /// <param name="gun_internal_name"> Name of the gun
        /// <paramref name="handler"/> Function modifying the magazine item
        public static void RegisterItemSpawnHandler(string gun_internal_name, OnItemSpawn handler) {
            if (ItemSpawnHandlers.ContainsKey(gun_internal_name)) {
                ItemSpawnHandlers[gun_internal_name] += handler;
            }
            else {
                ItemSpawnHandlers[gun_internal_name] = handler;
            }
        }
        internal static Dictionary<string, OnItemSpawn> ItemSpawnHandlers = new Dictionary<string, OnItemSpawn>();
    }
}