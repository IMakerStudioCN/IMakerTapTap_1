using QFramework;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace TapTapFirst
{
	public partial class MapMarker : ViewController,IController
	{
		public string placeName;
		Button haveCase;

        void Awake()
		{
            haveCase = GetComponent<Button>();
            haveCase.onClick.AddListener(OnClick);
        }
		public void ShowHaveCase()
		{
			if (haveCase == null) haveCase = GetComponent<Button>();
			if (haveCase == null) return;;
			if (!haveCase.isActiveAndEnabled)
			{
				haveCase.gameObject.SetActive(true);
			}
		}
		public void HideHaveCase()
		{
			if (haveCase == null) haveCase = GetComponent<Button>();
			if (haveCase == null) return;
			if (haveCase.isActiveAndEnabled)
			{
                haveCase.gameObject.SetActive(false);
			}
        }
        public void OnClick()
		{
			TaskSingle task = this.GetSystem<IMapSystem>().GetTaskForPlace(placeName);
			if (task == null)
			{
				Debug.LogWarning("地点无对应Task");
				return;
			}
			//主动拉取任务
			this.GetSystem<IDialogSystem>().SetPendingTask(task);
            //可见性统一交给 MapSystem：数据、通知、存档一起走
            this.GetSystem<IMapSystem>().HidePlace(placeName);
            // 点击地图标记时的逻辑,跳转到对应的
            UIKit.OpenPanel<Dialog>(UILevel.PopUI);
			
			//下面这个可以去除改为打开前直接拉取
			//发送事件配置对话
			//TapTap.Interface.SendEvent(new OnToPlaceEvent { Task = task });


        }

        public IArchitecture GetArchitecture()
        {
			return TapTap.Interface;
        }
    }
}
