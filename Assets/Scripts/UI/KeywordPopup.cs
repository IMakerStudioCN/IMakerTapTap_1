using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using System;

namespace TapTapFirst
{
    public class KeywordPopupData : UIPanelData
    {
        public string Keyword;
    }

    public partial class KeywordPopup : UIPanel
    {
        private const float AutoHideDelay = 2.5f;

        // 拖拽赋值的音效 Clip
        public AudioClip PopupSoundClip;

        // 关键词文字
        public TextMeshProUGUI KeywordText;

        public RectTransform PopupRectTransform;

        private Coroutine mAutoHideCoroutine;

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as KeywordPopupData ?? new KeywordPopupData();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            var data = uiData as KeywordPopupData;
            if (data != null && KeywordText != null)
            {
                KeywordText.text = $"{data.Keyword}";
                Debug.Log($"KeywordPopup: 显示关键词弹窗，关键词为 {data.Keyword}");
            }
        }

        protected override void OnShow()
        {
            // 使用 AudioKit 播放音效，自动受全局音效音量（SoundVolume）控制
            if (PopupSoundClip != null)
            {
                AudioKit.PlaySound(PopupSoundClip);
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
