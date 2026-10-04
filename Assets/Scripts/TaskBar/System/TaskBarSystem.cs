using QFramework;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    // 已生成模块接口 ITaskBarSystem，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterSystem<ITaskBarSystem>(new TaskBarSystem());
    public interface ITaskBarSystem : ISystem
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
        public void RegisterTaskBar(string taskBarName, TaskBarSingle taskBar);
        public void UnregisterTaskBar(string taskBarName);
        public TaskBarSingle GetTaskBarSingle(string taskBarName);
    }

    public class TaskBarSystem : AbstractSystem, ITaskBarSystem
    {
        Dictionary<string, TaskBarSingle> taskBars = new Dictionary<string, TaskBarSingle>();
        protected override void OnInit()
        {
        }

        public void RegisterTaskBar(string taskBarName, TaskBarSingle taskBar)
        {
            if (!taskBars.ContainsKey(taskBarName))
            {
                taskBars.Add(taskBarName, taskBar);
            }
        }
        public void UnregisterTaskBar(string taskBarName)
        {
            if (taskBars.ContainsKey(taskBarName))
            {
                taskBars.Remove(taskBarName);
            }
        }

        public TaskBarSingle GetTaskBarSingle(string taskBarName)
        {
            if (!taskBars.ContainsKey(taskBarName))
            {
                return null;
            }
            return taskBars[taskBarName];
        }
    }
}
