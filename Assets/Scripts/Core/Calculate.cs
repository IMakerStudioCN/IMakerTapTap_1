using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public  class Calculate
{
    public int basePageViews = 30000;

    public int baseMoralityValue = 70;

    public int basePressureValue = 40;


    public int CalculatePageViews(float pageViewsOnTag, float newsModelValue)
    {
        int pageViews = (int)(basePageViews + (basePageViews + pageViewsOnTag) * newsModelValue );
        return Mathf.Max(pageViews, 0); // 确保页面浏览量不为负数
    }

    public int CalculateReply(int pageView, float radomCase)
    {
        int reply = pageView/1000+(int)(Random.Range(0, basePageViews)*radomCase);
        return reply;
    }





   
}
