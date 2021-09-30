using UnityEditor;

namespace Startup.Editor
{
    internal class StartupManager : UnityEditor.Editor
    {
        [InitializeOnLoadMethod]
        private static void Start()
        {
            StartupController.instance.OnEnable();
        }
    }
}