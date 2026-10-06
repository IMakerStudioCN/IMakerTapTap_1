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

        public string[] Palce;
        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }

        private void Start()
        {
            NewPlace.GetComponent<Button>();
            NewPlace.onClick.AddListener(() =>
            {
                TapTap.Interface.SendEvent(new OnTakeTask { Place = this.Palce });
                TapTap.Interface.SendEvent(new HaveNewMessageEvent { webID = 1 });
                NewPlace.gameObject.SetActive(false);
            });
        }
    }
}