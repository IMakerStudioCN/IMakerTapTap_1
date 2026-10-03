using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
	// Generate Id:689616e3-3547-414f-9296-8ec58a89389f
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
