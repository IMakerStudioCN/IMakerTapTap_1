using QFramework;
using System.Collections.Generic;
using UnityEngine;
namespace TapTapFirst
{
    // 已生成模块接口 INewsTemplateModel，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterModel<INewsTemplateModel>(new NewsTemplateModel());
    public interface INewsTemplateModel : IModel
    {
        IReadOnlyList<NewsTemplate_SO> AcquriedNews { get; }
        NewsTemplate_SO GetConfig(int newsId);
        bool IsAcquired(int newsId);
        bool CanUse(int newsId);
        void Acquire(int newsId);
    }

    public class NewsTemplateModel : AbstractModel, INewsTemplateModel
    {
        JsonSaveUtility saveUtility => this.GetUtility<IJsonSaveUtility>() as JsonSaveUtility;
        private NewsTemplateList_SO newsList; //拥有所有新闻模板的列表
        //model存储已经改为JsonSaveUtility存储，model只存储运行时的值
        //private readonly List<NewsTemplate_SO> mAcquired2 = new List<NewsTemplate_SO>();//已获得的新闻模板列表
        //private readonly HashSet<int> mAcquiredIds2 = new HashSet<int>();//已获得的新闻模板ID集合,快速查找是否已获得

        public IReadOnlyList<NewsTemplate_SO> AcquriedNews => saveUtility.Get<NewsTemplateSaveData>("NewsTemplateSaveData").playerAcquired;//已获得的新闻模板列表
        public NewsTemplate_SO GetConfig(int newsId) => newsList.GetNewsTemplateById(newsId);//根据ID获取新闻模板
        public bool IsAcquired(int newsId) => saveUtility.Get<NewsTemplateSaveData>("NewsTemplateSaveData").playerAcquiredIds.Contains(newsId);//是否已获得，这个方法不用调用
        public bool CanUse(int newsId) => IsAcquired(newsId);//是否可以使用，直接调用这个方法
        //例子:if(CanUse(newsId))=>GetConfig(newsId)获取新闻模板配置

        // 这个方法用于获取新闻模板的配置，如果已经获得则返回对应的配置，否则返回null
        public void Acquire(int newsId)
        {
            if (IsAcquired(newsId))
                return;
            var news = GetConfig(newsId);
            if (news != null)
            {
                saveUtility.Get<NewsTemplateSaveData>("NewsTemplateSaveData").playerAcquired.Add(news);
                saveUtility.Get<NewsTemplateSaveData>("NewsTemplateSaveData").playerAcquiredIds.Add(newsId);
            }
        }
        protected override void OnInit()
        {
            // 初始化newsList
            //newsList = Resources.Load<NewsTemplateList_SO>("NewsTemplateList");
            Debug.LogWarning("记得初始化新闻列表，传入Resources获取位置");
        }
    }
    public class NewsTemplateSaveData
    {
        public List<NewsTemplate_SO> playerAcquired = new List<NewsTemplate_SO>();
        public HashSet<int> playerAcquiredIds = new HashSet<int>();
    }
}
