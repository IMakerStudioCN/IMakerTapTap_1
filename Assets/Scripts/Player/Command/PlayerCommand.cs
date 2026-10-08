using QFramework;
using System.Runtime.CompilerServices;

namespace TapTapFirst
{
    /// <summary>
    /// EvilvalueCommand 使用记得传入int数来+-邪恶值
    /// </summary>
    public class EvilValueCommand : AbstractCommand
    {
        private int evilValue;
        public EvilValueCommand(int evilValue)
        {
            this.evilValue = evilValue;
        }

        protected override void OnExecute()
        {
            this.GetModel<IPlayerModel>().EvilValue += evilValue;
            //发送事件通知
            this.SendEvent(new EvilValueChangeEvent());
        }
    }
    /// <summary>
    /// CredibilityValueCommand 使用记得传入int数来+-信誉值
    /// </summary>
    public class CredibilityValueCommand : AbstractCommand
    {
        private int credibilityValue;
        public CredibilityValueCommand(int credibilityValue)
        {
            this.credibilityValue = credibilityValue;
        }
        protected override void OnExecute()
        {
            this.GetModel<IPlayerModel>().CredibilityValue += credibilityValue;
            //发送事件通知
            this.SendEvent(new CredibilityValueChangeEvent());
        }
    }
    /// <summary>
    /// FundsValueCommand 使用记得传入int数来+-资金值
    /// </summary>
    public class FundsValueCommand : AbstractCommand
    {
        private int fundsValue;
        public FundsValueCommand(int fundsValue)
        {
            this.fundsValue = fundsValue;
        }
        protected override void OnExecute()
        {
            this.GetModel<IPlayerModel>().FundsValue += fundsValue;
            //发送事件通知
            this.SendEvent(new FundsValueChangeEvent());
        }
    }
    /// <summary>
    /// TargetFundsValueCommand 使用记得传入int数来+-目标资金值
    /// </summary>
    public class TargetFundsValueCommand : AbstractCommand
    {
        private int targetFundsValue;
        public TargetFundsValueCommand(int targetFundsValue)
        {
            this.targetFundsValue = targetFundsValue;
        }
        protected override void OnExecute()
        {
            this.GetModel<IPlayerModel>().TargetFundsValue += targetFundsValue;
            this.SendEvent(new TargetFundsValueChangeEvent());
        }
    }
  
   


}
