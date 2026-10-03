using QFramework;

namespace TapTapFirst
{
    public interface IPlayerModel : IModel
    {
        int FundsValue { get; set; }
        int EvilValue { get; set; }
        int CredibilityValue { get; set; }
    }
    public class PlayerModel : AbstractModel, IPlayerModel
    {
        public int FundsValue { get; set; } = 0;
        public int EvilValue { get; set; } = 0;
        public int CredibilityValue { get; set; } = 0;
        protected override void OnInit()
        {
        }
    }
}
