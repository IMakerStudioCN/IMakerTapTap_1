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

        private readonly HashSet<int> mShowIDs = new HashSet<int>();
        private void Start()
        {
            EmailListSystem = this.GetSystem<IEmailListSystem>();
            //EmailListSystem.StartEmilLine(EmailLineList);
            UpdateEmailList();
            this.RegisterEvent<ClickEmailEvent>(e => 
            {
                //判断所有的邮件子物体数量是否与拥有的邮件SO数量一致
                //如果一致不再添加邮件
                if (e.EmailID < 0)
                {
                    Debug.LogWarning($"[Email] EmailWebList 里没有下标 {e.EmailID} 的详情页");
                    return;
                }
                EmailLine_SO line = EmailListSystem.GetEmailLineByIDInYouHave(e.EmailID);
                if (line == null) return;
                //设置状态
                EmailWebList[0].SetActive(true);
                EmailWebSingle web = EmailWebList[0].GetComponent<EmailWebSingle>();
                web.ReFrash(line);
                //web.whoSend.text = line.WhoSend;
                //web.whenSend.text = line.WhenSend;
                //web.sendContent.text = line.SendContent;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<SendEmailEvent>(e =>
            {
                this.transform.DestroyChildren();
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
            
            foreach(var line in EmailListSystem.GetEmailYouHaveList())
            {
                if (line == null) continue;
                if (mShowIDs.Contains(line.EmailID)) {
                    CreatList(line); 
                    continue;
                }
                line.isCheck = false;
                if (EmailListSystem.GetEmailCheck().Contains(line))
                {
                    line.isCheck = true;
                }

                CreatList(line);

                mShowIDs.Add(line.EmailID);
            }

        }

        private void CreatList(EmailLine_SO line)
        {
            GameObject item = Instantiate(EmailPrefab, this.transform);

            item.transform.SetAsFirstSibling();
            item.GetComponent<EmailLineSingle>().EmailLineData = line;
        }

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}