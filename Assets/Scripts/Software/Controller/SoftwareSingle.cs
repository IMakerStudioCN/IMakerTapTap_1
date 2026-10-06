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
        private GameObject haveNewMessage;//红点标记

        private ISoftwareSystem softwareSystem;
        private void Start()
        {
            softwareSystem = this.GetSystem<ISoftwareSystem>();
            this.button = this.GetComponent<Button>();
            //this.haveNewMessage = GameObject.Find
            this.buttonText = this.GetComponentInChildren<TextMeshProUGUI>();

            this.buttonText.text = softwareData.softwareName;
            this.button.image.sprite = softwareData.softwareIcon;
            //this.haveNewMessage = GameObject.Find("HaveNewMessage").GetComponent<Image>();
            
            //点击软件，然后发送command
            button.onClick.AddListener(() =>
            {
                softwareSystem.clickSoftware(softwareData.webID);
                clickSoftware();
            });


            this.RegisterEvent<HaveNewMessageEvent>(e =>
            {
                if (e.webID != softwareData.webID)
                {
                    return;
                }
                softwareSystem.setDicisTrue(softwareData.webID);
                HaveOrNotNewMessage(true);
            }).UnRegisterWhenGameObjectDestroyed(this.gameObject);
        }

        public void clickSoftware()
        {
            //这是真打开网页了，孩子们不要用
            //Application.OpenURL($"https://www.taptap.com/webview/{softwareData.webID}");
            WindowKit.Open(softwareData.softwareName);
            //调用system
            this.softwareData.haveNewMessage = softwareSystem.updateSO(softwareData.webID);
            HaveOrNotNewMessage(false);
        }
        /// <summary>
        /// 设置红标状态
        /// </summary>
        public void HaveOrNotNewMessage(bool isNewMessage)
        {
            this.haveNewMessage?.SetActive(isNewMessage);
        }


        IArchitecture IBelongToArchitecture.GetArchitecture()
        {
            return TapTap.Interface;
        }

    }
}