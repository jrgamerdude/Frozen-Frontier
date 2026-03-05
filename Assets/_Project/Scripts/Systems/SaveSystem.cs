using System.IO;
using FrozenFrontier.Data;
using UnityEngine;

namespace FrozenFrontier.Systems
{
    public class SaveSystem : MonoBehaviour
    {
        private const int CurrentSaveVersion = 2;
        [SerializeField] private string saveFileName = "frozen_frontier_save.json";

        public string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, saveFileName);
        }

        public void Save(GameSaveData data)
        {
            if (data == null)
            {
                return;
            }

            data.saveVersion = CurrentSaveVersion;
            string path = GetSavePath();
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(path, json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SaveSystem.Save failed: {ex.Message}");
            }
        }

        public GameSaveData Load()
        {
            string path = GetSavePath();
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
                if (data == null)
                {
                    return null;
                }

                if (data.saveVersion < CurrentSaveVersion)
                {
                    Debug.LogWarning($"SaveSystem.Load ignored old save version {data.saveVersion}. Expected >= {CurrentSaveVersion}. Create a new save in this version.");
                    return null;
                }

                return data;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"SaveSystem.Load failed and will ignore save file: {ex.Message}");
                return null;
            }
        }

        public bool HasSave()
        {
            return File.Exists(GetSavePath());
        }
    }
}
