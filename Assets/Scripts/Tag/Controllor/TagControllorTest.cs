using QFramework;
using System.Collections;
using System.Collections.Generic;
using TapTapFirst;
using UnityEngine;
using UnityEngine.UI;


namespace TapTapFirst
{


    public class TagControllerTest : MonoBehaviour, IController
    {
        ITagModel tagModel => this.GetModel<ITagModel>();

        [SerializeField]
        private Button TagButton;

        private void Start()
        {

            TagButton = GameObject.Find("TagButton").GetComponent<Button>();

            TagButton.onClick.AddListener(() =>
            {
                //假设tagId为1
                int tagId = 1;
                this.SendCommand(new AddTagCommand(tagId));

            });
        }

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}
