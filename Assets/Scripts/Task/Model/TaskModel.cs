using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace TapTapFirst
{
    public class TaskModel
    {
    }
    

    //对接Json存储的纯C#类
    [Serializable]
    public class TaskModelData
    {
        public List<string> isDoneTask = new List<string>();
        public List<string> isDoingTask = new List<string>();
    }
}
