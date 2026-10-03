using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TapTapFirst
{


    public class SoftwareListController : MonoBehaviour
    {
        [SerializeField]
        private List<Software_SO> softwareList = new List<Software_SO>();
        [SerializeField]
        private GameObject softwareItemPrefab;
        
        private void Start()
        {
            for(int i = 0; i < softwareList.Count; i++)
            {
                GameObject item = Instantiate(softwareItemPrefab, this.transform);
                item.transform.parent = this.transform;
                SoftwareSingle single = item.GetComponent<SoftwareSingle>();
                single.softwareData = softwareList[i];
            }
        }
    }
}