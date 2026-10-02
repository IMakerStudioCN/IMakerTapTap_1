using QFramework;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    public class TagModel : AbstractModel,ITagModel
    {
        private ResLoader mResLoader = ResLoader.Allocate();
        JsonSaveUtility saveUtility => this.GetUtility<IJsonSaveUtility>() as JsonSaveUtility;
        #region 字段
        /// <summary>
        /// 用于存储所有的tag配置
        /// </summary>
        private TagList_SO taglist = new TagList_SO();
        /// <summary>
        ///用于存储已经获得的tag
        /// </summary>
        private readonly List<Tag_SO> mAcquired1 = new List<Tag_SO>();
        /// <summary>
        /// 用于快速判断是否已经获得了该tag
        /// </summary>
        private readonly HashSet<int> mAcquiredIds1 = new HashSet<int>();
        /// <summary>
        /// 这是一个只读属性，返回已经获得的tag列表
        /// </summary>
        public IReadOnlyList<Tag_SO> AcquriedTags => saveUtility.Get<TagListSaveData>("TagListSaveData").palyerAcquired;
        #endregion
        /// <summary>
        /// 获取tag的配置，传入tagId，返回对应的Tag_SO对象，使用时配合CanUse方法判断是否已经获得了该tag
        /// </summary>
        /// <param name="tagId"></param>
        /// <returns></returns>
        public Tag_SO GetConfig(int tagId) => taglist.GetTagById(tagId);
        //使用方法
        //if(CanUse(tagId))=>GetConfig(tagId)
        //


        /// <summary>
        /// 这个不用调用，直接调CanUse就行了，判断是否已经获得了该tag
        /// </summary>
        /// <param name="tagId"></param>
        /// <returns></returns>
        public bool IsAcquired(int tagId) => saveUtility.Get<TagListSaveData>("TagListSaveData").palyerAcquiredIds.Contains(tagId);
        /// <summary>
        /// 看看是否可以使用该tag，实际上就是判断是否已经获得了该tag
        /// </summary>
        /// <param name="tagId"></param>
        /// <returns></returns>
        public bool CanUse(int tagId) => IsAcquired(tagId);

        /// <summary>
        /// 玩家获取tag，传入tagId，如果已经获得了该tag，则不做任何操作，否则将该tag添加到已获得的列表中
        /// </summary>
        /// <param name="tagId"></param>
        public void Acquire(int tagId)
        {
            if(IsAcquired(tagId))
                return;

            var tag = GetConfig(tagId);
            if (tag != null)
            {
                saveUtility.Get<TagListSaveData>("TagListSaveData").palyerAcquired.Add(tag);
                saveUtility.Get<TagListSaveData>("TagListSaveData").palyerAcquiredIds.Add(tagId);
            }
        }

        protected override void OnInit()
        {
            //ResKit.Init();
            //获取设定好的含全部tag的列表
            //taglist = mResLoader.LoadSync<TagList_SO>("TagList");
            Debug.LogError("记得初始化tag列表，传入Resources获取位置");

        }
    }
    public class  TagListSaveData
    {
        //public TagList_SO tagList;

        public  List<Tag_SO> palyerAcquired = new List<Tag_SO>();
        
        public  HashSet<int> palyerAcquiredIds = new HashSet<int>();

    }
}
