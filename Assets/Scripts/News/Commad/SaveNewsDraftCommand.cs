using QFramework;

namespace TapTapFirst
{
    /// <summary>
    /// 保存一条新闻的【草稿】（填了一部分、还没提交的状态）。
    ///
    /// 用法：
    ///   bool ok = this.SendCommand(new SaveNewsDraftCommand(newsId, json));
    /// </summary>
    public class SaveNewsDraftCommand : AbstractCommand<bool>
    {
        private readonly int mNewsId;
        private readonly string mDocumentJson;

        public SaveNewsDraftCommand(int newsId, string documentJson)
        {
            mNewsId = newsId;
            mDocumentJson = documentJson;
        }

        protected override bool OnExecute()
        {
            return this.GetSystem<INewsSystem>().SaveDraft(mNewsId, mDocumentJson);
        }
    }
}