using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TapTapFirst
{
    /// <summary>
    /// Tag 的【初始槽位列表】（"老家"）。
    ///
    /// 把游戏开始时摆放好的所有 Tag 作为子物体放在这个节点下：
    ///
    ///   SourceList (本组件)
    ///   ├─ Tag_A      ← 初始位置就是它的"老家"
    ///   ├─ Tag_B
    ///   └─ Tag_C
    ///
    /// 规则：
    ///   - 游戏开始时 Tag 全部固定在这里
    ///   - Tag 被拖到槽位后，从列表里"空出来"
    ///   - 被摘下时 Tag 自动飞回这里它自己的初始位置
    ///
    /// 本组件不需要手动登记 Tag：子物体上的 TagSingle 在启用时自己记录初始位置。
    /// </summary>
    public class TagSourceList : MonoBehaviour
    {
        private static readonly List<TagSourceList> sInstances = new List<TagSourceList>();

        [Tooltip("列表容器；留空则用本节点自己的 RectTransform")]
        [SerializeField] private RectTransform mContainer;

        [Tooltip("开局把所有子 Tag 的位置记录为初始位置（一般保持勾选）")]
        [SerializeField] private bool mRecordHomeOnStart = true;

        [Tooltip("打开后会在 Console 打印列表状态。排查完请关掉")]
        [SerializeField] private bool mLogList;

        /// <summary>列表容器。</summary>
        public RectTransform Container => mContainer != null ? mContainer : (RectTransform)transform;

        private void Awake()
        {
            sInstances.Remove(this);
            sInstances.Add(this);
        }

        private void OnDestroy()
        {
            sInstances.Remove(this);
        }

        private void Start()
        {
            if (mRecordHomeOnStart)
            {
                RecordHomes();
            }
        }

        private void OnEnable()
        {
            // 面板重新打开时，把还留在列表里的 Tag 位置重新记一遍
            if (mRecordHomeOnStart)
            {
                StartCoroutine(RecordHomesNextFrame());
            }
        }

        private IEnumerator RecordHomesNextFrame()
        {
            // 等一帧，保证 LayoutGroup 之类的布局已经算完
            yield return null;
            RecordHomes();
        }

        /// <summary>把所有子 Tag 的当前位置记录为它们的初始位置。</summary>
        public void RecordHomes()
        {
            List<TagSingle> tags = GetAllTags();

            foreach (TagSingle tag in tags)
            {
                if (tag != null && tag.CurrentSlot == null)
                {
                    tag.RecordHome();
                }
            }

            Log($"已记录 {tags.Count} 个 Tag 的初始位置");
        }

        /// <summary>取出本列表下所有的 Tag（含未激活的）。</summary>
        public List<TagSingle> GetAllTags()
        {
            List<TagSingle> result = new List<TagSingle>();
            Container.GetComponentsInChildren(true, result);
            return result;
        }

        /// <summary>取第一个空闲的 Tag（没被放进槽位的）。</summary>
        public TagSingle GetFirstFreeTag()
        {
            foreach (TagSingle tag in GetAllTags())
            {
                if (tag != null && tag.CurrentSlot == null)
                {
                    return tag;
                }
            }

            return null;
        }

        /// <summary>是否所有 Tag 都已经放进槽位。</summary>
        public bool IsAllPlaced()
        {
            List<TagSingle> tags = GetAllTags();

            if (tags.Count == 0)
            {
                return false;
            }

            foreach (TagSingle tag in tags)
            {
                if (tag != null && tag.CurrentSlot == null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>把全部 Tag 收回初始位置。</summary>
        public void ResetAll(bool animated = true)
        {
            foreach (TagSingle tag in GetAllTags())
            {
                tag?.ReturnHome(animated);
            }
        }

        /// <summary>某个 Tag 是否正待在它的初始位置。</summary>
        public bool IsAtHome(TagSingle tag)
        {
            if (tag == null)
            {
                return false;
            }

            return tag.CurrentSlot == null && tag.transform.parent == Container;
        }

        private void Log(string message)
        {
            if (mLogList)
            {
                Debug.Log($"[TagSourceList/{name}] {message}");
            }
        }
    }
}
