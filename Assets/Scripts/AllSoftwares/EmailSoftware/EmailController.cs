using QFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace TapTapFirst
{
    public class EmailController : MonoBehaviour,IController
    {
        //[SerializeField]
        //private List<EmailLine_SO> EmailLineList;
        //预制体
        [SerializeField]
        private GameObject EmailPrefab;
        //邮件网页表
        [SerializeField]
        private List<GameObject> EmailWebList;

        //test button
        [SerializeField]
        private Button SendEmailbutton;
        private IEmailListSystem EmailListSystem;

        private void Start()
        {
            EmailListSystem = this.GetSystem<IEmailListSystem>();
            //EmailListSystem.StartEmilLine(EmailLineList);
            UpdateEmailList();
            this.RegisterEvent<ClickEmailEvent>(e => 
            {
                //判断所有的邮件子物体数量是否与拥有的邮件SO数量一致
                //如果一致不再添加邮件
                if (EmailWebList.Count<e.EmailID)
                    return;
                //设置邮件网页的状态
                EmailWebList[e.EmailID].SetActive(true);
                this.EmailWebList[e.EmailID].GetComponent<EmailWebSingle>().whoSend.text = EmailListSystem.GetEmailLineByIDInYouHave(e.EmailID).WhoSend;
                this.EmailWebList[e.EmailID].GetComponent<EmailWebSingle>().whenSend.text = EmailListSystem.GetEmailLineByIDInYouHave(e.EmailID).WhenSend;
                this.EmailWebList[e.EmailID].GetComponent<EmailWebSingle>().sendContent.text = EmailListSystem.GetEmailLineByIDInYouHave(e.EmailID).SendContent;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<SendEmailEvent>(e =>
            {
                //接受发送的邮件ID事件，更新邮件列表
                UpdateEmailList();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            //测试的按钮
            SendEmailbutton.onClick.AddListener(() => 
            {
                TapTap.Interface.SendEvent(new SendEmailEvent { EmailWebID = 0});
                Debug.Log("发送邮件");
            });

        }

        //更新邮件软件的邮件列表
        public void UpdateEmailList()
        {
            if (this.transform.childCount == EmailListSystem.GetEmailYouHaveList().Count)
                return;
            foreach(var i in EmailListSystem.GetEmailYouHaveList())
            {
                GameObject item = Instantiate(EmailPrefab, this.transform);
                item.transform.SetAsFirstSibling();
                item.GetComponent<EmailLineSingle>().EmailLineData = i;
            }

        }

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}