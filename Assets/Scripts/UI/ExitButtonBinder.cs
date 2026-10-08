using UnityEngine;
using UnityEngine.UI;
using TapTapFirst;

/// <summary>
/// 此脚本专门用于把 Exit 按钮绑定到退出确认面板，不改动原有代码。
/// </summary>
public class ExitButtonBinder : MonoBehaviour
{
    private Button mButton;

    private void Awake()
    {
        // 自动获取挂载此脚本物体上的 Button 组件
        mButton = GetComponent<Button>();

        if (mButton != null)
        {
            mButton.onClick.AddListener(OnExitClick);
        }
        else
        {
            Debug.LogError("[ExitButtonBinder] 挂载此脚本的物体上没有找到 Button 组件，请确认挂在 Exit 按钮上。");
        }
    }

    private void OnExitClick()
    {
        // 调用之前写好的静态入口，直接弹出退出确认面板
        GlobalExitConfirmUI.Open();
    }

    private void OnDestroy()
    {
        // 销毁时移除监听，防止内存泄漏
        if (mButton != null)
        {
            mButton.onClick.RemoveListener(OnExitClick);
        }
    }
}