using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using System;
namespace TapTapFirst
{
    public class PlayerControlTest : MonoBehaviour,IController
    {
        private IPlayerModel playerModel;
        
        //private TagModel tagModel;
        [SerializeField]
        private Slider fundsSlider;
        [SerializeField]
        private Slider evilSlider;
        [SerializeField]
        private Slider credibilitySlider;
        [SerializeField]
        private Button plusButton;

        
        void Start()
        {
            playerModel = this.GetModel<IPlayerModel>();
            //tagModel = this.GetModel<TagModel>();

            Slider foundFundsSlider = GameObject.Find("FundsValue")?.GetComponent<Slider>();
            if (foundFundsSlider != null) fundsSlider = foundFundsSlider;

            Slider foundEvilSlider = GameObject.Find("EvilValue")?.GetComponent<Slider>();
            if (foundEvilSlider != null) evilSlider = foundEvilSlider;

            Slider foundCredibilitySlider = GameObject.Find("CredibilityValue")?.GetComponent<Slider>();
            if (foundCredibilitySlider != null) credibilitySlider = foundCredibilitySlider;

            //plusButton = GameObject.Find("3ValuePuls").GetComponent<Button>();

            if (plusButton == null)
            {
                return;
            }

            plusButton.onClick.AddListener(() =>
            {
                //the value of plus
                this.SendCommand(new FundsValueCommand(1));
                this.SendCommand(new EvilValueCommand(1));
                this.SendCommand(new CredibilityValueCommand(1));
                //this.SendCommand(new AddTagCommand(1));
                UpdatePlayerStats();

            });
        }


        public void UpdatePlayerStats()
        {
            if (playerModel == null)
            {
                playerModel = this.GetModel<IPlayerModel>();
            }

            if (playerModel == null)
            {
                return;
            }

            if (fundsSlider != null) fundsSlider.value = playerModel.FundsValue;
            if (evilSlider != null) evilSlider.value = playerModel.EvilValue;
            if (credibilitySlider != null) credibilitySlider.value = playerModel.CredibilityValue;
            //Debug .Log("Acquired Tags: " + string.Join(", ", tagModel.AcquriedTags[0].tagName));
        }

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}
