using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
	// Generate Id:4f111a1f-d5bd-4db6-895d-0e71952addd3
	public partial class theFirstStartPlay
	{
		public const string Name = "theFirstStartPlay";
		
		[SerializeField]
		public UnityEngine.UI.Image Taskbar;
		[SerializeField]
		public UnityEngine.UI.Toggle PlayStatuMenu;
		[SerializeField]
		public UnityEngine.UI.Toggle Menu;
		[SerializeField]
		public UnityEngine.UI.Image word;
		[SerializeField]
		public UnityEngine.UI.Image map;
		[SerializeField]
		public UnityEngine.UI.Image sleep;
		[SerializeField]
		public UnityEngine.UI.Image emial;
		[SerializeField]
		public UnityEngine.UI.Image web;
		
		private theFirstStartPlayData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Taskbar = null;
			PlayStatuMenu = null;
			Menu = null;
			word = null;
			map = null;
			sleep = null;
			emial = null;
			web = null;
			
			mData = null;
		}
		
		public theFirstStartPlayData Data
		{
			get
			{
				return mData;
			}
		}
		
		theFirstStartPlayData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new theFirstStartPlayData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
