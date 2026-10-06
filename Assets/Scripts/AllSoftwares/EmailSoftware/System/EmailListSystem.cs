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
        List<int> GetIDList();
        //找对应ID的邮件
        EmailLine_SO GetEmailLineByID(int EmailID);
        //增删查改:针对EmailYouHaveList
        //删除邮件
        void DelEmailYouHave(int EmailID);
        //增加邮件
        void AddEmailYouHave(int EmailID);
        //查找是否有邮件
        bool CheckEmailYouHave(int EmailID);

        //获取拥有的邮件
        EmailLine_SO GetEmailLineByIDInYouHave(int EmailID);
        //初始化EmilLine
        void StartEmilLine(List<EmailLine_SO> emailLine);
    }

    public class EmailListSystem : AbstractSystem, IEmailListSystem
    {    
        private ResLoader mResLoader = ResLoader.Allocate();   
        public List<EmailLine_SO> EmailLineList = new List<EmailLine_SO>();
        public List<EmailLine_SO> EmailYouHaveList = new List<EmailLine_SO>();
        public List<int> EmailIDList = new List<int>();
        //找对应ID的邮件
        public EmailLine_SO GetEmailLineByID(int id)
        {
            if(!EmailIDList.Contains(id))
            {
                Debug.LogWarning("没有这个邮件");
                return null;
            }
            return EmailLineList[EmailIDList.IndexOf(id)];
        }
        public List<int> GetIDList()
        {
            return EmailIDList;
        }
        public void AddEmailYouHave(int EmailID)
        {
            EmailYouHaveList.Add(GetEmailLineByID(EmailID));

        }

        public bool CheckEmailYouHave(int EmailID)
        {
            return EmailYouHaveList.Contains(GetEmailLineByID(EmailID));
        }

        public void DelEmailYouHave(int EmailID)
        {
            EmailYouHaveList.Remove(GetEmailLineByID(EmailID));
        }
        public List<EmailLine_SO> getEmailSOList()
        {
            return EmailLineList;
        }

        public List<EmailLine_SO> GetEmailYouHaveList()
        {
            return EmailYouHaveList;
        }

        public EmailLine_SO GetEmailLineByIDInYouHave(int EmailID)
        {
            if(CheckEmailYouHave(EmailID))
            {
                return GetEmailLineByID(EmailID);
            }
            Debug.LogWarning("拥有的邮件中没有这个邮件");
            return null;

        }
        public void StartEmilLine(List<EmailLine_SO> emailLine)
        {
            EmailLineList = emailLine;
        }
        protected override void OnInit()
        {
            EmailLineList = mResLoader.LoadSync<EmailLineList_SO>("EmailLineList_SO").EmailLines;
            EmailYouHaveList = this.GetUtility<IJsonSaveUtility>().Get<EmailListSystemData>("EmailListSystemData").EmailYouHaveList;
            EmailIDList = this.GetUtility<IJsonSaveUtility>().Get<EmailListSystemData>("EmailListSystemData").EmailIDList;
            if(EmailIDList.Count == 0)
            {
                foreach (var email in EmailLineList)
                {
                    EmailIDList.Add(email.EmailID);
                }
            }
            //注册事件
            this.RegisterEvent<SendEmailEvent>(e =>
            {
                //接受发送的邮件ID事件，更新邮件列表
                AddEmailYouHave(e.EmailWebID);
            });
        }


    }
    public class EmailListSystemData 
    {
        public List<EmailLine_SO> EmailYouHaveList = new List<EmailLine_SO>();
        public List<int> EmailIDList = new List<int>();
    }
}
