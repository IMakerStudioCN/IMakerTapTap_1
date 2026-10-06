using UnityEngine;
using QFramework;
using UnityEngine.UI;

namespace TapTapFirst
{
	public partial class Test : ViewController,IController
	{


        void Start()
		{
			UIKit.OpenPanel<ComputerDisplay>();
            //UIKit.OpenPanel<WindowsSample>();




           
            

           
        }
        private void OnGUI()
        {


            if (GUI.Button(new Rect(20, 40, 100, 60), "隐藏"))
            {
                UIKit.GetPanel<Dialog>().Hide();
            }
            if (GUI.Button(new Rect(20, 240, 100, 60), "展示"))
            {
                UIKit.GetPanel<Dialog>().Show();
            }



            if (GUI.Button(new Rect(20, 440, 100, 60), "新Tag"))
            {

                int tagId = 1;
                AddTagCommand addTagCommand = new AddTagCommand(tagId);
                if (addTagCommand != null)
                {
                    this.SendCommand(new AddTagCommand(tagId));
                    Debug.Log("Command sent successfully");
                }
                else
                {
                    Debug.Log("Command is null");

                }
            }
        }


      

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }

       
    }
}
