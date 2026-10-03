using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;
namespace TapTapFirst
{
    public class SoftwareSingle : MonoBehaviour, IController
    {
        [SerializeField]
        public Software_SO softwareData;//SO文件
        [SerializeField]
        private Button button;//软件用按钮实现
        [SerializeField]
        private TextMeshProUGUI buttonText;//软件名称
        [SerializeField]
        private Image haveNewMessage;//红点标记

        private SoftwareModel softwareModel;
        private void Start()
        {
            softwareModel = this.GetModel<SoftwareModel>();
            this.button = this.GetComponent<Button>();
            this.haveNewMessage = this.GetComponentInChildren<Image>();
            this.buttonText = this.GetComponentInChildren<TextMeshProUGUI>();

            this.buttonText.text = softwareData.softwareName;
            this.button.image.sprite = softwareData.softwareIcon;
            //this.haveNewMessage = GameObject.Find("HaveNewMessage").GetComponent<Image>();

            button.onClick.AddListener(() =>
            {
                clickSoftware();
                this.SendCommand(new HaveNewMessageCommand(softwareData.webID, false));
            });


            this.RegisterEvent<HaveNewMessageEvent>(e =>
            {
                if (e.webID != softwareData.webID)
                {
                    return;
                }
                UpdateNewMessageStatus();
            }).UnRegisterWhenGameObjectDestroyed(this.gameObject);
        }

        public void clickSoftware()
        {
            //这是真打开网页了，孩子们不要用
            //Application.OpenURL($"https://www.taptap.com/webview/{softwareData.webID}");
            this.SendCommand(new SoftwareClickedCommand(softwareData.webID));


        }
        
        /// <summary>
        /// 去掉红点标记
        /// </summary>
        public void UpdateNewMessageStatus()
        {
            if (this.softwareModel == null)
                Debug.Log("Model是空的");
            if (!softwareModel.haveNewMessage.ContainsKey(this.softwareData.webID))
            {
                softwareModel.haveNewMessage.Add(this.softwareData.webID, this.softwareData.haveNewMessage);
            }
            this.softwareData.haveNewMessage = softwareModel.haveNewMessage[this.softwareData.webID];
            this.haveNewMessage.gameObject.SetActive(softwareData.haveNewMessage);
            //this.SendCommand(new HaveNewMessageCommand(softwareData, false));
        }

        IArchitecture IBelongToArchitecture.GetArchitecture()
        {
            return TapTap.Interface;
        }

    }
}