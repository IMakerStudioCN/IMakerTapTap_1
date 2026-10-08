using QFramework;

namespace TapTapFirst
{
    // 已生成模块接口 IDialogModel，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterModel<IDialogModel>(new DialogModel());
    public interface IDialogModel : IModel
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
    }

    public class DialogModel : AbstractModel, IDialogModel
    {
        protected override void OnInit()
        {
        }
    }
}
