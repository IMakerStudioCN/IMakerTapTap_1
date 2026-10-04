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
        public event Action<TaskBarSingle> mDestroy;
        public string WindowName { get { return mWindowName; } }


        public TaskBarSingle Init(string name)
        {
            mWindowName = name;
            gameObject.name = name;

            if (textMeshPro != null)
            {
                textMeshPro.text = name;
            }
            if (button == null)
            {
                button = GetComponent<Button>();
                button.onClick.AddListener(OnClick);
            }

            if(!mInited && button != null)
            {
                mInited = true;
                
            }
            RefreshStyle();
            return this;
        }


        public void CloseWindow()
        {
            Destroy(this.gameObject);
        }

        public void RefreshStyle()
        {
            var window = WindowKit.Get(mWindowName);

            bool focused = window != null && WindowKit.IsFocused(window);
            bool minimized = window != null && window.KitState == WindowKitState.Minimized;
            if(textMeshPro != null) textMeshPro.fontStyle =focused ? FontStyles.Bold : FontStyles.Normal;
            //美术需求：比如修改按钮颜色，最小化状态为灰色，其他状态为白色什么的
        }
        private void OnClick()
        {
            var w = WindowKit.Get(name);
            if (w == null)
            {
                WindowKit.Open(name);
                return;
            }
            if (w.KitState == WindowKitState.Minimized || !w.gameObject.activeInHierarchy)
            {
                WindowKit.Open(name);
            }
            else if (!WindowKit.IsFocused(w))
            {
                WindowKit.Focus(name);
                return;
            }
            else
            {
                WindowKit.Minimize(name);
                return;
            }
            
        }
        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
            if (mDestroy != null) mDestroy(this); 
            mDestroy = null;
        }

    }
}
