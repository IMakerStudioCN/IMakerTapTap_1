using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    public class MapEvent : MonoBehaviour
    {
    }
    struct OnTakeTask
    {
        public TaskSingle Task;
        public string[] Place;
    }
    struct OnMapVisibilityChangedEvent
    {
        public string PlaceName;
        public bool Visible;
    }
}
