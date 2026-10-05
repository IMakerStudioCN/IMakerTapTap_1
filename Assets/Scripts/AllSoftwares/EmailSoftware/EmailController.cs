using QFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace TapTapFirst
{
    public class EmailController : MonoBehaviour,IController
    {
        //邮件SO列表，存储全部邮件
        [SerializeField]
        private List<EmailLine_SO> EmailList = new List<EmailLine_SO>();
        //预制体
        [SerializeField]
        private GameObject EmailPrefab;
        //邮件网页表
        [SerializeField]
        private List<GameObject> EmailWebList;
        //速查表查看是否接受到了邮件
        private HashSet<int> EmailYouHaveList = new HashSet<int>();
        //test button
        [SerializeField]
        private Button SendEmailbutton;
        //private int y;
        private void Start()
        {
            UpdateEmailList();
            this.RegisterEvent<ClickEmailEvent>(e => 
            {
                //判断所有的邮件子物体数量是否与拥有的邮件SO数量一致
                //如果一致不再添加邮件
                if (EmailWebList.Count<e.EmailID)
                    return;
                //设置邮件网页的状态
                EmailWebList[e.EmailID].SetActive(true);
                this.EmailWebList[e.EmailID].GetComponent<EmailWebSingle>().whoSend.text = this.EmailList[e.EmailID].WhoSend;
                this.EmailWebList[e.EmailID].GetComponent<EmailWebSingle>().whenSend.text = this.EmailList[e.EmailID].WhenSend;
                this.EmailWebList[e.EmailID].GetComponent<EmailWebSingle>().sendContent.text = this.EmailList[e.EmailID].SendContent;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<SendEmailEvent>(e =>
            {
                //接受发送的邮件ID，更新
                EmailYouHaveList.Add(e.EmailWebID);
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
            if (this.transform.childCount == EmailYouHaveList.Count)
                return;
            foreach(int i in EmailYouHaveList)
            {
                GameObject item = Instantiate(EmailPrefab, this.transform);
                item.transform.parent = this.transform;
                item.GetComponent<EmailLineSingle>().EmailLineData = EmailList[i];
            }

        }

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}