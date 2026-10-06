using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    public class TaskEvent : MonoBehaviour
    {
    }
    public struct OnTaskStart
    {
       public TaskSingle startTask;
    }
    public struct OnTaskEnd
    {
        public TaskSingle endTask;
    }
}
