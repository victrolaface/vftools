using UnityEditor;

namespace Asset.Editor
{
    public interface IAssetSO
    {
        GUID Guid { get; }
        string Path { get; }
        public void Init(string path, GUID guid);
    }
}