using UnityEngine;
using QFramework;

namespace TapTapFirst
{
    // 具体窗口的业务脚本：继承 WindowsBasic，只写这个窗口特有的逻辑
    // 全屏 / 拖拽 / 缩放 / 堆叠 / 关闭 / 最小化 全部由 WindowsBasic + WindowKit 负责
    public partial class WindowsController : WindowsBasic
    {
        protected override void OnWindowReady()
        {
            // 例如：刷新任务栏、读存档、初始化 Content 里的内容
        }
    }
}
