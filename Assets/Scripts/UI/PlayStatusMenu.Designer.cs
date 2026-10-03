using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
	// Generate Id:8fdfa135-8ed5-4602-9ef7-b4b4da7aa1eb
	public partial class PlayStatusMenu
	{
		public const string Name = "PlayStatusMenu";
		
		[SerializeField]
		public UnityEngine.UI.Slider FundsValue;
		[SerializeField]
		public UnityEngine.UI.Slider EvilValue;
		[SerializeField]
		public UnityEngine.UI.Slider CredibilityValue;
		
		private PlayStatusMenuData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			FundsValue = null;
			EvilValue = null;
			CredibilityValue = null;
			
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
