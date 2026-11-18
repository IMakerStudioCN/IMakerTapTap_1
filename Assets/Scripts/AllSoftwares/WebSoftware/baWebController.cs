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
        public TextMeshProUGUI data;
        [SerializeField]
        public TextMeshProUGUI content;
        [SerializeField]
        public int baWebType;
        //content列表
        [SerializeField]
        public List<baLineSingleSO> balist = new List<baLineSingleSO>();
        [SerializeField]
        private GameObject baContentPrefabs;
        [SerializeField]
        private Transform ContentPostion;
        [SerializeField]
        public int getTagID;
        [SerializeField]
        public int[] getNewsTemplateID;
        [SerializeField]
        public int getBranchID;
        [SerializeField]
        private List<GameObject> baLineGOList = new List<GameObject>();
        //根据ID判读活性
        [SerializeField]
        private Button getTag;
        [SerializeField]
        private Button getNewsTemplate;
        [SerializeField]
        private Button getBranch;
        private void Start()
        {
            if (getTag != null)
            getTag.onClick.AddListener(() => 
            {
                Debug.LogWarning("获取对应的TagID"+getTagID);
                this.SendCommand(new AddTagCommand(getTagID));
                foreach(int i in getNewsTemplateID)
                    this.SendCommand(new AddNewsCommand(i));
            });
        }
        //测试
        //private void OnGUI()
        //{
        //    if (GUI.Button(new Rect(200, 200, 100, 60), "addmybaReply"))
        //    {
        //        TapTap.Interface.SendEvent(new mybaReplySendEvent {NewsType = 0,mybaID = 0,nums = 3 });

        //        Debug.Log("130246");

        //    }
        //}

        public void RebuildbaLineGOList()
        {
            ClearbaLineGOList();
            foreach (var i in balist)
            {
                GameObject baContent = Instantiate(baContentPrefabs, ContentPostion);
                baContent.transform.SetAsLastSibling();
                baLineGOList.Add(baContent);
                baContent.GetComponent<baContentController>().baLineSingle = i;
            }
        }
        public void ClearbaLineGOList()
        {
           
            for (int i = 0; i < baLineGOList.Count; i++)
            {
                if (baLineGOList[i] == null) continue;
                baLineGOList[i].transform.SetParent(null, false);   // 关键：立刻离开 Content
                Destroy(baLineGOList[i]);
            }
            baLineGOList.Clear();
        }


        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}