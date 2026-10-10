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
        public List<EmailLine_SO> IsCheckEmail;

        public List<int> EmailYouHaveData;

        private readonly Dictionary<int,TaskSingle> mTaskByEmail = new Dictionary<int,TaskSingle>();

        
        //找对应ID的邮件
        
        protected override void OnInit()
        {
            EmailLineList = mResLoader.LoadSync<EmailLineList_SO>("EmailLineList_SO").EmailLines;

            EmailYouHaveList = mJsonSaveUtility.Get<EmailListSystemData>("EmailListSystemData").EmailYouHaveList;
            IsCheckEmail = mJsonSaveUtility.Get<EmailListSystemData>("EmailListSystemData").IsCheckEmail;

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
            if (EmailYouHaveList.Contains(so)) return;
            EmailYouHaveList.Add(so);
        }

        public bool CheckEmailYouHave(int EmailID)
        {
            EmailLine_SO so = GetEmailLineByID(EmailID);
            return so != null && EmailYouHaveList.Contains(so);
        }

        public void AddEmailYouCheck(int EmailID)
        {
            EmailLine_SO so = GetEmailLineByID(EmailID);
            if (so == null) return;
            IsCheckEmail.Add(so);
        }

        public void DelEmailYouHave(int EmailID)
        {
            EmailLine_SO so = GetEmailLineByID(EmailID);
            if (so != null) EmailYouHaveList.Remove(so);
        }
        public List<EmailLine_SO> getEmailSOList()
        {
            return EmailLineList;
        }

        public List<EmailLine_SO> GetEmailYouHaveList()
        {
            return EmailYouHaveList;
        }

        public List<EmailLine_SO> GetEmailCheck()
        {
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
            foreach(EmailLine_SO line in EmailYouHaveList)
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
        public List<EmailLine_SO> EmailYouHaveList = new List<EmailLine_SO>();
        public List<EmailLine_SO> IsCheckEmail = new List<EmailLine_SO>();
    }
}
