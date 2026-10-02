using QFramework;
using System.IO;
using UnityEngine;

namespace TapTapFirst
{
    // 已生成模块接口 IJsonSaveUtility，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterUtility<IJsonSaveUtility>(new JsonSaveUtility());
    public interface IJsonSaveUtility : IUtility
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
        public void SaveToJson<T>(string fileName, T data);
        public void LoadFromJson<T>(string fileName, out T data);
    }

    public class JsonSaveUtility : IJsonSaveUtility
    {
        //写到最前面：如果后期有Dictionary这类哈希表要保存是要加代码的
        //保存API
        void IJsonSaveUtility.SaveToJson<T>(string fileName, T data)
        {
            string path = Path.Combine(Application.persistentDataPath, fileName + ".json");
            string jsonString = JsonUtility.ToJson(data);
            File.WriteAllText(path, jsonString);
        }
        //读取API
        void IJsonSaveUtility.LoadFromJson<T>(string fileName, out T data)
        {
            string path = Path.Combine(Application.persistentDataPath, fileName + ".json");
            if (File.Exists(path))
            {
                string jsonString = File.ReadAllText(path);
                data = JsonUtility.FromJson<T>(jsonString);
            }
            else
            {
                data = default(T);
            }
        }

        
    }
}
