using QFramework;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;
namespace TapTapFirst
{


    public class SoftwareListController : MonoBehaviour, IController
    {
        [SerializeField]
        private List<Software_SO> softwareList = new List<Software_SO>();
        [SerializeField]
        private GameObject softwareItemPrefab;
        [SerializeField]
        private List<GameObject> softwareListObjList = new List<GameObject>();
        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }

        private void Start()
        {
            for(int i = 0; i < softwareList.Count; i++)
            {
                GameObject item = Instantiate(softwareItemPrefab, this.transform);
                softwareListObjList.Add(item);
                item.transform.SetParent(this.transform, false);
                
                SoftwareSingle single = item.GetComponent<SoftwareSingle>();
                single.softwareData = softwareList[i];
            }
            this.RegisterEvent<SoftWareLockEvent>(e =>
            {
                LockSoftware(e.softWareID);
            });
            this.RegisterEvent<OnDaysChangeEvent>(e =>
            {
                UnlockAllSoftware();
            });
        }
        public void LockSoftware(int webID)
        {
            SoftwareSingle item = new SoftwareSingle();
            foreach (var i in softwareListObjList)
            {
                if(i == null)
                {
                    Debug.LogWarning("软件为空，无法上锁");
                    return;
                }
                item = i.GetComponent<SoftwareSingle>();
                if(item.softwareData.webID == webID)
                {
                    item.softwareIsLock = true;
                }
            }
        }
        public void UnlockAllSoftware()
        {
            SoftwareSingle item = new SoftwareSingle();
            foreach (var i in softwareListObjList)
            {
                if (i == null)
                {
                    Debug.LogWarning("软件为空，无法全部解锁");
                    return ;
                }
                item = i.GetComponent<SoftwareSingle>();
                item.softwareIsLock = false;
            }
        }
    }
}