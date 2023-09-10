//Based on BepInEx.Debug.ScriptEngine
//https://github.com/BepInEx/BepInEx.Debug

using BepInEx;
using BepInEx.Bootstrap;
using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Receiver2ModdingKit.ModInstaller {
	public class ScriptEngine {
		public static string ScriptDirectory => Path.Combine(Paths.BepInExRootPath, "./guns-temp");

		static GameObject scriptManager;

		public static void ReloadPlugins() {
			if (scriptManager != null) GameObject.Destroy(scriptManager);
			scriptManager = new GameObject($"ScriptEngine_{DateTime.Now.Ticks}");
			GameObject.DontDestroyOnLoad(scriptManager);

			var files = Directory.GetFiles(ScriptDirectory, "*.dll");
			if (files.Length > 0) {
				foreach (string path in Directory.GetFiles(ScriptDirectory, "*.dll"))
					LoadDLL(path, scriptManager);
			}
		}

		private static void LoadDLL(string path, GameObject obj) {
			var defaultResolver = new DefaultAssemblyResolver();
			defaultResolver.AddSearchDirectory(ScriptDirectory);
			defaultResolver.AddSearchDirectory(Paths.ManagedPath);
			defaultResolver.AddSearchDirectory(Paths.BepInExAssemblyDirectory);

			using (var dll = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { AssemblyResolver = defaultResolver })) {
				dll.Name.Name = $"{dll.Name.Name}-{DateTime.Now.Ticks}";

				using (var ms = new MemoryStream()) {
					dll.Write(ms);
					var ass = Assembly.Load(ms.ToArray());

					foreach (Type type in GetTypesSafe(ass)) {
						try {
							if (typeof(BaseUnityPlugin).IsAssignableFrom(type)) {
								var metadata = MetadataHelper.GetMetadata(type);
								if (metadata != null) {
									var typeDefinition = dll.MainModule.Types.First(x => x.FullName == type.FullName);
									var typeInfo = Chainloader.ToPluginInfo(typeDefinition);
									Chainloader.PluginInfos[metadata.GUID] = typeInfo;

									Debug.Log($"Loading {metadata.GUID}");

									ModdingKitCorePlugin.instance.StartCoroutine(DelayAction(() => {
										try {
											//obj.AddComponent(type); //Stupid
										}
										catch (Exception e) {
											Debug.LogError($"Failed to load plugin {metadata.GUID} because of exception: {e}");
										}
									}));
								}
							}
						}
						catch (Exception e) {
							Debug.LogError($"Failed to load plugin {type.Name} because of exception: {e}");
						}
					}
				}
			}
		}

		private static IEnumerable<Type> GetTypesSafe(Assembly ass) {
			try {
				return ass.GetTypes();
			}
			catch (ReflectionTypeLoadException ex) {
				var sbMessage = new StringBuilder();
				sbMessage.AppendLine("\r\n-- LoaderExceptions --");
				foreach (var l in ex.LoaderExceptions)
					sbMessage.AppendLine(l.ToString());
				sbMessage.AppendLine("\r\n-- StackTrace --");
				sbMessage.AppendLine(ex.StackTrace);
				Debug.LogError(sbMessage.ToString());
				return ex.Types.Where(x => x != null);
			}
		}

		private static IEnumerator DelayAction(Action action) {
			yield return null;
			action();
		}
	}
}