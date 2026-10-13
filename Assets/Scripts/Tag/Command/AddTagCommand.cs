using QFramework;
using System.Collections;
using System.Collections.Generic;
using TapTapFirst;
using UnityEngine;

namespace TapTapFirst
{
    /// <summary>
    /// 获得一个 Tag：先落数据，再发事件通知。
    ///
    /// 和 AddNewsCommand 保持同一套写法：
    ///   1. 通过架构拿 ITagSystem（不能自己 new），由它把 tagId 写进存档
    ///   2. 发 TagAcquiredEvent 通知 UI / 其它系统
    ///
    /// 注意：事件里**必须**带上 tagId。TagAcquiredEvent 是 struct，
    /// 漏赋值就是默认 0，TagSystem.OnTagAcquired 会拿 0 去查配置并报
    /// "ID为 0 的Tag在配置中不存在"。
    /// </summary>
    public class AddTagCommand : AbstractCommand
    {
        private readonly int tagId;

        private ITagSystem st => this.GetSystem<ITagSystem>();

        public AddTagCommand(int tagId)
        {
            this.tagId = tagId;
        }

        protected override void OnExecute()
        {
            ITagSystem tagSystem = this.GetSystem<ITagSystem>();

            if (tagSystem == null)
            {
                Debug.LogError("[AddTagCommand] 拿不到 ITagSystem，请检查 TapTap.Init() 是否注册了 TagSystem");
                return;
            }

            // 1) 先落数据（Acquire 内部会做重复检查 + 写存档 + 刷新已获得列表）
            tagSystem.Acquire(tagId);

            // 2) 再发事件通知（带上 tagId）
            this.SendEvent(new TagAcquiredEvent { tagId = tagId });
        }
    }
}