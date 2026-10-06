using QFramework;
using System.Diagnostics;

namespace TapTapFirst
{
    public class DaysAddCommand : AbstractCommand
    {
        public DaysAddCommand()
        {
        }
        protected override void OnExecute()
        {
            IJsonSaveUtility save = this.GetUtility<IJsonSaveUtility>();
            
            save.Get<TimeModelData>("TimeModelData").days += 1;
            this.SendEvent<OnDaysChangeEvent>();
            save.Save();
            
        }
    }
}
