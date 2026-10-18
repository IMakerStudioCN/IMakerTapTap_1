using System.Collections;
using UnityEngine;

namespace TapTapFirst.GameOverShow
{
    public static class GameOverImageSequence
    {
        public static IEnumerator Play(GameOverView view, Sprite[] sprites, float secondsPerImage, float fadeSeconds,
            GameOverSkipState skip = null)
        {
            if (view == null || sprites == null) yield break;

            var fade = Mathf.Max(0.01f, fadeSeconds);
            var hold = Mathf.Max(0f, secondsPerImage);
            var shown = false;

            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                if (sprite == null) continue;

                if (skip != null && skip.Requested) break;

                shown = true;
                view.SetPicture(sprite);

                yield return GameOverUi.Fade(view.PictureGroup, 0f, 1f, fade, skip);
                if (skip != null && skip.Requested) break;

                yield return GameOverUi.WaitSeconds(hold, skip);
                if (skip != null && skip.Requested) break;

                yield return GameOverUi.Fade(view.PictureGroup, 1f, 0f, fade, skip);
            }

            if (shown) view.HidePicture();
        }
    }
}
