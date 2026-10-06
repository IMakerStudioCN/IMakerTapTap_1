using QFramework;
using System.Collections;
using System.Collections.Generic;
using TapTapFirst;
using UnityEngine;

public class AddTagCommand : AbstractCommand
{
    private int tagId;

    private ITagSystem st => this.GetSystem<ITagSystem>();
    public AddTagCommand(int tagId)
    {
        this.tagId = tagId;
    }
    protected override void OnExecute()
    {
        var tagModel = this.GetModel<ITagModel>();
        //if (!st.IsAcquired(tagId))
        //{
        //    st.Acquire(tagId);
           
        //}

        

        //发送事件通知
        this.SendEvent(new TagAcquiredEvent());
    }
    
}
