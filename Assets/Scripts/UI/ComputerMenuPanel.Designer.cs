using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.Example
{
	// Generate Id:753b3ce9-d122-4742-b77c-63842351887a
	public partial class ComputerMenuPanel
	{
		public const string Name = "ComputerMenuPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Setting;
		[SerializeField]
		public UnityEngine.UI.Button Exit;
		
		private ComputerMenuPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Setting = null;
			Exit = null;
			
			mData = null;
		}
		
		public ComputerMenuPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		ComputerMenuPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ComputerMenuPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
