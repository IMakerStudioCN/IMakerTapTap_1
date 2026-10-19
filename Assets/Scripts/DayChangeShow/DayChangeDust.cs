using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TapTapFirst.DayChangeShow
{
    /// <summary>
    /// 崩解时从数字里炸出去的碎屑，重组时被吸回新数字的位置。
    /// 碎屑是纯色小方块，负责把"一个字形整体飞走"补成"数字被打碎"。
    /// </summary>
    public class DayChangeDust
    {
        class Block
        {
            public RectTransform rect;
            public Image image;
            public Vector2 start;
            public Vector2 burstTo;
            public Vector2 gatherTo;
            public float spin;
            public float delay;
        }

        readonly RectTransform mRoot;
        readonly DayChangeConfig mConfig;
        readonly List<Block> mBlocks = new List<Block>();
        readonly System.Random mRandom = new System.Random();

        public DayChangeDust(RectTransform root, DayChangeConfig config)
        {
            mRoot = root;
            mConfig = config;
        }

        public void Build(IList<Vector2> inkCenters)
        {
            if (inkCenters == null) return;

            var perDigit = Mathf.Max(1, mConfig.dustPerDigit);
            var halfWidth = mConfig.fontSize * 0.34f;
            var halfHeight = mConfig.fontSize * 0.52f;

            for (var d = 0; d < inkCenters.Count; d++)
            {
                for (var i = 0; i < perDigit; i++) mBlocks.Add(CreateBlock(inkCenters[d], halfWidth, halfHeight));
            }
        }

        public IEnumerator PlayBurst(float seconds)
        {
            seconds = Mathf.Max(0.01f, seconds);
            var total = seconds + 0.08f;

            var time = 0f;
            while (time < total)
            {
                time += Time.unscaledDeltaTime;
                StepBurst(seconds, time, false);
                yield return null;
            }

            StepBurst(seconds, time, true);
        }

        public IEnumerator PlayGather(IList<Vector2> inkCenters, float seconds)
        {
            seconds = Mathf.Max(0.01f, seconds);
            AssignGatherTargets(inkCenters);

            var time = 0f;
            while (time < seconds)
            {
                time += Time.unscaledDeltaTime;
                StepGather(seconds, time, false);
                yield return null;
            }

            StepGather(seconds, time, true);
        }

        public void Destroy()
        {
            for (var i = 0; i < mBlocks.Count; i++)
            {
                if (mBlocks[i].rect != null) UnityEngine.Object.Destroy(mBlocks[i].rect.gameObject);
            }

            mBlocks.Clear();
        }

        void StepBurst(float seconds, float time, bool finish)
        {
            for (var i = 0; i < mBlocks.Count; i++)
            {
                var block = mBlocks[i];
                var local = finish ? 1f : Mathf.Clamp01((time - block.delay) / seconds);
                var travel = DayChangeUi.EaseOutQuart(local);

                block.rect.anchoredPosition = Vector2.LerpUnclamped(block.start, block.burstTo, travel);
                block.rect.localEulerAngles = new Vector3(0f, 0f, block.spin * travel);

                var scale = Mathf.LerpUnclamped(1f, 0.55f, local);
                block.rect.localScale = new Vector3(scale, scale, 1f);

                DayChangeUi.SetAlpha(block.image, mConfig.dustAlpha * (1f - DayChangeUi.EaseInCubic(local) * 0.6f));
            }
        }

        void StepGather(float seconds, float time, bool finish)
        {
            var span = Mathf.Max(0.01f, seconds * 0.8f);

            for (var i = 0; i < mBlocks.Count; i++)
            {
                var block = mBlocks[i];
                var local = finish ? 1f : Mathf.Clamp01((time - block.delay) / span);
                var travel = DayChangeUi.EaseInCubic(local);

                block.rect.anchoredPosition = Vector2.LerpUnclamped(block.burstTo, block.gatherTo, travel);
                block.rect.localEulerAngles = new Vector3(0f, 0f, block.spin * (1f - travel));

                var scale = Mathf.LerpUnclamped(0.55f, 0.12f, travel);
                block.rect.localScale = new Vector3(scale, scale, 1f);

                DayChangeUi.SetAlpha(block.image, mConfig.dustAlpha * 0.45f * (1f - DayChangeUi.EaseInCubic(local)));
            }
        }

        void AssignGatherTargets(IList<Vector2> inkCenters)
        {
            if (inkCenters == null || inkCenters.Count == 0) return;

            var halfWidth = mConfig.fontSize * 0.3f;
            var halfHeight = mConfig.fontSize * 0.48f;

            for (var i = 0; i < mBlocks.Count; i++)
            {
                var center = inkCenters[i % inkCenters.Count];
                mBlocks[i].gatherTo = center + new Vector2(
                    DayChangeUi.Range(mRandom, -halfWidth, halfWidth),
                    DayChangeUi.Range(mRandom, -halfHeight, halfHeight));
            }
        }

        Block CreateBlock(Vector2 center, float halfWidth, float halfHeight)
        {
            var rect = DayChangeUi.CreateRect("Dust", mRoot);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var size = DayChangeUi.Range(mRandom, mConfig.dustSizeMin, Mathf.Max(mConfig.dustSizeMin, mConfig.dustSizeMax));
            rect.sizeDelta = new Vector2(size, size);

            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            var color = mConfig.digitColor;
            color.a = 0f;
            image.color = color;

            var offset = new Vector2(
                DayChangeUi.Range(mRandom, -halfWidth, halfWidth),
                DayChangeUi.Range(mRandom, -halfHeight, halfHeight));
            var start = center + offset;

            var direction = offset.sqrMagnitude > 1f ? offset.normalized : Vector2.up;
            direction += new Vector2(DayChangeUi.Range(mRandom, -1f, 1f),
                DayChangeUi.Range(mRandom, -1f, 1f)) * 0.8f;
            if (direction.sqrMagnitude < 0.01f) direction = Vector2.up;
            direction.Normalize();

            rect.anchoredPosition = start;
            rect.localScale = Vector3.one;

            return new Block
            {
                rect = rect,
                image = image,
                start = start,
                burstTo = start + direction * mConfig.dustScatterRadius * DayChangeUi.Range(mRandom, 0.45f, 1f),
                gatherTo = start,
                spin = DayChangeUi.Range(mRandom, -1f, 1f) * mConfig.dustSpin,
                delay = DayChangeUi.Range(mRandom, 0f, 0.08f)
            };
        }
    }
}
