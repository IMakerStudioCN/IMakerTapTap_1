using QFramework;
using UnityEngine;

namespace TapTapFirst
{
    /// <summary>
    /// 保存一篇【写好的新闻】。
    ///
    /// Controller 只管把当前排版序列化成 Json 丢过来，剩下的（解析、结算、
    /// 存进 NewsSystem、镜像进存档、清掉草稿、发事件）全部由 NewsSystem 做。
    ///
    /// 用法：
    ///   NewsResultData result = this.SendCommand(new SaveWrittenNewsCommand(newsId, json));
    ///   失败返回 null。
    /// </summary>
    public class SaveWrittenNewsCommand : AbstractCommand<NewsResultData>
    {
        private readonly int mNewsId;
        private readonly string mDocumentJson;

        public SaveWrittenNewsCommand(int newsId, string documentJson)
        {
            mNewsId = newsId;
            mDocumentJson = documentJson;
        }

        protected override NewsResultData OnExecute()
        {
            INewsSystem newsSystem = this.GetSystem<INewsSystem>();

            NewsResultData result = newsSystem.SaveWrittenNews(mNewsId, mDocumentJson);

            if (result == null)
            {
                return null;
            }


            this.SendEvent(new NewsWrittenEvent 
            {   newsId = mNewsId,
                result = result 
            }
            );

            this.SendEvent(new mybaSendEvent
            {
                content = result.content,
                click = result.pageViews
            }
            );

            this.SendCommand(new FundsValueCommand(result.fundsChange));
            this.SendCommand(new EvilValueCommand(result.evilChange));
            this.SendCommand(new CredibilityValueCommand(result.credibilityChange));

            Debug.Log($"向贴吧发送新闻：newsId={mNewsId}，浏览量={result.pageViews}");

            return result;
        }
    }
}