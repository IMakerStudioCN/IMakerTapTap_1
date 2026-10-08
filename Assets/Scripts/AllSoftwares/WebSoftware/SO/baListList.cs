using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TapTapFirst {

    [CreateAssetMenu(fileName = "baListList_SO", menuName = "ScriptableObjects/Ba/BaListList_SO", order = 1)]
    public class baListList_SO:ScriptableObject
    {
        [Header("baµÄÄÚÈÝ")]
        public List<baLineList_SO> baLineList;
    }
}