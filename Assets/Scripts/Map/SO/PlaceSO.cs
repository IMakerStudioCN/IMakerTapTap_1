using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TapTapFirst
{
    [CreateAssetMenu(fileName = "PlaceSO", menuName = "ScriptableObjects/PlaceSO", order = 1)]
    public class PlaceSO : ScriptableObject
    {
        public string placeName;
        public Sprite placeImage;
        public Button Function;
        
    }
}
