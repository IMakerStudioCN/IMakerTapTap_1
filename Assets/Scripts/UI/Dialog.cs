using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TapTapFirst.ScreenFx;

namespace TapTapFirst
{
	public class DialogData : UIPanelData
	{
	}
	public partial class Dialog : UIPanel
	{
		public CanvasGroup mGroup;
		public GameObject inDialog;
		IActionController mFadeCtrl;

		const float DialogGlobalIntensity = 0;
		const float ScreenFxFadeTime = 0.25f;
		IActionController mScreenFxCtrl;

        const float FadeTime = 1f;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as DialogData ?? new DialogData();
			// please add init code here
			if(mData != null ) mGroup = GetComponentInChildren<CanvasGroup>();

            mGroup.alpha = 0f;
		}

		protected override void OnOpen(IUIData uiData = null)
		{

		}
        // 开启时，先杀掉之前的淡入淡出动画，避免出现闪烁
        public override void Show()
        {
            KillFade();
            base.Show(); 
        }
        // 开启时，真正的淡入动画在 OnShow 中执行，关闭时，真正的淡出动画在 Hide 中执行
        protected override void OnShow()
		{
            mGroup.gameObject.SetActive(true);
            mGroup.blocksRaycasts = false;
            Fade(0f, 1f, 0f, () => mGroup.blocksRaycasts = true, true);
			FadeScreenIntensity(DialogGlobalIntensity, ScreenFxFadeTime);
			
        }
		public override void Hide()
		{
            mGroup.blocksRaycasts = false;
			FadeScreenIntensity(1f, ScreenFxFadeTime);
            Fade(0f, 1f, 0f, () => base.Hide(), false); // 动画结束才真正 SetActive(false)
        }

        protected override void OnHide()
		{
            mGroup.alpha = 0f;
            mFadeCtrl = null;
        }

		protected override void OnClose()
		{
			if(mScreenFxCtrl != null) { mScreenFxCtrl.Deinit(); mScreenFxCtrl = null; }
			ScreenEffect.ResetToDefaults();
        }
		void Fade(float from, float midle,float to, System.Action onDone ,bool isShow)
		{
			KillFade();
			mGroup.alpha = from;
			mFadeCtrl = ActionKit.Sequence()
                .Callback(() => { mGroup.gameObject.SetActive(true); })
                .Lerp(from, midle, FadeTime, a => mGroup.alpha = a)   // 要缓动: a => EaseUtility.OutQuad(0,1,a)
				.Callback(() => { if (inDialog != null) inDialog.SetActive(isShow); })
				.Delay(1)
				.Lerp(midle, to, FadeTime, a => mGroup.alpha = a)
				.Callback(() => { mFadeCtrl = null; if (onDone != null) onDone(); })
                .Callback(() => { mGroup.gameObject.SetActive(false); })
                .Start(this);
		}

		void KillFade()
		{
			if (mFadeCtrl != null) { mFadeCtrl.Deinit(); mFadeCtrl = null; }

		}

		void FadeScreenIntensity(float to,float duration = ScreenFxFadeTime)
		{
			if(mScreenFxCtrl != null) { mScreenFxCtrl.Deinit(); mScreenFxCtrl = null; }
			mScreenFxCtrl = ActionKit.Sequence()
				.Lerp(ScreenEffect.GlobalIntensity,to,duration,v => ScreenEffect.GlobalIntensity = v)
				.Callback(() => mScreenFxCtrl = null)
                .Start(this);
        }
    }
}
