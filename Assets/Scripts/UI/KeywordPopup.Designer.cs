using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
	// Generate Id:ab47cea5-a6eb-4607-ad78-f08c83cc82fc
	public partial class KeywordPopup
	{
		public const string Name = "KeywordPopup";
		
		
		private KeywordPopupData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			
			mData = null;
		}
		
		public KeywordPopupData Data
		{
			get
			{
				return mData;
			}
		}
		
		KeywordPopupData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new KeywordPopupData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
