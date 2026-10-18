using QFramework;

namespace TapTapFirst
{
    public interface IPlayerModel : IModel
    {
        int FundsValue { get; set; }
        int TargetFundsValue { get; set; }
        int EvilValue { get; set; }
        int CredibilityValue { get; set; }
        int PageViews { get; set; }
        int MoralityValue { get; set; }
        int PressureValue { get; set; }

        int basePageViews { get; }
        int baseMoralityValue { get; }
        int basePressureValue { get; }



    }
    public class PlayerModel : AbstractModel, IPlayerModel
    {   


            
        public int FundsValue { get; set; } = 0;
        public int EvilValue { get; set; } = 70;
        public int CredibilityValue { get; set; } = 50;

        public int TargetFundsValue { get; set; } = 65;
        public int PageViews { get; set; } = 30000;
        public int MoralityValue { get; set; } = 70;
        public int PressureValue { get; set; } = 400;

        public int basePageViews { get; } = 30000 ;

        public int baseMoralityValue { get; } = 70;

        public int basePressureValue { get; } = 400;

        protected override void OnInit()
        {
        }
    }
}
