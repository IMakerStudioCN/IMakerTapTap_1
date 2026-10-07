using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace TapTapFirst
{
    public class EmailLineSingle : MonoBehaviour,IController
    {
        [SerializeField]
        private TextMeshProUGUI whoSend;
        [SerializeField]
        private TextMeshProUGUI whenSend;
        [SerializeField]
        private TextMeshProUGUI sendContent;
        [SerializeField]
        public EmailLine_SO EmailLineData;
        [SerializeField]
        private Button checkEmailButton;
        private void Start()
        {
            this.whoSend.text = EmailLineData.WhoSend;
            this.whenSend.text = EmailLineData.WhenSend;
            this.sendContent.text = EmailLineData.SendContent;
            checkEmailButton.onClick.AddListener(() =>
            { 
                TapTap.Interface.SendEvent(new ClickEmailEvent { EmailID = this.EmailLineData.EmailID});
            });
        }

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}