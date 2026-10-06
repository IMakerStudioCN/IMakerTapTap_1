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
        List<TaskSingle> daysTask = new List<TaskSingle>();
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
            for (int i = daysTask.Count - 1; i >= 0; i--)
            {
                TaskSingle task = daysTask[i];
                if (days < task.startDay) continue;                 // 没到期：留着，下次再看
                if (days > task.endDay) { daysTask.RemoveAt(i); continue; }  // 没开始就过期
                if (task.prePosition != null && !taskIsDone.Contains(task.prePosition)) continue;   // 前置没完成：留着
                daysTask.RemoveAt(i);
                ActivateTask(task);                                  // 见下
            }
        }
        
        public void CheckEndDay(int days)
        {
            foreach (var task in runTaskInDo)
            {
                if(days == task.endDay)
                {
                    //是结束的天数，放入结束列表，并触发地图标志更新
                    DisableTask(task);
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
                    DisableTask(task);
                }
            }
        }
        void ActivateTask(TaskSingle task)
        {
            runTaskInDo.Add(task);
            if (!tasksInDoing.Contains(task.TaskName)) tasksInDoing.Add(task.TaskName);
            JsonSaveUtility.Save();                                  // 立刻落盘
            this.SendEvent(new OnTaskStart { startTask = task });     // 现在全项目没人发它
            this.SendEvent(new SendEmailEvent { EmailWebID = task.EmailId });
        }
        void DisableTask(TaskSingle task)
        {
            isDoneList.Add(task.TaskName);
            tasksInDoing.Remove(task.TaskName);
            taskIsDone.Add(task);
            runTaskInDo.Remove(task);

            this.SendEvent(new OnTaskEnd { endTask = task });
        }
        //再Controller获取其config
        public void InitTask(List<TaskSingle> config)
        {
            daysTask.Clear(); runTaskInDo.Clear(); taskIsDone.Clear();
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
                daysTask.Add(task);
            }
            daysTask.Sort((a,b) => a.startDay.CompareTo(b.startDay));
            CheckStartDay(mTimeModel.days);
            CheckEndDay(mTimeModel.days);
        }


    }
}
