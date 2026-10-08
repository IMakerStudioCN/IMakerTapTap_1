using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
	// Generate Id:57533102-8aa1-4be2-a5eb-20e4b64b0417
	public partial class ComputerDisplay
	{
		public const string Name = "ComputerDisplay";
		
		[SerializeField]
		public UnityEngine.UI.Image Taskbar;
		[SerializeField]
		public UnityEngine.UI.Toggle PlayStatuMenu;
		[SerializeField]
		public UnityEngine.UI.Toggle Menu;
		
		private ComputerDisplayData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Taskbar = null;
			PlayStatuMenu = null;
			Menu = null;
			
			mData = null;
		}
		
		public ComputerDisplayData Data
		{
			get
			{
				return mData;
			}
		}
		
		ComputerDisplayData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ComputerDisplayData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
