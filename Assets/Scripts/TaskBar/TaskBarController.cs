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
            WindowKit.OnOpened.Register(OnWindowOpened).UnRegisterWhenGameObjectDestroyed(gameObject);
            WindowKit.OnClosed.Register(OnWindowClosed).UnRegisterWhenGameObjectDestroyed(gameObject);
            WindowKit.OnFocused.Register(OnFocusChanged);
            WindowKit.OnStateChanged.Register(OnFocusChanged).UnRegisterWhenGameObjectDestroyed(gameObject);
            


            RefreshAllStyles();
        }


        public void RemoveTaskSingle(string softwareName)
        {
            var task = mTaskBarSystem.GetTaskBarSingle(softwareName);
            mTaskBarSystem.UnregisterTaskBar(softwareName);
            if (task != null)
            {
                Destroy(task.gameObject);
            }
        }

        private void OnWindowOpened(WindowsBasic window)
        {
            if (window == null) return;
            EnsureTaskSingle(window.WindowName);
            RefreshAllStyles();
        }
        private void OnWindowClosed(WindowsBasic window)
        {
            if (window == null) return;
            RemoveTaskSingle(window.WindowName);
            RefreshAllStyles();
        }
        private void OnFocusChanged(WindowsBasic window)
        {
            RefreshAllStyles();
        }

        private void EnsureTaskSingle(string windowName)
        {
            if(string.IsNullOrEmpty(windowName)) return;
            if (mTaskBarSystem.GetTaskBarSingle(windowName) != null) return;
            if(WindowKit.Get(windowName) == null) return;

            var go = Instantiate(taskSinglePrefab, transform);
            var task = go.GetComponent<TaskBarSingle>();
            if(task == null)
            {
                Debug.LogError("[TaskBar] taskSinglePrefab 上没有 TaskBarSingle 组件",go);
                Destroy(go);
                return;
            }
            task.mDestroy += OnTaskSingleDestoryed;
            task.Init(windowName);
            mTaskBarSystem.RegisterTaskBar(windowName, task);

        }

        private void RefreshAllStyles()
        {
            foreach(var window in WindowKit.Windows)
            {
                var task = mTaskBarSystem.GetTaskBarSingle(window.WindowName);
                if(task != null)
                {
                    task.RefreshStyle();
                }
            }
        }

        private void OnTaskSingleDestoryed(TaskBarSingle task)
        {
            if(task == null) return;
            mTaskBarSystem.UnregisterTaskBar(task.WindowName);
        }


        IArchitecture IBelongToArchitecture.GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}
