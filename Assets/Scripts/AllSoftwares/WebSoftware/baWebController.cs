using QFramework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace TapTapFirst
{
    public class baWebController : MonoBehaviour, IController
    {

        [SerializeField]
        public TextMeshProUGUI witter;
        [SerializeField]
        public TextMeshProUGUI clicks;
        [SerializeField]
        public TextMeshProUGUI content;
        [SerializeField]
        public int baWebType;
        //根据ID判读活性
        [SerializeField]
        private Button getTag;
        [SerializeField]
        private Button getNewsTemplate;
        [SerializeField]
        private Button getBranch;

        private void Start()
        {
            //根据type种类来设置buttion的活性
        }
        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}