using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
    public class KeywordPopupData : UIPanelData
    {
    }

    public partial class KeywordPopup : UIPanel
    {
        // 弹窗停留时长（秒）
        private const float AutoHideDelay = 2.5f;

        // 弹窗音效名（对应 Resources 里音频文件的名字，不含扩展名）
        //private const string PopupSoundName = "keyword_popup";

        private Coroutine mAutoHideCoroutine;

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as KeywordPopupData ?? new KeywordPopupData();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
        }

        protected override void OnShow()
        {
            // 显示时播放弹窗音效
           // AudioKit.PlaySound(PopupSoundName);

            // 每次显示都重新计时，避免残留上一个计时器
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
            CloseSelf();   // 关闭并销毁面板
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
