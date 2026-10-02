using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    /// <summary>
    /// taglist 的 model 接口
    /// 传入id，返回是否已经获得了该tag
    /// </summary>
    public interface ITagModel : IModel
    {
        IReadOnlyList<Tag_SO> AcquriedTags { get; }
        Tag_SO GetConfig(int tagId);
        bool IsAcquired(int tagId);
        bool CanUse(int tagId);
        void Acquire(int tagId);
    }
}
        
