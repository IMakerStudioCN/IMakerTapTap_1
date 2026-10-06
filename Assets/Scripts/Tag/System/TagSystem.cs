using Markdig.Extensions.TaskLists;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using TapTapFirst;
using UnityEngine;
using UnityEngine.UI;

namespace TapTapFirst
{
    public interface ITagSystem : ISystem
    {
        Tag_SO GetConfig(int tagId);
        bool IsAcquired(int tagId);
        bool CanUse(int tagId);
        void Acquire(int tagId);

    }

   

    public class TagSystem : AbstractSystem , ITagSystem
    {
        IJsonSaveUtility saveUtility => this.GetUtility<IJsonSaveUtility>();

        private IUnRegister mPlayerDiedUnRegister;

        private ResLoader mResLoader = ResLoader.Allocate();





        protected override void OnInit()
        {
            InitList();
            mPlayerDiedUnRegister = this.RegisterEvent<TagAcquiredEvent>(OnTagAcquired);
        }


        protected override void OnDeinit()
        {
            base.OnDeinit();
            mPlayerDiedUnRegister?.UnRegister();

        }
        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;

        }



        #region 字段
        /// <summary>
        /// 总Tag列表，存储所有的tag数据
        /// <summary>
        private TagList_SO allTaglist = new();


        /// <summary>
        /// 用于存储已经获得的tag
        /// </summary>
        private  List<Tag_SO> acquiredTaglist = new();
        #endregion

        private void InitList()
        {
            allTaglist = mResLoader.LoadSync<TagList_SO>("Tag_List_SO");
            acquiredTaglist = saveUtility.Get<TagListSaveData>("TagListSaveData").palyerAcquired;

            if(acquiredTaglist == null )
            {   
                Debug.LogWarning("acquiredTaglist is null, initializing a new list");
            }

            if(allTaglist == null)
            {
                Debug.LogError("allTaglist is null, please check the resource path and ensure TagList_SO exists");
            }
        }

        public void UpdateAcquiredTaglist()
        {
            acquiredTaglist = saveUtility.Get<TagListSaveData>("TagListSaveData").palyerAcquired;
        }

        /// <summary>
        /// 获取tag的配置，根据tagId返回对应的Tag_SO，使用时结合CanUse方法判断是否已经获得了该tag
        /// </summary>
        /// <param name="tagId"></param>
        /// <returns></returns>
        public Tag_SO GetConfig(int tagId) => allTaglist.GetTagById(tagId);
        //使用方法
        //if(CanUse(tagId))=>GetConfig(tagId)
        //


        /// <summary>
        /// 如果已经获得的话，直接调CanUse就行了，判断是否已经获得了该tag
        /// </summary>
        /// <param name="tagId"></param>
        /// <returns></returns>
        public bool IsAcquired(int tagId) => saveUtility.Get<TagListSaveData>("TagListSaveData").palyerAcquiredIds.Contains(tagId);


        /// <summary>
        /// 判断是否可以使用该tag，实际上就是判断是否已经获得了该tag
        /// </summary>
        /// <param name="tagId"></param>
        /// <returns></returns>
        public bool CanUse(int tagId) => IsAcquired(tagId);//判断方法暂定为是否已经获得了该tag，后续可以根据需求修改


        /// <summary>
        /// 首次获取tag，如果根据tagId发现已经获得了该tag，则不做任何操作；否则将该tag加入已获得的列表中
        /// </summary>
        /// <param name="tagId"></param>
        public void Acquire(int tagId)
        {
            if (IsAcquired(tagId))
            {
                Debug.LogWarning($"Tag {GetConfig(tagId).tagName} 已经在已获取列表中，无法重复添加");
                return;

            }

            var tag = GetConfig(tagId);
            if (tag == null)
            {
               Debug.LogError($"ID为 {tagId}的Tag在配置中不存在，请检查TagList_SO.");
                return;
            }

            saveUtility.Get<TagListSaveData>("TagListSaveData").palyerAcquired.Add(tag);
            saveUtility.Get<TagListSaveData>("TagListSaveData").palyerAcquiredIds.Add(tagId);
            Debug.Log($"玩家首次获得tag {tag.tagName}，已加入已获得列表");
            UpdateAcquiredTaglist();

        }


        

       

        private void OnTagAcquired(TagAcquiredEvent e)
        {
            Acquire(e.tagId);
            Debug.Log($"TagAcquiredEvent触发，tagId: {e.tagId}，已调用Acquire方法");

        }


    }

}
 
