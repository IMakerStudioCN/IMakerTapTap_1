using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace TapTapFirst
{
    public class baContentController : MonoBehaviour
    {
        // Start is called before the first frame update
        [SerializeField]
        private TextMeshProUGUI writter;
        [SerializeField]
        private TextMeshProUGUI content;
        [SerializeField]
        private TextMeshProUGUI data;
        [SerializeField]
        public baLineSingleSO baLineSingle;
        void Start()
        {
            writter.text = baLineSingle.writter;
            content.text = baLineSingle.content;
            data.text = baLineSingle.data;
        }


    }
}