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

            WindowKit.OnOpened.Register((w) =>
            {
                if (!mTaskBarSystem.GetTaskBarSingle(w.WindowName)) {
                    AddTaskSingle(w.WindowName);
                }
                
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            WindowKit.OnClosed.Register((w) =>
            {
                
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

        }

        public void AddTaskSingle(string softwareName)
        {
            GameObject taskSingle = Instantiate(taskSinglePrefab, transform);
            taskSingle.name = softwareName;
            TaskBarSingle task = taskSingle.GetComponent<TaskBarSingle>().Init(softwareName);
            mTaskBarSystem.RegisterTaskBar(softwareName, task);
            
        }


        IArchitecture IBelongToArchitecture.GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}
