using QFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace TapTapFirst
{
    public class EmailController : MonoBehaviour
    {
        [SerializeField]
        private RectTransform rect;
        [SerializeField]
        private GridLayoutGroup gridLayoutGroup;
        [SerializeField]
        private int widthSize = -21;
        [SerializeField]
        private List<EmailLine_SO> EmailList = new List<EmailLine_SO>();
        [SerializeField]
        private GameObject EmailPrefab;
        
        private int y;
        private void Start()
        {
            gridLayoutGroup = this.GetComponent<GridLayoutGroup>();
            y = (int)gridLayoutGroup.cellSize.y;
            UpdateEmailList();
        }
        private void Update()
        {
            gridLayoutGroup.cellSize = new Vector2(rect.rect.width + widthSize, y);
        }
        
        public void UpdateEmailList()
        {
            if (this.transform.childCount == EmailList.Count)
                return;
            foreach(var emali in EmailList)
            {
                GameObject item = Instantiate(EmailPrefab, this.transform);
                item.transform.parent = this.transform;
                item.GetComponent<EmailLineSingle>().EmailLineData = emali;
            }
        }
    }
}