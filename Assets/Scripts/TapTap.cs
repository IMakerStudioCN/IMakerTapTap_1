using QFramework;

namespace TapTapFirst
{
    public class TapTap : Architecture<TapTap>
    {
        protected override void Init()
        {
            #region 注册模块
            RegisterUtility<IJsonSaveUtility>(new JsonSaveUtility());
            #endregion
        }
    }
}
