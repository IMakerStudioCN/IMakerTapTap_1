using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
	// Generate Id:5d0f709e-d091-4759-8029-08dae3e30013
	public partial class ComputerDisplay
	{
		public const string Name = "ComputerDisplay";
		
		[SerializeField]
		public UnityEngine.UI.Image Image;
		[SerializeField]
		public UnityEngine.UI.Toggle PlayStatuMenu;
		[SerializeField]
		public UnityEngine.UI.Toggle Menu;
		
		private ComputerDisplayData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Image = null;
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
