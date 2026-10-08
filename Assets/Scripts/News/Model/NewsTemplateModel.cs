using QFramework;
using System.Collections.Generic;
using UnityEngine;
namespace TapTapFirst
{
    // 已生成模块接口 INewsTemplateModel，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterModel<INewsTemplateModel>(new NewsTemplateModel());
    public interface INewsTemplateModel : IModel
    {
        public NewsTemplate_SO newsList { get; }
    }

    public class NewsTemplateModel : AbstractModel, INewsTemplateModel
    {

        public NewsTemplate_SO newsList { get; private set; }
        NewsTemplate_SO INewsTemplateModel.newsList => newsList;


        protected override void OnInit()
        {
            // 初始化newsList
            //newsList = Resources.Load<NewsTemplateList_SO>("NewsTemplateList");
            Debug.LogWarning("记得初始化新闻列表，传入Resources获取位置");
        }
    }
    public class NewsTemplateSaveData
    {
        // 只存 ID。
        // Unity 序列化 / JsonUtility 不支持 HashSet<T>，也不支持保存 NewsTemplate_SO 这类资源引用，
        // 存进去的 SO 列表读档后会变成空对象。已获得的新闻由 NewsSystem 用这里的 ID 反查配置。
        public List<int> playerAcquiredIds = new List<int>();
    }
}
