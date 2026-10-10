using QFramework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace TapTapFirst
{
    // 已生成模块接口 IGlobalManagerSystem，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterSystem<IGlobalManagerSystem>(new GlobalManagerSystem());
    public interface IGlobalManagerSystem : ISystem
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
        int WhichEnd(int target);
        void EndTheGame();

    }

    public class GlobalManagerSystem : AbstractSystem, IGlobalManagerSystem
    {

        private IPlayerModel playerModel;
        private ITimeModel timeModel;
        private IGlobalManagerModel globalModel;

        protected override void OnInit()
        {
            playerModel = this.GetModel<IPlayerModel>();
            timeModel = this.GetModel<ITimeModel>();
            globalModel = this.GetModel<IGlobalManagerModel>();
            //在这里注册监听者
            this.RegisterEvent<OnDaysChangeEvent>(e =>
            {
                if (timeModel.days >= timeModel.endDays)
                {
                    Debug.Log("游戏结束");
                    EndTheGame();
                }


            });
            this.RegisterEvent<EvilValueChangeEvent>(e =>
            {
                if (playerModel.EvilValue <= 0)
                {
                    Debug.Log("游戏结束");
                    EndTheGame();
                }
            });
            this.RegisterEvent<CredibilityValueChangeEvent>(e =>
            {
                if (playerModel.CredibilityValue <= 0)
                {
                    Debug.Log("游戏结束");
                    EndTheGame();
                }
            });
            this.RegisterEvent<FundsValueChangeEvent>(e =>
            {
                if (playerModel.FundsValue < playerModel.TargetFundsValue * 0.8)
                {
                    Debug.Log("游戏结束");
                    EndTheGame();
                }
            });
        }

        /// <summary>
        /// 返回结局的序号 
        /// 0为信任为0,1为道德为0,2为钱低于目标值,3为普通解决,4为NB结局
        /// -1为没触发结局
        /// </summary>
        /// <param name="target">金钱的目标值</param>
        /// <returns></returns>
        public int WhichEnd(int target)
        {
            if(playerModel.CredibilityValue <= 0)
            {
                return 0;
            }
            if(playerModel.EvilValue <= 0)
            {
                return 1;
            }
            if (playerModel.FundsValue < target)
            {
                return 2;
            }
            if(timeModel.days == timeModel.endDays)
            {
                if (playerModel.CredibilityValue >= 80
                    && playerModel.EvilValue >= 80
                    && playerModel.FundsValue > target * 1.8)
                    return 4;
                else
                    return 3;
            }
            return -1;        
        }


        /// <summary>
        /// 结束游戏
        /// </summary>
        public void EndTheGame()
        {
            //暂定1000为目标资金
            Debug.LogWarning("触发结局：" + WhichEnd(1000));
        }
        

    }
}
