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
        private TextMeshProUGUI clicks;//点击数
        [SerializeField]
        private TextMeshProUGUI replies;//回复数
        [SerializeField]
        private TextMeshProUGUI title;//标题
        [SerializeField]
        private TextMeshProUGUI data;//日期
        [SerializeField]
        private TextMeshProUGUI writter;
        [SerializeField]
        public baLineSingleSO baLineSingleData;//SOData文件读取数据
        [SerializeField]
        private Button clickButton;
        private void Start()
        {

            if (this.writter != null)
                this.writter.text = baLineSingleData.writter;
            if (this.title != null)
                this.title.text = baLineSingleData.title;
            if (this.data != null)
                this.data.text = baLineSingleData.data;
            if(this.clicks != null)
                this.clicks.text = baLineSingleData.clicks;
            if (this.replies != null)
                this.replies.text = baLineSingleData.replies;
            if(clickButton!=null)
                clickButton.onClick.AddListener(() =>
                {
                    TapTap.Interface.SendEvent(new baLineClickEvent { baWebID =baLineSingleData.baID });
                });
        }



    }
}