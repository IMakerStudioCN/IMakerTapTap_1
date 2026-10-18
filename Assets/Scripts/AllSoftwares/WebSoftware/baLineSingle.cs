using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace TapTapFirst
{
    public class baLineSingle : MonoBehaviour
    {
        [SerializeField]
        public TextMeshProUGUI clicks;//点击数
        [SerializeField]
        public TextMeshProUGUI replies;//回复数
        [SerializeField]
        public TextMeshProUGUI title;//标题
        [SerializeField]
        public TextMeshProUGUI data;//日期
        [SerializeField]
        public TextMeshProUGUI writter;
        [SerializeField]
        public baLineSingleSO baLineSingleData;//SOData文件读取数据
        [SerializeField]
        private Button clickButton;
        [SerializeField]
        public int mybaWebID = 0;//myba的ID

        [SerializeField]
        public int baListID = -1;//所属帖子的 listID，由 ba 主页列表写入
        private void Start()
        {
            if (baLineSingleData != null)
            {
                if (this.writter != null)
                    this.writter.text = baLineSingleData.writter;
                if (this.title != null)
                    this.title.text = baLineSingleData.title;
                if (this.data != null)
                    this.data.text = baLineSingleData.data;
                if (this.clicks != null)
                    this.clicks.text = baLineSingleData.clicks;
                if (this.replies != null)
                    this.replies.text = baLineSingleData.replies;
            }
            if(clickButton!=null)
                clickButton.onClick.AddListener(() =>
                {
                    if (this.baLineSingleData != null)
                        TapTap.Interface.SendEvent(new baLineClickEvent { baWebID = baListID,baWebType = 0 });
                    else
                    {
                        TapTap.Interface.SendEvent(new baLineClickEvent { baWebID = mybaWebID, baWebType = 1 });
                    }
                });
        }



    }
}