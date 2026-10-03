using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
	// Generate Id:91b24a33-ef4b-486d-9c6d-a0ebc24611a0
	public partial class softwarePanel
	{
		public const string Name = "softwarePanel";
		
		[SerializeField]
		public UnityEngine.UI.Image Panel;
		
		private softwarePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Panel = null;
			
			mData = null;
		}
		
		public softwarePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		softwarePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new softwarePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
