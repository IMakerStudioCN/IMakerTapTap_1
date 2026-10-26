using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TapTapFirst
{
    public class baListController : MonoBehaviour, IController
    {
        [SerializeField]
        List<GameObject> baWebList = new List<GameObject>();//webList
        [SerializeField]
        GameObject baLineProject; //预制体
        //private IbaSystem baSystem;//系统
        private IbaListSystem baListSystem;//全部吧的系统
        private IMybaSystem mybaSystem;

        private readonly List<GameObject> mBaLineItems = new List<GameObject>();//列表项实例，只清自己生成的
        //private void OnGUI()
        //{
        //    if(GUI.Button(new Rect(0, 0, 100, 60), "addba"))
        //    {
        //        for(int i = 0;i<22;i++)
        //        {
        //            TapTap.Interface.SendEvent(new SendBaEvent { baID = i });
        //        }
        //        Debug.Log("123123");
               
        //    }
        //}
        private void Awake()
        {
            baListSystem = this.GetSystem<IbaListSystem>();
            mybaSystem = this.GetSystem<IMybaSystem>();
        }
        private void OnEnable()
        {
            EnsureTodayLists();
            UpdateBarList();
        }

        // 补齐“序章（第 0 天）~当天”应该拥有的帖子；顺便清掉读档反序列化出来的空壳条目
        private void EnsureTodayLists()
        {
            if (baListSystem == null) return;

            List<baLineList_SO> owned = baListSystem.GetbaLineListYouHave();

            if (owned != null)
            {
                owned.RemoveAll(x => x == null || x.baLineList == null || x.baLineList.Count == 0);
            }

            int today = this.GetModel<ITimeModel>().days;

            for (int day = 0; day <= today; day++)
            {
                baListSystem.AddbaLineList(day);
            }
        }

        private void Start()
        {
            //baSystem = this.GetSystem<IbaSystem>();
            this.RegisterEvent<OnDaysChangeEvent>(e => 
            {
                TapTap.Interface.SendEvent(new SendBaEvent { baID = this.GetModel<ITimeModel>().days }); 
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            EnsureTodayLists();
            UpdateBarList();
            //根据Line改变网页状态
            this.RegisterEvent<baLineClickEvent>(e => 
            {
                if(e.baWebType == 0)
                {
                    if (!baListSystem.CheckbaLineListYouHave(e.baWebID)) return;

                    GameObject baWeb = baWebList[e.baWebType];
                    baWebController ctrl = baWeb.GetComponent<baWebController>();

                    //  先把数据准备好
                    baLineList_SO line = baListSystem.GetbaLineListByIDInYouHave(e.baWebID);
                    ctrl.balist = line.baLineList;
                    ctrl.getTagID = line.tagID;
                    ctrl.getNewsTemplateID = line.NewsTemplateID;
                    ctrl.getBranchID = line.BranchID;         
                    ctrl.baWebType = e.baWebType;

                    //  再显示
                    baWeb.SetActive(true);

                    //  如果它本来就是 active，SetActive 不会触发 OnEnable，这里兜一下
                    ctrl.RebuildbaLineGOList();
                }
                if(e.baWebType == 1)
                {
                    if (!mybaSystem.checkMyba(e.baWebID))
                    {
                        return;
                    }

                    GameObject baWeb = baWebList[e.baWebType];
                    //设置第一层玩家的内容
                    baWebController baWebController = baWeb.GetComponent<baWebController>();
                    baWebController.content.text = mybaSystem.GetMyBaLineContent(e.baWebID);
                    baWebController.data.text = mybaSystem.GetdataList()[e.baWebID].ToString();
                    baWebController.witter.text = "温";
                    baWebController.balist = mybaSystem.GetmybaReplyesByID(e.baWebID);
                    baWebController.baWebType = e.baWebType;

                    baWeb.SetActive(true);
                    baWebController.RebuildbaLineGOList();
                }

            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.RegisterEvent<SendBaEvent>(e =>
            {
                //Debug.Log(baSystem.GetBaLineSingleList()[0].title);
                UpdateBarList();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            

        }

        //只销毁自己生成的列表项，常驻子物体（Heardbar / baImage / Myba）不动
        private void ClearBaLineList()
        {
            for (int i = 0; i < mBaLineItems.Count; i++)
            {
                if (mBaLineItems[i] == null) continue;
                mBaLineItems[i].transform.SetParent(null, false);
                Destroy(mBaLineItems[i]);
            }
            mBaLineItems.Clear();
        }

        //全量重建：先清空自己生成的项，再按拥有列表重新生成，不会翻倍也不会重复
        public void UpdateBarList()
        {
            ClearBaLineList();

            foreach (baLineList_SO line in baListSystem.GetbaLineListYouHave())
            {
                if (line == null) continue;
                if (line.baLineList == null || line.baLineList.Count == 0) continue;

                if (baListSystem.GetIsCheckbaList().Contains(line))
                {
                    line.isGetTag = true;
                    line.isGetNewsTemp = true;
                }

                GameObject item = Instantiate(baLineProject, this.transform);
                //后发先至
                item.transform.SetSiblingIndex(3);
                baLineSingle single = item.GetComponent<baLineSingle>();
                single.baLineSingleData = line.baLineList[0];
                single.baListID = line.listID;
                mBaLineItems.Add(item);
            }
        }

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}