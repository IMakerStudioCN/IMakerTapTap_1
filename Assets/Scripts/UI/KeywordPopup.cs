using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;

namespace TapTapFirst
{
    public class KeywordPopupData : UIPanelData
    {
        public string Keyword;
    }

    public partial class KeywordPopup : UIPanel
    {
        private const float AutoHideDelay = 2.5f;

        // 【修改点1】删除原来的常量，改为直接拖拽赋值的 AudioClip
        public AudioClip PopupSoundClip;

        // 关键词文字
        public TextMeshProUGUI KeywordText;

        private Coroutine mAutoHideCoroutine;
        private AudioSource mAudioSource; // 【修改点2】新增 AudioSource 组件引用

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as KeywordPopupData ?? new KeywordPopupData();

            // 【修改点3】获取或自动添加 AudioSource 组件
            mAudioSource = GetComponent<AudioSource>();
            if (mAudioSource == null)
            {
                mAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            var data = uiData as KeywordPopupData;
            if (data != null && KeywordText != null)
            {
                KeywordText.text = $"获得了 {data.Keyword} 关键词";
            }
        }

        protected override void OnShow()
        {
            // 【修改点4】使用 AudioSource 播放拖拽好的音效
            if (PopupSoundClip != null && mAudioSource != null)
            {
                mAudioSource.PlayOneShot(PopupSoundClip);
            }

            StopAutoHide();
            mAutoHideCoroutine = StartCoroutine(AutoHideRoutine());
        }

        protected override void OnHide()
        {
            StopAutoHide();
        }

        protected override void OnClose()
        {
            StopAutoHide();
        }

        protected override void OnBeforeDestroy()
        {
            StopAutoHide();
            base.OnBeforeDestroy();
        }

        private IEnumerator AutoHideRoutine()
        {
            yield return new WaitForSeconds(AutoHideDelay);
            mAutoHideCoroutine = null;
            CloseSelf();
        }

        private void StopAutoHide()
        {
            if (mAutoHideCoroutine != null)
            {
                StopCoroutine(mAutoHideCoroutine);
                mAutoHideCoroutine = null;
            }
        }
    }
}