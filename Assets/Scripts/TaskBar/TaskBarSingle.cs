using System;
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
        [SerializeField]
        private Button button;

        private string mWindowName;
        private bool mInited;
        //销毁通知Controller,注销
        private event Action<TaskBarSingle> mDestroy;
        public string WindowName { get { return mWindowName; } }


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
                else if(w.gameObject.activeInHierarchy && w.KitState != WindowKitState.Minimized && !WindowKit.IsFocused(w))
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

        public void RefreshStyle()
        {
            var window = WindowKit.Get(textMeshPro.text);
        }

    }
}
