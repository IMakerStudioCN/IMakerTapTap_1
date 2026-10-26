using QFramework;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    // 已生成模块接口 IEmailListSystem，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterSystem<IEmailListSystem>(new EmailListSystem());
    public interface IEmailListSystem : ISystem
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
        List<EmailLine_SO> getEmailSOList();
        List<EmailLine_SO> GetEmailYouHaveList();
        public List<EmailLine_SO> GetEmailCheck();

        //找对应ID的邮件
        EmailLine_SO GetEmailLineByID(int EmailID);
        //增删查改:针对EmailYouHaveList
        //删除邮件
        void DelEmailYouHave(int EmailID);
        //增加邮件
        void AddEmailYouHave(int EmailID);
        public void AddEmailYouCheck(int EmailID);
        //查找是否有邮件
        bool CheckEmailYouHave(int EmailID);


        //获取拥有的邮件
        EmailLine_SO GetEmailLineByIDInYouHave(int EmailID);
        //初始化EmilLine
        void StartEmilLine(List<EmailLine_SO> emailLine);

        //供外部传递Task
        public TaskSingle GetTaskByEmail(int EmailID);
        public TaskSingle GetTaskByPlace(string placeName);
    }

    public class EmailListSystem : AbstractSystem, IEmailListSystem
    {    

        private ResLoader mResLoader = ResLoader.Allocate();  
        private IJsonSaveUtility mJsonSaveUtility => this.GetUtility<IJsonSaveUtility>();


        public List<EmailLine_SO> EmailLineList = new List<EmailLine_SO>();
        public List<EmailLine_SO> EmailYouHaveList = new List<EmailLine_SO>();
        public List<EmailLine_SO> IsCheckEmail = new List<EmailLine_SO>();

        public List<int> EmailYouHaveData;

        private readonly Dictionary<int,TaskSingle> mTaskByEmail = new Dictionary<int,TaskSingle>();

        
        //找对应ID的邮件
        
        protected override void OnInit()
        {
            // 这一步失败也不能抛出去：下面的 SendEmailEvent / OnTaskEnd 订阅如果没注册，邮件就永远收不到，
            // 而且异常还会中断 InitArchitecture 里后续所有系统的初始化。
            try
            {
                EmailLineList_SO listSO = mResLoader.LoadSync<EmailLineList_SO>("EmailLineList_SO");

                EmailLineList = (listSO != null && listSO.EmailLines != null)
                    ? listSO.EmailLines
                    : new List<EmailLine_SO>();

                if (listSO == null || listSO.EmailLines == null)
                {
                    Debug.LogError("[EmailListSystem] 邮件总表 EmailLineList_SO 内容为空，本次收不到任何邮件");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[EmailListSystem] 加载邮件总表 EmailLineList_SO 失败，本次收不到任何邮件：" + e);
                EmailLineList = new List<EmailLine_SO>();
            }

            EmailListSystemData saveData = mJsonSaveUtility.Get<EmailListSystemData>("EmailListSystemData");

            if (saveData == null)
            {
                Debug.LogError("[EmailListSystem] 存档里没有 EmailListSystemData，请检查 TapTap.Init() 是否执行了 save.Add<EmailListSystemData>()");
                saveData = new EmailListSystemData();
            }

            if (saveData.EmailYouHaveIds == null) saveData.EmailYouHaveIds = new List<int>();
            if (saveData.IsCheckEmailIds == null) saveData.IsCheckEmailIds = new List<int>();

            // 列表内容一律由 id 反查重建，读档 / 换槽位后都保持和存档一致
            RebuildEmailYouHaveList();
            RebuildEmailCheckList();

            //注册事件
            this.RegisterEvent<SendEmailEvent>(e =>
            {
                if(e.Task != null)
                {
                    mTaskByEmail[e.EmailWebID] = e.Task;
                    //接受发送的邮件ID事件，更新邮件列表
                    AddEmailYouHave(e.Task.EmailId);
                }  
            });
            this.RegisterEvent<OnTaskEnd>(e =>
            {
                if (CheckEmailYouHave(e.endTask.EmailId))
                {
                    DelEmailYouHave(e.endTask.EmailId);
                    this.SendEvent<SendEmailEvent>();
                }
            });
        }
        public EmailLine_SO GetEmailLineByID(int id)
        {
            EmailLine_SO so = EmailLineList.Find(x => x != null && x.EmailID == id);

            if (so == null)
            {
                Debug.LogWarning("没有这个邮件");
            }
            return so;
        }
        public void AddEmailYouHave(int EmailID)
        {
            EmailLine_SO so = GetEmailLineByID(EmailID);
            if (so == null) return;

            EmailListSystemData data = mJsonSaveUtility.Get<EmailListSystemData>("EmailListSystemData");
            if (data == null) return;
            if (data.EmailYouHaveIds == null) data.EmailYouHaveIds = new List<int>();

            if (!data.EmailYouHaveIds.Contains(EmailID)) data.EmailYouHaveIds.Add(EmailID);
            if (!EmailYouHaveList.Contains(so)) EmailYouHaveList.Add(so);
        }

        public bool CheckEmailYouHave(int EmailID)
        {
            EmailListSystemData data = mJsonSaveUtility.Get<EmailListSystemData>("EmailListSystemData");

            if (data != null && data.EmailYouHaveIds != null)
            {
                return data.EmailYouHaveIds.Contains(EmailID);
            }

            EmailLine_SO so = FindEmailLineByID(EmailID);
            return so != null && EmailYouHaveList.Contains(so);
        }

        public void AddEmailYouCheck(int EmailID)
        {
            EmailLine_SO so = GetEmailLineByID(EmailID);
            if (so == null) return;

            EmailListSystemData data = mJsonSaveUtility.Get<EmailListSystemData>("EmailListSystemData");
            if (data == null) return;
            if (data.IsCheckEmailIds == null) data.IsCheckEmailIds = new List<int>();

            if (!data.IsCheckEmailIds.Contains(EmailID)) data.IsCheckEmailIds.Add(EmailID);
            if (!IsCheckEmail.Contains(so)) IsCheckEmail.Add(so);
        }

        public void DelEmailYouHave(int EmailID)
        {
            EmailListSystemData data = mJsonSaveUtility.Get<EmailListSystemData>("EmailListSystemData");

            if (data != null && data.EmailYouHaveIds != null) data.EmailYouHaveIds.Remove(EmailID);

            EmailLine_SO so = FindEmailLineByID(EmailID);
            if (so != null) EmailYouHaveList.Remove(so);
        }

        /// <summary>按 id 找邮件配置；找不到返回 null，不刷警告（重建列表时用）。</summary>
        private EmailLine_SO FindEmailLineByID(int id)
        {
            return EmailLineList != null ? EmailLineList.Find(x => x != null && x.EmailID == id) : null;
        }

        /// <summary>按存档里的 id 重建“拥有的邮件”。</summary>
        private void RebuildEmailYouHaveList()
        {
            EmailYouHaveList.Clear();

            EmailListSystemData data = mJsonSaveUtility.Get<EmailListSystemData>("EmailListSystemData");
            if (data == null || data.EmailYouHaveIds == null) return;

            for (int i = 0; i < data.EmailYouHaveIds.Count; i++)
            {
                EmailLine_SO so = FindEmailLineByID(data.EmailYouHaveIds[i]);
                if (so != null && !EmailYouHaveList.Contains(so)) EmailYouHaveList.Add(so);
            }
        }

        /// <summary>按存档里的 id 重建“已查看的邮件”。</summary>
        private void RebuildEmailCheckList()
        {
            IsCheckEmail.Clear();

            EmailListSystemData data = mJsonSaveUtility.Get<EmailListSystemData>("EmailListSystemData");
            if (data == null || data.IsCheckEmailIds == null) return;

            for (int i = 0; i < data.IsCheckEmailIds.Count; i++)
            {
                EmailLine_SO so = FindEmailLineByID(data.IsCheckEmailIds[i]);
                if (so != null && !IsCheckEmail.Contains(so)) IsCheckEmail.Add(so);
            }
        }
        public List<EmailLine_SO> getEmailSOList()
        {
            return EmailLineList;
        }

        public List<EmailLine_SO> GetEmailYouHaveList()
        {
            RebuildEmailYouHaveList();
            return EmailYouHaveList;
        }

        public List<EmailLine_SO> GetEmailCheck()
        {
            RebuildEmailCheckList();
            return IsCheckEmail;
        }

        public EmailLine_SO GetEmailLineByIDInYouHave(int EmailID)
        {
            if (CheckEmailYouHave(EmailID))
            {
                return GetEmailLineByID(EmailID);
            }
            Debug.LogWarning("拥有的邮件中没有这个邮件");
            return null;

        }
        public void StartEmilLine(List<EmailLine_SO> emailLine)
        {
            EmailLineList_SO listSO = mResLoader.LoadSync<EmailLineList_SO>("EmailLineList_SO");
            EmailLineList = (listSO != null && listSO.EmailLines != null) ? listSO.EmailLines : new List<EmailLine_SO>();
        }

        //新增两个方法供模块外引用
        public TaskSingle GetTaskByEmail(int EmailID)
        {
            return mTaskByEmail.TryGetValue(EmailID,out TaskSingle task) ? task : null;
        }

        public TaskSingle GetTaskByPlace(string placeName)
        {
            if (string.IsNullOrEmpty(placeName)) return null;
            foreach (EmailLine_SO line in GetEmailYouHaveList())
            {
                if(line == null || line.Place == null) continue;
                if(System.Array.IndexOf(line.Place,placeName) <0) continue;
                TaskSingle task = GetTaskByEmail(line.EmailID);
                if(task != null) return task;
            }
            return null;
        }

    }
    public class EmailListSystemData 
    {
        // 存档只存 id：JsonUtility 存不了 ScriptableObject 引用，读档回来会变成 null，
        // 运行时再按 id 反查 EmailLineList_SO 重建（和 TagSystem / NewsSystem 的做法一致）。
        public List<int> EmailYouHaveIds = new List<int>();
        public List<int> IsCheckEmailIds = new List<int>();
    }
}
