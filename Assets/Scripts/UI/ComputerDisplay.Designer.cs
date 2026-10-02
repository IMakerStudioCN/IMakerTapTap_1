using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.Example
{
	// Generate Id:45edf20a-4c82-4b32-9f3a-6470c7e93773
	public partial class ComputerDisplay
	{
		public const string Name = "ComputerDisplay";
		
		[SerializeField]
		public UnityEngine.UI.Image Image;
		[SerializeField]
		public UnityEngine.UI.Button Menu;
		[SerializeField]
		public UnityEngine.UI.Button PlayStatus;
		
		private ComputerDisplayData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Image = null;
			Menu = null;
			PlayStatus = null;
			
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
