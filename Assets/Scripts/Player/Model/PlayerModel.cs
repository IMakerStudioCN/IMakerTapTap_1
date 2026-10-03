using QFramework;

namespace TapTapFirst
{
    public interface IPlayerModel : IModel
    {
        int FundsValue { get; set; }
        int TargetFundsValue { get; set; }
        int EvilValue { get; set; }
        int CredibilityValue { get; set; }
    }
    public class PlayerModel : AbstractModel, IPlayerModel
    {
        public int FundsValue { get; set; } = 1;
        public int EvilValue { get; set; } = 1;
        public int CredibilityValue { get; set; } = 1;

        public int TargetFundsValue { get; set; } = 1000;
        protected override void OnInit()
        {
        }
    }
}
