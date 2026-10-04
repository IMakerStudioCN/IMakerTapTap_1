using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace TapTapFirst
{
    public class TaskBarSingle : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI textMeshPro;

        private Button button;
        public TaskBarSingle Init(string name)
        {
            textMeshPro.text = name;
            button = GetComponent<Button>();
            //获得窗口对象
            var w = WindowKit.Get(name);
            button.onClick.AddListener(() =>
            {
                if(w == null)
                {
                    WindowKit.Open(name);
                }
                if(!w.gameObject.activeInHierarchy && w.KitState == WindowKitState.Minimized)
                {
                    WindowKit.Open(name);
                }
                else if(w.gameObject.activeInHierarchy && w.KitState != WindowKitState.Minimized)
                {
                    WindowKit.Focus(name);
                }
                else
                {
                    WindowKit.Minimize(name);
                }

            });
            return this;
        }

        public void CloseWindow()
        {
            Destroy(this.gameObject);
        }

    }
}
