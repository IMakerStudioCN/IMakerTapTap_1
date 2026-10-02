using QFramework;

namespace TapTapFirst
{
    public class TapTap : Architecture<TapTap>
    {
        protected override void Init()
        {

            var save = new JsonSaveUtility();
            #region 注册表
            //工具注册
            this.RegisterUtility<IJsonSaveUtility>(save);
            //Model注册

            #endregion


            // 游戏开始：提前把要保存的纯 C# 类放进字典 ，不要在Controller里用Add和Remove
            #region 添加要纯当的数据
            #endregion


            // 一键读取：有存档就原地灌回上面这些实例。
            //下面这行代码是测试留下的，后面大概率要改
            //不过我认为现在没写完游戏的加载和存储的功能所有保留
            save.Load();                       
        }
    }
}
