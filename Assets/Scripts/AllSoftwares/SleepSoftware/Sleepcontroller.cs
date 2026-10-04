using QFramework;
using UnityEngine;
using UnityEngine.UI;
namespace TapTapFirst
{

    public class Sleepcontroller : MonoBehaviour,IController
    {
        [SerializeField]
        private Button sleepButton;

        private ITimeModel sleepModel;

        private Animator skipDay;
        
        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }

        void Start()
        {
            skipDay = this.GetComponent<Animator>();
            this.sleepModel = this.GetModel<ITimeModel>();
            sleepButton.onClick.AddListener(()=> 
            {
                
                //添加过场动画
                this.skipDay.SetTrigger("skipTheDay");
                //天数增加
                this.SendCommand<DaysAddCommand>();
                Debug.Log("天数增加" + this.GetModel<ITimeModel>().days);
            });
        }
        

    }
}