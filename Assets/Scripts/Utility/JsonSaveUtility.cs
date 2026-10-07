using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        bool TrySelectLatestPlayedSlot();
        bool DeleteSlot(int slotIndex);
        SaveSlotInfo GetSlotInfo(int slotIndex);
        void SetSlotName(int slotIndex, string displayName);

        // ===== 取回纯 C# 数据（Model 形态）=====
        T Get<T>(string key) where T : class;
        bool TryGet<T>(string key, out T data) where T : class;

        int Count { get; }
        int CurrentSlot { get; }
        int LatestPlayedSlot { get; }
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

        public int LatestPlayedSlot => FindLatestPlayedSlot();

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
            SetLatestPlayedSlot(slotIndex);
        }

        public bool TrySelectLatestPlayedSlot()
        {
            int slotIndex = FindLatestPlayedSlot();
            if (slotIndex == 0)
            {
                return false;
            }

            SelectSlot(slotIndex);
            return true;
        }

        public bool DeleteSlot(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            string path = GetSlotFilePath(slotIndex);

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonSaveUtility] 删除槽位 {slotIndex} 失败 -> {path}\n{e}");
                return false;
            }

            PlayerPrefs.DeleteKey(GetSlotNameKey(slotIndex));

            if (PlayerPrefs.GetInt(GetLatestPlayedSlotKey(), 0) == slotIndex)
            {
                int fallbackSlot = FindMostRecentlyModifiedSlot();
                if (fallbackSlot == 0)
                {
                    PlayerPrefs.DeleteKey(GetLatestPlayedSlotKey());
                }
                else
                {
                    PlayerPrefs.SetInt(GetLatestPlayedSlotKey(), fallbackSlot);
                }
            }

            PlayerPrefs.Save();

            if (mCurrentSlot == slotIndex)
            {
                ResetRegisteredData();
            }

            Debug.Log($"[JsonSaveUtility] 已删除槽位 {slotIndex} -> {path}");
            return true;
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

                    // 已登记的数据始终保留同一个对象实例，并原地更新集合内容。
                    // 这样系统即使缓存过 List/HashSet 等引用，切换槽位后也不会继续读写旧槽位。
                    if (mSaveDataCache.TryGetValue(entry.key, out object exist) && exist != null && exist.GetType() == type)
                    {
                        object loaded = JsonUtility.FromJson(entry.json, type);

                        if (loaded == null)
                        {
                            Debug.LogWarning($"[JsonSaveUtility] {entry.key} 内容为空，已跳过");
                            continue;
                        }

                        CopySerializedDataPreservingReferences(exist, loaded);
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
            // 移除旧版本遗留、但当前版本未登记的条目，避免它们被写进另一个槽位。
            List<string> cachedKeys = new List<string>(mSaveDataCache.Keys);
            foreach (string key in cachedKeys)
            {
                if (!mRegisteredTypes.ContainsKey(key))
                {
                    mSaveDataCache.Remove(key);
                }
            }

            foreach (KeyValuePair<string, Type> pair in mRegisteredTypes)
            {
                try
                {
                    object defaultData = Activator.CreateInstance(pair.Value);

                    if (mSaveDataCache.TryGetValue(pair.Key, out object currentData) &&
                        currentData != null &&
                        currentData.GetType() == pair.Value)
                    {
                        CopySerializedDataPreservingReferences(currentData, defaultData);
                    }
                    else
                    {
                        mSaveDataCache[pair.Key] = defaultData;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[JsonSaveUtility] 无法重置 {pair.Key}：{e.Message}");
                }
            }
        }

        /// <summary>
        /// 将 source 的可序列化字段复制到 target，同时保留 target 中已有对象和集合的引用。
        /// </summary>
        private static void CopySerializedDataPreservingReferences(object target, object source)
        {
            if (target == null || source == null || target.GetType() != source.GetType())
            {
                return;
            }

            CopySerializedDataPreservingReferences(
                target,
                source,
                new HashSet<object>(ReferenceEqualityComparer.Instance));
        }

        private static void CopySerializedDataPreservingReferences(
            object target,
            object source,
            HashSet<object> visitedTargets)
        {
            if (!visitedTargets.Add(target))
            {
                return;
            }

            foreach (FieldInfo field in GetSerializableFields(target.GetType()))
            {
                object sourceValue = field.GetValue(source);
                object targetValue = field.GetValue(target);

                if (sourceValue == null)
                {
                    if (!TryClearCollection(targetValue))
                    {
                        field.SetValue(target, null);
                    }

                    continue;
                }

                Type fieldType = field.FieldType;

                if (ShouldAssignDirectly(fieldType) || targetValue == null)
                {
                    field.SetValue(target, sourceValue);
                    continue;
                }

                if (TryCopyCollectionContents(targetValue, sourceValue))
                {
                    continue;
                }

                if (fieldType.IsArray || typeof(IEnumerable).IsAssignableFrom(fieldType))
                {
                    field.SetValue(target, sourceValue);
                    continue;
                }

                if (targetValue.GetType() == sourceValue.GetType())
                {
                    CopySerializedDataPreservingReferences(targetValue, sourceValue, visitedTargets);
                    continue;
                }

                field.SetValue(target, sourceValue);
            }
        }

        private static IEnumerable<FieldInfo> GetSerializableFields(Type type)
        {
            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                FieldInfo[] fields = current.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                foreach (FieldInfo field in fields)
                {
                    if (field.IsStatic || field.IsInitOnly || field.IsNotSerialized)
                    {
                        continue;
                    }

                    if (field.IsPublic || field.IsDefined(typeof(SerializeField), true))
                    {
                        yield return field;
                    }
                }
            }
        }

        private static bool ShouldAssignDirectly(Type type)
        {
            return type.IsValueType ||
                   type == typeof(string) ||
                   typeof(UnityEngine.Object).IsAssignableFrom(type);
        }

        private static bool TryClearCollection(object value)
        {
            if (value == null || value is string || value.GetType().IsArray)
            {
                return false;
            }

            if (value is IDictionary dictionary)
            {
                if (dictionary.IsReadOnly)
                {
                    return false;
                }

                dictionary.Clear();
                return true;
            }

            if (value is IList list)
            {
                if (list.IsReadOnly || list.IsFixedSize)
                {
                    return false;
                }

                list.Clear();
                return true;
            }

            MethodInfo clearMethod = value.GetType().GetMethod(
                "Clear",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                Type.EmptyTypes,
                null);

            if (clearMethod == null)
            {
                return false;
            }

            clearMethod.Invoke(value, null);
            return true;
        }

        private static bool TryCopyCollectionContents(object target, object source)
        {
            if (ReferenceEquals(target, source))
            {
                return true;
            }

            if (target is Array targetArray && source is Array sourceArray)
            {
                if (targetArray.Rank != 1 || sourceArray.Rank != 1 || targetArray.Length != sourceArray.Length)
                {
                    return false;
                }

                for (int i = 0; i < sourceArray.Length; i++)
                {
                    targetArray.SetValue(sourceArray.GetValue(i), i);
                }

                return true;
            }

            if (target is IDictionary targetDictionary && source is IDictionary sourceDictionary)
            {
                if (targetDictionary.IsReadOnly)
                {
                    return false;
                }

                targetDictionary.Clear();
                foreach (DictionaryEntry entry in sourceDictionary)
                {
                    targetDictionary.Add(entry.Key, entry.Value);
                }

                return true;
            }

            if (target is IList targetList && source is IEnumerable sourceEnumerable)
            {
                if (targetList.IsReadOnly || targetList.IsFixedSize)
                {
                    return false;
                }

                targetList.Clear();
                foreach (object item in sourceEnumerable)
                {
                    targetList.Add(item);
                }

                return true;
            }

            if (target is string || !(source is IEnumerable enumerable))
            {
                return false;
            }

            Type targetType = target.GetType();
            MethodInfo clearMethod = targetType.GetMethod(
                "Clear",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                Type.EmptyTypes,
                null);
            MethodInfo addMethod = null;

            foreach (MethodInfo method in targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (method.Name == "Add" && method.GetParameters().Length == 1)
                {
                    addMethod = method;
                    break;
                }
            }

            if (clearMethod == null || addMethod == null)
            {
                return false;
            }

            clearMethod.Invoke(target, null);
            foreach (object item in enumerable)
            {
                addMethod.Invoke(target, new[] { item });
            }

            return true;
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            private ReferenceEqualityComparer()
            {
            }

            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
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

        private string GetLatestPlayedSlotKey()
        {
            return $"{mBaseFileName}.LatestPlayedSlot";
        }

        private int FindLatestPlayedSlot()
        {
            int storedSlot = PlayerPrefs.GetInt(GetLatestPlayedSlotKey(), 0);
            if (IsValidSlotIndex(storedSlot) && File.Exists(GetSlotFilePath(storedSlot)))
            {
                return storedSlot;
            }

            return FindMostRecentlyModifiedSlot();
        }

        private int FindMostRecentlyModifiedSlot()
        {
            int latestSlot = 0;
            DateTime latestWriteTime = DateTime.MinValue;

            for (int slotIndex = 1; slotIndex <= 3; slotIndex++)
            {
                string path = GetSlotFilePath(slotIndex);
                if (!File.Exists(path))
                {
                    continue;
                }

                DateTime writeTime = File.GetLastWriteTimeUtc(path);
                if (latestSlot == 0 || writeTime > latestWriteTime)
                {
                    latestSlot = slotIndex;
                    latestWriteTime = writeTime;
                }
            }

            return latestSlot;
        }

        private void SetLatestPlayedSlot(int slotIndex)
        {
            PlayerPrefs.SetInt(GetLatestPlayedSlotKey(), slotIndex);
            PlayerPrefs.Save();
        }

        private static bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 1 && slotIndex <= 3;
        }

        private static void ValidateSlotIndex(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
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
