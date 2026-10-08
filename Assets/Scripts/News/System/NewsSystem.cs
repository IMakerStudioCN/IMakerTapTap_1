using QFramework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    public interface INewsSystem : ISystem
    {
        /// <summary>根据 ID 获取新闻模板配置，取不到返回 null</summary>
        NewsTemplate_SO GetConfig(int newsId);

        /// <summary>是否已经获得该新闻（一般直接调 CanUse 即可）</summary>
        bool IsAcquired(int newsId);

        /// <summary>是否可以使用该新闻，用法：if (CanUse(newsId)) => GetConfig(newsId)</summary>
        bool CanUse(int newsId);

        /// <summary>首次获取新闻，重复获取会被忽略</summary>
        void Acquire(int newsId);

        /// <summary>已获得的新闻模板列表（由存档中的 ID 反查得到）</summary>
        List<NewsTemplate_SO> GetAcquiredNewsList();
    }

    public class NewsSystem : AbstractSystem, INewsSystem
    {
        /// <summary>存档 key，必须和 TapTap.Init() 里 save.Add&lt;NewsTemplateSaveData&gt;() 注册的 key 一致</summary>
        private const string SaveKey = nameof(NewsTemplateSaveData);

        /// <summary>新闻总表在 ResKit 中的资源名，对应 Assets/Config/News/NewsTemplateList.asset</summary>
        private const string NewsListResName = "NewsTemplateList";

        private readonly ResLoader mResLoader = ResLoader.Allocate();

        private IJsonSaveUtility saveUtility => this.GetUtility<IJsonSaveUtility>();

        private NewsTemplateList_SO newsList; //拥有所有新闻模板的列表

        private readonly List<NewsTemplate_SO> mAcquiredNews = new();//已获得的新闻模板列表（由 ID 反查，始终和存档一致）

        protected override void OnInit()
        {
            NewsTemplateSaveData save = GetSaveData();
            if (save == null)
            {
                Debug.LogError($"[NewsSystem] 存档中没有 {SaveKey}，请检查 TapTap.Init() 是否执行了 save.Add<{SaveKey}>()");
            }
            else if (save.playerAcquiredIds == null)
            {
                save.playerAcquiredIds = new List<int>();
            }

            try
            {
                newsList = mResLoader.LoadSync<NewsTemplateList_SO>(NewsListResName);
            }
            catch (Exception e)
            {
                // ResKit 在资源缺失时内部会直接抛异常，这里兜住，避免整个架构的初始化被中断
                Debug.LogError($"[NewsSystem] 加载新闻总表 {NewsListResName} 失败：{e}");
                newsList = null;
            }

            if (newsList == null)
            {
                Debug.LogError($"[NewsSystem] 没有找到新闻总表 {NewsListResName}，请检查 Assets/Config/News 下的资源以及它的 AssetBundle 配置");
            }

            UpdateAcquiredNewsList();
        }

        protected override void OnDeinit()
        {
            base.OnDeinit();
            mResLoader.Dispose();
            mAcquiredNews.Clear();
        }

        public NewsTemplate_SO GetConfig(int newsId) =>
            newsList != null ? newsList.GetNewsTemplateById(newsId) : null;//根据ID获取新闻模板

        public bool IsAcquired(int newsId)
        {
            NewsTemplateSaveData save = GetSaveData();
            return save != null && save.playerAcquiredIds != null && save.playerAcquiredIds.Contains(newsId);
        }

        public bool CanUse(int newsId) => IsAcquired(newsId);//是否可以使用，直接调用这个方法
        //例子:if(CanUse(newsId))=>GetConfig(newsId)获取新闻模板配置

        public void Acquire(int newsId)
        {
            NewsTemplateSaveData save = GetSaveData();
            if (save == null)
            {
                Debug.LogError($"[NewsSystem] 存档中没有 {SaveKey}，无法记录新闻 {newsId}");
                return;
            }

            if (save.playerAcquiredIds.Contains(newsId))
            {
                Debug.LogWarning($"[NewsSystem] 新闻 {newsId} 已经获得过，忽略重复获取");
                return;
            }

            NewsTemplate_SO news = GetConfig(newsId);
            if (news == null)
            {
                Debug.LogError($"[NewsSystem] ID 为 {newsId} 的新闻在 {NewsListResName} 中不存在，请检查配置");
                return;
            }

            save.playerAcquiredIds.Add(newsId);
            UpdateAcquiredNewsList();
            Debug.Log($"[NewsSystem] 首次获得新闻 {news.newsTitle}({newsId})");
        }

        public List<NewsTemplate_SO> GetAcquiredNewsList()
        {
            UpdateAcquiredNewsList();
            return mAcquiredNews;
        }

        private NewsTemplateSaveData GetSaveData() => saveUtility.Get<NewsTemplateSaveData>(SaveKey);

        private void UpdateAcquiredNewsList()
        {
            mAcquiredNews.Clear();

            NewsTemplateSaveData save = GetSaveData();
            if (save == null || save.playerAcquiredIds == null)
            {
                return;
            }

            foreach (int newsId in save.playerAcquiredIds)
            {
                NewsTemplate_SO news = GetConfig(newsId);
                if (news == null)
                {
                    Debug.LogWarning($"[NewsSystem] 存档里的新闻 {newsId} 在 {NewsListResName} 中不存在，已跳过");
                    continue;
                }

                if (!mAcquiredNews.Contains(news))
                {
                    mAcquiredNews.Add(news);
                }
            }
        }
    }
}
