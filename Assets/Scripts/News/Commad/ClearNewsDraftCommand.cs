using QFramework;

namespace TapTapFirst
{
    /// <summary>
    /// 删掉一条新闻的草稿。
    ///
    /// 用法：
    ///   bool ok = this.SendCommand(new ClearNewsDraftCommand(newsId));
    /// </summary>
    public class ClearNewsDraftCommand : AbstractCommand<bool>
    {
        private readonly int mNewsId;

        public ClearNewsDraftCommand(int newsId)
        {
            mNewsId = newsId;
        }

        protected override bool OnExecute()
        {
            return this.GetSystem<INewsSystem>().RemoveDraft(mNewsId);
        }
    }
}