using UnityEngine;
using QFramework;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

namespace TapTapFirst
{
	public partial class DialogController : ViewController,IController
	{
		public Button mButton;
		public Image mStanding;
		public ScrollRect mScrollRect;

		IDialogSystem mDialogSystem;
        DialogSO mDialogSO;
		TaskSingle mTask;
		int index;
		void Awake()
		{

            mDialogSystem = this.GetSystem<IDialogSystem>();
			index = 0;
			//得到一个So
			this.RegisterEvent<OnToPlaceEvent>((e) =>
			{
				SetDialog(e.Task);
			}).UnRegisterWhenGameObjectDestroyed(gameObject);
			Debug.Log($"Dialog Awake id={GetInstanceID()} parent={transform.parent?.name}");

        }
		void OnEnable()
		{
			PullPendingTask();
		}
        void Start()
		{
            mButton.onClick.AddListener(() =>
            {
                DialogTransition();
            });
        }
		void Update()
		{
			//兜底的防止玩家同时点击到另一给marker
			if (mDialogSystem.HasPendingTask)
			{
				PullPendingTask();
			}
		}
		//拉取任务
		void PullPendingTask()
		{
			TaskSingle task = mDialogSystem.ConsumePendingTask(); ;
			if(task == null)
			{
				return;
			}
			SetDialog(task);
		}

		//对话点击继续功能
		public void DialogTransition()
		{
			if(mDialogSO == null|| mDialogSO.dialog == null || mDialogSO.dialog.Count == 0)
			{
				Debug.LogWarning("无对话可以用");
				return;
			}
			//对话结束的时候
            if (index < 0 || index >= mDialogSO.dialog.Count)
            {
				if (mTask != null) this.GetSystem<ITaskSystem>().TaskDone(mTask.TaskName);
				if(mButton != null) mButton.gameObject.SetActive(false);
                //清空挂载的预制体
                this.transform.DetachChildren();
				//隐藏地图图标
                UIKit.GetPanel<Dialog>().Hide();
                return;
            }
            DialogSOSingle mSingle = mDialogSO.dialog[index];
			index ++;
			if (mSingle == null || mSingle.prefab == null)
			{
				Debug.LogWarning($"{mDialogSO.title}第{index}没有配置prefab");
				return;
			}
			mStanding.sprite = mSingle.Standing;
			GameObject mObject =  Instantiate<GameObject>(mSingle.prefab,this.transform);
			mObject.GetComponentInChildren<TextMeshProUGUI>().text = mSingle.Sentence;
			
			
		}
		//设置对话内容
		void SetDialog(TaskSingle task)
		{
			Debug.Log($"SetDialog {task?.dialogTitle}");

            if (task == null)
			{
				Debug.LogWarning("Dialog传进的task为空");
			}
			mTask = task;
			DialogSO so = mDialogSystem.GetDialogSO(task.dialogTitle);

			if (so == null)
			{
				Debug.LogWarning("DialogSo为空");
				return;
			}
			mDialogSO = so;
			index = 0;
			if (mButton != null) {
				mButton.gameObject.SetActive(true);
			}
		}

    }
}
