using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "AssetBundle Directory List", menuName = "Receiver 2 Modding/AssetBundle Directory List", order = 3)]
public class AssetBundleDirectoryList : ScriptableObject {
    [System.Serializable]
    public class AssetBundleDirectoryTuple {
        public string assetBundleName;
        public string path;
    }

    [HideInInspector]
    [SerializeField]
    public List<AssetBundleDirectoryTuple> directoryTuples = new List<AssetBundleDirectoryTuple>();

    public void OnEnable() {
        var tempTuples = new List<AssetBundleDirectoryTuple>(directoryTuples);

        foreach (var tuple in tempTuples) {
            if (!AssetDatabase.GetAllAssetBundleNames().Contains(tuple.assetBundleName)) directoryTuples.Remove(tuple);
        }

        foreach (var name in AssetDatabase.GetAllAssetBundleNames()) {
            if (!directoryTuples.Any(tuple => { return tuple.assetBundleName == name; })) directoryTuples.Add(new AssetBundleDirectoryTuple() { assetBundleName = name });
        }
    }

    public string getPath(string assetBundleName) {
        return directoryTuples.First(tuple => { return tuple.assetBundleName == assetBundleName; }).path;
    }

    public bool hasPath(string assetBundleName) {
        bool flag = directoryTuples.Any(tuple => { return tuple.assetBundleName == assetBundleName; });
        if (!flag) return false;
        else return getPath(assetBundleName) != "";
    }
}
