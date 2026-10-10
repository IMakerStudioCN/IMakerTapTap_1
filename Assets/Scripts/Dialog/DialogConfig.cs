using QFramework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
	public partial class DialogConfig : ViewController,IController
	{
        [SerializeField]
        public List<DialogSO> DialogConfigs;

        IDialogSystem mDialogSystem;

        private void Awake()
        {
            mDialogSystem = this.GetSystem<IDialogSystem>();

            mDialogSystem.GetConfig(DialogConfigs);

        }
        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
	}
}
