using QFramework;
using System.Collections;
using System.Collections.Generic;

namespace TapTapFirst
{
    // 已生成模块接口 ITaskSystem，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterSystem<ITaskSystem>(new TaskSystem());
    public interface ITaskSystem : ISystem
    {
        // TODO: 在这里声明模块对外 API（属性/方法）

        public void InitTask(List<TaskSingle> config);
        public void TaskDone(string taskname);
        public void CheckEndDay(int days);
        public void CheckStartDay(int days);
    }

    public class TaskSystem : AbstractSystem, ITaskSystem
    {
        private IJsonSaveUtility JsonSaveUtility => this.GetUtility<IJsonSaveUtility>();
        private ITimeModel mTimeModel;
        public List<string> isDoneList => JsonSaveUtility.Get<TaskModelData>("TaskModelData").isDoneTask;

        public List<string> tasksInDoing => JsonSaveUtility.Get<TaskModelData>("TaskModelData").isDoingTask;
        Queue<TaskSingle> daysTask = new Queue<TaskSingle>();
        List<TaskSingle> runTaskInDo = new List<TaskSingle>();
        //完成速度查
        HashSet<TaskSingle> taskIsDone = new HashSet<TaskSingle>();
        
        protected override void OnInit()
        {
            mTimeModel = this.GetModel<ITimeModel>();
            this.RegisterEvent<OnDaysChangeEvent>((e) =>
            {
                CheckStartDay(mTimeModel.days);
                CheckEndDay(mTimeModel.days);
            });
        }

        public void CheckStartDay(int days)
        {
            if (daysTask.Count == 0) return;
            TaskSingle task = daysTask.Peek();
            if (days == task.startDay)
            {
                //是开始的天数并且完成前置任务，放入进行任务列表,并触发邮件刷新
                if (task.prePosition == null || taskIsDone.Contains(task.prePosition))
                {
                    runTaskInDo.Add(task);
                    tasksInDoing.Add(task.TaskName);
                    daysTask.Dequeue();
                    this.SendEvent(new OnTaskStart { startTask  = task});
                }   
            }
        }
        public void CheckEndDay(int days)
        {
            foreach (var task in runTaskInDo)
            {
                if(days == task.endDay)
                {
                    //是结束的天数，放入结束列表，并触发地图标志更新
                    isDoneList.Add(task.TaskName);
                    tasksInDoing.Remove(task.TaskName);
                    taskIsDone.Add(task);
                    runTaskInDo.Remove(task);
                    this.SendEvent(new OnTaskEnd { endTask = task });
                }
            }
        }
        //对话完成之后就处理任务为完成状态
        public void TaskDone(string taskname)
        {
            foreach (var task in runTaskInDo)
            {
                if (taskname == task.TaskName)
                {
                    //是结束的天数，放入结束列表，并触发地图标志更新
                    isDoneList.Add(task.TaskName);
                    tasksInDoing.Remove(task.TaskName);
                    taskIsDone.Add(task);
                    runTaskInDo.Remove(task);

                    this.SendEvent(new OnTaskEnd { endTask = task });
                }
            }
        }
        //再Controller获取其config
        public void InitTask(List<TaskSingle> config)
        {
            foreach (TaskSingle task in config) {
                if (isDoneList.Contains(task.TaskName))
                {
                    taskIsDone.Add(task);
                    continue;
                }
                else if (tasksInDoing.Contains(task.TaskName))
                {
                    runTaskInDo.Add(task);
                    continue;
                }
                daysTask.Enqueue(task);
            }
        }


    }
}
