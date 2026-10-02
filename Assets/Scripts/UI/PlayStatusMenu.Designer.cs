using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
	// Generate Id:b94d4539-1cff-4e53-a2ea-8343f54c327c
	public partial class PlayStatusMenu
	{
		public const string Name = "PlayStatusMenu";
		
		[SerializeField]
		public UnityEngine.UI.Slider FundsValue;
		[SerializeField]
		public UnityEngine.UI.Slider EvilValue;
		[SerializeField]
		public UnityEngine.UI.Slider CredibilityValue;
		[SerializeField]
		public TMPro.TextMeshProUGUI NowDays;
		
		private PlayStatusMenuData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			FundsValue = null;
			EvilValue = null;
			CredibilityValue = null;
			NowDays = null;
			
			mData = null;
		}
		
		public PlayStatusMenuData Data
		{
			get
			{
				return mData;
			}
		}
		
		PlayStatusMenuData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new PlayStatusMenuData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
