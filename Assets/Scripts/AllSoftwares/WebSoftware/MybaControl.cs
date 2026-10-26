using QFramework;
using System.Collections.Generic;
using UnityEngine;
namespace TapTapFirst
{
    public class MybaControl : MonoBehaviour,IController
    {
        // Start is called before the first frame update
        //[SerializeField]
        //public List<string> contentList = new List<string>();
        //[SerializeField]
        //public List<int> dataList = new List<int>();
        //[SerializeField]
        //public List<int> valueList = new List<int>();
        [SerializeField]
        private readonly List<GameObject> mybaLineListObject = new List<GameObject>();
        [SerializeField]
        private Transform position;//位置
        [SerializeField]
        private GameObject mybaLinePrefab;//预制体
        private IMybaSystem mybaSystem;//myba系统


        //private void OnGUI()
        //{
        //    if (GUI.Button(new Rect(100, 100, 100, 60), "mybaAdd"))
        //    {
        //        TapTap.Interface.SendEvent(new mybaSendEvent { content = "测试",data = 0 });
        //        Debug.Log("123123");

        //    }
        //}
        private void Awake()
        {
            mybaSystem = this.GetSystem<IMybaSystem>();

        }
        void Start()
        {
            
            this.RegisterEvent<mybaSendEvent>(e => 
            {
                updataMybaLenList();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
        private void OnEnable()
        {
            if(this.transform.childCount - 3 < mybaSystem.GetMyBaLineContentList().Count)
            updataMybaLenList();

        }
        public void ClearMyBaLineList()
        {
            for (int i = 0; i < mybaLineListObject.Count; i++)
            {
                if (mybaLineListObject[i] == null) continue;
                mybaLineListObject[i].transform.SetParent(null, false);
                Destroy(mybaLineListObject[i]);
            }
            mybaLineListObject.Clear();
        }
        public void updataMybaLenList()
        {
            if (mybaSystem == null)
            {
                mybaSystem = this.GetSystem<IMybaSystem>();
            }

            if (mybaSystem == null || mybaLinePrefab == null || position == null)
            {
                Debug.LogWarning("[MybaControl] 缺少 mybaSystem / mybaLinePrefab / position，贴吧列表不刷新");
                return;
            }

            List<string> contents = mybaSystem.GetMyBaLineContentList();

            if (contents == null)
            {
                return;
            }

            List<int> clicks = mybaSystem.GetClickList();
            List<int> replies = mybaSystem.GetRepliseList();
            List<int> datas = mybaSystem.GetdataList();

            // 只补【新增】的条目。
            // 原来是无条件从 0 开始把整张表重新 Instantiate 一遍（而且从不清旧物件）：
            // 每提交一条新闻，就按当前总条数再造成 N 个物件 —— 越点越多，Console 还被
            // 每个物件一条的 LogWarning 刷屏。
            for (int i = mybaLineListObject.Count; i < contents.Count; i++)
            {
                GameObject item = Instantiate(mybaLinePrefab, position);

                if (item == null)
                {
                    continue;
                }

                mybaLineListObject.Add(item);
                item.transform.SetSiblingIndex(3);

                //根据玩家来进行创建，不能使用SO
                baLineSingle mybaLineSingle = item.GetComponent<baLineSingle>();

                if (mybaLineSingle == null)
                {
                    Debug.LogWarning("[MybaControl] mybaLinePrefab 上没挂 baLineSingle");
                    continue;
                }

                if (mybaLineSingle.title != null) mybaLineSingle.title.text = contents[i];
                if (mybaLineSingle.writter != null) mybaLineSingle.writter.text = "温";
                if (mybaLineSingle.clicks != null) mybaLineSingle.clicks.text = GetAt(clicks, i).ToString();
                if (mybaLineSingle.replies != null) mybaLineSingle.replies.text = mybaSystem.GetmybaReplyesByID(i).Count.ToString();
                
                if (mybaLineSingle.data != null) mybaLineSingle.data.text = GetAt(datas, i).ToString();

                mybaLineSingle.mybaWebID = i;
            }
        }

        /// <summary>越界时返回 0，避免列表长度不一致直接抛 IndexOutOfRange。</summary>
        private static int GetAt(List<int> list, int index)
        {
            return list != null && index >= 0 && index < list.Count ? list[index] : 0;
        }



        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}