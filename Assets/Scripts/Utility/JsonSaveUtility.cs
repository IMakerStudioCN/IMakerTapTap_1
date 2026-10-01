using QFramework;

namespace TapTapFirst
{
    // 已生成模块接口 IJsonSaveUtility，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterUtility<IJsonSaveUtility>(new JsonSaveUtility());
    public interface IJsonSaveUtility : IUtility
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
        public void SaveToJson<T>(string filePath, T data);
        public void LoadFromJson<T>(string filePath, out T data);
    }

    public class JsonSaveUtility : IJsonSaveUtility
    {
        void IJsonSaveUtility.LoadFromJson<T>(string filePath, out T data)
        {
            throw new System.NotImplementedException();
        }

        void IJsonSaveUtility.SaveToJson<T>(string filePath, T data)
        {
            throw new System.NotImplementedException();
        }
    }
}
