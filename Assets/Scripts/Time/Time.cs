using UnityEngine;
using QFramework;

namespace TapTapFirst
{
	public partial class Time : ViewController
	{
		private ITimeModel _mTimeMdodel;
		private IJsonSaveUtility jsonUtility;
        void Start()
		{
			_mTimeMdodel = this.GetModel<ITimeModel>();
			jsonUtility = this.GetUtility<IJsonSaveUtility>();
        }

		void Update()
		{

		}
	}
}
