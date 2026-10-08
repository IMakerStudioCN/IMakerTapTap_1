using UnityEngine;
using QFramework;

namespace TapTapFirst
{
	public partial class DaysShow : ViewController,IController
	{
		private ITimeModel mTimeModel;
        void Start()
        {
            mTimeModel = this.GetModel<ITimeModel>();
            UpdateShow();
            this.RegisterEvent<OnDaysChangeEvent>(e =>
            {
                //先留在这里，后续迁移并完善游戏结束逻辑
                //后续增加天数判断用于触发剧情
                if (mTimeModel.days > mTimeModel.endDays)
                {
                   //游戏结束逻辑
                   Debug.Log("游戏结束");
                }
                UpdateShow();
            });
        }

        public void UpdateShow()
        {
            if (mTimeModel != null)
            {
                int disPlayDays = mTimeModel.days;
                DaysText.text = " " + "Days: " + disPlayDays.ToString();

            }
        }
        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }

        
	}
}
