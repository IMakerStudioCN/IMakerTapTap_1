using QFramework;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    public class TagModel : AbstractModel,ITagModel
    {
        private ResLoader mResLoader = ResLoader.Allocate();
        JsonSaveUtility saveUtility => this.GetUtility<IJsonSaveUtility>() as JsonSaveUtility;
        #region �ֶ�
        /// <summary>
        /// ���ڴ洢���е�tag����
        /// </summary>
        private TagList_SO taglist = new TagList_SO();
        /// <summary>
        ///���ڴ洢�Ѿ���õ�tag
        /// </summary>
        private readonly List<Tag_SO> mAcquired1 = new List<Tag_SO>();
        /// <summary>
        /// ���ڿ����ж��Ƿ��Ѿ�����˸�tag
        /// </summary>
        private readonly HashSet<int> mAcquiredIds1 = new HashSet<int>();
        /// <summary>
        /// ����һ��ֻ�����ԣ������Ѿ���õ�tag�б�
        /// </summary>
        public IReadOnlyList<Tag_SO> AcquriedTags => saveUtility.Get<TagListSaveData>("TagListSaveData").palyerAcquired;
        #endregion
        /// <summary>
        /// ��ȡtag�����ã�����tagId�����ض�Ӧ��Tag_SO����ʹ��ʱ���CanUse�����ж��Ƿ��Ѿ�����˸�tag
        /// </summary>
        /// <param name="tagId"></param>
        /// <returns></returns>
        public Tag_SO GetConfig(int tagId) => taglist.GetTagById(tagId);
        //ʹ�÷���
        //if(CanUse(tagId))=>GetConfig(tagId)
        //


        /// <summary>
        /// ������õ��ã�ֱ�ӵ�CanUse�����ˣ��ж��Ƿ��Ѿ�����˸�tag
        /// </summary>
        /// <param name="tagId"></param>
        /// <returns></returns>
        public bool IsAcquired(int tagId) => saveUtility.Get<TagListSaveData>("TagListSaveData").palyerAcquiredIds.Contains(tagId);
        /// <summary>
        /// �����Ƿ����ʹ�ø�tag��ʵ���Ͼ����ж��Ƿ��Ѿ�����˸�tag
        /// </summary>
        /// <param name="tagId"></param>
        /// <returns></returns>
        public bool CanUse(int tagId) => IsAcquired(tagId);

        /// <summary>
        /// ��һ�ȡtag������tagId������Ѿ�����˸�tag�������κβ��������򽫸�tag��ӵ��ѻ�õ��б���
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

            //Debug.Log("记得初始化tag列表，传入Resources获取位置");

            //Debug.LogWarning("记得初始化tag列表，传入Resources获取位置");


        }
    }
    public class  TagListSaveData
    {
        //public TagList_SO tagList;

        public  List<Tag_SO> palyerAcquired = new List<Tag_SO>();
        
        public  HashSet<int> palyerAcquiredIds = new HashSet<int>();

    }
}
