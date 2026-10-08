using QFramework;
using TapTapFirst;
using UnityEngine;
namespace TapTapFirst
{
    public class AddNewsCommand : AbstractCommand
    {
        private readonly int newsId;

        public AddNewsCommand(int newsId)
        {
            this.newsId = newsId;
        }

        protected override void OnExecute()
        {
            // 必须通过架构拿系统，不能自己 new NewsSystem()，
            // 否则用的是另一个实例、也拿不到架构里的存档工具和事件
            INewsSystem newsSystem = this.GetSystem<INewsSystem>();
            newsSystem.Acquire(newsId);

            //发送事件通知（新闻数据已经由 Acquire 写入存档）
            this.SendEvent(new NewsAcquiredEvent { newsId = newsId });
        }
    }

}
