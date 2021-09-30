using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Asset.Editor
{
    [CreateAssetMenu(order = 0, fileName = "AssetManagerDataSO", menuName = "VFTools/Asset Manager/Asset Manager Data")]
    public class AssetManagerDataSO : ScriptableObject
    {
        public void OnEnable()
        {
            if (AssetsNotNull && AssetsAmount <= 0) assets?.Clear();
            else assets = new List<AssetSO>();
        }

        /*public bool TryGetAssets(string[] paths, out List<AssetSO> assetList)
        {
            var match = true;
            foreach (var path in paths)
            {
                for (var idx = 0; idx < AssetsAmount; idx++)
                {
                    
                }
            }
        }*/

        public bool Loaded => AssetsNotNull && AssetsAmount > 0;

        private bool AssetsNotNull => assets != null;

        [SerializeField] [CanBeNull] private List<AssetSO> assets;

        public void Load(List<AssetSO> loadedAssets)
        {
            assets = loadedAssets;
        }
        
        //public bool Initialized => assets != null && AssetsAmount > 0;

        public List<AssetSO> Assets
        {
            set => assets = value;
        }

        //public int AssetsAmount => assets.Count;

        public string Path(int index)
        {
            return assets[index].Path;
        }

        public void RemoveAt(int index)
        {
            assets.RemoveAt(index);
        }

        public void Add(AssetSO asset)
        {
            assets.Add(asset);
        }

        public void Path(int index, string path)
        {
            assets[index].Path = path;
        }
    }
}