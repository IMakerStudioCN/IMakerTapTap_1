using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.AnimatedValues;
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
        private IbaListSystem baListSystem;//系统
        private void OnGUI()
        {
            if(GUI.Button(new Rect(0, 0, 100, 60), "addba"))
            {
                TapTap.Interface.SendEvent(new SendBaEvent { baID = 0 });
                Debug.Log("123123");
               
            }
        }
        private void Start()
        {
            //baSystem = this.GetSystem<IbaSystem>();
            baListSystem = this.GetSystem<IbaListSystem>();
            UpdateBarList();
            //根据Line改变网页状态
            this.RegisterEvent<baLineClickEvent>(e => 

            {
                if(!baListSystem.CheckbaLineListYouHave(e.baWebID))
                {
                    return;
                }
                //通过获取type种类来设置对应页面的显示
                GameObject baWeb = baWebList[e.baWebID];
                baWeb.SetActive(true);
                baWebController baWebController = baWeb.GetComponent<baWebController>();
                //设置对应ba的楼层
                baWebController.balist = baListSystem.GetbaLineListByIDInYouHave(e.baWebID).baLineList;
                baWebController.baWebType = e.baWebType;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.RegisterEvent<SendBaEvent>(e =>
            {
                //Debug.Log(baSystem.GetBaLineSingleList()[0].title);
                UpdateBarList();
            });
        }
        public void UpdateBarList()
        {
            if (baListSystem.GetbaLineListYouHave().Count <= this.transform.childCount - 3)
            {
                Debug.Log("不更新");
                return;
            }
            foreach (var i in baListSystem.GetbaLineListYouHave())
            {
                if (i == null) continue;
                if(baListSystem.GetIsCheckbaList().Contains(i))
                {
                    i.isGetTag = true;
                    i.isGetNewsTemp = true;
                }
                GameObject baLine = Instantiate(baLineProject, this.transform);
                baLine.GetComponent<baLineSingle>().baLineSingleData = i.baLineList[0];
                baLine.transform.SetSiblingIndex(3);
            }
            
        }

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}