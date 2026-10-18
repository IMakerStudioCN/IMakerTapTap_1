namespace TapTapFirst
{
    /// <summary>
    /// 结局触发事件：由 GlobalManagerSystem.EndTheGame() 发出，
    /// GameOverShow 里的 GameOverDirector 收到后播放对应结局表现。
    /// </summary>
    public struct GameOverEvent
    {
        /// <summary>0 信任归零 / 1 道德归零 / 2 资金不足 / 3 普通结局 / 4 NB结局</summary>
        public int EndingId;
    }
}
