using UnityEditor;
using UnityEngine;
using static UnityEditor.AssetDatabase;

namespace Asset.Editor
{
    public class AssetSO : ScriptableObject, IAssetSO
    {
        public GUID Guid { get; set; }
        public string Path { get; set; }

        public void Init(string path, GUID guid)
        {
            Path = path;
            Guid = guid;
        }
    }
}