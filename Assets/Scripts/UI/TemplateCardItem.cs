using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TapTapFirst
{
    /// <summary>
    /// 写作面板左侧模板列表里的一张模板卡片。
    ///
    /// 挂载：卡片节点上（由列表生成器创建；这类"运行时 Instantiate 的列表项"按
    /// 《WindowKit 使用手册》§2.4 的例外，脚本挂在自己的 item prefab 根上）。
    ///
    /// 只做三件事：显示标题、被点击时把 newsId 广播出去、切换选中态外观。
    ///
    /// 颜色规则（方案 B：预制体说了算）：
    ///   - 常态颜色 = **预制体上 Image 的颜色**（Awake 时从根 Image 抓取，运行时不覆盖）
    ///   - 选中颜色 = 常态颜色**加深**（RGB 乘以 mSelectedDarken，Alpha 保持不变）
    ///   - mNormalColor / mSelectedColor 只在"没找到 Image"时兜底，已不在 Inspector 暴露
    /// </summary>
    public class TemplateCardItem : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        [Header("这张卡片对应哪条新闻模板")]
        [SerializeField] private int mNewsId = -1;

        [SerializeField] private TMP_Text mTitleText;

        [Header("选中反馈")]
        [Tooltip("要染色的背景图；留空自动取根节点的 Image")]
        [SerializeField] private Image mBackground;

        [Tooltip("选中时把常态颜色的 RGB 乘以这个系数（越小越深）；Alpha 不变。1 = 不加深")]
        [Range(0.3f, 1f)]
        [SerializeField] private float mSelectedDarken = 0.72f;

        // 以下两个只做兜底（没找到 Image 时用），不再在 Inspector 暴露，
        // 常态颜色一律以预制体上 Image 的 m_Color 为准。
        [HideInInspector] [SerializeField] private Color mNormalColor = new Color(1f, 1f, 1f, 0.14f);
        [HideInInspector] [SerializeField] private Color mSelectedColor = new Color(0.35f, 0.75f, 1f, 0.45f);

        private bool mCapturedColor;
        private Color mCapturedNormalColor = Color.white;
        private Color mCapturedSelectedColor = Color.white;

        /// <summary>点击这张卡片时把 newsId 抛出去。</summary>
        public event Action<int> OnSelected;

        /// <summary>这张卡片对应的新闻 id。</summary>
        public int NewsId => mNewsId;

        /// <summary>是否处于选中态。</summary>
        public bool IsSelected { get; private set; }

        /// <summary>常态颜色（= 预制体上的颜色）。</summary>
        public Color NormalColor => mCapturedColor ? mCapturedNormalColor : mNormalColor;

        /// <summary>选中颜色（= 常态颜色加深）。</summary>
        public Color SelectedColor => mCapturedColor ? mCapturedSelectedColor : mSelectedColor;

        private void Awake()
        {
            if (mTitleText == null)
            {
                mTitleText = GetComponentInChildren<TMP_Text>(true);
            }

            if (mBackground == null)
            {
                mBackground = GetComponent<Image>();
            }

            CaptureColors();
        }

        /// <summary>
        /// 抓取"常态颜色"：优先读预制体上 Image 的当前颜色，
        /// 再按 mSelectedDarken 算出选中颜色。重复调用安全（已抓过就跳过）。
        /// </summary>
        public void CaptureColors()
        {
            if (mCapturedColor)
            {
                return;
            }

            Color normal = mBackground != null ? mBackground.color : mNormalColor;

            mCapturedNormalColor = normal;
            mCapturedSelectedColor = Darken(normal, mSelectedDarken);
            mCapturedColor = true;
        }

        /// <summary>按预制体颜色重新抓一次（改了 Image 颜色后想立即生效可手动调）。</summary>
        public void RecaptureColors()
        {
            mCapturedColor = false;
            CaptureColors();

            if (mBackground != null)
            {
                mBackground.color = IsSelected ? mCapturedSelectedColor : mCapturedNormalColor;
            }
        }

        /// <summary>RGB 乘以系数变深，Alpha 保持不变。</summary>
        private static Color Darken(Color color, float factor)
        {
            factor = Mathf.Clamp01(factor);

            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
        }

        /// <summary>填数据（生成卡片时调用）。</summary>
        public void Setup(NewsTemplate_SO news)
        {
            if (news == null)
            {
                return;
            }

            mNewsId = news.newsId;

            if (mTitleText == null)
            {
                mTitleText = GetComponentInChildren<TMP_Text>(true);
            }

            if (mTitleText != null)
            {
                mTitleText.text = news.newsTitle;
            }
        }

        /// <summary>直接指定 id 与标题（生成器用）。</summary>
        public void Setup(int newsId, string title)
        {
            mNewsId = newsId;

            if (mTitleText == null)
            {
                mTitleText = GetComponentInChildren<TMP_Text>(true);
            }

            if (mTitleText != null)
            {
                mTitleText.text = title;
            }
        }

        /// <summary>选中态反馈：常态 = 预制体颜色，选中 = 加深。</summary>
        public void SetSelected(bool selected)
        {
            IsSelected = selected;

            CaptureColors();

            if (mBackground != null)
            {
                mBackground.color = selected ? mCapturedSelectedColor : mCapturedNormalColor;
            }
        }

        // 空实现：抢下 pointerPress，OnPointerClick 才会派发到本节点（手册 §2.3 规范 3）
        public void OnPointerDown(PointerEventData eventData) { }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnSelected?.Invoke(mNewsId);
        }
    }
}