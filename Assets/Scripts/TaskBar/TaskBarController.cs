using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine;
namespace TapTapFirst
{
    public class TaskBarController : MonoBehaviour, IController
    {
        [SerializeField]
        private GameObject taskSinglePrefab;

        private ITaskBarSystem mTaskBarSystem;

        void Start()
        {
            mTaskBarSystem = this.GetSystem<ITaskBarSystem>();

            //订阅事件
            WindowKit.OnOpened.Register((w) =>
            {
                if (!mTaskBarSystem.GetTaskBarSingle(w.WindowName)) {
                    AddTaskSingle(w.WindowName);
                }
                
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            WindowKit.OnClosed.Register((w) =>
            {
                RemoveTaskSingle(w.WindowName);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            WindowKit.OnStateChanged.Register((w) =>
            {
                
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        public void AddTaskSingle(string softwareName)
        {
            //创建一个新的任务栏单元，并注册到任务栏系统中
            GameObject taskSingle = Instantiate(taskSinglePrefab, transform);
            taskSingle.name = softwareName;
            TaskBarSingle task = taskSingle.GetComponent<TaskBarSingle>().Init(softwareName);
            mTaskBarSystem.RegisterTaskBar(softwareName, task);
            
        }
        public void RemoveTaskSingle(string softwareName)
        {
            TaskBarSingle task = mTaskBarSystem.GetTaskBarSingle(softwareName);
            if (task != null)
            {
                task.CloseWindow();
                mTaskBarSystem.UnregisterTaskBar(softwareName);
            }
        }


        IArchitecture IBelongToArchitecture.GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}
