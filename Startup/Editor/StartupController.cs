using Asset.Editor;
using UnityEditor;

namespace Startup.Editor
{
    public class StartupController : ScriptableSingleton<StartupController>
    {
        private bool _enabled;

        internal void OnEnable()
        {
            Enable();
        }

        private void OnValidate()
        {
            Enable();
        }

        private void Enable()
        {
            if (_enabled) return;
            AssetManager.instance.OnEnable();
            _enabled = true;
        }
    }
}