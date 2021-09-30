using System.Collections.Generic;
using System.Linq;
using Asset.Editor;
using UnityEditor;

namespace ProcessedAsset.Editor
{
    using static ProcessedAssetType;
    using static ScriptableSingleton<AssetManager>;

    internal class PostProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            var hasImportedAssets = importedAssets.Length > 0;
            var hasDeletedAssets = deletedAssets.Length > 0;
            var movedAssetsAmount = movedAssets.Length;
            var movedFromAssetsAmount = movedFromAssetPaths.Length;
            var hasMovedAssets = movedAssetsAmount > 0 && movedFromAssetsAmount > 0 &&
                                 movedAssetsAmount == movedFromAssetsAmount &&
                                 movedFromAssetsAmount - movedAssetsAmount == 0;
            var hasAssetsOfType = new List<bool> { hasImportedAssets, hasDeletedAssets, hasMovedAssets };
            var signalsToEmit = hasAssetsOfType.Count(hasAssetOfType => hasAssetOfType);
            var hasSignalsToEmit = signalsToEmit > 0;
            if (hasAssetsOfType.Count <= 0 && !hasSignalsToEmit && !hasImportedAssets && !hasDeletedAssets &&
                !hasMovedAssets) return;
            if (hasImportedAssets) instance.Assets(Imported, importedAssets);
            if (hasDeletedAssets) instance.Assets(Deleted, deletedAssets);
            if (hasMovedAssets) instance.Assets(Moved, movedAssets, movedFromAssetPaths);
        }
    }
}