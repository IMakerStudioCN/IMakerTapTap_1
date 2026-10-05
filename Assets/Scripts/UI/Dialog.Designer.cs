using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
	// Generate Id:688cdff5-6517-4529-8bc0-72b4c43bd598
	public partial class Dialog
	{
		public const string Name = "Dialog";
		
		
		private DialogData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			
			mData = null;
		}
		
		public DialogData Data
		{
			get
			{
				return mData;
			}
		}
		
		DialogData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new DialogData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
