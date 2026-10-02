using QFramework;

namespace TapTapFirst
{
    // 已生成模块接口 ITimeModel，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterModel<ITimeModel>(new TimeModel());
    public interface ITimeModel : IModel
    {
        public int endDays { get; set; }
    }

    public class TimeModel : AbstractModel, ITimeModel
    {
        public int endDays { get; set; } = 13;
        protected override void OnInit()
        {
        }
    }
    public class TimeModelData
    {
        public int days;
    }
}
