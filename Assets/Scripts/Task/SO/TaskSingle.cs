using System.Collections;
using System.Collections.Generic;
using TapTapFirst;
using UnityEngine;

[CreateAssetMenu(fileName = "TaskSo", menuName = "ScriptableObjects/TaskSO", order = 1)]
public class TaskSingle : ScriptableObject
{
    public string TaskName;
    public string place;
    //存对话系统的key，检索对话
    public string dialogTitle;

    public bool ismain;
    public TaskSingle prePosition;

    public int startDay;
    public int endDay;
}
