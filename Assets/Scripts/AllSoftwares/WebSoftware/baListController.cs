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
        private IbaSystem baSystem;//系统
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
            baSystem = this.GetSystem<IbaSystem>();
            UpdateBarList();
            //根据Line改变网页状态
            this.RegisterEvent<baLineClickEvent>(e => 
            {
                if(!baSystem.ChickBaYouHave(e.baWebID))
                {
                    return;
                }
                //通过获取type种类来设置对应页面的显示
                GameObject baWeb = baWebList[e.baWebID];
                baWeb.SetActive(true);
                baWebController baWebController = baWeb.GetComponent<baWebController>();
                baWebController.witter.text = baSystem.GetYouHaveListSOByBaIDIn(e.baWebID).writter;
                baWebController.clicks.text = baSystem.GetYouHaveListSOByBaIDIn(e.baWebID).clicks;
                baWebController.content.text = baSystem.GetYouHaveListSOByBaIDIn(e.baWebID).content;
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
            if(baSystem.GetBaYouHaveList().Count < this.transform.childCount - 3)
            {
                Debug.Log("不更新");
                return;
            }
            foreach(var i in baSystem.GetBaYouHaveList())
            {
                GameObject baLine = Instantiate(baLineProject,this.transform);
                baLine.GetComponent<baLineSingle>().baLineSingleData = i;
                baLine.transform.SetSiblingIndex(3);
            }
        }

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}