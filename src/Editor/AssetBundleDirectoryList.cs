#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "AssetBundle Directory List", menuName = "Receiver 2 Modding/AssetBundle Directory List", order = 3)]
public class AssetBundleDirectoryList : ScriptableObject {
    [System.Serializable]
    public class AssetBundleDirectoryTuple {
        public string asset_bundle_name;
        public string path;
    }

    //[HideInInspector]
    [SerializeField]
    public List<AssetBundleDirectoryTuple> directory_tuples = new List<AssetBundleDirectoryTuple>();

	public void Refresh() {
		directory_tuples.RemoveAll(tuple => !AssetDatabase.GetAllAssetBundleNames().Contains(tuple.asset_bundle_name));

        foreach (var name in AssetDatabase.GetAllAssetBundleNames()) {
            if (!directory_tuples.Any(tuple => { return tuple.asset_bundle_name == name; })) directory_tuples.Add(new AssetBundleDirectoryTuple() { asset_bundle_name = name });
        }
	}

    public void OnEnable() {
		Refresh();
    }

    public string GetPath(string assetbundle_name) {
        return directory_tuples.First(tuple => { return tuple.asset_bundle_name == assetbundle_name; }).path;
    }

    public bool hasPath(string assetbundle_name) {
        bool flag = directory_tuples.Any(tuple => { return tuple.asset_bundle_name == assetbundle_name; });
        if (!flag) return false;
        else return GetPath(assetbundle_name) != "";
    }
}

#endif