#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FrozenFrontier.Util.Editor
{
    public static class FrozenFrontierMaintenanceTools
    {
        private const string SaveFileName = "frozen_frontier_save.json";

        [MenuItem("Tools/Frozen Frontier/Clear Save File")]
        public static void ClearSaveFile()
        {
            string path = Path.Combine(Application.persistentDataPath, SaveFileName);
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("Frozen Frontier", $"No save file found.\n{path}", "OK");
                return;
            }

            try
            {
                File.Delete(path);
                EditorUtility.DisplayDialog("Frozen Frontier", $"Save file deleted:\n{path}", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Frozen Frontier", $"Failed to delete save file.\n{ex.Message}", "OK");
            }
        }
    }
}
#endif
