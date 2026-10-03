using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
	// Generate Id:a082fcd7-d04f-4e86-a373-1607afe1f50f
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
