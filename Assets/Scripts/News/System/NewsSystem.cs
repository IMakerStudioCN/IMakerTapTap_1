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

        // ============================================================
        //  写好的新闻数据（真身在本系统里，存档只是它的镜像）
        // ============================================================

        /// <summary>
        /// 保存一篇写好的新闻：正文 + 用到的所有 Tag 的数值合计 + 模版倾向及其数值。
        /// documentJson 由 TagTextTemplate.SaveToJson() 得到。
        /// 成功返回结算出来的结果，失败返回 null。
        /// </summary>
        NewsResultData SaveWrittenNews(int newsId, string documentJson);

        /// <summary>直接存一份已经结算好的结果（覆盖同名 newsId）。</summary>
        bool SaveWrittenNews(NewsResultData result);

        /// <summary>取某篇写好的新闻；没写过返回 null。</summary>
        NewsResultData GetWrittenNews(int newsId);

        bool TryGetWrittenNews(int newsId, out NewsResultData result);

        /// <summary>这篇新闻写好了没有。</summary>
        bool HasWrittenNews(int newsId);

        /// <summary>所有写好的新闻的 Id。</summary>
        List<int> GetWrittenNewsIds();

        /// <summary>所有写好的新闻。</summary>
        List<NewsResultData> GetWrittenNewsList();

        /// <summary>写好的新闻数量。</summary>
        int WrittenNewsCount { get; }

        // ---------------- 单篇的数据查询 ----------------

        /// <summary>某篇写好的新闻里，所有 Tag 的该类型数值加起来的总值。</summary>
        float GetWrittenTagValueTotal(int newsId, TagValueType type);

        /// <summary>某篇写好的新闻里，所有 Tag 的所有类型数值加起来的总值。</summary>
        float GetWrittenTagValueTotal(int newsId);

        /// <summary>某篇写好的新闻用的模版倾向。</summary>
        NewsEffectType GetWrittenEffectType(int newsId);

        /// <summary>某篇写好的新闻的模版倾向对应数值。</summary>
        float GetWrittenEffectValue(int newsId);

        /// <summary>某篇写好的新闻拼好的正文。</summary>
        string GetWrittenContent(int newsId);

        /// <summary>把某篇写好的新闻还原成填空文档（读档回填用）；没有返回 null。</summary>
        TagFillDocument GetWrittenDocument(int newsId);

        //// ---------------- 全部新闻的合计 ----------------

        /// <summary>所有写好的新闻里，Tag 数值按类型加起来的总值。</summary>
        Dictionary<TagValueType, float> GetTotalTagValues();

        /// <summary>所有写好的新闻里，Tag 的所有类型数值再加一起。</summary>
        float GetTotalTagValue();

        /// <summary>所有写好的新闻里，按模版倾向分类的数值合计。</summary>
        Dictionary<NewsEffectType, float> GetTotalEffectValues();

        /// <summary>所有写好的新闻里，按模版倾向分类的篇数。</summary>
        Dictionary<NewsEffectType, int> GetEffectNewsCounts();

        // ============================================================
        //  草稿（填了但还没提交的）
        // ============================================================

        /// <summary>存草稿（覆盖同名 newsId）。</summary>
        bool SaveDraft(int newsId, string documentJson);

        /// <summary>取草稿 Json；没有返回 null。</summary>
        string GetDraftJson(int newsId);

        bool TryGetDraft(int newsId, out string documentJson);

        /// <summary>取草稿文档；没有返回 null。</summary>
        TagFillDocument GetDraftDocument(int newsId);

        /// <summary>这篇新闻有没有草稿。</summary>
        bool HasDraft(int newsId);

        /// <summary>删掉草稿。</summary>
        bool RemoveDraft(int newsId);

        /// <summary>所有有草稿的新闻 Id。</summary>
        List<int> GetDraftNewsIds();
    }

    /// <summary>
    /// 新闻系统。
    ///
    /// 它同时是【写好的新闻数据】的唯一持有者：
    ///   - 内存里是 Dictionary&lt;newsId, NewsResultData&gt;，读取一律走内存；
    ///   - 每次改动立刻镜像进存档（NewsResultSaveData / TagFillSaveData）并落盘；
    ///   - Controller 不碰存档工具，只发命令（SaveWrittenNewsCommand / SaveNewsDraftCommand ...）。
    /// </summary>
    public class NewsSystem : AbstractSystem, INewsSystem
    {
        /// <summary>存档 key，必须和 TapTap.Init() 里 save.Add&lt;NewsTemplateSaveData&gt;() 注册的 key 一致</summary>
        private const string SaveKey = nameof(NewsTemplateSaveData);

        private const string ResultSaveKey = nameof(NewsResultSaveData);

        private const string FillSaveKey = nameof(TagFillSaveData);

        /// <summary>新闻总表在 ResKit 中的资源名，对应 Assets/Config/News/NewsTemplateList.asset</summary>
        private const string NewsListResName = "NewsTemplateList";

        private readonly ResLoader mResLoader = ResLoader.Allocate();

        private IJsonSaveUtility saveUtility => this.GetUtility<IJsonSaveUtility>();

        private NewsTemplateList_SO newsList; //拥有所有新闻模板的列表

        private readonly List<NewsTemplate_SO> mAcquiredNews = new();//已获得的新闻模板列表（由 ID 反查，始终和存档一致）

        #region 写好的新闻数据（真身）

        /// <summary>写好的新闻：newsId -> 结果。内存为准，读档时从存档灌回来。</summary>
        private readonly Dictionary<int, NewsResultData> mWrittenNews = new Dictionary<int, NewsResultData>();

        /// <summary>写好的新闻的持久化载体（镜像，不是真身）。</summary>
        private NewsResultSaveData mResultSave;

        /// <summary>草稿的持久化载体。</summary>
        private TagFillSaveData mFillSave;

        #endregion

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

            mResultSave = saveUtility != null ? saveUtility.Get<NewsResultSaveData>(ResultSaveKey) : null;

            if (mResultSave == null)
            {
                Debug.LogError($"[NewsSystem] 存档中没有 {ResultSaveKey}，请检查 TapTap.Init() 是否执行了 save.Add<{ResultSaveKey}>()");
            }

            mFillSave = saveUtility != null ? saveUtility.Get<TagFillSaveData>(FillSaveKey) : null;

            if (mFillSave == null)
            {
                Debug.LogError($"[NewsSystem] 存档中没有 {FillSaveKey}，请检查 TapTap.Init() 是否执行了 save.Add<{FillSaveKey}>()");
            }

            // 把存档里的写好的新闻灌回内存
            LoadWrittenNewsFromSave();

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
            mWrittenNews.Clear();
        }

        public NewsTemplate_SO GetConfig(int newsId) => GetNewsTemplateById(newsId);
            //newsList != null ? newsList.GetNewsTemplateById(newsId) : null;//根据ID获取新闻模板

        public NewsTemplate_SO GetNewsTemplateById(int newsId)
        {
            return newsList.newsTemplateList.Find(tag => tag.newsId == newsId);
        }
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

        // ============================================================
        //  写好的新闻数据
        // ============================================================

        public NewsResultData SaveWrittenNews(int newsId, string documentJson)
        {
            if (!TagFillDocument.TryParse(documentJson, out TagFillDocument document))
            {
                Debug.LogError($"[NewsSystem] 新闻 {newsId} 的内容文档解析失败，未保存");
                return null;
            }

            // 结算：正文 + 所有 Tag 数值合计 + 模版倾向及其数值
            NewsResultData result = NewsResultBuilder.Build(GetConfig(newsId), document, this.GetSystem<ITagSystem>());

            if (result == null)
            {
                Debug.LogError($"[NewsSystem] 新闻 {newsId} 结算失败，未保存");
                return null;
            }

            result.newsId = newsId;

            if (!SaveWrittenNews(result))
            {
                return null;
            }

            // 提交之后草稿就没用了
            RemoveDraft(newsId);
            return result;
        }

        public bool SaveWrittenNews(NewsResultData result)
        {
            if (result == null)
            {
                Debug.LogError("[NewsSystem] 要保存的新闻结果是 null");
                return false;
            }

            // 1) 内存先更新（读取一律走这里）
            mWrittenNews[result.newsId] = result;

            // 2) 镜像进存档并落盘
            if (mResultSave == null)
            {
                mResultSave = saveUtility != null ? saveUtility.Get<NewsResultSaveData>(ResultSaveKey) : null;
            }

            if (mResultSave == null)
            {
                Debug.LogError($"[NewsSystem] 存档中没有 {ResultSaveKey}，数据只在内存里，重启会丢");
                return false;
            }

            mResultSave.SetResult(result);
            saveUtility.Save();

            Debug.Log($"[NewsSystem] 新闻 {result.newsId}({result.newsTitle}) 已保存：倾向 {result.GetEffectTypeName()}，" +
                      $"用到 {result.tagCount} 个 Tag，浏览量合计 {result.PageViewsTotal}，压力合计 {result.PressureValueTotal}");
            return true;
        }

        public NewsResultData GetWrittenNews(int newsId)
        {
            return mWrittenNews.TryGetValue(newsId, out NewsResultData result) ? result : null;
        }

        public bool TryGetWrittenNews(int newsId, out NewsResultData result)
        {
            return mWrittenNews.TryGetValue(newsId, out result) && result != null;
        }

        public bool HasWrittenNews(int newsId) => mWrittenNews.ContainsKey(newsId);

        public List<int> GetWrittenNewsIds()
        {
            return new List<int>(mWrittenNews.Keys);
        }

        public List<NewsResultData> GetWrittenNewsList()
        {
            List<NewsResultData> list = new List<NewsResultData>();

            foreach (KeyValuePair<int, NewsResultData> pair in mWrittenNews)
            {
                if (pair.Value != null)
                {
                    list.Add(pair.Value);
                }
            }

            return list;
        }

        public int WrittenNewsCount => mWrittenNews.Count;

        // ---------------- 单篇查询 ----------------

        public float GetWrittenTagValueTotal(int newsId, TagValueType type)
        {
            NewsResultData result = GetWrittenNews(newsId);
            return result != null ? result.GetTagValueTotal(type) : 0f;
        }

        public float GetWrittenTagValueTotal(int newsId)
        {
            NewsResultData result = GetWrittenNews(newsId);
            return result != null ? result.GetTagValueTotal() : 0f;
        }

        public NewsEffectType GetWrittenEffectType(int newsId)
        {
            NewsResultData result = GetWrittenNews(newsId);
            return result != null ? result.GetEffectType() : NewsEffectType.Objective;
        }

        public float GetWrittenEffectValue(int newsId)
        {
            NewsResultData result = GetWrittenNews(newsId);
            return result != null ? result.GetEffectValue() : 0f;
        }

        public string GetWrittenContent(int newsId)
        {
            NewsResultData result = GetWrittenNews(newsId);
            return result != null ? result.GetContent() : null;
        }

        public TagFillDocument GetWrittenDocument(int newsId)
        {
            NewsResultData result = GetWrittenNews(newsId);
            return result != null ? result.ToDocument() : null;
        }

        // ---------------- 全局合计 ----------------

        public Dictionary<TagValueType, float> GetTotalTagValues()
        {
            Dictionary<TagValueType, float> totals = new Dictionary<TagValueType, float>();

            foreach (TagValueType type in (TagValueType[])Enum.GetValues(typeof(TagValueType)))
            {
                totals[type] = 0f;
            }

            foreach (KeyValuePair<int, NewsResultData> pair in mWrittenNews)
            {
                NewsResultData result = pair.Value;

                if (result == null || result.tagValueTotals == null)
                {
                    continue;
                }

                for (int i = 0; i < result.tagValueTotals.Count; i++)
                {
                    NewsTagValueTotal total = result.tagValueTotals[i];

                    if (total == null)
                    {
                        continue;
                    }

                    totals[total.type] = totals.TryGetValue(total.type, out float exist)
                        ? exist + total.total
                        : total.total;
                }
            }

            return totals;
        }

        public float GetTotalTagValue()
        {
            float sum = 0f;

            foreach (KeyValuePair<TagValueType, float> pair in GetTotalTagValues())
            {
                sum += pair.Value;
            }

            return sum;
        }

        public Dictionary<NewsEffectType, float> GetTotalEffectValues()
        {
            Dictionary<NewsEffectType, float> totals = new Dictionary<NewsEffectType, float>();

            foreach (NewsEffectType type in (NewsEffectType[])Enum.GetValues(typeof(NewsEffectType)))
            {
                totals[type] = 0f;
            }

            foreach (KeyValuePair<int, NewsResultData> pair in mWrittenNews)
            {
                NewsResultData result = pair.Value;

                if (result == null)
                {
                    continue;
                }

                totals[result.effectType] = totals.TryGetValue(result.effectType, out float exist)
                    ? exist + result.effectValue
                    : result.effectValue;
            }

            return totals;
        }

        public Dictionary<NewsEffectType, int> GetEffectNewsCounts()
        {
            Dictionary<NewsEffectType, int> counts = new Dictionary<NewsEffectType, int>();

            foreach (NewsEffectType type in (NewsEffectType[])Enum.GetValues(typeof(NewsEffectType)))
            {
                counts[type] = 0;
            }

            foreach (KeyValuePair<int, NewsResultData> pair in mWrittenNews)
            {
                NewsResultData result = pair.Value;

                if (result == null)
                {
                    continue;
                }

                counts[result.effectType] = counts.TryGetValue(result.effectType, out int exist) ? exist + 1 : 1;
            }

            return counts;
        }

        /// <summary>把存档里的写好的新闻灌回内存（读档/初始化时调）。</summary>
        private void LoadWrittenNewsFromSave()
        {
            mWrittenNews.Clear();

            if (mResultSave == null || mResultSave.results == null)
            {
                return;
            }

            int loaded = 0;

            foreach (NewsResultEntryData entry in mResultSave.results)
            {
                if (entry == null || string.IsNullOrEmpty(entry.json))
                {
                    continue;
                }

                NewsResultData result = NewsResultData.FromJson(entry.json);

                if (result == null)
                {
                    Debug.LogWarning($"[NewsSystem] 存档里的新闻结果 {entry.newsId} 解析失败，已跳过");
                    continue;
                }

                mWrittenNews[result.newsId] = result;
                loaded++;
            }

            if (loaded > 0)
            {
                Debug.Log($"[NewsSystem] 已从存档读回 {loaded} 篇写好的新闻");
            }
        }

        // ============================================================
        //  草稿
        // ============================================================

        public bool SaveDraft(int newsId, string documentJson)
        {
            if (string.IsNullOrEmpty(documentJson))
            {
                Debug.LogWarning($"[NewsSystem] 新闻 {newsId} 的草稿内容为空，已忽略");
                return false;
            }

            if (mFillSave == null)
            {
                mFillSave = saveUtility != null ? saveUtility.Get<TagFillSaveData>(FillSaveKey) : null;
            }

            if (mFillSave == null)
            {
                Debug.LogError($"[NewsSystem] 存档中没有 {FillSaveKey}，草稿无法保存");
                return false;
            }

            mFillSave.Set(newsId.ToString(), documentJson);
            saveUtility.Save();
            return true;
        }

        public string GetDraftJson(int newsId)
        {
            if (mFillSave == null)
            {
                mFillSave = saveUtility != null ? saveUtility.Get<TagFillSaveData>(FillSaveKey) : null;
            }

            if (mFillSave == null)
            {
                return null;
            }

            return mFillSave.TryGet(newsId.ToString(), out string json) ? json : null;
        }

        public bool TryGetDraft(int newsId, out string documentJson)
        {
            documentJson = GetDraftJson(newsId);
            return !string.IsNullOrEmpty(documentJson);
        }

        public TagFillDocument GetDraftDocument(int newsId)
        {
            string json = GetDraftJson(newsId);

            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return TagFillDocument.TryParse(json, out TagFillDocument document) ? document : null;
        }

        public bool HasDraft(int newsId) => !string.IsNullOrEmpty(GetDraftJson(newsId));

        public bool RemoveDraft(int newsId)
        {
            if (mFillSave == null)
            {
                mFillSave = saveUtility != null ? saveUtility.Get<TagFillSaveData>(FillSaveKey) : null;
            }

            if (mFillSave == null)
            {
                return false;
            }

            bool removed = mFillSave.Remove(newsId.ToString());

            if (removed)
            {
                saveUtility.Save();
            }

            return removed;
        }

        public List<int> GetDraftNewsIds()
        {
            List<int> ids = new List<int>();

            if (mFillSave == null)
            {
                mFillSave = saveUtility != null ? saveUtility.Get<TagFillSaveData>(FillSaveKey) : null;
            }

            if (mFillSave == null || mFillSave.documents == null)
            {
                return ids;
            }

            foreach (TagFillEntryData entry in mFillSave.documents)
            {
                if (entry != null && int.TryParse(entry.documentId, out int newsId))
                {
                    ids.Add(newsId);
                }
            }

            return ids;
        }
    }
}