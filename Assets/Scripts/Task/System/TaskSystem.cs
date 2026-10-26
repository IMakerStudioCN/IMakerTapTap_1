using QFramework;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

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
        public bool CheckTaskIsDone(string taskName);

        /// <summary>是否存在“已过期但没有完成”的主线任务（ismain == true）。</summary>
        bool HasUnfinishedExpiredMainTask();
    }

    public class TaskSystem : AbstractSystem, ITaskSystem
    {
        private IJsonSaveUtility JsonSaveUtility => this.GetUtility<IJsonSaveUtility>();
        private ITimeModel mTimeModel;
        //存档内
        public List<string> isDoneList => JsonSaveUtility.Get<TaskModelData>("TaskModelData").isDoneTask;
        public List<string> tasksInDoing => JsonSaveUtility.Get<TaskModelData>("TaskModelData").isDoingTask;
        public List<string> expiredList => JsonSaveUtility.Get<TaskModelData>("TaskModelData").ExpiredTask;

        //游戏内运行时
        List<TaskSingle> daysTask = new List<TaskSingle>();
        List<TaskSingle> runTaskInDo = new List<TaskSingle>();
        List<TaskSingle> expiredRuntimeTask = new List<TaskSingle>();
        //已了结查表：完成或过期都算已了结，只用于前置任务判定
        HashSet<TaskSingle> taskIsSettled = new HashSet<TaskSingle>();

        protected override void OnInit()
        {
            mTimeModel = this.GetModel<ITimeModel>();
            //同步状态
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
                if (days >= task.endDay)                            // 没开始就过期
                {
                    daysTask.RemoveAt(i);
                    CloseQueuedTask(task);
                    continue;
                }
                if (task.prePosition != null && !taskIsSettled.Contains(task.prePosition)) continue;   // 前置没了结：留着
                daysTask.RemoveAt(i);
                ActivateTask(task);
            }
        }

        public void CheckEndDay(int days)
        {

            for(int i = runTaskInDo.Count - 1;i >= 0; i--)
            {
                if(days < runTaskInDo[i].endDay) continue;
                CloseRunningTask(i, true);                          // true = 过期
            }
        }
        //对话完成之后就处理任务为完成状态
        public void TaskDone(string taskname)
        {
            for (int i = runTaskInDo.Count - 1; i >= 0; i--)
            {
                if (runTaskInDo[i].TaskName != taskname) continue;
                CloseRunningTask(i, false);                         // false = 正常完成
            }
        }
        void ActivateTask(TaskSingle task)
        {
            runTaskInDo.Add(task);
            if (!tasksInDoing.Contains(task.TaskName)) tasksInDoing.Add(task.TaskName);

            this.SendEvent(new HaveNewMessageEvent { webID = 4 });
            this.SendEvent(new SendEmailEvent { EmailWebID = task.EmailId,Task = task });

            this.SendEvent(new OnTaskStart { startTask = task });
        }

        //进行中收尾：完成和过期都走这里，区别只在记到哪个存档列表
        void CloseRunningTask(int index, bool isExpired)
        {
            TaskSingle task = runTaskInDo[index];
            runTaskInDo.RemoveAt(index);                             // 关键：一定要从进行中列表摘掉
            tasksInDoing.Remove(task.TaskName);

            if (isExpired) MarkExpired(task);
            else MarkDone(task);

            this.SendEvent(new OnTaskEnd { endTask = task , isExpired = isExpired });
        }

        //排队中收尾：必然过期。过期跟玩家接取没接取无关，所以这里也要走同一套收尾：
        //任务可能早就激活过（邮件已发、"前往"也点过、地图标记已显示），后来因为存档状态不完整又回到了 daysTask，
        //这时只写 ExpiredTask 而不清痕迹，那封邮件和那个地图标记会一直留着，玩家还能点进去做一条已经过期的任务。
        //对从没激活过的任务，MapSystem / EmailListSystem 的监听本来都是空操作（邮件不在列表里、地点没显示过）。
        void CloseQueuedTask(TaskSingle task)
        {
            tasksInDoing.Remove(task.TaskName);
            MarkExpired(task);
            this.SendEvent(new OnTaskEnd { endTask = task , isExpired = true});
        }

        void MarkDone(TaskSingle task)
        {
            expiredList.Remove(task.TaskName);                       // 完成优先，保证两个列表互斥
            if (!isDoneList.Contains(task.TaskName)) isDoneList.Add(task.TaskName);
            taskIsSettled.Add(task);
        }

        void MarkExpired(TaskSingle task)
        {
            if (isDoneList.Contains(task.TaskName)) return;          // 已完成的记录不被过期覆盖

            if (!expiredList.Contains(task.TaskName)) expiredList.Add(task.TaskName);
            if (!expiredRuntimeTask.Contains(task)) expiredRuntimeTask.Add(task);
            taskIsSettled.Add(task);
        }

        //再Controller获取其config
        public void InitTask(List<TaskSingle> config)
        {
            daysTask.Clear(); runTaskInDo.Clear(); taskIsSettled.Clear(); expiredRuntimeTask.Clear();
            foreach (TaskSingle task in config) {
                if (task == null) continue;
                if (isDoneList.Contains(task.TaskName))
                {
                    expiredList.Remove(task.TaskName);               // 历史脏数据自愈：完成优先
                    taskIsSettled.Add(task);
                    continue;
                }
                else if (expiredList.Contains(task.TaskName))
                {
                    expiredRuntimeTask.Add(task);
                    taskIsSettled.Add(task);                         // 过期也算已了结，后继任务能正常解锁
                    continue;
                }
                else if (tasksInDoing.Contains(task.TaskName))
                {
                    runTaskInDo.Add(task);
                    this.GetSystem<ISoftwareSystem>().setDicisTrue(4);
                    this.SendEvent(new SendEmailEvent { EmailWebID = task.EmailId, Task = task });
                    continue;
                }
                    daysTask.Add(task);
            }
            daysTask.Sort((a,b) => a.startDay.CompareTo(b.startDay));
            CheckStartDay(mTimeModel.days);
            CheckEndDay(mTimeModel.days);
        }

        /// <summary>该任务是否已经完成（完成记录写进存档的 isDoneTask）。</summary>
        public bool CheckTaskIsDone(string taskName)
        {
            if (string.IsNullOrEmpty(taskName))
            {
                return false;
            }

            TaskModelData data = JsonSaveUtility.Get<TaskModelData>("TaskModelData");

            if (data == null || data.isDoneTask == null)
            {
                return false;
            }

            return data.isDoneTask.Contains(taskName);
        }

        /// <summary>是否存在“已过期但没有完成”的主线任务（只统计 ismain == true 的）。</summary>
        public bool HasUnfinishedExpiredMainTask()
        {
            for (int i = 0; i < expiredRuntimeTask.Count; i++)
            {
                TaskSingle task = expiredRuntimeTask[i];

                if (task == null || !task.ismain)
                {
                    continue;
                }

                if (!CheckTaskIsDone(task.TaskName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

