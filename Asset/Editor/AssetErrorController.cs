using System;
using ProcessedAsset.Editor;

// ReSharper disable ConvertSwitchStatementToSwitchExpression
namespace Asset.Editor
{
    using static ProcessedAssetType;

    public static class AssetErrorController
    {
        public static Exception AssetException(bool metaFileIsNull, string path, string guid)
        {
            var assetAtPathGuidNot = $"asset at path: {path} with GUID: {guid} does not";
            const string file = "meta file";
            var e = metaFileIsNull
                ? $"The {file} of selected {assetAtPathGuidNot} exist."
                : $"GUID of {assetAtPathGuidNot} match GUID in asset's {file}.";
            return new Exception(e);
        }

        public static ArgumentOutOfRangeException AssetOutOfRangeException(ProcessedAssetType? assetType)
        {
            string msg;
            const string typeName = nameof(ProcessedAssetType);
            const string param = "parameter";
            const string methodOf = "Assets method of AssetManager";
            const string typeStringArr = "of type String Array";
            const string enumType = "Enum parameter of type";
            const string wValue = "with value of";
            var methodClassPassed = $"{methodOf} class";
            var methodOfTakes = $"{methodOf} takes";
            var @params = $"{param}s";
            var oneParam = $"one {param}";
            var twoParams = $"two {@params}";
            var inRangeParams = $"incorrect range of {@params}";
            var methodClassPassedInvalidTypeWithVal = $"{methodClassPassed} invalid {enumType} {typeName} {wValue}";
            var methodPassedInRangeParams = $"{methodClassPassed} {inRangeParams} {typeStringArr}. " +
                                            $"{methodOfTakes} ";
            var enumTypeNameWithVal = $"{typeStringArr} with {enumType} {typeName} {wValue} {assetType}";
            var importedOrDeletedMsg = $"{methodPassedInRangeParams} {oneParam} {enumTypeNameWithVal}";
            switch (assetType)
            {
                case Imported:
                    msg = $"{importedOrDeletedMsg}";
                    break;
                case Deleted:
                    msg = $"{importedOrDeletedMsg}";
                    break;
                case Moved:
                    msg = $"{methodPassedInRangeParams} {twoParams} {enumTypeNameWithVal}";
                    break;
                case null:
                    msg = $"{methodClassPassedInvalidTypeWithVal} null.";
                    break;
                default:
                    msg = $"{methodClassPassedInvalidTypeWithVal} {assetType}.";
                    break;
            }

            return new ArgumentOutOfRangeException(typeName, assetType, msg);
        }
    }
}