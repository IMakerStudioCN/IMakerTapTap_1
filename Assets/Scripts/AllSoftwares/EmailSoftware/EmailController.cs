using QFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace TapTapFirst
{
    public class EmailController : MonoBehaviour,IController
    {
        //[SerializeField]
        //private RectTransform rect;
        //[SerializeField]
        //private GridLayoutGroup gridLayoutGroup;
        [SerializeField]
        private VerticalLayoutGroup verticalLayoutGroup;
        //[SerializeField]
        //private int widthSize = -21;
        [SerializeField]
        private List<EmailLine_SO> EmailList = new List<EmailLine_SO>();
        [SerializeField]
        private GameObject EmailPrefab;
        [SerializeField]
        private List<GameObject> EmailWebList;

        private HashSet<int> EmailYouHaveList = new HashSet<int>();
        //test button
        [SerializeField]
        private Button SendEmailbutton;
        //private int y;
        private void Start()
        {
            verticalLayoutGroup = this.GetComponent<VerticalLayoutGroup>();
            //y = (int)gridLayoutGroup.cellSize.y;
            UpdateEmailList();
            this.RegisterEvent<ClickEmailEvent>(e => 
            {
                if (EmailWebList.Count<e.EmailID)
                    return;
                EmailWebList[e.EmailID].SetActive(true);
                this.EmailWebList[e.EmailID].GetComponent<EmailWebSingle>().whoSend.text = this.EmailList[e.EmailID].WhoSend;
                this.EmailWebList[e.EmailID].GetComponent<EmailWebSingle>().whenSend.text = this.EmailList[e.EmailID].WhenSend;
                this.EmailWebList[e.EmailID].GetComponent<EmailWebSingle>().sendContent.text = this.EmailList[e.EmailID].SendContent;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<SendEmailEvent>(e =>
            {
                EmailYouHaveList.Add(e.EmailWebID);
                UpdateEmailList();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            SendEmailbutton.onClick.AddListener(() => 
            {
                TapTap.Interface.SendEvent(new SendEmailEvent { EmailWebID = 0});
                Debug.Log("·¢ËÍÓÊ¼þ");
            });

        }
        //private void Update()
        //{
        //    gridLayoutGroup.cellSize = new Vector2(rect.rect.width + widthSize, y);
        //}
        
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
            //foreach(var emali in EmailList)
            //{
            //    GameObject item = Instantiate(EmailPrefab, this.transform);
            //    item.transform.parent = this.transform;
            //    item.GetComponent<EmailLineSingle>().EmailLineData = emali;
            //}
        }

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}