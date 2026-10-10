using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    /// <summary>
    /// Tag 放置结果事件，由 TagSingle 在松手时发出。
    /// </summary>
    public struct TagSinglePlacedEvent
    {
        public TagSingle Item;
        public DropSlot Slot;
    }

    public struct TagSinglePlaceFailedEvent
    {
        public TagSingle Item;
    }

    /// <summary>
    /// 所有空缺都被填满时由 TagSystem 发出。
    /// </summary>
    public struct PuzzleCompletedEvent { }

}