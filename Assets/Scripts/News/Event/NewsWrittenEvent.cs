namespace TapTapFirst
{
    /// <summary>一篇新闻写好了并且已经存进 NewsSystem 时发出。</summary>
    public struct NewsWrittenEvent
    {
        public int newsId;

        /// <summary>这次结算出来的结果（正文 + Tag 数值合计 + 模版倾向数值）。</summary>
        public NewsResultData result;
    }
}