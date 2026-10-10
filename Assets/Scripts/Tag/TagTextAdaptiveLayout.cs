using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

namespace TapTapFirst
{
    /// <summary>
    /// 让 TagTextTemplate 的子物体【自动适配父节点大小】。
    ///
    ///   - 可用区域 = 父节点矩形 - padding（父节点尺寸不可用时退回自身矩形）
    ///   - 固定文本与空缺一起参与排版，按实际宽度贪心折行
    ///   - 每行水平居中，整块内容垂直居中
    ///   - 单行放不下时按比例缩小该行字号（不低于 minFontSize）
    ///   - 内容变化 / 尺寸变化 / 尺寸由 0 变为有效值时都会自动重排
    ///
    /// 排版算法在 TagSegmentLayout 里，与生成器共用同一份实现。
    /// 挂在 TagTextTemplate 同一个节点上即可。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TagTextTemplate))]
    public class TagTextAdaptiveLayout : MonoBehaviour
    {
        [Header("内边距（左, 上, 右, 下）")]
        [SerializeField] private Vector4 mPadding = new Vector4(24f, 16f, 24f, 16f);

        [Header("间距")]
        [Tooltip("同一行内相邻两段的水平间距")]
        [SerializeField] private float mSegmentSpacing = 8f;

        [Tooltip("行间距")]
        [SerializeField] private float mLineSpacing = 12f;

        [Header("字号")]
        [SerializeField] private float mMinFontSize = 16f;
        [SerializeField] private float mMaxFontSize = 48f;

        [Header("空缺尺寸")]
        [Tooltip("空缺方框相对字号的整体缩放系数")]
        [SerializeField] private float mSlotScale = 1f;

        [Tooltip("每行最多放几段；0 = 不限")]
        [SerializeField] private int mMaxSegmentsPerLine = 0;

        [Header("自动重排")]
        [Tooltip("尺寸变化时自动重排（关掉就只能手动调 Relayout）")]
        [SerializeField] private bool mAutoRelayout = true;

        [Header("诊断")]
        [Tooltip("打印每次重排的计算结果")]
        [SerializeField] private bool mLog;

        private TagTextTemplate mTemplate;
        private RectTransform mRect;

        private Vector2 mLastSelfSize = Vector2.negativeInfinity;
        private Vector2 mLastParentSize = Vector2.negativeInfinity;
        private bool mLaidOutOnce;

        private void Awake()
        {
            mTemplate = GetComponent<TagTextTemplate>();
            mRect = (RectTransform)transform;
        }

        private void OnEnable()
        {
            if (mTemplate == null)
            {
                mTemplate = GetComponent<TagTextTemplate>();
            }

            if (mRect == null)
            {
                mRect = (RectTransform)transform;
            }

            mTemplate.SegmentsChanged += Relayout;
            mTemplate.Collect();
            Relayout();
            StartCoroutine(RelayoutAfterLayout());
        }

        private void OnDisable()
        {
            if (mTemplate != null)
            {
                mTemplate.SegmentsChanged -= Relayout;
            }
        }

        private void LateUpdate()
        {
            if (!mAutoRelayout || mRect == null)
            {
                return;
            }

            Vector2 selfSize = mRect.rect.size;
            Vector2 parentSize = mRect.parent is RectTransform parentRect
                ? parentRect.rect.size
                : Vector2.zero;

            // 尺寸还没算完时也要能自愈，所以两个尺寸都盯着
            if (!mLaidOutOnce || selfSize != mLastSelfSize || parentSize != mLastParentSize)
            {
                Relayout();
            }
        }

        private IEnumerator RelayoutAfterLayout()
        {
            // 等布局算完（LayoutGroup / CanvasScaler 通常要一两帧）
            yield return null;
            Relayout();
            yield return new WaitForEndOfFrame();
            Relayout();
        }

        /// <summary>立即按当前内容与可用区域重排。</summary>
        public void Relayout()
        {
            if (mTemplate == null || mRect == null)
            {
                return;
            }

            mTemplate.EnsureCollected();

            mLastSelfSize = mRect.rect.size;
            mLastParentSize = mRect.parent is RectTransform parentRect
                ? parentRect.rect.size
                : Vector2.zero;

            TagLayoutSettings settings = BuildSettings();

            if (TagSegmentLayout.Layout(mRect, mTemplate.Segments, settings, out string report))
            {
                mLaidOutOnce = true;
                Log($"重排完成：{report}");
            }
            else
            {
                Log($"重排跳过：{report}");
            }
        }

        private TagLayoutSettings BuildSettings()
        {
            return new TagLayoutSettings
            {
                Padding = mPadding,
                SegmentSpacing = mSegmentSpacing,
                LineSpacing = mLineSpacing,
                MinFontSize = mMinFontSize,
                MaxFontSize = mMaxFontSize,
                SlotScale = mSlotScale,
                MaxSegmentsPerLine = mMaxSegmentsPerLine,
            };
        }

        /// <summary>把当前所有子物体的锚点/位置打印出来，用于排查排版问题。</summary>
        [ContextMenu("打印排版诊断")]
        public void DumpLayout()
        {
            if (mRect == null)
            {
                mRect = (RectTransform)transform;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"===== 排版诊断 {name} =====");
            sb.AppendLine($"自身 rect={mRect.rect.size} anchorMin={mRect.anchorMin} anchorMax={mRect.anchorMax}");

            if (mRect.parent is RectTransform parentRect)
            {
                sb.AppendLine($"父节点 {parentRect.name} rect={parentRect.rect.size}");
            }
            else
            {
                sb.AppendLine("父节点不是 RectTransform！");
            }

            if (mTemplate == null)
            {
                mTemplate = GetComponent<TagTextTemplate>();
            }

            sb.AppendLine($"片段数={(mTemplate != null ? mTemplate.Segments.Count : -1)} 子物体数={transform.childCount}");

            for (int i = 0; i < transform.childCount; i++)
            {
                RectTransform child = transform.GetChild(i) as RectTransform;

                if (child == null)
                {
                    continue;
                }

                bool hasTmp = child.GetComponent<TMP_Text>() != null;
                sb.AppendLine($"  [{i}] {child.name} anchoredPos={child.anchoredPosition} " +
                              $"size={child.sizeDelta} TMP={hasTmp}");
            }

            sb.AppendLine("=========================");
            Debug.Log(sb.ToString());
        }

        private void Log(string message)
        {
            if (mLog)
            {
                Debug.Log($"[TagTextAdaptiveLayout/{name}] {message}");
            }
        }
    }
}