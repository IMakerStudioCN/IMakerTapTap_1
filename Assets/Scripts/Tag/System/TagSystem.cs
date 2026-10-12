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
        Tag_SO GetTagById(int id);
        void UpdateAcquiredTaglist();
    }

    public class TagSystem : AbstractSystem, ITagSystem
    {
        IJsonSaveUtility saveUtility => this.GetUtility<IJsonSaveUtility>();

        private IUnRegister mPlayerDiedUnRegister;

        private IUnRegister mSlotFilledUnRegister;

        private ResLoader mResLoader = ResLoader.Allocate();

        protected override void OnInit()
        {
            InitList();
            mPlayerDiedUnRegister = this.RegisterEvent<TagAcquiredEvent>(OnTagAcquired);
            mSlotFilledUnRegister = this.RegisterEvent<TagSlotFilledEvent>(OnSlotFilled);
        }

        protected override void OnDeinit()
        {
            base.OnDeinit();
            mPlayerDiedUnRegister?.UnRegister();
            mSlotFilledUnRegister?.UnRegister();
        }

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }

        #region 字段
        /// <summary>
        /// 总Tag列表，存储所有的tag数据
        /// </summary>
        private TagList_SO allTaglist = new();

        /// <summary>
        /// 用于存储已经获得的tag
        /// </summary>
        private List<Tag_SO> acquiredTaglist = new();
        #endregion

        private void InitList()
        {
            allTaglist = mResLoader.LoadSync<TagList_SO>("Tag_List_SO");

            TagListSaveData data = saveUtility.Get<TagListSaveData>("TagListSaveData");

            acquiredTaglist = data != null ? data.palyerAcquired : new List<Tag_SO>();

            if (acquiredTaglist == null)
            {
                acquiredTaglist = new List<Tag_SO>();
                Debug.LogWarning("acquiredTaglist is null, initializing a new list");
            }

            if (allTaglist == null)
            {
                Debug.LogError("allTaglist is null, please check the resource path and ensure TagList_SO exists");
            }
        }

        public void UpdateAcquiredTaglist()
        {
            TagListSaveData data = saveUtility.Get<TagListSaveData>("TagListSaveData");

            if (data != null)
            {
                acquiredTaglist = data.palyerAcquired;
            }
        }

        /// <summary>
        /// 获取tag的配置，根据tagId返回对应的Tag_SO，使用时结合CanUse方法判断是否已经获得了该tag
        /// </summary>
        public Tag_SO GetConfig(int tagId) => GetTagById(tagId);

        //使用方法
        //if(CanUse(tagId))=>GetConfig(tagId)

        public Tag_SO GetTagById(int id)
        {
            if (allTaglist == null || allTaglist.allTagList == null)
            {
                return null;
            }

            return allTaglist.allTagList.Find(tag => tag != null && tag.tagId == id);
        }

        /// <summary>
        /// 如果已经获得的话，直接调CanUse就行了，判断是否已经获得了该tag
        /// </summary>
        public bool IsAcquired(int tagId)
        {
            TagListSaveData data = saveUtility.Get<TagListSaveData>("TagListSaveData");

            return data != null && data.palyerAcquiredIds != null && data.palyerAcquiredIds.Contains(tagId);
        }

        /// <summary>
        /// 判断是否可以使用该tag，实际上就是判断是否已经获得了该tag
        /// </summary>
        public bool CanUse(int tagId) => IsAcquired(tagId); //判断方法暂定为是否已经获得了该tag，后续可以根据需求修改

        /// <summary>
        /// 首次获取tag，如果根据tagId发现已经获得了该tag，则不做任何操作；否则将该tag加入已获得的列表中
        /// </summary>
        public void Acquire(int tagId)
        {
            if (IsAcquired(tagId))
            {
                Tag_SO exist = GetConfig(tagId);
                Debug.LogWarning($"Tag {(exist != null ? exist.tagName : tagId.ToString())} 已经在已获取列表中，无法重复添加");
                return;
            }

            // 先取配置再判空，否则配置里没有这个 id 时会在取 tagName 的地方直接空引用
            Tag_SO tag = GetConfig(tagId);

            if (tag == null)
            {
                Debug.LogError($"ID为 {tagId} 的Tag在配置中不存在，请检查TagList_SO.");
                return;
            }

            TagListSaveData data = saveUtility.Get<TagListSaveData>("TagListSaveData");

            if (data == null)
            {
                Debug.LogError("存档里没有 TagListSaveData，请检查 TapTap.Init() 是否执行了 save.Add<TagListSaveData>()");
                return;
            }

            data.palyerAcquired.Add(tag);
            data.palyerAcquiredIds.Add(tagId);
            Debug.Log($"玩家首次获得tag {tag.tagName}，已加入已获得列表");
            UpdateAcquiredTaglist();
        }

        private void OnTagAcquired(TagAcquiredEvent e)
        {
            Acquire(e.tagId);
            Debug.Log($"TagAcquiredEvent触发，tagId: {e.tagId}，已调用Acquire方法");
        }

        /// <summary>
        /// 某个空缺被填上/清空后，检查整张模版的空缺是否都填满了。
        /// 全部填满则发 PuzzleCompletedEvent。
        /// </summary>
        private void OnSlotFilled(TagSlotFilledEvent e)
        {
            if (e.Template == null || !e.Template.IsAllSlotsFilled)
            {
                return;
            }

            Debug.Log($"[TagSystem] 模版 {e.Template.name} 的空缺已全部填满");
            this.SendEvent(new PuzzleCompletedEvent());
        }
    }
}