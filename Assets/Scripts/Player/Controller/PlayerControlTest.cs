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
        private PlayerModel playerModel;
        
        private TagModel tagModel;
        [SerializeField]
        private Text fundsText;
        [SerializeField]
        private Text evilText;
        [SerializeField]
        private Text credibilityText;
        [SerializeField]
        private Button plusButton;

        
        void Start()
        {
            playerModel = this.GetModel<PlayerModel>();
            tagModel = this.GetModel<TagModel>();

            fundsText = GameObject.Find("FundsValue").GetComponent<Text>();
            evilText = GameObject.Find("EvilValue").GetComponent<Text>();
            credibilityText = GameObject.Find("CredibilityValue").GetComponent<Text>();
            plusButton = GameObject.Find("PlusButton").GetComponent<Button>();

            plusButton.onClick.AddListener(() =>
            {
                
                this.SendCommand(new FundsValueCommand(1));
                this.SendCommand(new EvilValueCommand(1));
                this.SendCommand(new CredibilityValueCommand(1));
                this.SendCommand(new AddTagCommand(1));
                UpdatePlayerStats();

            });
        }


        public void UpdatePlayerStats()
        {
            fundsText.text = "Funds: " + playerModel.FundsValue.ToString();
            evilText.text = "Evil: " + playerModel.EvilValue.ToString();
            credibilityText.text = "Credibility: " + playerModel.CredibilityValue.ToString();
            Debug .Log("Acquired Tags: " + string.Join(", ", tagModel.AcquriedTags[0].tagName));
        }

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}