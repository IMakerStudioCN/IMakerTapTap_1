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


        private void OnGUI()
        {
            if (GUI.Button(new Rect(100, 100, 100, 60), "mybaAdd"))
            {
                TapTap.Interface.SendEvent(new mybaSendEvent { content = "测试",data = 0 });
                Debug.Log("123123");

            }
        }
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
            if (mybaSystem.GetMyBaLineContentList() == null)
                return;
            for(int i = 0;i<mybaSystem.GetMyBaLineContentList().Count;i++)
            {
                GameObject item = Instantiate(mybaLinePrefab, position);
                mybaLineListObject.Add(item);
                item.transform.SetSiblingIndex(3);
                //根据玩家来进行创建，不能使用SO
                baLineSingle mybaLineSingle = item.GetComponent<baLineSingle>();
                mybaLineSingle.title.text = mybaSystem.GetMyBaLineContentList()[i];
                mybaLineSingle.writter.text = "名字";
                mybaLineSingle.clicks.text = mybaSystem.GetClickList()[i].ToString();
                mybaLineSingle.replies.text = mybaSystem.GetRepliseList()[i].ToString();
                mybaLineSingle.data.text = mybaSystem.GetdataList()[i].ToString();
                mybaLineSingle.mybaWebID = i;
                Debug.LogWarning("mybaWebID"+i);
            }
        }



        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}