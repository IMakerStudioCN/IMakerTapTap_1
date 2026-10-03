using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WindowsSO", menuName = "ScriptableObjects/WindowsSO", order = 1)]
public class WindowsSO : ScriptableObject
{
    [Header("窗口基本属性")]
    public int windowID;
    public string windowName;
    public bool isFullScreen;
}
