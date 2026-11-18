using QFramework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using TapTapFirst.Audio;

namespace TapTapFirst
{
    // 已生成模块接口 IGlobalManagerSystem，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterSystem<IGlobalManagerSystem>(new GlobalManagerSystem());
    public interface IGlobalManagerSystem : ISystem
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
        int WhichEnd(int target);
        void EndTheGame();
        void ResetEndState();

    }

    public class GlobalManagerSystem : AbstractSystem, IGlobalManagerSystem
    {

        private IPlayerModel playerModel;
        private ITimeModel timeModel;
        private IGlobalManagerModel globalModel;

        private IJsonSaveUtility jsonSaveUtility;
        private ITaskSystem taskSystem;

        protected override void OnInit()
        {
            jsonSaveUtility = this.GetUtility<IJsonSaveUtility>();
            taskSystem = this.GetSystem<ITaskSystem>();
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
                if (timeModel.days >= timeModel.endDays
                    && playerModel.FundsValue < playerModel.TargetFundsValue * 0.8)
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
            if(timeModel.days >= timeModel.endDays)
            {
                // 邪恶值每次固定 ±3、从 70 起步，80 只有极端流派才够得到；这里放宽到“不低于开局值”
                if (playerModel.CredibilityValue >= 80 && playerModel.EvilValue >= 70
                    && playerModel.FundsValue > target * 1.8)
                {
                    // 主线任务有过期又没完成就算没做到，不给完美结局（支线不影响）
                    if (!HasUnfinishedExpiredMainTask()) return 4;
                }

                if (playerModel.FundsValue < target)
                    return 2;

                return 3;
            }
            return -1;        
        }


        /// <summary>是否存在“已过期但没有完成”的主线任务（只统计 ismain == true 的）。</summary>
        private bool HasUnfinishedExpiredMainTask()
        {
            return taskSystem != null && taskSystem.HasUnfinishedExpiredMainTask();
        }

        private bool mGameEnded;

        /// <summary>
        /// 结束游戏
        /// </summary>
        public void EndTheGame()
        {
            //目标资金改用玩家模型里的配置，避免和资金事件判定用两个值
            var end = WhichEnd(playerModel.TargetFundsValue);
            Debug.LogWarning("触发结局：" + end);

            // 结局条件可能被多个数值变化事件重复触发，音频只响一次
            if (mGameEnded || end < 0) return;
            mGameEnded = true;

            if (end <= 2)                       // 0 信任 / 1 道德 / 2 资金：坏结局
            {
                GameAudio.PlaySfx(GameAudioId.SfxGameOver);
                GameAudio.PlayBadEndingBgm();
            }
            else if (end == 4)                  // NB 结局
            {
                GameAudio.PlaySfx(GameAudioId.SfxEndingPerfect);
            }

            EndingId = end;
            //交给 GameOverShow：0/1/2 碎屏、3 关机、4 去屏幕效果，然后图片 -> 文字 -> 制作人名单
            TapTap.Interface.SendEvent(new GameOverEvent { EndingId = end });
        }

        /// <summary>
        /// 重开一局前复位结局状态。
        /// mGameEnded 挂在架构单例上，场景重载不会重建，不复位的话第二局永远结算不了结局。
        /// </summary>
        public void ResetEndState()
        {
            mGameEnded = false;
            EndingId = -1;
        }

        /// <summary>当前触发的结局编号，-1 表示还没结束</summary>
        public int EndingId { get; private set; } = -1;

        /// <summary>游戏是否已经结束</summary>
        public bool IsEnded => mGameEnded;
        

    }
}
