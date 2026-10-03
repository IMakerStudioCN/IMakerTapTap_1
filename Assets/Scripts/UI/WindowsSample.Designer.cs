using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
	// Generate Id:f4ffe03e-d9ac-4898-bf5d-dcb0009dc04e
	public partial class WindowsSample
	{
		public const string Name = "WindowsSample";
		
		
		private WindowsSampleData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			
			mData = null;
		}
		
		public WindowsSampleData Data
		{
			get
			{
				return mData;
			}
		}
		
		WindowsSampleData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new WindowsSampleData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
