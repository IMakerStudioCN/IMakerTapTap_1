using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    public class WindowState
    {
        public enum WindowStateEnum
        {
            Normal,
            Full,
            Minimized
        }
        public class NormalWindowState : AbstractState<WindowStateEnum, WindowState>
        {
            public NormalWindowState(FSM<WindowStateEnum> fsm, WindowState owner) : base(fsm, owner)
            {
            }
            protected override bool OnCondition()
            {
                return mFSM.CurrentStateId == WindowStateEnum.Normal;
            }
        }
        public class FullWindowState : AbstractState<WindowStateEnum, WindowState>
        {
            public FullWindowState(FSM<WindowStateEnum> fsm, WindowState owner) : base(fsm, owner)
            {
            }
            protected override bool OnCondition()
            {
                return mFSM.CurrentStateId == WindowStateEnum.Full;
            }
        }
        public class MinimizedWindowState : AbstractState<WindowStateEnum, WindowState>
        {
            public MinimizedWindowState(FSM<WindowStateEnum> fsm, WindowState owner) : base(fsm, owner)
            {
            }
            protected override bool OnCondition()
            {
                return mFSM.CurrentStateId == WindowStateEnum.Minimized;
            }
        }
    }
}
