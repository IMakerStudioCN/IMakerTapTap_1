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
            this.writter.text = baLineSingleData.writter; 
            this.clicks.text = baLineSingleData.clicks;
            this.replies.text = baLineSingleData.replies;
            this.title.text = baLineSingleData.title;
            this.data.text = baLineSingleData.data;
            clickButton.onClick.AddListener(() =>
            {
                TapTap.Interface.SendEvent(new baLineClickEvent { baWebID =baLineSingleData.baID ,baWebType = baLineSingleData.baWebType});
            });
        }



    }
}