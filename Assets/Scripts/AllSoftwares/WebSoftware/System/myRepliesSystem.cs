using JetBrains.Annotations;
using QFramework;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    // 已生成模块接口 ImyRepliesSystem，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterSystem<ImyRepliesSystem>(new myRepliesSystem());
    public interface ImyRepliesSystem : ISystem
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
        baLineSingleSO GetevilReply();
        baLineSingleSO GetnormalReply();
        baLineSingleSO GetfairReply();
        baLineSingleSO GetbalaReply();
        baLineSingleSO GetotherReply();
    }

    public class myRepliesSystem : AbstractSystem, ImyRepliesSystem
    {
        private ResLoader mResLoader = ResLoader.Allocate();
        System.Random mRandom = new System.Random();
        public List<baLineSingleSO> evilReplyList { get; set; }
        public List<baLineSingleSO> normalReplyList { get; set; }
        public List<baLineSingleSO> fairReplyList { get; set; }
        public List<baLineSingleSO> balaReplyList { get; set; }
        public List<baLineSingleSO> otherReplyList { get; set; }
        protected override void OnInit()
        {
            evilReplyList = mResLoader.LoadSync<baLineList_SO>("evilReplyList").baLineList;
            normalReplyList = mResLoader.LoadSync<baLineList_SO>("normalReplyList").baLineList;
            fairReplyList = mResLoader.LoadSync<baLineList_SO>("fairReplyList").baLineList;
            balaReplyList = mResLoader.LoadSync<baLineList_SO>("balaReplyList").baLineList;
            otherReplyList = mResLoader.LoadSync<baLineList_SO>("otherReplyList").baLineList;
        }
        public baLineSingleSO GetbalaReply()
        {
            if (balaReplyList == null || balaReplyList.Count == 0)
            {
                Debug.LogWarning("胡扯评论列表没有东西");
                return null;
            }
            return balaReplyList[mRandom.Next(0,balaReplyList.Count)];
        }

        public baLineSingleSO GetevilReply()
        {
            if (evilReplyList == null || evilReplyList.Count == 0)
            {
                Debug.LogWarning("邪恶评论列表没有东西");
                return null;
            }
            return evilReplyList[mRandom.Next(0, evilReplyList.Count)];
        }

        public baLineSingleSO GetfairReply()
        {
            if (fairReplyList == null || fairReplyList.Count == 0)
            {
                Debug.LogWarning("公正评论列表没有东西");
                return null;
            }
            return fairReplyList[mRandom.Next(0, fairReplyList.Count)];
        }

        public baLineSingleSO GetnormalReply()
        {
            if (normalReplyList == null || normalReplyList.Count == 0)
            {
                Debug.LogWarning("正常评论列表没有东西");
                return null;
            }
            return normalReplyList[mRandom.Next(0, normalReplyList.Count)];
        }

        public baLineSingleSO GetotherReply()
        {
            if (otherReplyList == null || otherReplyList.Count == 0)
            {
                Debug.LogWarning("其他评论列表没有东西");
                return null;
            }
            return otherReplyList[mRandom.Next(0, otherReplyList.Count)];
        }





    }
}
