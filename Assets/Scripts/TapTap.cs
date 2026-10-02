using QFramework;

namespace TapTapFirst
{
    public class TapTap : Architecture<TapTap>
    {
        protected override void Init()
        {

            var save = new JsonSaveUtility();
            #region 注册表
            // 注册工具类
            this.RegisterUtility<IJsonSaveUtility>(save);
            // 注册模型类
            RegisterModel<PlayerModel>(new PlayerModel());
            RegisterModel<TagModel>(new TagModel());
            RegisterModel<NewsTemplateModel>(new NewsTemplateModel());
            #endregion


            // 游戏开始：提前把要保存的纯 C# 类放进字典 ，不要在Controller里用Add和Remove
            #region 添加要纯C#的数据
            save.Add<TagListSaveData>();
            save.Add<NewsTemplateModel>();
            #endregion


            // 一键读取：有存档就原地灌回上面这些实例。
            //下面这行代码是测试留下的，后面大概率要改
            //不过我认为现在没写完游戏的加载和存储的功能所有保留
            save.Load();                       
        }
    }
}
