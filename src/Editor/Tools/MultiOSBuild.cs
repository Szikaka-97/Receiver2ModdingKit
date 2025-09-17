#if UNITY_EDITOR
using ThunderKit.Core.Pipelines;
using System.Threading.Tasks;
using ThunderKit.Core.Attributes;
using ThunderKit.Core.Manifests.Datums;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using ThunderKit.Pipelines.Jobs;
using ThunderKit.Core.Paths;
using System.IO;
using ThunderKit.Core.Data;
using UnityEngine;
using System.Text;
using UnityEditor.Compilation;

[PipelineSupport(typeof(Pipeline)), RequiresManifestDatumTypeAttribute(typeof(AssetBundleDefinitions))]
public class MultiOSBuild : PipelineJob
{
	[EnumFlag] public BuildAssetBundleOptions assetBundleOptions = BuildAssetBundleOptions.ChunkBasedCompression;

	public bool onlyBuildCurrentPlatform;

	//public bool sendCurrentPlatformAssetBundleToManifestStagingPath;

	[PathReferenceResolver] public string bundleArtifactPath = "<AssetBundleStaging>";

	public override Task Execute(Pipeline pipeline)
	{
		var excludedExtensions = new[] { ".dll", ".cs", ".meta" };

		AssetDatabase.SaveAssets();
		var manifests = pipeline.Manifests;
		var abdIndices = new Dictionary<AssetBundleDefinitions, int>();
		var abds = new List<AssetBundleDefinitions>();

		for (int i = 0; i < manifests.Length; i++)
		{
			foreach (var abd in manifests[i].Data.OfType<AssetBundleDefinitions>())
			{
				abds.Add(abd);
				abdIndices.Add(abd, i);
			}
		}

		var assetBundleDefs = abds.ToArray();

		var hasValidBundles = assetBundleDefs.Any(abd => abd.assetBundles.Any(ab => !string.IsNullOrEmpty(ab.assetBundleName) && ab.assets.Any()));
		if (!hasValidBundles)
		{
			var scriptPath = UnityWebRequest.EscapeURL(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)));
			pipeline.Log(LogLevel.Warning, $"No valid AssetBundleDefinitions defined, skipping [{nameof(StageAssetBundles)}](assetlink://{scriptPath}) PipelineJob");
			return Task.CompletedTask;
		}

		var resolvedBundleArtifactPath = bundleArtifactPath.Resolve(pipeline, this);
		pipeline.Log(LogLevel.Information, resolvedBundleArtifactPath);
		Directory.CreateDirectory(resolvedBundleArtifactPath);

		var explicitAssets = assetBundleDefs
							 .SelectMany(abd => abd.assetBundles)
							 .SelectMany(ab => ab.assets)
							 .ToArray();

		var explicitAssetPaths = new List<string>();
		PopulateWithExplicitAssets(explicitAssets, explicitAssetPaths);

		var defBuildDetails = new List<string>();
		var logBuilder = new StringBuilder();
		var builds = new AssetBundleBuild[assetBundleDefs.Sum(abd => abd.assetBundles.Length)];

		var buildsIndex = 0;
		for (int defIndex = 0; defIndex < assetBundleDefs.Length; defIndex++)
		{
			var assetBundleDef = assetBundleDefs[defIndex];
			var playerAssemblies = CompilationPipeline.GetAssemblies();
			var assemblyFiles = playerAssemblies.Select(pa => pa.outputPath).ToArray();
			var sourceFiles = playerAssemblies.SelectMany(pa => pa.sourceFiles).ToArray();

			//reset logging containers
			defBuildDetails.Clear();

			for (int i = 0; i < assetBundleDef.assetBundles.Length; i++)
			{
				var def = assetBundleDef.assetBundles[i];
				var build = builds[buildsIndex];
				var assets = new List<string>();

				if (def.assets.FirstOrDefault(x => x is SceneAsset) != null)
				{
					foreach (var sceneAsset in def.assets.Where(x => x is SceneAsset))
						assets.Add(AssetDatabase.GetAssetPath(sceneAsset));
				}
				else
				{
					PopulateWithExplicitAssets(def.assets, assets);

					var dependencies = assets
						.SelectMany(assetPath => AssetDatabase.GetDependencies(assetPath))
						.Where(dap => !explicitAssetPaths.Contains(dap))
						.ToArray();
					assets.AddRange(dependencies);
				}

				build.assetNames = assets
					.Select(ap => ap.Replace("\\", "/"))
					.Where(dap => !ArrayUtility.Contains(excludedExtensions, Path.GetExtension(dap)) &&
								  !ArrayUtility.Contains(sourceFiles, dap) &&
								  !ArrayUtility.Contains(assemblyFiles, dap) &&
								  !AssetDatabase.IsValidFolder(dap))
					.Distinct()
					.ToArray();
				build.assetBundleName = def.assetBundleName;
				builds[buildsIndex] = build;
				buildsIndex++;

				LogBundleDetails(logBuilder, build);

				defBuildDetails.Add(logBuilder.ToString());
				logBuilder.Clear();
			}

			var prevInd = pipeline.ManifestIndex;
			pipeline.ManifestIndex = abdIndices[assetBundleDef];
			pipeline.Log(LogLevel.Information, $"Creating {assetBundleDef.assetBundles.Length} AssetBundles", defBuildDetails.ToArray());
			pipeline.ManifestIndex = prevInd;
		}

		string[] baseNames = new string[builds.Length];

		for (int buildNameIndex = 0; buildNameIndex < baseNames.Length; buildNameIndex++)
		{
			baseNames[buildNameIndex] = builds[buildNameIndex].assetBundleName;
		}

		for (int target = 0; target < 3; target++)
		{
			BuildTarget buildTarget;
			string additionalPath;

			if (target == 0)
			{
				buildTarget = BuildTarget.StandaloneOSX;
				additionalPath = "MacOSX";
			}
			else if (target == 1)
			{
				buildTarget = BuildTarget.StandaloneLinux64;
				additionalPath = "Linux";
			}
			else
			{
				buildTarget = BuildTarget.StandaloneWindows64;
				additionalPath = "Windows";
			}

			if (onlyBuildCurrentPlatform)
			{
				pipeline.Log(LogLevel.Information, "build Target: " + buildTarget + " vs active build target " + EditorUserBuildSettings.activeBuildTarget);
				if (buildTarget != EditorUserBuildSettings.activeBuildTarget)
					continue;
			}

			AssetBundleBuild[] platformBuilds = builds;

			for (int buildIndex = 0; buildIndex < platformBuilds.Length; buildIndex++)
			{
				platformBuilds[buildIndex].assetBundleName = baseNames[buildIndex] + "." + additionalPath.ToString().ToLowerInvariant();
			}

			var fullBundleArtifactPath = Path.Combine(resolvedBundleArtifactPath, additionalPath);
			Directory.CreateDirectory(fullBundleArtifactPath);

			BuildPipeline.BuildAssetBundles(fullBundleArtifactPath, builds, assetBundleOptions, buildTarget);

			/*if (sendCurrentPlatformAssetBundleToManifestStagingPath)
			{
				if (buildTarget == EditorUserBuildSettings.activeBuildTarget)
				{
					for (pipeline.ManifestIndex = 0; pipeline.ManifestIndex < pipeline.Manifests.Length; pipeline.ManifestIndex++) 
					{
						var manifest = pipeline.Manifest;
						foreach (var assetBundleDef in manifest.Data.OfType<AssetBundleDefinitions>())
						{
							foreach (var path in assetBundleDef.StagingPaths)
							{
								FileUtil.CopyFileOrDirectory(fullBundleArtifactPath, path.Resolve(pipeline, this));
							}
						}
					}
				}
			}*/

			/*for (pipeline.ManifestIndex = 0; pipeline.ManifestIndex < pipeline.Manifests.Length; pipeline.ManifestIndex++)
			{
				var manifest = pipeline.Manifest;
				foreach (var assetBundleDef in manifest.Data.OfType<AssetBundleDefinitions>())
				{
					var bundleNames = assetBundleDef.assetBundles.Select(ab => ab.assetBundleName).ToArray();
					foreach (var outputPath in assetBundleDef.StagingPaths.Select(path => path.Resolve(pipeline, this)))
					{
						foreach (string dirPath in Directory.GetDirectories(fullBundleArtifactPath, "*", SearchOption.AllDirectories))
							Directory.CreateDirectory(dirPath.Replace(fullBundleArtifactPath, outputPath));

						foreach (string filePath in Directory.GetFiles(fullBundleArtifactPath, "*", SearchOption.AllDirectories))
						{
							bool found = false;
							foreach (var bundleName in bundleNames)
							{
								pipeline.Log(LogLevel.Information, "bundleName: " + bundleName);
								if (filePath.IndexOf(bundleName, System.StringComparison.OrdinalIgnoreCase) >= 0)
								{
									found = true;
									break;
								}
							}
							if (!found)
								continue;
							string destFileName = filePath.Replace(fullBundleArtifactPath, outputPath) + additionalPath.ToString().ToLowerInvariant();
							Directory.CreateDirectory(Path.GetDirectoryName(destFileName));
							FileUtil.ReplaceFile(filePath, destFileName);
						}

						var manifestSource = Path.Combine(fullBundleArtifactPath, $"{Path.GetFileName(fullBundleArtifactPath)}.manifest");
						var manifestDestination = Path.Combine(outputPath, $"{manifest.Identity.Name}.manifest");
						FileUtil.ReplaceFile(manifestSource, manifestDestination);
					}
				}
			}*/

			pipeline.ManifestIndex = -1;
		}

		return Task.CompletedTask;
	}

	private static void LogBundleDetails(StringBuilder logBuilder, AssetBundleBuild build)
	{
		logBuilder.AppendLine($"{build.assetBundleName}");
		foreach (var asset in build.assetNames)
		{
			var name = Path.GetFileNameWithoutExtension(asset);
			if (name.Length == 0)
				continue;
			logBuilder.AppendLine($"[{name}](assetlink://{UnityWebRequest.EscapeURL(asset)})");
			logBuilder.AppendLine();
		}

		logBuilder.AppendLine();
	}

	private static void PopulateWithExplicitAssets(IEnumerable<Object> inputAssets, List<string> outputAssets)
	{
		foreach (var asset in inputAssets)
		{
			var assetPath = AssetDatabase.GetAssetPath(asset);

			if (AssetDatabase.IsValidFolder(assetPath))
			{
				var files = Directory.GetFiles(assetPath, "*", SearchOption.AllDirectories);
				var assets = files.Select(path => AssetDatabase.LoadAssetAtPath<Object>(path));
				PopulateWithExplicitAssets(assets, outputAssets);
			}
			else if (asset is UnityPackage up)
			{
				PopulateWithExplicitAssets(up.AssetFiles, outputAssets);
			}
			else
			{
				outputAssets.Add(assetPath);
			}
		}
	}
}
#endif