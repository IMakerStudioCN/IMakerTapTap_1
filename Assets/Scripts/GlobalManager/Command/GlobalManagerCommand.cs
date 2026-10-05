using QFramework;
using UnityEngine;

namespace TapTapFirst
{
    public class SendEmail : AbstractCommand
    {
        public SendEmail()
        {
            
        }

        protected override void OnExecute()
        {
            Debug.Log("command 向你发送了邮件");

        }
    }
    /// <summary>
    /// 加入分支任务 Add the branch mession
    /// </summary>
    public class AddBranchs : AbstractCommand
    {
        int branchID;
        public AddBranchs(int branchID) 
        {
            this.branchID = branchID;
        }
        protected override void OnExecute()
        {
            if(!this.GetModel<IGlobalManagerModel>().branchs.ContainsKey(branchID))
            {
                this.GetModel<IGlobalManagerModel>().branchs.Add(branchID, false);
                Debug.Log(branchID + " mession is accepted");
            }
        }
    }
    /// <summary>
    /// 完成分支任务 finish the branch mession
    /// </summary>
    public class FinishBranchs : AbstractCommand
    {
        int branchID;
        public FinishBranchs(int branchID) 
        {
            this.branchID = branchID;
        }
        protected override void OnExecute()
        {
            if (this.GetModel<IGlobalManagerModel>().branchs.ContainsKey(branchID))
            {
                this.GetModel<IGlobalManagerModel>().branchs[branchID] = true;
                Debug.Log(branchID+" mession is finished");
            }
            else
            {
                //孩子们，我发现写英文注释不会乱码
                Debug.LogWarning("not have the "+branchID+" of mession,you can not finish it");
            }
        }
    }
}
