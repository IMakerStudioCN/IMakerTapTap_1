using QFramework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TapTapFirst
{
    public sealed class SaveSlotInfo
    {
        public int SlotIndex { get; internal set; }
        public string DisplayName { get; internal set; }
        public bool HasSave { get; internal set; }
        public DateTime? LastSaveTime { get; internal set; }
        public string FilePath { get; internal set; }
    }

    // 已生成模块接口 IJsonSaveUtility，请在 Architecture.Init() 中按接口类型注册：
    //存储用纯C#类型，分立开写
    //Model放游戏运行时不需要保存的值
    //纯C#放需要保存的值

    //public interface ITestModel : IModel
    //{
    //    int testintValue { get; set; }
    //}
    //public class TestModel : AbstractModel, ITestModel
    //{
    //    public int testintValue { get; set; } = 1;
    //    protected override void OnInit()
    //    {
    //    }
    //}
    //[System.Serializable]
    //public class TestSaveModel
    //{
    //    public int testintValue;
    //}

    public interface IJsonSaveUtility : IUtility
    {
        // ===== 加入纯 C# 类型 =====
        void Add<T>() where T : class, new();
        void Add<T>(string key) where T : class, new();
        void Add<T>(T data) where T : class;
        void Add<T>(string key, T data) where T : class;

        // ===== 减去纯 C# 类型 =====
        bool Remove(string key);
        bool Remove<T>();
        void Clear();

        // ===== 一键存储 / 一键读取 =====
        void Save();
        void Load();

        // ===== 三槽位存档 =====
        void SelectSlot(int slotIndex);
        SaveSlotInfo GetSlotInfo(int slotIndex);
        void SetSlotName(int slotIndex, string displayName);

        // ===== 取回纯 C# 数据（Model 形态）=====
        T Get<T>(string key) where T : class;
        bool TryGet<T>(string key, out T data) where T : class;

        int Count { get; }
        int CurrentSlot { get; }
        string FilePath { get; }
    }

    public class JsonSaveUtility : IJsonSaveUtility
    {
        private const string DefaultFileName = "SaveData";

        [Serializable]
        private class SaveEntry
        {
            public string key;
            public string type;
            public string json;
        }

        [Serializable]
        //用来简化存档存为一个Json文件
        private class SaveFile
        {
            public List<SaveEntry> entries = new List<SaveEntry>();
        }

        private readonly Dictionary<string, object> mSaveDataCache = new Dictionary<string, object>();
        private readonly Dictionary<string, Type> mRegisteredTypes = new Dictionary<string, Type>();
        private readonly string mBaseFileName;
        private int mCurrentSlot = 1;

        public JsonSaveUtility() : this(DefaultFileName)
        {
        }

        public JsonSaveUtility(string fileName)
        {
            mBaseFileName = string.IsNullOrWhiteSpace(fileName) ? DefaultFileName : fileName;
        }

        public int Count => mSaveDataCache.Count;

        public int CurrentSlot => mCurrentSlot;

        public string FilePath => GetSlotFilePath(mCurrentSlot);

        // ===== 加入纯 C# 类型 =====

        public void Add<T>() where T : class, new()
        {
            Add(typeof(T).Name, new T());
        }

        public void Add<T>(string key) where T : class, new()
        {
            Add(key, new T());
        }

        public void Add<T>(T data) where T : class
        {
            Add(typeof(T).Name, data);
        }

        public void Add<T>(string key, T data) where T : class
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[JsonSaveUtility] key 为空，已忽略");
                return;
            }

            if (data == null)
            {
                Debug.LogWarning($"[JsonSaveUtility] {key} 数据为 null，已忽略");
                return;
            }

            mSaveDataCache[key] = data;
            mRegisteredTypes[key] = data.GetType();
        }

        // ===== 三槽位存档 =====

        public void SelectSlot(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            mCurrentSlot = slotIndex;
            ResetRegisteredData();
            Load();
        }

        public SaveSlotInfo GetSlotInfo(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            string path = GetSlotFilePath(slotIndex);
            bool hasSave = File.Exists(path);
            return new SaveSlotInfo
            {
                SlotIndex = slotIndex,
                DisplayName = PlayerPrefs.GetString(GetSlotNameKey(slotIndex), $"存档 {slotIndex}"),
                HasSave = hasSave,
                LastSaveTime = hasSave ? File.GetLastWriteTime(path) : (DateTime?)null,
                FilePath = path
            };
        }

        public void SetSlotName(int slotIndex, string displayName)
        {
            ValidateSlotIndex(slotIndex);
            string finalName = string.IsNullOrWhiteSpace(displayName)
                ? $"存档 {slotIndex}"
                : displayName.Trim();
            PlayerPrefs.SetString(GetSlotNameKey(slotIndex), finalName);
            PlayerPrefs.Save();
        }

        // ===== 减去纯 C# 类型 =====

        public bool Remove(string key)
        {
            return !string.IsNullOrEmpty(key) && mSaveDataCache.Remove(key);
        }

        public bool Remove<T>()
        {
            return Remove(typeof(T).Name);
        }

        public void Clear()
        {
            mSaveDataCache.Clear();
        }

        // ===== 一键存储 / 一键读取 =====

        public void Save()
        {
            try
            {
                SaveFile file = new SaveFile();

                foreach (KeyValuePair<string, object> pair in mSaveDataCache)
                {
                    file.entries.Add(new SaveEntry
                    {
                        key = pair.Key,
                        //通过映射获取纯C#类
                        type = pair.Value.GetType().AssemblyQualifiedName,
                        json = JsonUtility.ToJson(pair.Value)
                    });
                }

                string directory = Path.GetDirectoryName(FilePath);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(FilePath, JsonUtility.ToJson(file, true));

                Debug.Log($"[JsonSaveUtility] 已存储槽位 {mCurrentSlot} 的 {file.entries.Count} 条数据 -> {FilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonSaveUtility] 存储失败 -> {FilePath}\n{e}");
            }
        }

        public void Load()
        {
            if (!File.Exists(FilePath))
            {
                Debug.Log($"[JsonSaveUtility] 槽位 {mCurrentSlot} 为空，将使用初始数据");
                return;
            }

            SaveFile file;

            try
            {
                file = JsonUtility.FromJson<SaveFile>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonSaveUtility] 存档损坏，已跳过读取 -> {FilePath}\n{e.Message}");
                return;
            }

            if (file == null || file.entries == null)
            {
                Debug.LogWarning($"[JsonSaveUtility] 存档内容无法解析：{FilePath}");
                return;
            }

            int successCount = 0;

            foreach (SaveEntry entry in file.entries)
            {
                if (entry == null)
                {
                    continue;
                }

                try
                {
                    Type type = ResolveType(entry.type);

                    if (type == null)
                    {
                        Debug.LogWarning($"[JsonSaveUtility] 找不到类型 {entry.type}，跳过 {entry.key}");
                        continue;
                    }

                    // 游戏开始已用 Add<T>() 提前登记过：原地覆盖，引用保持不变
                    if (mSaveDataCache.TryGetValue(entry.key, out object exist) && exist != null && exist.GetType() == type)
                    {
                        JsonUtility.FromJsonOverwrite(entry.json, exist);
                    }
                    else
                    {
                        mSaveDataCache[entry.key] = JsonUtility.FromJson(entry.json, type);
                    }

                    successCount++;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[JsonSaveUtility] {entry.key} 读取失败，已跳过：{e.Message}");
                }
            }

            Debug.Log($"[JsonSaveUtility] 已读取槽位 {mCurrentSlot} 的 {successCount}/{file.entries.Count} 条数据 -> {FilePath}");
        }

        // ===== 取回纯 C# 数据（Model 形态）=====

        public T Get<T>(string key) where T : class
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            return mSaveDataCache.TryGetValue(key, out object data) && data is T value ? value : null;
        }

        public bool TryGet<T>(string key, out T data) where T : class
        {
            data = Get<T>(key);
            return data != null;
        }

        private void ResetRegisteredData()
        {
            foreach (KeyValuePair<string, Type> pair in mRegisteredTypes)
            {
                try
                {
                    mSaveDataCache[pair.Key] = Activator.CreateInstance(pair.Value);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[JsonSaveUtility] 无法重置 {pair.Key}：{e.Message}");
                }
            }
        }

        private string GetSlotFilePath(int slotIndex)
        {
            // 槽位 1 沿用旧文件名，兼容已经存在的 SaveData.json。
            string suffix = slotIndex == 1 ? string.Empty : $"_Slot{slotIndex}";
            return Path.Combine(Application.persistentDataPath, mBaseFileName + suffix + ".json");
        }

        private string GetSlotNameKey(int slotIndex)
        {
            return $"{mBaseFileName}.Slot{slotIndex}.DisplayName";
        }

        private static void ValidateSlotIndex(int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), "存档槽位只能是 1、2、3");
            }
        }

        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            Type type = Type.GetType(typeName);

            if (type != null)
            {
                return type;
            }

            string fullName = typeName.Split(',')[0].Trim();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName);

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
