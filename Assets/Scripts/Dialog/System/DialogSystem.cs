using QFramework;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

namespace TapTapFirst
{
    // 已生成模块接口 IDialogSystem，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterSystem<IDialogSystem>(new DialogSystem());
    public interface IDialogSystem : ISystem
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
        public void GetConfig(List<DialogSO> config);
        public DialogSO GetDialogSO(string dialogID);

        //添加方法，解决因为淡入淡出造成的时序冲突
        void SetPendingTask(TaskSingle task);
        TaskSingle ConsumePendingTask();
        bool HasPendingTask { get; }
    }

    public class DialogSystem : AbstractSystem, IDialogSystem
    {
        Dictionary<string, DialogSO> configs = new Dictionary<string, DialogSO>();
        private TaskSingle mPendingTask;
        public bool HasPendingTask => mPendingTask != null;
        protected override void OnInit()
        {


        }

        //得到配置文件，刷新对话框用
        public void GetConfig(List<DialogSO> config)
        {
            if (config == null) return;
            foreach (DialogSO s in config) {
                if (!config.Contains(s))
                {
                    Debug.LogWarning("DialogSo的title为空");
                    continue;
                }

                configs[s.title] = s;
            }


        }
        // 得到全局配置
        //目前健壮性不太强
        public DialogSO GetDialogSO(string dialogID)
        {
            if (configs.ContainsKey(dialogID))
            {
                return configs[dialogID];
            }
            return null;
        }

        //更改pendingTask的状态
        public void SetPendingTask(TaskSingle task)
        {
            mPendingTask = task;
        }
        public TaskSingle ConsumePendingTask()
        {
            TaskSingle task = mPendingTask;
            mPendingTask = null;
            return task;
        }
    }
}
