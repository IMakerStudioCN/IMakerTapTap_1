using System.Collections;
using System.Collections.Generic;
using TapTapFirst;
using UnityEngine;

public  class Calculate
{
    public int basePageViews = 30000;

    public int baseMoralityValue = 70;

    public int basePressureValue = 40;

    // ============================================================
    //  新闻对主角数值（资金 / 邪恶 / 可信度）的影响
    // ============================================================

    /// <summary>每篇新闻固定增加的资金（与浏览量脱钩）。</summary>
    public int fundsPerNews = 5;

    /// <summary>邪恶 / 可信度单次变化的幅度下限（含）。</summary>
    public int statChangeMin = 3;

    /// <summary>邪恶 / 可信度单次变化的幅度上限（含）。</summary>
    public int statChangeMax = 8;

    /// <summary>浏览量 &lt;= 这个值时，按浏览量算出来的幅度取下限。</summary>
    public int statPageViewsFloor = 30000;

    /// <summary>浏览量 &gt;= 这个值时，按浏览量算出来的幅度取上限。</summary>
    public int statPageViewsCeil = 120000;

    public int CalculatePageViews(float pageViewsOntag, float newsModelValue)
    {
        int pageViews = (int)(basePageViews + (basePageViews + pageViewsOntag) * newsModelValue );
        return Mathf.Max(pageViews, 0); // 确保页面浏览量不为负数
    }

    public int CalculateReply(int pageView, float radomCase)
    {
        int reply = pageView/1000+(int)(Random.Range(0, basePageViews)*radomCase);
        return reply;
    }

    /// <summary>资金变化 = 每篇新闻固定 +fundsPerNews（默认 5），不再随浏览量变化。</summary>
    public int CalculateFundsChange(int pageViews)
    {
        return Mathf.Max(0, fundsPerNews);
    }

    /// <summary>
    /// 邪恶变化：消极（负面）模版加邪恶，正面模版减邪恶，客观模版不改变邪恶值。
    /// 幅度稳定在 [statChangeMin, statChangeMax]，浏览量越大越接近上限。
    /// </summary>
    public int CalculateEvilChange(int pageViews, NewsEffectType effectType)
    {
        if (effectType == NewsEffectType.Objective)
        {
            return 0;
        }

        int magnitude = CalculateStatMagnitudeByPageViews(pageViews);

        return effectType == NewsEffectType.Negative ? magnitude : -magnitude;
    }

    /// <summary>
    /// 可信度变化：客观模版加可信度，其余（正面 / 负面）减可信度。
    /// 幅度稳定在 [statChangeMin, statChangeMax]，胡扯 Tag 占比越高越接近上限。
    /// </summary>
    /// <param name="nonsenseRatio">新闻用到的 Tag 里胡扯（blbl）的占比，0~1。</param>
    public int CalculateCredibilityChange(float nonsenseRatio, NewsEffectType effectType)
    {
        int magnitude = CalculateStatMagnitudeByRatio(nonsenseRatio);

        return effectType == NewsEffectType.Objective ? magnitude : -magnitude;
    }

    /// <summary>按浏览量把幅度映射到 [statChangeMin, statChangeMax]。</summary>
    public int CalculateStatMagnitudeByPageViews(int pageViews)
    {
        float t = Mathf.InverseLerp(statPageViewsFloor, statPageViewsCeil, pageViews);

        return ClampStatMagnitude(Mathf.RoundToInt(Mathf.Lerp(statChangeMin, statChangeMax, t)));
    }

    /// <summary>按 0~1 的比例把幅度映射到 [statChangeMin, statChangeMax]。</summary>
    public int CalculateStatMagnitudeByRatio(float ratio)
    {
        float t = Mathf.Clamp01(ratio);

        return ClampStatMagnitude(Mathf.RoundToInt(Mathf.Lerp(statChangeMin, statChangeMax, t)));
    }

    /// <summary>把幅度夹在 [min, max] 里（配置写反了也不至于得到负幅度）。</summary>
    private int ClampStatMagnitude(int value)
    {
        int min = Mathf.Min(statChangeMin, statChangeMax);
        int max = Mathf.Max(statChangeMin, statChangeMax);

        return Mathf.Clamp(value, min, max);
    }
}
