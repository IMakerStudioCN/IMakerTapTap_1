using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
	public class PlayStatusMenuData : UIPanelData
	{
	}
	public partial class PlayStatusMenu : UIPanel
	{
		private IPlayerModel mPlayerModel;
		private bool mBound;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as PlayStatusMenuData ?? new PlayStatusMenuData();
			Bind();
		}

		private void Bind()
		{
			if (mBound)
			{
				return;
			}

			mBound = true;
			mPlayerModel = TapTap.Interface.GetModel<IPlayerModel>();

			TapTap.Interface.RegisterEvent<FundsValueChangeEvent>(OnFundsValueChanged);
			TapTap.Interface.RegisterEvent<EvilValueChangeEvent>(OnEvilValueChanged);
			TapTap.Interface.RegisterEvent<CredibilityValueChangeEvent>(OnCredibilityValueChanged);

			RefreshValues();
		}

		private void Unbind()
		{
			if (!mBound)
			{
				return;
			}

			mBound = false;

			TapTap.Interface.UnRegisterEvent<FundsValueChangeEvent>(OnFundsValueChanged);
			TapTap.Interface.UnRegisterEvent<EvilValueChangeEvent>(OnEvilValueChanged);
			TapTap.Interface.UnRegisterEvent<CredibilityValueChangeEvent>(OnCredibilityValueChanged);

			mPlayerModel = null;
		}

		private void OnFundsValueChanged(FundsValueChangeEvent e)
		{
			RefreshValues();
		}

		private void OnEvilValueChanged(EvilValueChangeEvent e)
		{
			RefreshValues();
		}

		private void OnCredibilityValueChanged(CredibilityValueChangeEvent e)
		{
			RefreshValues();
		}

		/// <summary>用模型里的当前值刷新三个滑条；资金没有上限，交给滑条动态跟随。</summary>
		private void RefreshValues()
		{
			if (mPlayerModel == null)
			{
				return;
			}

			if (FundsValue != null)
			{
				int funds = mPlayerModel.FundsValue;

				if (funds > FundsValue.maxValue)
				{
					FundsValue.maxValue = funds;
				}

				FundsValue.value = funds;
			}

			if (EvilValue != null)
			{
				EvilValue.value = mPlayerModel.EvilValue;
			}

			if (CredibilityValue != null)
			{
				CredibilityValue.value = mPlayerModel.CredibilityValue;
			}
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			RefreshValues();
		}

		protected override void OnShow()
		{
		}

		protected override void OnHide()
		{
		}

		protected override void OnClose()
		{
			Unbind();
		}

		protected override void OnBeforeDestroy()
		{
			Unbind();
			base.OnBeforeDestroy();
		}
	}
}
