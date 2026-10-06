using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.AnimatedValues;
using UnityEngine;
namespace TapTapFirst
{
    public class baListController : MonoBehaviour, IController
    {
        //全部的baLineSO的列表
        [SerializeField]
        List<baLineSingleSO> balist = new List<baLineSingleSO>();
        [SerializeField]
        List<GameObject> baWebList = new List<GameObject>();
        [SerializeField]
        GameObject baLineProject;
        //拥有的baLine的
        HashSet<int> baYouHaveList = new HashSet<int>();
        private void Start()
        {
            baYouHaveList.Add(0);
            UpdateBarList();
            //根据Line改变网页状态
            this.RegisterEvent<baLineClickEvent>(e => 
            {
                if(!baYouHaveList.Contains(e.baWebID))
                {
                    return;
                }
                //通过获取type种类来设置对应页面的显示
                baWebList[e.baWebType].SetActive(true);
                baWebList[e.baWebType].GetComponent<baWebController>().witter.text = balist[e.baWebID].writter;
                baWebList[e.baWebType].GetComponent<baWebController>().clicks.text = balist[e.baWebID].clicks;
                baWebList[e.baWebType].GetComponent<baWebController>().content.text = balist[e.baWebID].content;
                baWebList[e.baWebType].GetComponent<baWebController>().baWebType = e.baWebType;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
        public void UpdateBarList()
        {
            if(baYouHaveList.Count<this.transform.childCount - 3)
            {
                Debug.Log("不更新");
                return;
            }
            foreach(int i in baYouHaveList)
            {
                GameObject baLine = Instantiate(baLineProject,this.transform);
                baLine.GetComponent<baLineSingle>().baLineSingleData = balist[i];
                baLine.transform.SetSiblingIndex(3);
            }
        }

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}