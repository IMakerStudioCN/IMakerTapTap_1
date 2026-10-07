using QFramework;
using TMPro;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEngine.UI;
namespace TapTapFirst
{
    public class EmailWebSingle : MonoBehaviour,IController
    {
        [SerializeField]
        public TextMeshProUGUI whoSend;
        [SerializeField]
        public TextMeshProUGUI sendContent;
        [SerializeField]
        public TextMeshProUGUI whenSend;
        [SerializeField]
        public Button NewPlace;

        //private bool ischeck;
        private int currentId;
        private string[] Place;
        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
        IEmailListSystem mEmailSystem;
        private void OnEnable()
        {
            mEmailSystem = this.GetSystem<IEmailListSystem>();
            NewPlace.GetComponent<Button>();
            
            NewPlace.onClick.AddListener(() =>
            {
                TapTap.Interface.SendEvent(new OnTakeTask { Place = this.Place });
                TapTap.Interface.SendEvent(new HaveNewMessageEvent { webID = 1 });
                NewPlace.gameObject.SetActive(false);
                //加入到IsCheck列表
                mEmailSystem.AddEmailYouCheck(currentId);
                mEmailSystem.GetEmailLineByIDInYouHave(currentId).isCheck = true;
              
            });
        }
        public void ReFrash(EmailLine_SO currentSO)
        {
            if (currentSO == null)
            {
                Debug.LogWarning("WebSingle里的SO丢失");
                return;
            }

            whenSend.text = currentSO.WhoSend;
            sendContent.text = currentSO.SendContent;
            whenSend.text = currentSO.WhenSend;

            //ischeck = currentSO.isCheck;
            Place = currentSO.Place;

            currentId = currentSO.EmailID;

            Debug.LogWarning(mEmailSystem.GetEmailLineByIDInYouHave(currentId).isCheck);
            NewPlace.gameObject.SetActive(!currentSO.isCheck);
        }
    }
}