using UnityEngine;
using QFramework;
using TapTapFirst;

public class KeywordPopupTester : MonoBehaviour
{
    private void Start()
    {
        // 确保 AudioKit 已经初始化（如果在全局游戏启动时已经初始化过，这行可以省略）
        // AudioKit.Initialize(); 
    }

    private void Update()
    {
        // 按下空格键测试弹窗
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("发送打开 KeywordPopup 请求");

            // 目前 Data 是空的，直接 Open
            UIKit.OpenPanel<KeywordPopup>();

            // 如果以后队友传入了关键词，应该这样写：
            // UIKit.OpenPanel<KeywordPopup>(new KeywordPopupData { Keyword = "测试词" });
        }
    }
}