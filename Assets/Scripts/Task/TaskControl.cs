using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace TapTapFirst
{
	public partial class TaskControl : ViewController,IController
	{
		[SerializeField]
		private List<TaskSingle> config;

		private ITaskSystem mTaskSystem;

		void Start()
		{
			mTaskSystem = this.GetSystem<ITaskSystem>();
			mTaskSystem.InitTask(config);
		}

	}
}
