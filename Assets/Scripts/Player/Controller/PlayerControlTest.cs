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

            fundsSlider = GameObject.Find("FundsValue").GetComponent<Slider>();
            evilSlider = GameObject.Find("EvilValue").GetComponent<Slider>();
            credibilitySlider = GameObject.Find("CredibilityValue").GetComponent<Slider>();
            plusButton = GameObject.Find("3ValuePuls").GetComponent<Button>();

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
            fundsSlider.value = playerModel.FundsValue;
            evilSlider.value = playerModel.EvilValue;
            credibilitySlider.value =  playerModel.CredibilityValue;
            //Debug .Log("Acquired Tags: " + string.Join(", ", tagModel.AcquriedTags[0].tagName));
        }

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}