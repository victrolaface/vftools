using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProcessedAsset.Editor;
using UnityEditor;
using UnityEngine;

// ReSharper disable SuggestBaseTypeForParameter
namespace Asset.Editor
{
    using static EventArgs;
    using static AssetDatabase;
    using static EditorApplication;
    using static Task;
    using static TimeSpan;
    using static File;
    using static GUID;
    using static AssetErrorController;
    using static FilePathAttribute.Location;
    using static ProcessedAssetType;
    using static FileAttributes;

    [FilePath("Packages/com.victrolaface.tools/Asset/Editor/AssetManager.asset", ProjectFolder)]
    public class AssetManager : ScriptableSingleton<AssetManager>
    {
        [SerializeField] private List<AssetSO> assets;
        private bool _enabled;
        private int _eventsToHandle;
        private bool _raisedEvent;
        private string _assetPath;
        private List<int> _indexes;
        private List<AssetSO> _currentAssets;
        private EventHandler<EventArgs> _event;
        private event EventHandler<EventArgs> ResetGuids;
        private event EventHandler<EventArgs> ImportedAssets;
        private event EventHandler<EventArgs> DeletedAssets;
        private event EventHandler<EventArgs> MovedAssets;
        private int AssetsAmount => assets.Count;
        private bool AllEventsHandled => _eventsToHandle == 0;

        private HashSet<string> PathHashes
        {
            get
            {
                var pathHashes = new HashSet<string>();
                foreach (var asset in assets) pathHashes.Add(asset.Path);
                return pathHashes;
            }
        }

        public void OnEnable()
        {
            Enable();
        }

        private void OnValidate()
        {
            Enable();
        }

        private void OnDestroy()
        {
            OnDisable();
        }

        private void Enable()
        {
            if (_enabled) return;
            ImportedAssets += OnEventHandled;
            DeletedAssets += OnEventHandled;
            MovedAssets += OnEventHandled;
            quitting += OnDisable;
            var filter = new string[] { "Assets" };
            var pathsArr = FindAssets("t:Object", filter);
            if (AssetsAmount > 0)
            {
                var serializedIndexesToKeep = new List<int>();
                var pathIndexesToRemove = new List<int>();
                for (var idx = 0; idx < AssetsAmount; idx++)
                {
                    var serializedAsset = assets[idx];
                    for (var idxPath = 0; idxPath < pathsArr.Length; idxPath++)
                    {
                        var path = pathsArr[idxPath];
                        if (!path.Equals(serializedAsset.Path) ||
                            !GUIDFromAssetPath(path).Equals(serializedAsset.Guid)) continue;
                        serializedIndexesToKeep.Add(idx);
                        pathIndexesToRemove.Add(idxPath);
                    }
                }

                if (serializedIndexesToKeep.Count > 0)
                {
                    var serializedIndexToKeepHashes = new HashSet<int>();
                    foreach (var idx in serializedIndexesToKeep) serializedIndexToKeepHashes.Add(idx);
                    var serializedIndexesToRemove = new List<int>();
                    for (var idx = 0; idx < AssetsAmount; idx++)
                    {
                        if (serializedIndexToKeepHashes.Contains(idx)) continue;
                        serializedIndexesToRemove.Add(idx);
                    }

                    if (serializedIndexesToRemove.Count > 0)
                        foreach (var idx in serializedIndexesToRemove)
                            assets.RemoveAt(idx);
                }

                var paths = pathsArr.ToList();
                if (pathIndexesToRemove.Count > 0)
                    foreach (var idx in pathIndexesToRemove)
                        paths.RemoveAt(idx);
                if (paths.Count > 0)
                {
                    var newAssets = Assets(paths.ToArray());
                    foreach (var asset in newAssets) assets.Add(asset);
                }
            }
            else
            {
                assets = Assets(pathsArr); //new List<AssetSO>());
            }

            _eventsToHandle = 0;
            _enabled = true;
            Save(true);
        }

        private async void OnDisable()
        {
            if (!AllEventsHandled)
            {
                var awaiting = true;
                while (awaiting)
                {
                    var task = HandledAllEventsAsync();
                    await task;
                    awaiting = !task.Result;
                }
            }

            ImportedAssets -= OnEventHandled;
            DeletedAssets -= OnEventHandled;
            MovedAssets -= OnEventHandled;
            quitting -= OnDisable;
            Save(true);
        }

