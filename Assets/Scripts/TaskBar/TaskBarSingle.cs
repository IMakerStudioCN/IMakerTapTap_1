using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace TapTapFirst
{
    public class TaskBarSingle : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI textMeshPro;

        private Toggle toggle;
        public TaskBarSingle Init(string name)
        {
            textMeshPro.text = name;
            toggle = GetComponent<Toggle>();

            toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    // Open the corresponding window
                    WindowKit.Open(name);
                }
                else
                {
                    // Close the corresponding window
                    WindowKit.Close(name);
                }
            });
            return this;
        }

        public void CloseWindow()
        {
            Destroy(this.gameObject);
        }

    }
}
