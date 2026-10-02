using QFramework;

namespace TapTapFirst
{
    public class PlayerModel : AbstractModel
    {
        public int FundsValue { get; set; } = 0;
        public int EvilValue { get; set; } = 0;
        public int CredibilityValue { get; set; } = 0;
        protected override void OnInit()
        {
        }
    }
}