        private List<AssetSO> Assets(string[] paths)
        {
            if (paths.Length <= 0) return new List<AssetSO>();
            var assetItems = new List<AssetSO>();
            foreach (var path in paths)
            {
                if (InvalidPath(path)) continue;
                var guid = GUIDFromAssetPath(path);
                if (InvalidGuid(guid.ToString())) continue;
                var asset = CreateInstance<AssetSO>();
                asset.Init(path, guid);
                assetItems.Add(asset);
            }

            var idxAssets = 0;
            var pathsByIndex = new Dictionary<int, string>();
            foreach (var asset in assetItems)
            {
                for (var idx = 0; idx < assetItems.Count; idx++)
                {
                    _assetPath = assetItems[idx].Path;
                    if (InvalidGuid(assetItems[idx].Guid.ToString())) pathsByIndex.Add(idx, _assetPath);
                    if (idxAssets != idx && (_assetPath.Equals(asset.Path) || assetItems[idx].Guid.Equals(asset.Guid)))
                        pathsByIndex.Add(idx, _assetPath);
                }

                idxAssets++;
            }

            if (pathsByIndex.Count <= 0) return assetItems;
            ResetGuids += OnEventHandled;
            _eventsToHandle++;
            var guidsByIndex = new Dictionary<int, GUID>();
            StartAssetEditing();
            foreach (var kvp in pathsByIndex)
                try
                {
                    _assetPath = kvp.Value;
                    var guidAsString = GUIDFromAssetPath(_assetPath).ToString();
                    var metaPath = GetTextMetaFilePathFromAssetPath(_assetPath);
                    if (!Exists(metaPath)) throw AssetException(true, _assetPath, guidAsString);
                    var metaContent = ReadAllText(metaPath);
                    if (!metaContent.Contains(guidAsString)) throw AssetException(false, _assetPath, guidAsString);
                    if (InvalidPath(_assetPath)) continue;
                    var attributes = GetAttributes(metaPath);
                    var hidden = false;
                    if (attributes.HasFlag(Hidden))
                    {
                        hidden = true;
                        attributes &= Hidden;
                        SetAttributes(metaPath, attributes);
                    }

                    var resetGuid = Generate();
                    metaContent = metaContent.Replace(guidAsString, resetGuid.ToString());
                    WriteAllText(metaPath, metaContent);
                    if (hidden)
                    {
                        attributes |= Hidden;
                        SetAttributes(metaPath, attributes);
                    }

                    guidsByIndex.Add(kvp.Key, resetGuid);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

            StopAssetEditing();
            AssetDatabase.SaveAssets();
            Refresh();
            _event = ResetGuids;
            _event?.Invoke(this, Empty);
            ResetGuids -= OnEventHandled;
            foreach (var kvp in guidsByIndex) assetItems[kvp.Key].Guid = kvp.Value;
            return assetItems;
        }

        private static bool InvalidPath(string path)
        {
            return path.EndsWith(".unity") || GetAttributes(path).HasFlag(Directory) || string.IsNullOrEmpty(path);
        }

        private static bool InvalidGuid(string guid)
        {
            return guid.Equals("0") || string.IsNullOrEmpty(guid);
        }

        private void OnEventHandled(object sender, EventArgs e)
        {
            _eventsToHandle--;
        }

        private async Task<bool> HandledAllEventsAsync()
        {
            var tcs = new TaskCompletionSource<bool>(false);
            while (!AllEventsHandled)
            {
                await Delay(FromMilliseconds(100));
                tcs.TrySetResult(AllEventsHandled);
            }

            return tcs.Task.Result;
        }

        private bool OnProcessAssets(int pathsAmount)
        {
            if (pathsAmount <= 0) return false;
            _eventsToHandle++;
            return true;
        }

        private static bool OnRaiseEvent(int assetsAmount)
        {
            return assetsAmount > 0;
        }

        public void Assets(ProcessedAssetType assetType, string[] paths)
        {
            if (!OnProcessAssets(paths.Length)) return;
            switch (assetType)
            {
                case Imported:
                    _currentAssets = Assets(paths);
                    _raisedEvent = OnRaiseEvent(_currentAssets.Count);
                    if (_raisedEvent)
                    {
                        foreach (var asset in _currentAssets) assets.Add(asset);
                        _event = ImportedAssets;
                        _event?.Invoke(this, Empty);
                    }

                    break;
                case Deleted:
                    _indexes = new List<int>();
                    var pathHashes = new HashSet<string>();
                    foreach (var path in paths) pathHashes.Add(path);
                    for (var idx = 0; idx < AssetsAmount; idx++)
                    {
                        var assetPath = data.Path(idx);
                        if (pathHashes.Contains(assetPath)) _indexes.Add(idx);
                    }

                    _raisedEvent = OnRaiseEvent(_indexes.Count);
                    if (_raisedEvent)
                    {
                        foreach (var idx in _indexes) data.RemoveAt(idx);
                        _event = DeletedAssets;
                        _event?.Invoke(this, Empty);
                    }

                    break;
                case Moved: throw AssetOutOfRangeException(Moved);
                default: throw AssetOutOfRangeException(null);
            }

            OnRaisedEvent();
        }

        public void Assets(ProcessedAssetType assetType, string[] assetPaths, string[] fromPaths)
        {
            if (!OnProcessAssets(assetPaths.Length)) return;
            switch (assetType)
            {
                case Moved:
                    var newPathsAmount = 0;
                    var pathsAmount = assetPaths.Length;
                    for (var idxPaths = 0; idxPaths < pathsAmount; idxPaths++)
                    for (var idx = 0; idx < AssetsAmount; idx++)
                    {
                        _assetPath = data.Path(idx);
                        if (!_assetPath.Equals(fromPaths[idxPaths])) continue;
                        var newPath = assetPaths[idxPaths];
                        data.Path(idx, newPath);
                        newPathsAmount++;
                    }

                    _raisedEvent = OnRaiseEvent(newPathsAmount) && newPathsAmount == pathsAmount;
                    if (_raisedEvent)
                    {
                        _event = MovedAssets;
                        _event?.Invoke(this, Empty);
                    }

                    break;
                case Imported: throw AssetOutOfRangeException(Imported);
                case Deleted: throw AssetOutOfRangeException(Deleted);
                default: throw AssetOutOfRangeException(null);
            }

            OnRaisedEvent();
        }

        private void OnRaisedEvent()
        {
            if (_raisedEvent) Save(true);
        }
    }
}