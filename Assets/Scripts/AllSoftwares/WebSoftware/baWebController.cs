using QFramework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
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
        //content列表
        [SerializeField]
        public List<baLineSingleSO> balist;
        [SerializeField]
        private GameObject baContentPrefabs;
        [SerializeField]
        private Transform ContentPostion;
        [SerializeField]
        public int getTagID;
        [SerializeField]
        public int getNewsTemplateID;
        [SerializeField]
        public int getBranchID;
        //根据ID判读活性
        [SerializeField]
        private Button getTag;
        [SerializeField]
        private Button getNewsTemplate;
        [SerializeField]
        private Button getBranch;
        private void Start()
        {
            getTag.onClick.AddListener(() => 
            {
                Debug.LogWarning("获取对应的"+getTagID);
            });
        }
        private void OnEnable()
        {

            foreach(var i in balist)
            {
                GameObject baContent = Instantiate(baContentPrefabs, ContentPostion);
                baContent.transform.SetAsLastSibling();
                baContent.GetComponent<baContentController>().baLineSingle = i;
            }
            //根据type种类来设置buttion的活性
        }
        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}