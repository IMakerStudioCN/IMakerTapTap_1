﻿using QFramework;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    public class TagModel : AbstractModel,ITagModel
    {
        protected override void OnInit()
        {
        }

        Tag_SO newTag = null;

    }
    public class  TagListSaveData
    {
        //public TagList_SO tagList;

        public  List<Tag_SO> palyerAcquired = new List<Tag_SO>();
        
        public  HashSet<int> palyerAcquiredIds = new HashSet<int>();

    }
}
