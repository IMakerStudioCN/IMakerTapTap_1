using QFramework;
using UnityEngine;

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
        private IJsonSaveUtility mSaveUtility;

        private IJsonSaveUtility SaveUtility
        {
            get
            {
                if (mSaveUtility == null)
                {
                    mSaveUtility = this.GetUtility<IJsonSaveUtility>();
                }

                return mSaveUtility;
            }
        }

        /// <summary>前三个玩家数值存在纯 C# 存档数据里，切换槽位 / 新游戏时由 JsonSaveUtility 统一复位。</summary>
        private PlayerSaveData SaveData
        {
            get
            {
                IJsonSaveUtility save = SaveUtility;

                if (save == null)
                {
                    return null;
                }

                PlayerSaveData data = save.Get<PlayerSaveData>(PlayerSaveData.Key);

                if (data == null)
                {
                    save.Add<PlayerSaveData>();
                    data = save.Get<PlayerSaveData>(PlayerSaveData.Key);
                }

                return data;
            }
        }

        public int FundsValue
        {
            get
            {
                PlayerSaveData data = SaveData;
                return data != null ? data.FundsValue : PlayerSaveData.DefaultFundsValue;
            }
            set
            {
                PlayerSaveData data = SaveData;

                if (data != null)
                {
                    data.FundsValue = Mathf.Max(0, value);
                }
            }
        }

        public int EvilValue
        {
            get
            {
                PlayerSaveData data = SaveData;
                return data != null ? data.EvilValue : PlayerSaveData.DefaultEvilValue;
            }
            set
            {
                PlayerSaveData data = SaveData;

                if (data != null)
                {
                    data.EvilValue = Mathf.Clamp(value, 0, PlayerSaveData.MaxValue);
                }
            }
        }

        public int CredibilityValue
        {
            get
            {
                PlayerSaveData data = SaveData;
                return data != null ? data.CredibilityValue : PlayerSaveData.DefaultCredibilityValue;
            }
            set
            {
                PlayerSaveData data = SaveData;

                if (data != null)
                {
                    data.CredibilityValue = Mathf.Clamp(value, 0, PlayerSaveData.MaxValue);
                }
            }
        }

        public int TargetFundsValue { get; set; } = 65;

        public int PageViews { get; set; } = 30000;
        public int MoralityValue { get; set; } = 70;
        public int PressureValue { get; set; } = 400;

        public int basePageViews { get; } = 30000;

        public int baseMoralityValue { get; } = 70;

        public int basePressureValue { get; } = 400;

        protected override void OnInit()
        {
        }
    }

    /// <summary>
    /// 玩家前三个数值（资金 / 邪恶 / 可信度）的存档形态。
    /// JsonUtility 只序列化字段，所以这里用 public 字段。
    /// </summary>
    [System.Serializable]
    public class PlayerSaveData
    {
        public const string Key = "PlayerSaveData";
        public const int MaxValue = 100;
        public const int DefaultFundsValue = 0;
        public const int DefaultEvilValue = 70;
        public const int DefaultCredibilityValue = 50;

        public int FundsValue = DefaultFundsValue;
        public int EvilValue = DefaultEvilValue;
        public int CredibilityValue = DefaultCredibilityValue;
    }
}
