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

    /// <summary>资金 = 浏览量 / 这个数。</summary>
    public int fundsPerPageViews = 1000;

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

    /// <summary>资金变化 = 该新闻的浏览量 / 1000（四舍五入到整数）。</summary>
    public int CalculateFundsChange(int pageViews)
    {
        if (pageViews <= 0)
        {
            return 0;
        }

        return Mathf.RoundToInt((float)pageViews / Mathf.Max(1, fundsPerPageViews));
    }

    /// <summary>
    /// 邪恶变化：消极（负面）模版加邪恶，其它模版减邪恶。
    /// 幅度稳定在 [statChangeMin, statChangeMax]，浏览量越大越接近上限。
    /// </summary>
    public int CalculateEvilChange(int pageViews, NewsEffectType effectType)
    {
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
